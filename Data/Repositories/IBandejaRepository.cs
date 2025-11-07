using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar la infraestructura de Bandejas del mortuorio.
    /// Maneja la tabla maestra de Bandejas, su estado y disponibilidad.
    /// </summary>
    public interface IBandejaRepository
    {
        /// <summary>
        /// Obtiene todas las bandejas (incluyendo ocupadas, en mantenimiento, etc.).
        /// </summary>
        Task<List<Bandeja>> GetAllAsync();

        /// <summary>
        /// Obtiene una bandeja específica por su ID.
        /// </summary>
        Task<Bandeja?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene una bandeja específica por su código (ej. "B-01").
        /// </summary>
        Task<Bandeja?> GetByCodigoAsync(string codigo);

        /// <summary>
        /// Obtiene solo las bandejas que están actualmente disponibles.
        /// </summary>
        Task<List<Bandeja>> GetDisponiblesAsync();

        /// <summary>
        /// Obtiene bandejas filtradas por estado.
        /// </summary>
        Task<List<Bandeja>> GetByEstadoAsync(EstadoBandeja estado);

        /// <summary>
        /// Obtiene la bandeja que actualmente ocupa un expediente.
        /// </summary>
        Task<Bandeja?> GetByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene bandejas ocupadas que superan el tiempo de permanencia.
        /// </summary>
        /// <param name="horasAlerta">Horas de permanencia para considerar alerta (default: 24)</param>
        Task<List<Bandeja>> GetBandejasConAlertaAsync(int horasAlerta = 24);

        /// <summary>
        /// Obtiene estadísticas de ocupación del mortuorio.
        /// </summary>
        Task<BandejaEstadisticas> GetEstadisticasAsync();

        /// <summary>
        /// Actualiza el estado de una bandeja (ej. Ocupar, Liberar).
        /// </summary>
        Task UpdateAsync(Bandeja bandeja);
    }

    /// <summary>
    /// Clase para retornar estadísticas de bandejas
    /// </summary>
    public class BandejaEstadisticas
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public int Ocupadas { get; set; }
        public int EnMantenimiento { get; set; }
        public int FueraDeServicio { get; set; }
        public double PorcentajeOcupacion { get; set; }
        public int ConAlerta24h { get; set; }
        public int ConAlerta48h { get; set; }
    }
}