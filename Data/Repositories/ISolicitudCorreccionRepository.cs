using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar las solicitudes de corrección de expedientes.
    /// Maneja el "ticket" generado cuando una verificación es rechazada.
    /// </summary>
    public interface ISolicitudCorreccionRepository
    {
        /// <summary>
        /// Crea una nueva solicitud de corrección.
        /// </summary>
        Task<SolicitudCorreccionExpediente> CreateAsync(SolicitudCorreccionExpediente solicitud);

        /// <summary>
        /// Actualiza una solicitud (ej. para marcarla como resuelta).
        /// </summary>
        Task UpdateAsync(SolicitudCorreccionExpediente solicitud);

        /// <summary>
        /// Obtiene una solicitud específica por su ID.
        /// </summary>
        Task<SolicitudCorreccionExpediente?> GetByIdAsync(int solicitudId);

        /// <summary>
        /// Obtiene todas las solicitudes de corrección pendientes (no resueltas).
        /// </summary>
        Task<List<SolicitudCorreccionExpediente>> GetPendientesAsync();

        /// <summary>
        /// Obtiene las solicitudes pendientes asociadas a un servicio de enfermería específico.
        /// </summary>
        Task<List<SolicitudCorreccionExpediente>> GetPendientesByServicioAsync(string servicio);

        /// <summary>
        /// Obtiene el historial de solicitudes para un expediente.
        /// </summary>
        Task<List<SolicitudCorreccionExpediente>> GetHistorialByExpedienteIdAsync(int expedienteId);
        /// <summary>
        /// Obtiene solicitudes pendientes que superan el tiempo de alerta (default: 2 horas).
        /// </summary>
        Task<List<SolicitudCorreccionExpediente>> GetSolicitudesConAlertaAsync(int horasAlerta = 2);

        /// <summary>
        /// Obtiene solicitudes en un rango de fechas (para reportes).
        /// </summary>
        Task<List<SolicitudCorreccionExpediente>> GetSolicitudesPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Obtiene estadísticas de solicitudes.
        /// </summary>
        Task<SolicitudEstadisticas> GetEstadisticasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);

        /// <summary>
        /// Clase para estadísticas
        /// </summary>
        public class SolicitudEstadisticas
        {
            public int TotalSolicitudes { get; set; }
            public int Pendientes { get; set; }
            public int Resueltas { get; set; }
            public int ConAlerta { get; set; }
            public double TiempoPromedioResolucionHoras { get; set; }
        }
    }

}