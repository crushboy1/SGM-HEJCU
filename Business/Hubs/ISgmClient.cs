using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.DTOs.Solicitud;

namespace SisMortuorio.Business.Hubs
{
    /// <summary>
    /// Define el contrato (los métodos) que el cliente de SignalR (Angular)
    /// debe implementar para recibir notificaciones en tiempo real desde el Hub.
    /// 
    /// IMPORTANTE: Estos métodos se invocan desde el servidor hacia el cliente.
    /// El cliente Angular debe suscribirse a estos eventos usando HubConnection.
    /// </summary>
    public interface ISgmClient
    {
        /// <summary>
        /// Notificación genérica estructurada.
        /// Se usa para alertas puntuales, confirmaciones y mensajes informativos.
        /// 
        /// Casos de uso:
        /// - "Nueva solicitud de corrección pendiente"
        /// - "Expediente asignado a bandeja B-03"
        /// - "Sistema en mantenimiento en 5 minutos"
        /// 
        /// El frontend muestra estas notificaciones en el dropdown del header.
        /// </summary>
        /// <param name="notificacion">DTO con título, mensaje, tipo, metadata</param>
        /// <returns>Task completado cuando el cliente procesa la notificación</returns>
        /// <example>
        /// // Backend:
        /// await Clients.User(userId).RecibirNotificacion(new NotificacionDTO {
        ///     Titulo = "Nuevo Expediente",
        ///     Mensaje = "SGM-2025-00015 requiere tu atención",
        ///     Tipo = "info",
        ///     ExpedienteId = 15
        /// });
        /// </example>
        Task RecibirNotificacion(NotificacionDTO notificacion);

        /// <summary>
        /// Evento disparado cuando la ocupación del mortuorio supera el 70%.
        /// Se envía al asignar una bandeja que cruza el umbral.
        /// 
        /// Destinatarios: Roles con permisos de administración y jefatura.
        /// Acción esperada: Actualizar gráfico de ocupación en el dashboard.
        /// </summary>
        /// <param name="estadisticas">Estadísticas completas de ocupación</param>
        /// <returns>Task completado cuando el cliente actualiza el dashboard</returns>
        /// <example>
        /// // Backend (BandejaService):
        /// if (porcentajeOcupacion > 70)
        /// {
        ///     await _hubContext.Clients.Groups("Administrador", "JefeGuardia")
        ///         .RecibirAlertaOcupacion(estadisticas);
        /// }
        /// </example>
        Task RecibirAlertaOcupacion(EstadisticasBandejaDTO estadisticas);

        /// <summary>
        /// Evento disparado por el Worker de permanencia (cada 60 min).
        /// Lista cuerpos que superan las 24 horas en el mortuorio.
        /// 
        /// Destinatarios: Admisión, JefeGuardia, Administrador.
        /// Acción esperada: Revisar expedientes y gestionar retiros urgentes.
        /// </summary>
        /// <param name="bandejasConAlerta">Lista de bandejas con cuerpos en alerta</param>
        /// <returns>Task completado cuando el cliente procesa las alertas</returns>
        /// <example>
        /// // Backend (PermanenciaAlertWorker):
        /// var bandejasAlerta = await _bandejaRepo.ObtenerConPermanenciaMayorA24Horas();
        /// await _hubContext.Clients.Groups("Admision", "JefeGuardia")
        ///     .RecibirAlertaPermanencia(bandejasAlerta);
        /// </example>
        Task RecibirAlertaPermanencia(List<BandejaDTO> bandejasConAlerta);

        /// <summary>
        /// Evento disparado por el Worker de solicitudes (cada 15 min).
        /// Lista solicitudes de corrección que superan las 2 horas sin resolverse.
        /// 
        /// Destinatarios: Rol responsable de aprobar correcciones (Administrador).
        /// Acción esperada: Revisar y resolver solicitudes vencidas.
        /// </summary>
        /// <param name="solicitudesVencidas">Lista de solicitudes con timeout</param>
        /// <returns>Task completado cuando el cliente procesa las solicitudes</returns>
        /// <example>
        /// // Backend (SolicitudAlertWorker):
        /// var solicitudesVencidas = await _solicitudRepo.ObtenerVencidas();
        /// await _hubContext.Clients.Group("Administrador")
        ///     .RecibirAlertaSolicitudesVencidas(solicitudesVencidas);
        /// </example>
        Task RecibirAlertaSolicitudesVencidas(List<SolicitudCorreccionDTO> solicitudesVencidas);

        /// <summary>
        /// Evento disparado cuando una bandeja cambia de estado.
        /// Se usa para actualizar el mapa del mortuorio en tiempo real.
        /// 
        /// Casos de uso:
        /// - Bandeja asignada (Disponible → Ocupada)
        /// - Bandeja liberada (Ocupada → Disponible)
        /// - Bandeja en mantenimiento (Disponible → Mantenimiento)
        /// 
        /// Destinatarios: Todos los usuarios conectados (broadcast).
        /// Acción esperada: Actualizar color/estado de la bandeja en el mapa visual.
        /// </summary>
        /// <param name="bandeja">DTO de la bandeja actualizada</param>
        /// <returns>Task completado cuando el cliente actualiza el mapa</returns>
        /// <example>
        /// // Backend (BandejaService):
        /// await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);
        /// </example>
        Task RecibirActualizacionBandeja(BandejaDTO bandeja);

        /// <summary>
        /// Confirmación inmediata de una acción crítica ejecutada por el usuario.
        /// Se usa para dar feedback instantáneo sin esperar respuesta HTTP.
        /// 
        /// Casos de uso:
        /// - "Retiro autorizado correctamente"
        /// - "Deuda de sangre regularizada"
        /// - "Error: No se pudo asignar bandeja (ocupada por otro usuario)"
        /// 
        /// Destinatarios: Usuario específico que ejecutó la acción.
        /// Acción esperada: Mostrar toast/snackbar con el resultado.
        /// </summary>
        /// <param name="accion">Nombre de la acción ejecutada (ej: "Autorizar Retiro")</param>
        /// <param name="exito">true = éxito, false = error</param>
        /// <param name="mensaje">Mensaje descriptivo del resultado</param>
        /// <returns>Task completado cuando el cliente muestra el feedback</returns>
        /// <example>
        /// // Backend (tras autorizar retiro):
        /// await Clients.User(userId).RecibirConfirmacionAccion(
        ///     "Autorizar Retiro",
        ///     true,
        ///     "El retiro del expediente SGM-2025-00015 ha sido autorizado"
        /// );
        /// </example>
        Task RecibirConfirmacionAccion(string accion, bool exito, string mensaje);
    }
}