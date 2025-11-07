using SisMortuorio.Business.DTOs.Solicitud;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de negocio para gestionar el flujo de corrección de expedientes.
    /// Utilizado por Enfermería para resolver tickets generados por Vigilancia.
    /// </summary>
    public interface ISolicitudCorreccionService
    {
        /// <summary>
        /// Resuelve una solicitud de corrección pendiente.
        /// 1. Marca la solicitud como Resuelta.
        /// 2. Dispara el trigger 'CorregirDatos' en el expediente.
        /// 3. El expediente vuelve al estado 'EnTrasladoMortuorio'.
        /// </summary>
        /// <param name="solicitudId">ID de la solicitud a resolver</param>
        /// <param name="dto">Datos de la resolución</param>
        /// <param name="usuarioId">ID del usuario (Enfermería) que resuelve (del Token)</param>
        /// <returns>El DTO de la solicitud actualizada/resuelta</returns>
        Task<SolicitudCorreccionDTO> ResolverSolicitudAsync(int solicitudId, ResolverSolicitudDTO dto, int usuarioId);

        /// <summary>
        /// Obtiene una solicitud de corrección por su ID.
        /// </summary>
        Task<SolicitudCorreccionDTO?> GetByIdAsync(int solicitudId);

        /// <summary>
        /// Obtiene todas las solicitudes de corrección pendientes (no resueltas).
        /// </summary>
        Task<List<SolicitudCorreccionDTO>> GetPendientesAsync();

        /// <summary>
        /// Obtiene las solicitudes pendientes para un servicio de enfermería específico.
        /// </summary>
        Task<List<SolicitudCorreccionDTO>> GetPendientesByServicioAsync(string servicio);

        /// <summary>
        /// Obtiene el historial de solicitudes (resueltas o no) para un expediente.
        /// </summary>
        Task<List<SolicitudCorreccionDTO>> GetHistorialByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene estadísticas sobre las solicitudes de corrección.
        /// </summary>
        Task<EstadisticasSolicitudDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin);
    }
}