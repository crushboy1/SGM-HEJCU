using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.Services
{
    public interface IExpedienteService
    {
        Task<ExpedienteDTO?> GetByIdAsync(int id);
        Task<List<ExpedienteDTO>> GetAllAsync();
        Task<List<ExpedienteDTO>> GetByFiltrosAsync(string? hc, string? dni, string? servicio, DateTime? fechaDesde, DateTime? fechaHasta, EstadoExpediente? estado);
        Task<ExpedienteDTO> CreateAsync(CreateExpedienteDTO dto, int usuarioCreadorId);
        Task<ExpedienteDTO?> UpdateAsync(int id, UpdateExpedienteDTO dto);
        Task<bool> ValidarHCUnicoAsync(string hc);
        Task<bool> ValidarCertificadoSINADEFUnicoAsync(string certificado);
        Task<ExpedienteDTO> ValidarDocumentacionAdmisionAsync(int expedienteId, int usuarioAdmisionId);
        Task<List<ExpedienteDTO>> GetPendientesValidacionAdmisionAsync();
        Task<List<ExpedienteDTO>> GetPendientesRecojoAsync();
        Task<ExpedienteDTO?> BuscarPorHCAsync(string hc);
        Task<ExpedienteDTO?> BuscarPorDNIAsync(string dni);
        Task<ExpedienteDTO?> BuscarPorCodigoAsync(string codigoExpediente);
    }
}