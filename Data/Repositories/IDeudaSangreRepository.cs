using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    public interface IDeudaSangreRepository
    {
        Task<DeudaSangre?> GetByExpedienteIdAsync(int expedienteId);

        // Para la pantalla "Input" de Banco de Sangre
        Task<List<DeudaSangre>> GetPendientesAsync();

        Task<DeudaSangre> CreateAsync(DeudaSangre deuda);
        Task UpdateAsync(DeudaSangre deuda);
    }
}