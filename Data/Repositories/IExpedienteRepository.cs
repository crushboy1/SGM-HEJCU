using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    public interface IExpedienteRepository
    {
        Task<Expediente?> GetByIdAsync(int id);
        Task<Expediente?> GetByCodigoAsync(string codigoExpediente);
        Task<Expediente?> GetByHCAsync(string hc);
        Task<List<Expediente>> GetAllAsync();
        Task<List<Expediente>> GetByFiltrosAsync(string? hc, string? dni, string? servicio, DateTime? fechaDesde, DateTime? fechaHasta, string? estado);
        Task<Expediente> CreateAsync(Expediente expediente);
        Task UpdateAsync(Expediente expediente);
        Task<bool> ExistsHCAsync(string hc);
        Task<bool> ExistsCertificadoSINADEFAsync(string certificado);
        Task<int> GetCountByServicioAsync(string servicio);
    }
}