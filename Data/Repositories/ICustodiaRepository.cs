using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    public interface ICustodiaRepository
    {
        /// <summary>
        /// Crea un nuevo registro de transferencia de custodia
        /// </summary>
        Task<CustodiaTransferencia> CreateAsync(CustodiaTransferencia transferencia);

        /// <summary>
        /// Obtiene el historial completo de transferencias de un expediente
        /// </summary>
        Task<List<CustodiaTransferencia>> GetHistorialByExpedienteAsync(int expedienteId);

        /// <summary>
        /// Obtiene la última transferencia de custodia de un expediente
        /// </summary>
        Task<CustodiaTransferencia?> GetUltimaTransferenciaAsync(int expedienteId);

        /// <summary>
        /// Verifica si existe una transferencia reciente del mismo usuario
        /// Para evitar duplicados accidentales
        /// </summary>
        Task<bool> ExisteTransferenciaRecienteAsync(int expedienteId, int usuarioDestinoId, int minutosMargen = 5);
    }
}