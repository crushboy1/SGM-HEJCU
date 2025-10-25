namespace SisMortuorio.Data.ExternalSystems
{
    public interface IGalenhosService
    {
        Task<PacienteGalenhos?> GetPacienteByHCAsync(string hc);
        Task<PacienteGalenhos?> GetPacienteByDocumentoAsync(string numeroDocumento);
        Task<List<string>> GetServiciosActivos();
    }
}