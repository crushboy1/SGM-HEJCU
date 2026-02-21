using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestionar autoridades externas (PNP, Fiscal, Médico Legista).
    /// </summary>
    public interface IAutoridadExternaRepository
    {
        /// <summary>
        /// Crea una nueva autoridad externa.
        /// </summary>
        Task<AutoridadExterna> CreateAsync(AutoridadExterna autoridad);

        /// <summary>
        /// Obtiene una autoridad por su ID.
        /// </summary>
        Task<AutoridadExterna?> GetByIdAsync(int autoridadId);

        /// <summary>
        /// Obtiene todas las autoridades de un expediente legal.
        /// </summary>
        Task<List<AutoridadExterna>> GetByExpedienteLegalIdAsync(int expedienteLegalId);

        /// <summary>
        /// Elimina una autoridad externa.
        /// </summary>
        Task DeleteAsync(int autoridadId);

        /// <summary>
        /// Verifica si existe una autoridad con el mismo documento.
        /// </summary>
        Task<bool> ExistsByDocumentoAsync(string numeroDocumento, int expedienteLegalId);
    }
}