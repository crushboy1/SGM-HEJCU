namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio centralizado para notificaciones SignalR relacionadas con deudas.
    /// Encapsula la construcción de NotificacionDTO y envío a través de SignalR Hub.
    /// </summary>
    public interface INotificacionDeudaService
    {
        /// <summary>
        /// Notifica creación de nueva deuda (económica o sangre).
        /// Bloquea retiro del expediente.
        /// </summary>
        Task NotificarDeudaCreadaAsync(
            string tipoDeuda,
            int expedienteId,
            string codigoExpediente,
            string detalle);

        /// <summary>
        /// Notifica resolución de deuda individual (liquidada, exonerada, anulada).
        /// No implica desbloqueo total si hay otras deudas pendientes.
        /// </summary>
        Task NotificarDeudaResueltaAsync(
            string tipoDeuda,
            string accionRealizada,
            int expedienteId,
            string codigoExpediente);

        /// <summary>
        /// Notifica desbloqueo total del expediente.
        /// Todas las deudas han sido resueltas.
        /// </summary>
        Task NotificarDesbloqueoTotalAsync(
            int expedienteId,
            string codigoExpediente,
            int deudasResueltas,
            int totalDeudas);

        /// <summary>
        /// Notifica desbloqueo parcial del expediente.
        /// Algunas deudas resueltas, otras aún pendientes.
        /// </summary>
        Task NotificarDesbloqueoParcialAsync(
            int expedienteId,
            string codigoExpediente,
            string tipoDeudaResuelto,
            int deudasResueltas,
            int totalDeudas);
    }
}