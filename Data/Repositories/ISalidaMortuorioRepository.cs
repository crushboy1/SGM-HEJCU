using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar el registro de salida física del mortuorio.
    /// Este es el registro final de auditoría del expediente.
    /// </summary>
    public interface ISalidaMortuorioRepository
    {
        /// <summary>
        /// Crea el registro único de salida para un expediente.
        /// </summary>
        Task<SalidaMortuorio> CreateAsync(SalidaMortuorio salida);

        /// <summary>
        /// Obtiene una salida por su ID.
        /// </summary>
        Task<SalidaMortuorio?> GetByIdAsync(int salidaId);

        /// <summary>
        /// Obtiene el registro de salida asociado a un expediente.
        /// </summary>
        Task<SalidaMortuorio?> GetByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Verifica si ya existe un registro de salida para un expediente.
        /// </summary>
        Task<bool> ExistsByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene todas las salidas realizadas por un vigilante específico.
        /// </summary>
        Task<List<SalidaMortuorio>> GetByVigilanteIdAsync(int vigilanteId);

        /// <summary>
        /// Obtiene salidas filtradas por tipo (Familiar, AutoridadLegal, etc.).
        /// </summary>
        Task<List<SalidaMortuorio>> GetByTipoSalidaAsync(TipoSalida tipoSalida);

        /// <summary>
        /// Obtiene salidas en un rango de fechas (para reportes).
        /// </summary>
        Task<List<SalidaMortuorio>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Obtiene salidas que tuvieron incidentes registrados.
        /// </summary>
        Task<List<SalidaMortuorio>> GetSalidasConIncidentesAsync();

        /// <summary>
        /// Obtiene salidas realizadas por una funeraria específica.
        /// </summary>
        Task<List<SalidaMortuorio>> GetByFunerariaAsync(string nombreFuneraria);

        /// <summary>
        /// Obtiene estadísticas de salidas (por tipo, incidentes, etc.).
        /// </summary>
        Task<SalidaEstadisticas> GetEstadisticasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
        /// <summary>
        /// Obtiene salidas que excedieron 48 horas de permanencia
        /// </summary>
        Task<List<SalidaMortuorio>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin);

        /// <summary>
        /// Obtiene salidas por tipo específico
        /// </summary>
        Task<List<SalidaMortuorio>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin);

        Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId);
    }

    /// <summary>
    /// Clase para retornar estadísticas de salidas
    /// </summary>
    public class SalidaEstadisticas
    {
        public int TotalSalidas { get; set; }
        public int SalidasFamiliar { get; set; }
        public int SalidasAutoridadLegal { get; set; }
        public int SalidasTrasladoHospital { get; set; }
        public int SalidasOtro { get; set; }
        public int ConIncidentes { get; set; }
        public int ConFuneraria { get; set; }
        public double PorcentajeIncidentes { get; set; }
    }
}