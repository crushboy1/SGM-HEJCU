using SisMortuorio.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Salida
{
    /// <summary>
    /// DTO de entrada (Input) con todos los datos necesarios
    /// para registrar la salida física de un cuerpo del mortuorio.
    /// </summary>
    public class RegistrarSalidaDTO
    {
        [Required(ErrorMessage = "El ID del expediente es obligatorio")]
        public int ExpedienteID { get; set; }

        // El ID del Vigilante se tomará del Token (Claim)

        [Required(ErrorMessage = "El tipo de salida es obligatorio")]
        public TipoSalida TipoSalida { get; set; }

        // --- Datos del Responsable ---
        [Required(ErrorMessage = "El nombre del responsable es obligatorio")]
        [StringLength(200)]
        public string ResponsableNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento del responsable es obligatorio")]
        [StringLength(20)]
        public string ResponsableTipoDocumento { get; set; } = "DNI";

        [Required(ErrorMessage = "El número de documento del responsable es obligatorio")]
        [StringLength(20)]
        public string ResponsableNumeroDocumento { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ResponsableParentesco { get; set; }

        [StringLength(20)]
        public string? ResponsableTelefono { get; set; }

        // --- Autorización (si aplica) ---
        [StringLength(100)]
        public string? NumeroAutorizacion { get; set; } // Ej. Orden fiscal

        [StringLength(200)]
        public string? EntidadAutorizante { get; set; } // Ej. "Fiscalía Provincial"

        // --- Verificación (Checklists del Vigilante) ---
        [Required]
        public bool DocumentacionVerificada { get; set; } = false;

        [Required]
        public bool PagoRealizado { get; set; } = false; // Check si aplica (PARTICULAR)

        [StringLength(50)]
        public string? NumeroRecibo { get; set; }

        // --- Datos Funeraria (si aplica) ---
        [StringLength(200)]
        public string? NombreFuneraria { get; set; }

        [StringLength(200)]
        public string? ConductorFuneraria { get; set; }

        [StringLength(20)]
        public string? DNIConductor { get; set; }

        [StringLength(20)]
        public string? PlacaVehiculo { get; set; }

        // --- Destino y Cierre ---
        [StringLength(200)]
        public string? Destino { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }
    }
}