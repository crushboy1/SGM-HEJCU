using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Verificacion
{
    /// <summary>
    /// DTO de entrada (Input) para registrar un intento de verificación de ingreso.
    /// El Vigilante envía los datos leídos del brazalete.
    /// El servicio comparará estos datos contra la BD.
    /// </summary>
    public class VerificacionRequestDTO
    {
        // El ID del Vigilante se tomará del Token (Claim)

        [Required(ErrorMessage = "Debe escanear el QR del brazalete")]
        [StringLength(50)]
        public string CodigoExpedienteBrazalete { get; set; } = string.Empty;

        [Required(ErrorMessage = "La HC del brazalete es obligatoria")]
        [StringLength(20)]
        public string HCBrazalete { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI/Documento del brazalete es obligatorio")]
        [StringLength(20)]
        public string DNIBrazalete { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Nombre del brazalete es obligatorio")]
        [StringLength(300)]
        public string NombreCompletoBrazalete { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Servicio del brazalete es obligatorio")]
        [StringLength(100)]
        public string ServicioBrazalete { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? Observaciones { get; set; }
    }
}