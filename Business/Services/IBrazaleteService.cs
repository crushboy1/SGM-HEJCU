using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    public interface IBrazaleteService
    {
        /// <summary>
        /// Genera PDF del brazalete con QR
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="esReimpresion">True si es reimpresión, False si es primera vez</param>
        Task<BrazaleteDTO> GenerarBrazaleteAsync(int expedienteId, bool esReimpresion = false);
    }
}