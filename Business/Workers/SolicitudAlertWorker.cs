using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.DTOs.Solicitud;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Workers
{
    /// <summary>
    /// Worker que monitorea el tiempo de respuesta de Solicitudes de Corrección de Expedientes.
    /// 
    /// Responsabilidades:
    /// 1. Verificar cada 15 minutos si hay solicitudes con >2h sin resolver
    /// 2. Enviar alerta específica (RecibirAlertaSolicitudesVencidas) con lista de solicitudes
    /// 3. Enviar notificación genérica (RecibirNotificacion) al Administrador
    /// 
    /// Contexto de negocio:
    /// Cuando Enfermería detecta un error en un expediente (datos incorrectos, pertenencias faltantes),
    /// crea una Solicitud de Corrección. El Administrador debe revisar y resolver estas solicitudes.
    /// Si una solicitud supera 2 horas sin respuesta, se considera "vencida" y requiere atención urgente.
    /// 
    /// Destinatarios:
    /// - Administrador (responsable de aprobar correcciones)
    /// 
    /// Configuración:
    /// - Intervalo: 15 minutos
    /// - Umbral de alerta: 2 horas
    /// </summary>
    public class SolicitudAlertWorker(
        ILogger<SolicitudAlertWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<SgmHub, ISgmClient> hubContext) : BackgroundService
    {
        private readonly ILogger<SolicitudAlertWorker> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext = hubContext;

        // Configuración del worker
        private readonly TimeSpan _timerDelay = TimeSpan.FromMinutes(15); // Ejecutar cada 15 min
        private const int HorasAlerta = 2; // Umbral: 2 horas

        // Rol responsable de resolver solicitudes
        private const string RolDestino = "Administrador";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SolicitudAlertWorker iniciado. Intervalo: {Minutos} min, Umbral: {Horas}h",
                _timerDelay.TotalMinutes,
                HorasAlerta
            );

            // Loop infinito hasta que la app se detenga
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarSolicitudesVencidasAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "SolicitudAlertWorker: Error inesperado durante verificación"
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
                    _logger.LogInformation("SolicitudAlertWorker: Cancelación solicitada, deteniendo...");
                    break;
                }
            }

            _logger.LogInformation("SolicitudAlertWorker detenido");
        }

        /// <summary>
        /// Verifica solicitudes de corrección que superan el tiempo de respuesta esperado.
        /// Envía alertas por SignalR si encuentra casos.
        /// </summary>
        private async Task VerificarSolicitudesVencidasAsync(CancellationToken _)
        {
            _logger.LogDebug("SolicitudAlertWorker: Iniciando verificación de solicitudes vencidas");

            // Crear scope para servicios Scoped
            using var scope = _scopeFactory.CreateScope();

            var solicitudRepo = scope.ServiceProvider.GetRequiredService<ISolicitudCorreccionRepository>();

            try
            {
                // 1. Buscar solicitudes pendientes que superen el umbral
                var solicitudesVencidas = await solicitudRepo.GetSolicitudesConAlertaAsync(HorasAlerta);

                if (solicitudesVencidas.Count == 0)
                {
                    _logger.LogDebug(
                        "SolicitudAlertWorker: No se encontraron solicitudes con >{Horas}h sin resolver",
                        HorasAlerta
                    );
                    return;
                }

                // 2. Loggear alerta encontrada
                _logger.LogWarning(
                    "SolicitudAlertWorker: ¡ALERTA! {Count} solicitud(es) con >{Horas}h sin resolver detectadas",
                    solicitudesVencidas.Count,
                    HorasAlerta
                );

                // 3. Mapear entidades a DTOs
                var dtos = solicitudesVencidas.Select(MapToSolicitudDTO).ToList();

                // 4. Enviar alerta específica de solicitudes vencidas (lista de solicitudes)
                await _hubContext.Clients
                    .Group(RolDestino)
                    .RecibirAlertaSolicitudesVencidas(dtos);

                _logger.LogInformation(
                    "SolicitudAlertWorker: Alerta específica enviada a rol: {Rol}",
                    RolDestino
                );

                // 5. Enviar notificación genérica (para dropdown en header)
                var notificacion = new NotificacionDTO
                {
                    Titulo = "Solicitudes de Corrección Vencidas",
                    Mensaje = $"{solicitudesVencidas.Count} solicitud(es) de corrección llevan más de {HorasAlerta} horas sin resolver. Requiere atención inmediata.",
                    Tipo = "error",
                    RolesDestino = RolDestino,
                    RequiereAccion = true,
                    AccionSugerida = "Revisar Solicitudes",
                    UrlNavegacion = "/solicitudes-correccion"
                };

                await _hubContext.Clients
                    .Group(RolDestino)
                    .RecibirNotificacion(notificacion);

                _logger.LogInformation(
                    "SolicitudAlertWorker: Notificación genérica enviada. Total alertas: {Count}",
                    solicitudesVencidas.Count
                );

                // 6. Loggear detalles de cada solicitud vencida (para auditoría)
                foreach (var solicitud in solicitudesVencidas)
                {
                    var tiempo = solicitud.TiempoTranscurrido();
                    _logger.LogWarning(
                        "SolicitudAlertWorker: Solicitud vencida - ID: {SolicitudID}, Expediente: {CodigoExpediente}, " +
                        "Solicitante: {Solicitante}, Tiempo: {Horas}h {Minutos}m",
                        solicitud.SolicitudID,
                        solicitud.Expediente?.CodigoExpediente ?? "N/A",
                        solicitud.UsuarioSolicita?.NombreCompleto ?? "N/A",
                        (int)tiempo.TotalHours,
                        tiempo.Minutes
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SolicitudAlertWorker: Error al verificar solicitudes o enviar alertas"
                );
                throw; // Re-throw para que el catch del ExecuteAsync lo maneje
            }
        }

        /// <summary>
        /// Mapea una entidad SolicitudCorreccionExpediente a su DTO correspondiente.
        /// Calcula tiempo transcurrido y determina si supera el umbral de alerta.
        /// </summary>
        private static SolicitudCorreccionDTO MapToSolicitudDTO(SolicitudCorreccionExpediente solicitud)
        {
            var tiempo = solicitud.TiempoTranscurrido();
            var tiempoTexto = $"{(int)tiempo.TotalHours}h {tiempo.Minutes}m";

            return new SolicitudCorreccionDTO
            {
                SolicitudID = solicitud.SolicitudID,
                ExpedienteID = solicitud.ExpedienteID,
                CodigoExpediente = solicitud.Expediente?.CodigoExpediente ?? "N/A",
                FechaHoraSolicitud = solicitud.FechaHoraSolicitud,
                UsuarioSolicitaNombre = solicitud.UsuarioSolicita?.NombreCompleto ?? "N/A",
                UsuarioResponsableNombre = solicitud.UsuarioResponsable?.NombreCompleto ?? "N/A",
                DescripcionProblema = solicitud.DescripcionProblema,
                DatosIncorrectos = solicitud.DatosIncorrectos,
                ObservacionesSolicitud = solicitud.ObservacionesSolicitud,
                Resuelta = solicitud.Resuelta,
                FechaHoraResolucion = solicitud.FechaHoraResolucion,
                DescripcionResolucion = solicitud.DescripcionResolucion,
                BrazaleteReimpreso = solicitud.BrazaleteReimpreso,
                TiempoTranscurrido = tiempoTexto,
                SuperaTiempoAlerta = solicitud.SuperaTiempoAlerta()
            };
        }

        /// <summary>
        /// Se ejecuta al detener la aplicación.
        /// Permite ejecutar una última verificación antes de apagar.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SolicitudAlertWorker: Deteniendo worker...");

            try
            {
                // Verificación final antes de apagar
                await VerificarSolicitudesVencidasAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SolicitudAlertWorker: Error en verificación final");
            }

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("SolicitudAlertWorker: Worker detenido completamente");
        }
    }
}