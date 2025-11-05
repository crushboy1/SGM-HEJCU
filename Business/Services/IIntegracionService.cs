using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para integración con sistemas externos (Galenhos y SIGEM)
    /// </summary>
    public interface IIntegracionService
    {
        /// <summary>
        /// Consulta datos combinados de un paciente desde Galenhos y SIGEM
        /// </summary>
        Task<ConsultarPacienteDTO?> ConsultarPacienteByHCAsync(string hc);
    }
}