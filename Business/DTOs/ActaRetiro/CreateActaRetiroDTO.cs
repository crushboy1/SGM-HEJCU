using System.ComponentModel.DataAnnotations;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs.ActaRetiro;

/// <summary>
/// DTO para crear un Acta de Retiro.
/// Los campos obligatorios varían según TipoSalida.
/// El bypass de deuda NO va aquí — tiene su propio endpoint y DTO.
/// </summary>
public class CreateActaRetiroDTO
{
    // ═══════════════════════════════════════════════════════════
    // EXPEDIENTE
    // ═══════════════════════════════════════════════════════════
    [Required(ErrorMessage = "El ID del expediente es obligatorio")]
    public int ExpedienteID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DOCUMENTO LEGAL (CONDICIONAL SEGÚN TIPO)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// N° Certificado SINADEF.
    /// Para TipoSalida = Familiar: SIEMPRE obligatorio.
    /// El médico externo (si aplica) genera el SINADEF antes de venir a Admisión.
    /// Para TipoSalida = AutoridadLegal: no aplica.
    /// TODO: Agregar validación en ActaRetiroService.CreateAsync() que exija
    /// NumeroCertificadoDefuncion cuando TipoSalida = Familiar.
    /// Actualmente el frontend garantiza esto — prioridad: Media.
    /// </summary>
    [StringLength(50)]
    public string? NumeroCertificadoDefuncion { get; set; }

    /// <summary>
    /// N° Oficio Legal.
    /// OBLIGATORIO si TipoSalida = AutoridadLegal.
    /// Formato real: "644-2026-REGPOLLIMA/DIVOPOS SUR1-COM SAN ANTONIO.SE"
    /// </summary>
    [StringLength(150)]
    public string? NumeroOficioPolicial { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DEL FALLECIDO
    // ═══════════════════════════════════════════════════════════
    [Required]
    [StringLength(300)]
    public string NombreCompletoFallecido { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string HistoriaClinica { get; set; } = string.Empty;

    [Required]
    public TipoDocumentoIdentidad TipoDocumentoFallecido { get; set; }

    [Required]
    [StringLength(50)]
    public string NumeroDocumentoFallecido { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ServicioFallecimiento { get; set; } = string.Empty;

    [Required]
    public DateTime FechaHoraFallecimiento { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MÉDICO CERTIFICANTE (hospital)
    // ═══════════════════════════════════════════════════════════
    [Required]
    [StringLength(300)]
    public string MedicoCertificaNombre { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string MedicoCMP { get; set; } = string.Empty;

    [StringLength(20)]
    public string? MedicoRNE { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MÉDICO EXTERNO (condicional)
    // Aplica cuando CausaViolentaODudosa = false y familia trae
    // médico de cabecera. Válido para Interno <24h y Externo (DOA).
    // Nunca aplica cuando CausaViolentaODudosa = true.
    //
    // TODO: Validación condicional en ActaRetiroService.CreateAsync()
    // Si MedicoExternoNombre tiene valor → MedicoExternoCMP obligatorio.
    // Ambos campos son opcionales en el DTO (nullable) porque la validación
    // depende del contexto del expediente, no del DTO aislado.
    // Actualmente el frontend garantiza esto con el checkbox tieneMedicoExterno.
    // Prioridad: Media — el frontend ya bloquea el caso inválido.
    // ═══════════════════════════════════════════════════════════
    [StringLength(300)]
    public string? MedicoExternoNombre { get; set; }

    [StringLength(20)]
    public string? MedicoExternoCMP { get; set; }

    // ═══════════════════════════════════════════════════════════
    // JEFE DE GUARDIA
    // ═══════════════════════════════════════════════════════════
    [Required]
    [StringLength(300)]
    public string JefeGuardiaNombre { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string JefeGuardiaCMP { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // TIPO DE SALIDA
    // ═══════════════════════════════════════════════════════════
    [Required]
    public TipoSalida TipoSalida { get; set; } = TipoSalida.Familiar;

    // ═══════════════════════════════════════════════════════════
    // FAMILIAR
    // ═══════════════════════════════════════════════════════════
    [StringLength(100)]
    public string? FamiliarApellidoPaterno { get; set; }

    [StringLength(100)]
    public string? FamiliarApellidoMaterno { get; set; }

    [StringLength(100)]
    public string? FamiliarNombres { get; set; }

    public TipoDocumentoIdentidad? FamiliarTipoDocumento { get; set; }

    [StringLength(50)]
    public string? FamiliarNumeroDocumento { get; set; }

    [StringLength(100)]
    public string? FamiliarParentesco { get; set; }

    [StringLength(20)]
    public string? FamiliarTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIDAD LEGAL
    // ═══════════════════════════════════════════════════════════
    [StringLength(100)]
    public string? AutoridadApellidoPaterno { get; set; }

    [StringLength(100)]
    public string? AutoridadApellidoMaterno { get; set; }

    [StringLength(100)]
    public string? AutoridadNombres { get; set; }

    public TipoAutoridadExterna? TipoAutoridad { get; set; }

    public TipoDocumentoIdentidad? AutoridadTipoDocumento { get; set; }

    [StringLength(50)]
    public string? AutoridadNumeroDocumento { get; set; }

    /// <summary>
    /// Grado y cargo del efectivo.
    /// Ej: "SO3 PNP", "Fiscal Provincial", "Médico Legista II"
    /// </summary>
    [StringLength(100)]
    public string? AutoridadCargo { get; set; }

    /// <summary>
    /// Comisaría o institución.
    /// Ej: "Comisaría San Antonio", "Fiscalía de Turno Miraflores"
    /// </summary>
    [StringLength(200)]
    public string? AutoridadInstitucion { get; set; }

    [StringLength(20)]
    public string? AutoridadTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS ADICIONALES
    // ═══════════════════════════════════════════════════════════
    [StringLength(500)]
    public string? DatosAdicionales { get; set; }

    [StringLength(200)]
    public string? Destino { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    // ═══════════════════════════════════════════════════════════
    // USUARIO
    // ═══════════════════════════════════════════════════════════
    [Required]
    public int UsuarioAdmisionID { get; set; }
}