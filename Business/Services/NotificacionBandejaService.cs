using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones para Bandejas.
    /// Centraliza todas las notificaciones SignalR relacionadas con gestión de bandejas.
    /// </summary>
    public class NotificacionBandejaService : INotificacionBandejaService
    {
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext;
        private readonly ILogger<NotificacionBandejaService> _logger;

        // Roles que reciben alertas de ocupación crítica
        private static readonly string[] RolesAlertaOcupacion = ["Admision", "JefeGuardia", "Administrador", "VigilanteSupervisor"];

        public NotificacionBandejaService(
            IHubContext<SgmHub, ISgmClient> hubContext,
            ILogger<NotificacionBandejaService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotificarBandejaAsignadaAsync(BandejaDTO bandejaDTO)
        {
            try
            {
                await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

                _logger.LogDebug(
                    "SignalR: Bandeja {CodigoBandeja} asignada - Actualización enviada a todos los clientes",
                    bandejaDTO.Codigo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar asignación de bandeja {CodigoBandeja}", bandejaDTO.Codigo);
            }
        }

        public async Task NotificarBandejaLiberadaAsync(BandejaDTO bandejaDTO)
        {
            try
            {
                await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

                _logger.LogDebug(
                    "SignalR: Bandeja {CodigoBandeja} liberada - Actualización enviada a todos los clientes",
                    bandejaDTO.Codigo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar liberación de bandeja {CodigoBandeja}", bandejaDTO.Codigo);
            }
        }

        public async Task NotificarBandejaEnMantenimientoAsync(BandejaDTO bandejaDTO)
        {
            try
            {
                await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

                _logger.LogDebug(
                    "SignalR: Bandeja {CodigoBandeja} en mantenimiento - Actualización enviada",
                    bandejaDTO.Codigo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar mantenimiento de bandeja {CodigoBandeja}", bandejaDTO.Codigo);
            }
        }

        public async Task NotificarBandejaSalidaMantenimientoAsync(BandejaDTO bandejaDTO)
        {
            try
            {
                await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

                _logger.LogDebug(
                    "SignalR: Bandeja {CodigoBandeja} disponible tras mantenimiento - Actualización enviada",
                    bandejaDTO.Codigo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar salida de mantenimiento de bandeja {CodigoBandeja}", bandejaDTO.Codigo);
            }
        }

        public async Task VerificarYNotificarOcupacionCriticaAsync(EstadisticasBandejaDTO estadisticas)
        {
            try
            {
                // Umbral de alerta: 70%
                if (estadisticas.PorcentajeOcupacion <= 70)
                    return;

                _logger.LogWarning(
                    "ALERTA DE OCUPACIÓN: Mortuorio ha superado el 70% de capacidad. Ocupación actual: {PorcentajeOcupacion}%",
                    estadisticas.PorcentajeOcupacion.ToString("F2")
                );

                // 1. Enviar alerta específica con estadísticas completas
                await _hubContext.Clients
                    .Groups(RolesAlertaOcupacion)
                    .RecibirAlertaOcupacion(estadisticas);

                _logger.LogInformation(
                    "SignalR: Alerta de ocupación enviada a roles: {Roles}",
                    string.Join(", ", RolesAlertaOcupacion)
                );

                // 2. Enviar notificación genérica para dropdown
                var notificacion = new NotificacionDTO
                {
                    Titulo = "Capacidad Crítica del Mortuorio",
                    Mensaje = $"El mortuorio ha alcanzado el {estadisticas.PorcentajeOcupacion:F1}% de ocupación ({estadisticas.Ocupadas}/{estadisticas.Total} bandejas). Se recomienda agilizar retiros.",
                    Tipo = "warning",
                    RolesDestino = string.Join(",", RolesAlertaOcupacion),
                    RequiereAccion = true,
                    AccionSugerida = "Ver Mapa del Mortuorio",
                    UrlNavegacion = "/mapa-mortuorio"
                };

                await _hubContext.Clients
                    .Groups(RolesAlertaOcupacion)
                    .RecibirNotificacion(notificacion);

                _logger.LogInformation(
                    "SignalR: Notificación genérica de ocupación enviada. Ocupación: {Porcentaje}%",
                    estadisticas.PorcentajeOcupacion.ToString("F2")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar/notificar ocupación crítica");
            }
        }
    }
}