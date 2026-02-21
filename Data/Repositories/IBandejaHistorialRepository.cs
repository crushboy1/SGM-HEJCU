using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar el historial de ocupaciones de bandejas.
    /// Cada registro representa una asignación de un expediente a una bandeja.
    /// </summary>
    public interface IBandejaHistorialRepository
    {
        /// <summary>
        /// Registra un nuevo evento de ocupación (asignación) en la base de datos.
        /// </summary>
        Task<BandejaHistorial> CreateAsync(BandejaHistorial ocupacion);

        /// <summary>
        /// Actualiza un registro de ocupación (ej. para registrar la salida).
        /// </summary>
        Task UpdateAsync(BandejaHistorial ocupacion);

        /// <summary>
        /// Obtiene el historial completo de ocupaciones para un expediente.
        /// </summary>
        Task<List<BandejaHistorial>> GetHistorialByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene la ocupación activa actual para un expediente.
        /// (Donde FechaHoraSalida es null).
        /// </summary>
        Task<BandejaHistorial?> GetActualByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene el historial completo de ocupaciones de una bandeja específica.
        /// </summary>
        Task<List<BandejaHistorial>> GetHistorialByBandejaIdAsync(int bandejaId);

        /// <summary>
        /// Obtiene todas las ocupaciones actualmente activas (sin fecha de salida).
        /// </summary>
        Task<List<BandejaHistorial>> GetOcupacionesActivasAsync();

        /// <summary>
        /// Obtiene ocupaciones activas que superan el tiempo de permanencia especificado.
        /// </summary>
        /// <param name="horasAlerta">Horas de permanencia para considerar alerta (default: 24)</param>
        Task<List<BandejaHistorial>> GetOcupacionesConAlertaAsync(int horasAlerta = 24);

        /// <summary>
        /// Obtiene ocupaciones en un rango de fechas (para reportes).
        /// </summary>
        Task<List<BandejaHistorial>> GetOcupacionesPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
    }
}