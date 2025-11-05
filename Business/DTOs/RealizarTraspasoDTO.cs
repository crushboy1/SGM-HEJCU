using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO para realizar traspaso de custodia
    /// El técnico de ambulancia escanea el QR y envía este DTO
    /// </summary>
    public class RealizarTraspasoDTO
    {
        [Required(ErrorMessage = "El código QR es obligatorio")]
        public string CodigoQR { get; set; } = string.Empty;

        /// <summary>
        /// Observaciones opcionales del técnico al recibir la custodia
        /// </summary>
        public string? Observaciones { get; set; }
    }
}