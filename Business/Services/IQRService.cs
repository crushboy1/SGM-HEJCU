using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    public interface IQRService
    {
        /// <summary>
        /// Genera QR por primera vez para un expediente
        /// Cambia estado a "Pendiente de Recojo"
        /// </summary>
        Task<QRGeneradoDTO> GenerarQRAsync(int expedienteId);

        /// <summary>
        /// Obtiene el QR existente (para reimpresión)
        /// NO regenera el QR ni cambia estado
        /// </summary>
        Task<QRGeneradoDTO> ObtenerQRExistenteAsync(int expedienteId);

        /// <summary>
        /// Consulta expediente por código QR escaneado
        /// </summary>
        Task<ExpedienteDTO> ConsultarPorQRAsync(string codigoQR);
    }
}