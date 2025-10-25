namespace SisMortuorio.Data.ExternalSystems
{
    public interface ISigemService
    {
        Task<DiagnosticoSigem?> GetDiagnosticoByHCAsync(string hc);
    }
}