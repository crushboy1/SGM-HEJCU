using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar los registros de verificación de ingreso al mortuorio.
    /// Cada registro es una auditoría de un intento de verificación por parte del vigilante.
    /// </summary>
    public interface IVerificacionMortuorioRepository
    {
        /// <summary>
        /// Crea un nuevo registro de verificación (aprobado o rechazado).
        /// </summary>
        Task<VerificacionMortuorio> CreateAsync(VerificacionMortuorio verificacion);

        /// <summary>
        /// Obtiene una verificación por su ID.
        /// </summary>
        Task<VerificacionMortuorio?> GetByIdAsync(int verificacionId);

        /// <summary>
        /// Obtiene el historial completo de verificaciones para un expediente.
        /// </summary>
        Task<List<VerificacionMortuorio>> GetHistorialByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene el último intento de verificación para un expediente.
        /// </summary>
        Task<VerificacionMortuorio?> GetUltimaVerificacionAsync(int expedienteId);

        /// <summary>
        /// Obtiene todas las verificaciones realizadas por un vigilante específico.
        /// </summary>
        Task<List<VerificacionMortuorio>> GetByVigilanteIdAsync(int vigilanteId);

        /// <summary>
        /// Obtiene todas las verificaciones rechazadas (para seguimiento).
        /// </summary>
        Task<List<VerificacionMortuorio>> GetVerificacionesRechazadasAsync();

        /// <summary>
        /// Obtiene verificaciones en un rango de fechas (para reportes).
        /// </summary>
        Task<List<VerificacionMortuorio>> GetVerificacionesPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Obtiene estadísticas de verificaciones (aprobadas vs rechazadas).
        /// </summary>
        Task<VerificacionEstadisticas> GetEstadisticasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
    }

    /// <summary>
    /// Clase para retornar estadísticas de verificaciones
    /// </summary>
    public class VerificacionEstadisticas
    {
        public int TotalVerificaciones { get; set; }
        public int Aprobadas { get; set; }
        public int Rechazadas { get; set; }
        public double PorcentajeAprobacion { get; set; }
        public int ConDiscrepanciaHC { get; set; }
        public int ConDiscrepanciaDNI { get; set; }
        public int ConDiscrepanciaNombre { get; set; }
    }
}