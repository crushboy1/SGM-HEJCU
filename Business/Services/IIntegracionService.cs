using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.ExternalSystems;

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

        /// <summary>
        /// Obtiene la lista de pacientes marcados como fallecidos en SIGEM
        /// que aún no han sido procesados (no tienen expediente en SGM).
        /// </summary>
        Task<List<PacienteGalenhos>> GetPacientesPendientesAsync();
    }
}