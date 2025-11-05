using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    public interface IUsuarioRepository
    {
        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        Task<Usuario?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene un usuario por su número de documento
        /// </summary>
        Task<Usuario?> GetByNumeroDocumentoAsync(string numeroDocumento);

        /// <summary>
        /// Obtiene todos los usuarios activos
        /// </summary>
        Task<List<Usuario>> GetAllActivosAsync();

        /// <summary>
        /// Obtiene usuarios por rol
        /// </summary>
        Task<List<Usuario>> GetByRolAsync(string nombreRol);
    }
}