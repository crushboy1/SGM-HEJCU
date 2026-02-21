using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Workers
{
    /// <summary>
    /// Worker que monitorea el tiempo de permanencia de cuerpos en bandejas del mortuorio.
    /// 
    /// Responsabilidades:
    /// 1. Verificar cada 60 minutos si hay cuerpos con >24h de permanencia
    /// 2. Enviar alerta específica (RecibirAlertaPermanencia) con lista de bandejas
    /// 3. Enviar notificación genérica (RecibirNotificacion) a roles clave
    /// 
    /// Destinatarios:
    /// - Admision (principal responsable de gestionar retiros)
    /// - JefeGuardia (autoriza excepciones)
    /// - Administrador (supervisión general)
    /// 
    /// Configuración:
    /// - Intervalo: 60 minutos
    /// - Umbral de alerta: 24 horas
    /// </summary>
    public class PermanenciaAlertWorker(
        ILogger<PermanenciaAlertWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<SgmHub, ISgmClient> hubContext) : BackgroundService
    {
        private readonly ILogger<PermanenciaAlertWorker> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext = hubContext;

        // Configuración del worker
        private readonly TimeSpan _timerDelay = TimeSpan.FromMinutes(60); // Ejecutar cada 60 min
        private const int HorasAlerta = 24; // Umbral: 24 horas

        // Roles que deben recibir las alertas
        private static readonly string[] RolesDestino = ["Admision", "JefeGuardia", "Administrador"];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "PermanenciaAlertWorker iniciado. Intervalo: {Minutos} min, Umbral: {Horas}h",
                _timerDelay.TotalMinutes,
                HorasAlerta
            );

            // Loop infinito hasta que la app se detenga
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarPermanenciaAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "PermanenciaAlertWorker: Error inesperado durante verificación"
                    );
                    // NO re-throw - el worker debe continuar ejecutándose
                }

                // Esperar hasta la próxima ejecución
                try
                {
                    await Task.Delay(_timerDelay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("PermanenciaAlertWorker: Cancelación solicitada, deteniendo...");
                    break;
                }
            }

            _logger.LogInformation("PermanenciaAlertWorker detenido");
        }

        /// <summary>
        /// Verifica bandejas con tiempo de permanencia superior al umbral.
        /// Envía alertas por SignalR si encuentra casos.
        /// </summary>
        private async Task VerificarPermanenciaAsync(CancellationToken _)
        {
            _logger.LogDebug("PermanenciaAlertWorker: Iniciando verificación de permanencia");

            // Crear scope para servicios Scoped
            using var scope = _scopeFactory.CreateScope();

            var bandejaRepo = scope.ServiceProvider.GetRequiredService<IBandejaRepository>();
            var mapper = scope.ServiceProvider.GetRequiredService<IExpedienteMapperService>();

            try
            {
                // 1. Buscar bandejas que superen el umbral
                var bandejasConAlerta = await bandejaRepo.GetBandejasConAlertaAsync(HorasAlerta);

                if (bandejasConAlerta.Count == 0)
                {
                    _logger.LogDebug(
                        "PermanenciaAlertWorker: No se encontraron bandejas con >{Horas}h de permanencia",
                        HorasAlerta
                    );
                    return;
                }

                // 2. Loggear alerta encontrada
                _logger.LogWarning(
                    "PermanenciaAlertWorker: ¡ALERTA! {Count} bandeja(s) con >{Horas}h de ocupación detectadas",
                    bandejasConAlerta.Count,
                    HorasAlerta
                );

                // 3. Mapear entidades a DTOs
                var dtos = bandejasConAlerta.Select(b => MapToBandejaDTO(b, mapper)).ToList();

                // 4. Enviar alerta específica de permanencia (lista de bandejas)
                await _hubContext.Clients
                    .Groups(RolesDestino)
                    .RecibirAlertaPermanencia(dtos);

                _logger.LogInformation(
                    "PermanenciaAlertWorker: Alerta específica enviada a roles: {Roles}",
                    string.Join(", ", RolesDestino)
                );

                // 5. Enviar notificación genérica (para dropdown en header)
                var notificacion = new NotificacionDTO
                {
                    Titulo = "Alerta de Permanencia",
                    Mensaje = $"{bandejasConAlerta.Count} cuerpo(s) llevan más de {HorasAlerta} horas en el mortuorio. Requiere atención urgente.",
                    Tipo = "warning",
                    CategoriaNotificacion = "alerta_permanencia",
                    RolesDestino = string.Join(",", RolesDestino),
                    RequiereAccion = true,
                    AccionSugerida = "Ver Bandejas con Alerta",
                    UrlNavegacion = "/mapa-mortuorio"
                };

                await _hubContext.Clients
                    .Groups(RolesDestino)
                    .RecibirNotificacion(notificacion);

                _logger.LogInformation(
                    "PermanenciaAlertWorker: Notificación genérica enviada. Total alertas: {Count}",
                    bandejasConAlerta.Count
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PermanenciaAlertWorker: Error al verificar permanencia o enviar alertas"
                );
                throw; // Re-throw para que el catch del ExecuteAsync lo maneje
            }
        }

        /// <summary>
        /// Mapea una entidad Bandeja a su DTO correspondiente.
        /// Calcula tiempo de ocupación y determina si tiene alerta.
        /// </summary>
        private static BandejaDTO MapToBandejaDTO(Bandeja bandeja, IExpedienteMapperService _)
        {
            var dto = new BandejaDTO
            {
                BandejaID = bandeja.BandejaID,
                Codigo = bandeja.Codigo,
                Estado = bandeja.Estado.ToString(),
                Observaciones = bandeja.Observaciones
            };

            // Si la bandeja está ocupada, agregar información del expediente
            if (bandeja.EstaOcupada())
            {
                dto.ExpedienteID = bandeja.ExpedienteID;
                dto.CodigoExpediente = bandeja.Expediente?.CodigoExpediente;
                dto.NombrePaciente = bandeja.Expediente?.NombreCompleto;
                dto.UsuarioAsignaNombre = bandeja.UsuarioAsigna?.NombreCompleto;
                dto.FechaHoraAsignacion = bandeja.FechaHoraAsignacion;

                // Calcular tiempo de ocupación
                var tiempoOcupada = bandeja.TiempoOcupada();
                if (tiempoOcupada.HasValue)
                {
                    dto.TiempoOcupada = $"{(int)tiempoOcupada.Value.TotalHours}h {tiempoOcupada.Value.Minutes}m";
                    dto.TieneAlerta = tiempoOcupada.Value.TotalHours >= HorasAlerta;
                }
            }

            return dto;
        }

        /// <summary>
        /// Se ejecuta al detener la aplicación.
        /// Permite ejecutar una última verificación antes de apagar.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PermanenciaAlertWorker: Deteniendo worker...");

            try
            {
                // Verificación final antes de apagar
                await VerificarPermanenciaAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PermanenciaAlertWorker: Error en verificación final");
            }

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("PermanenciaAlertWorker: Worker detenido completamente");
        }
    }
}