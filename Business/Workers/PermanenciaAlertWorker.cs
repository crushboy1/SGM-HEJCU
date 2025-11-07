using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Workers
{
    /// <summary>
    /// Servicio en segundo plano (Worker) que monitorea el tiempo de permanencia
    /// de los cuerpos en las bandejas.
    /// Si un cuerpo supera las 24 horas, envía una alerta por SignalR.
    /// </summary>
    public class PermanenciaAlertWorker : BackgroundService
    {
        private readonly ILogger<PermanenciaAlertWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext;

        // Define cada cuánto tiempo se ejecutará el worker
        private readonly TimeSpan _timerDelay = TimeSpan.FromMinutes(60); // 60 minutos
        private const int HorasAlerta = 24; // 24 horas

        public PermanenciaAlertWorker(
            ILogger<PermanenciaAlertWorker> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<SgmHub, ISgmClient> hubContext)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Alerta de Permanencia (> 24h) iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker de Permanencia ejecutando chequeo (cada {Minutos} min)...", _timerDelay.TotalMinutes);

                    // Creamos un nuevo "scope" para obtener servicios Scoped
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var bandejaRepo = scope.ServiceProvider.GetRequiredService<IBandejaRepository>();
                        // Necesitamos el mapper para convertir Entidad -> DTO
                        var mapper = scope.ServiceProvider.GetRequiredService<IExpedienteMapperService>();

                        // 1. Buscar bandejas que superen las 24 horas
                        var bandejasConAlerta = await bandejaRepo.GetBandejasConAlertaAsync(HorasAlerta);

                        if (bandejasConAlerta.Any())
                        {
                            _logger.LogWarning("¡ALERTA DE PERMANENCIA! Se encontraron {Count} bandejas con más de {Horas}h de ocupación.",
                                bandejasConAlerta.Count, HorasAlerta);

                            // 2. Mapear a DTOs para enviar al cliente
                            // (Reutilizamos la lógica del BandejaService para el mapeo, aunque
                            // podríamos crear un Mapper específico para BandejaDTO si fuera necesario)
                            var dtos = bandejasConAlerta.Select(b => MapToBandejaDTO(b, mapper)).ToList();

                            // 3. Enviar notificación por SignalR a todos los clientes
                            await _hubContext.Clients.All.RecibirAlertaPermanencia(dtos);
                        }
                        else
                        {
                            _logger.LogInformation("Chequeo de permanencia finalizado. 0 alertas encontradas.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el Worker de Alerta de Permanencia.");
                }

                // Esperar al siguiente ciclo
                await Task.Delay(_timerDelay, stoppingToken);
            }

            _logger.LogInformation("Worker de Alerta de Permanencia detenido.");
        }

        /// <summary>
        /// Método privado para mapear Entidad -> DTO.
        /// Idealmente, esto estaría en un servicio de Mapper de Bandejas.
        /// </summary>
        private BandejaDTO MapToBandejaDTO(Bandeja bandeja, IExpedienteMapperService mapper)
        {
            var dto = new BandejaDTO
            {
                BandejaID = bandeja.BandejaID,
                Codigo = bandeja.Codigo,
                Estado = bandeja.Estado.ToString(),
                Observaciones = bandeja.Observaciones
            };

            if (bandeja.EstaOcupada())
            {
                dto.ExpedienteID = bandeja.ExpedienteID;
                dto.CodigoExpediente = bandeja.Expediente?.CodigoExpediente;
                dto.NombrePaciente = bandeja.Expediente?.NombreCompleto;
                dto.UsuarioAsignaNombre = bandeja.UsuarioAsigna?.NombreCompleto;
                dto.FechaHoraAsignacion = bandeja.FechaHoraAsignacion;

                var tiempoOcupada = bandeja.TiempoOcupada();
                if (tiempoOcupada.HasValue)
                {
                    dto.TiempoOcupada = $"{(int)tiempoOcupada.Value.TotalHours}h {tiempoOcupada.Value.Minutes}m";
                    dto.TieneAlerta = tiempoOcupada.Value.TotalHours >= HorasAlerta;
                }
            }
            return dto;
        }
    }
}