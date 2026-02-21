using SisMortuorio.Business.DTOs.Bandeja;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio centralizado para notificaciones de Bandejas vía SignalR.
    /// Separa la lógica de negocio de BandejaService de las notificaciones en tiempo real.
    /// </summary>
    public interface INotificacionBandejaService
    {
        /// <summary>
        /// Notifica que una bandeja fue asignada a un expediente.
        /// Envía actualización al mapa en tiempo real.
        /// </summary>
        /// <param name="bandejaDTO">DTO de la bandeja actualizada</param>
        Task NotificarBandejaAsignadaAsync(BandejaDTO bandejaDTO);

        /// <summary>
        /// Notifica que una bandeja fue liberada.
        /// Envía actualización al mapa en tiempo real.
        /// </summary>
        /// <param name="bandejaDTO">DTO de la bandeja actualizada</param>
        Task NotificarBandejaLiberadaAsync(BandejaDTO bandejaDTO);

        /// <summary>
        /// Notifica que una bandeja entró en mantenimiento.
        /// Envía actualización al mapa en tiempo real.
        /// </summary>
        /// <param name="bandejaDTO">DTO de la bandeja actualizada</param>
        Task NotificarBandejaEnMantenimientoAsync(BandejaDTO bandejaDTO);

        /// <summary>
        /// Notifica que una bandeja salió de mantenimiento.
        /// Envía actualización al mapa en tiempo real.
        /// </summary>
        /// <param name="bandejaDTO">DTO de la bandeja actualizada</param>
        Task NotificarBandejaSalidaMantenimientoAsync(BandejaDTO bandejaDTO);

        /// <summary>
        /// Verifica la ocupación y envía alerta crítica si supera el umbral (70%).
        /// Envía dos notificaciones:
        /// 1. RecibirAlertaOcupacion: Alerta con estadísticas completas
        /// 2. RecibirNotificacion: Notificación genérica para dropdown
        /// </summary>
        /// <param name="estadisticas">DTO con estadísticas de ocupación</param>
        Task VerificarYNotificarOcupacionCriticaAsync(EstadisticasBandejaDTO estadisticas);
    }
}