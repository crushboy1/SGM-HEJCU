using System.ComponentModel.DataAnnotations;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs
{
    public class CreateExpedienteDTO
    {
        /// <summary>
        /// Tipo de ingreso: Interno (hospitalizado) o Externo .
        /// </summary>
        [Required(ErrorMessage = "El tipo de expediente es obligatorio")]
        public TipoIngreso TipoExpediente { get; set; }

        // Datos del Paciente
        [Required(ErrorMessage = "La HC es obligatoria")]
        [StringLength(20)]
        public string HC { get; set; } = string.Empty;

        [Required]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        /// <summary>Vacío para pacientes NN. Validación condicional en servicio.</summary>
        [StringLength(50)]
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

        /// <summary>1900-01-01 para pacientes NN (fecha referencial).</summary>
        public DateTime FechaNacimiento { get; set; } = new DateTime(1900, 1, 1);

        [Required]
        [RegularExpression("^[MF]$", ErrorMessage = "El sexo debe ser M o F")]
        public string Sexo { get; set; } = string.Empty;

        /// <summary>
        /// Default: PendientePago. En flujo normal viene de Galenhos (readonly).
        /// Editable solo en registro manual.
        /// </summary>
        public FuenteFinanciamiento FuenteFinanciamiento { get; set; } = FuenteFinanciamiento.PendientePago;

        // Datos del Fallecimiento
        [Required]
        [StringLength(100)]
        public string ServicioFallecimiento { get; set; } = string.Empty;

        [StringLength(20)]
        public string? NumeroCama { get; set; }

        [Required]
        public DateTime FechaHoraFallecimiento { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string MedicoCMP { get; set; } = string.Empty;

        [StringLength(10)]
        public string? MedicoRNE { get; set; }

        /// <summary>
        /// Diagnóstico final CIE-10. Opcional al crear — puede completarse después.
        /// Validación en servicio según TipoExpediente.
        /// </summary>
        [StringLength(500)]
        public string? DiagnosticoFinal { get; set; }

        /// <summary>
        /// Paciente no identificado. Default false.
        /// </summary>
        public bool EsNN { get; set; } = false;

        /// <summary>
        /// Causa violenta o dudosa. Bloquea médico externo y fuerza AutoridadLegal.
        /// </summary>
        public bool CausaViolentaODudosa { get; set; } = false;

        /// <summary>
        /// Médico externo que certifica el fallecimiento.
        /// Aplica cuando CausaViolentaODudosa = false, independientemente del TipoExpediente.
        /// Casos: Externo (DOA) o Interno con menos de 24h de hospitalización.
        /// </summary>
        [StringLength(200)]
        public string? MedicoExternoNombre { get; set; }

        [StringLength(10)]
        public string? MedicoExternoCMP { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

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