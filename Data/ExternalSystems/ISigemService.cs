namespace SisMortuorio.Data.ExternalSystems
{
    public interface ISigemService
    {
        Task<EpisodioSigem?> GetUltimoEpisodioByHCAsync(string hc);
    }
}