using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    public interface IDeudaEconomicaRepository
    {
        // Para verificar estado en el semáforo
        Task<DeudaEconomica?> GetByExpedienteIdAsync(int expedienteId);

        // Para la pantalla "Input" de Cuentas/Social (listar solo los que deben)
        Task<List<DeudaEconomica>> GetPendientesAsync();

        // CRUD básico
        Task<DeudaEconomica> CreateAsync(DeudaEconomica deuda);
        Task UpdateAsync(DeudaEconomica deuda);
    }
}