using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    public interface IExpedienteService
    {
        Task<ExpedienteDTO?> GetByIdAsync(int id);
        Task<List<ExpedienteDTO>> GetAllAsync();
        Task<List<ExpedienteDTO>> GetByFiltrosAsync(string? hc, string? dni, string? servicio, DateTime? fechaDesde, DateTime? fechaHasta, string? estado);
        Task<ExpedienteDTO> CreateAsync(CreateExpedienteDTO dto, int usuarioCreadorId);
        Task<ExpedienteDTO?> UpdateAsync(int id, UpdateExpedienteDTO dto);
        Task<bool> ValidarHCUnicoAsync(string hc);
        Task<bool> ValidarCertificadoSINADEFUnicoAsync(string certificado);
    }
}