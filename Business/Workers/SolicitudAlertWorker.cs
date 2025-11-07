using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Solicitud;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Workers
{
    /// <summary>
    /// Servicio en segundo plano (Worker) que monitorea el tiempo de respuesta
    /// de las Solicitudes de Corrección.
    /// Si una solicitud supera las 2 horas sin resolverse, envía una alerta.
    /// </summary>
    public class SolicitudAlertWorker : BackgroundService
    {
        private readonly ILogger<SolicitudAlertWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext;

        // Define cada cuánto tiempo se ejecutará el worker
        private readonly TimeSpan _timerDelay = TimeSpan.FromMinutes(15); // 15 minutos
        private const int HorasAlerta = 2; // 2 horas

        public SolicitudAlertWorker(
            ILogger<SolicitudAlertWorker> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<SgmHub, ISgmClient> hubContext)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Alerta de Solicitudes (> 2h) iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker de Solicitudes ejecutando chequeo (cada {Minutos} min)...", _timerDelay.TotalMinutes);

                    // Creamos un nuevo "scope" para obtener servicios Scoped
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var solicitudRepo = scope.ServiceProvider.GetRequiredService<ISolicitudCorreccionRepository>();

                        // 1. Buscar solicitudes pendientes que superen las 2 horas
                        var solicitudesVencidas = await solicitudRepo.GetSolicitudesConAlertaAsync(HorasAlerta);

                        if (solicitudesVencidas.Any())
                        {
                            _logger.LogWarning("¡ALERTA DE SOLICITUDES VENCIDAS! Se encontraron {Count} solicitudes con más de {Horas}h sin resolver.",
                                solicitudesVencidas.Count, HorasAlerta);

                            // 2. Mapear a DTOs para enviar al cliente
                            var dtos = solicitudesVencidas.Select(MapToSolicitudDTO).ToList();

                            // 3. Enviar notificación por SignalR a todos los clientes
                            // TODO: En el futuro, enviar solo al grupo "Enfermeria"
                            await _hubContext.Clients.All.RecibirAlertaSolicitudesVencidas(dtos);
                        }
                        else
                        {
                            _logger.LogInformation("Chequeo de solicitudes vencidas finalizado. 0 alertas encontradas.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el Worker de Alerta de Solicitudes.");
                }

                // Esperar al siguiente ciclo
                await Task.Delay(_timerDelay, stoppingToken);
            }

            _logger.LogInformation("Worker de Alerta de Solicitudes detenido.");
        }

        /// <summary>
        /// Método privado para mapear Entidad -> DTO.
        /// </summary>
        private SolicitudCorreccionDTO MapToSolicitudDTO(SolicitudCorreccionExpediente solicitud)
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
    }
}