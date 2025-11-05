using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    public interface ICustodiaService
    {
        /// <summary>
        /// Realiza traspaso de custodia (Enfermería → Ambulancia)
        /// Escanea QR, valida estado, registra transferencia, cambia estado
        /// </summary>
        Task<TraspasoRealizadoDTO> RealizarTraspasoAsync(RealizarTraspasoDTO dto, int usuarioDestinoId);

        /// <summary>
        /// Obtiene el historial completo de custodia de un expediente
        /// </summary>
        Task<List<CustodiaTransferenciaDTO>> GetHistorialCustodiaAsync(int expedienteId);

        /// <summary>
        /// Obtiene la última custodia (quién tiene el cuerpo actualmente)
        /// </summary>
        Task<CustodiaActualDTO?> GetUltimaCustodiaAsync(int expedienteId);
    }
}