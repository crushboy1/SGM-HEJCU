using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Verificacion
{
    /// <summary>
    /// DTO de entrada para registrar un intento de verificación de ingreso al mortuorio.
    /// El sistema llena automáticamente los campos del brazalete desde la BD al escanear el QR.
    /// La única confirmación manual del Vigilante es <see cref="BrazaletePresente"/>.
    /// </summary>
    public class VerificacionRequestDTO
    {
        // El ID del Vigilante se toma del Token (Claim)

        /// <summary>Código SGM escaneado del brazalete QR.</summary>
        [Required(ErrorMessage = "Debe escanear el QR del brazalete")]
        [StringLength(50)]
        public string CodigoExpedienteBrazalete { get; set; } = string.Empty;

        // --- Campos informativos llenados automáticamente por el frontend ---
        // Se guardan para auditoría. No son la condición de decisión del happy/sad path.

        /// <summary>HC leída del brazalete (pre-llenada por el sistema).</summary>
        [StringLength(20)]
        public string HCBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento leído del brazalete (ej: "DNI", "CE", "Pasaporte", "NN").
        /// Puede ser vacío para pacientes NN.
        /// </summary>
        [StringLength(20)]
        public string TipoDocumentoBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento leído del brazalete.
        /// Puede ser vacío para pacientes NN.
        /// </summary>
        [StringLength(20)]
        public string NumeroDocumentoBrazalete { get; set; } = string.Empty;

        /// <summary>Nombre completo leído del brazalete (pre-llenado por el sistema).</summary>
        [StringLength(300)]
        public string NombreCompletoBrazalete { get; set; } = string.Empty;

        /// <summary>Servicio de fallecimiento leído del brazalete (pre-llenado por el sistema).</summary>
        [StringLength(100)]
        public string ServicioBrazalete { get; set; } = string.Empty;

        // --- Confirmación manual del Vigilante ---

        /// <summary>
        /// Confirmación explícita del Vigilante de que el brazalete físico
        /// está presente en el cuerpo y coincide visualmente con el expediente mostrado en pantalla.
        /// Es la única validación que no puede hacer el sistema automáticamente.
        /// </summary>
        [Required(ErrorMessage = "Debe confirmar la presencia del brazalete físico")]
        public bool BrazaletePresente { get; set; }

        /// <summary>Observaciones opcionales del Vigilante.</summary>
        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? Observaciones { get; set; }
    }
}