using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Solicitud;

namespace SisMortuorio.Business.Hubs
{
    /// <summary>
    /// Define el contrato (los métodos) que el cliente de SignalR (Angular)
    /// debe implementar para recibir notificaciones en tiempo real desde el Hub.
    /// </summary>
    public interface ISgmClient
    {
        /// <summary>
        /// Evento notificado cuando la ocupación del mortuorio supera el 70%.
        /// Se envía al asignar una bandeja que cruza el umbral.
        /// </summary>
        /// <param name="estadisticas">Las estadísticas de ocupación actualizadas.</param>
        Task RecibirAlertaOcupacion(EstadisticasBandejaDTO estadisticas);

        /// <summary>
        /// Evento notificado por el Worker (ej. cada hora) con la lista de
        /// cuerpos que superan las 24 horas de permanencia.
        /// </summary>
        /// <param name="bandejasConAlerta">Lista de bandejas en alerta.</param>
        Task RecibirAlertaPermanencia(List<BandejaDTO> bandejasConAlerta);

        /// <summary>
        /// Evento notificado por el Worker (ej. cada 15 min) con la lista de
        /// solicitudes de corrección que superan las 2 horas sin resolverse.
        /// </summary>
        /// <param name="solicitudesVencidas">Lista de solicitudes vencidas.</param>
        Task RecibirAlertaSolicitudesVencidas(List<SolicitudCorreccionDTO> solicitudesVencidas);
    }
}