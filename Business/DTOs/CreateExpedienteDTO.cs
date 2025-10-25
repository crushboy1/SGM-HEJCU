using SisMortuorio.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs
{
    public class CreateExpedienteDTO
    {
        [Required(ErrorMessage = "El tipo de expediente es obligatorio")]
        public string TipoExpediente { get; set; } = string.Empty; // "Interno" o "Externo"

        // Datos del Paciente
        [Required(ErrorMessage = "La HC es obligatoria")]
        [StringLength(20)]
        public string HC { get; set; } = string.Empty;

        [Required]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [RegularExpression("^[MF]$", ErrorMessage = "El sexo debe ser M o F")]
        public string Sexo { get; set; } = string.Empty;

        [Required]
        public string TipoSeguro { get; set; } = string.Empty; // "SIS" o "PARTICULAR"

        // Datos del Fallecimiento
        [Required]
        public string ServicioFallecimiento { get; set; } = string.Empty;

        public string? NumeroCama { get; set; }

        [Required]
        public DateTime FechaHoraFallecimiento { get; set; }

        [Required]
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string MedicoCMP { get; set; } = string.Empty;

        public string? MedicoRNE { get; set; }

        public string? NumeroCertificadoSINADEF { get; set; }

        [Required]
        [StringLength(500)]
        public string CausaMuerte { get; set; } = string.Empty;

        // Pertenencias (opcional al crear)
        public List<CreatePertenenciaDTO>? Pertenencias { get; set; }
    }

    public class CreatePertenenciaDTO
    {
        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        public string? Observaciones { get; set; }
    }
}