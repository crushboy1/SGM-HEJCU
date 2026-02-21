using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    public interface IExpedienteRepository
    {
        Task<Expediente?> GetByIdAsync(int id);
        Task<Expediente?> GetByCodigoAsync(string codigoExpediente);
        Task<Expediente?> GetByHCAsync(string hc);
        Task<List<Expediente>> GetAllAsync();
        Task<List<Expediente>> GetByFiltrosAsync(string? hc, string? dni, string? servicio, DateTime? fechaDesde, DateTime? fechaHasta, EstadoExpediente? estado);
        Task<Expediente?> GetUltimoExpedienteDelAñoAsync(int año);
        Task<Expediente?> GetByCodigoQRAsync(string codigoQR);
        Task<Expediente> CreateAsync(Expediente expediente);
        Task UpdateAsync(Expediente expediente);
        Task<bool> ExistsHCAsync(string hc);
        Task<bool> ExistsCertificadoSINADEFAsync(string certificado);
        Task<int> GetCountByServicioAsync(string servicio);
        Task<List<Expediente>> GetPendientesValidacionAdmisionAsync();
        Task<List<Expediente>> GetPendientesRecojoAsync();
        // Búsqueda simple (para módulos de deudas)
        Task<Expediente?> GetByHCMasRecienteAsync(string hc);
        Task<Expediente?> GetByDNIMasRecienteAsync(string dni);
    }
}