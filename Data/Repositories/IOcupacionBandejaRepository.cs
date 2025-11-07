using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar el historial de ocupaciones de bandejas.
    /// Cada registro representa una asignación de un expediente a una bandeja.
    /// </summary>
    public interface IOcupacionBandejaRepository
    {
        /// <summary>
        /// Registra un nuevo evento de ocupación (asignación) en la base de datos.
        /// </summary>
        Task<OcupacionBandeja> CreateAsync(OcupacionBandeja ocupacion);

        /// <summary>
        /// Actualiza un registro de ocupación (ej. para registrar la salida).
        /// </summary>
        Task UpdateAsync(OcupacionBandeja ocupacion);

        /// <summary>
        /// Obtiene el historial completo de ocupaciones para un expediente.
        /// </summary>
        Task<List<OcupacionBandeja>> GetHistorialByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene la ocupación activa actual para un expediente.
        /// (Donde FechaHoraSalida es null).
        /// </summary>
        Task<OcupacionBandeja?> GetActualByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene el historial completo de ocupaciones de una bandeja específica.
        /// </summary>
        Task<List<OcupacionBandeja>> GetHistorialByBandejaIdAsync(int bandejaId);

        /// <summary>
        /// Obtiene todas las ocupaciones actualmente activas (sin fecha de salida).
        /// </summary>
        Task<List<OcupacionBandeja>> GetOcupacionesActivasAsync();

        /// <summary>
        /// Obtiene ocupaciones activas que superan el tiempo de permanencia especificado.
        /// </summary>
        /// <param name="horasAlerta">Horas de permanencia para considerar alerta (default: 24)</param>
        Task<List<OcupacionBandeja>> GetOcupacionesConAlertaAsync(int horasAlerta = 24);

        /// <summary>
        /// Obtiene ocupaciones en un rango de fechas (para reportes).
        /// </summary>
        Task<List<OcupacionBandeja>> GetOcupacionesPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
    }
}