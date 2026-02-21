using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs.ActaRetiro;

/// <summary>
/// DTO de respuesta para Acta de Retiro
/// </summary>
public class ActaRetiroDTO
{
    public int ActaRetiroID { get; set; }
    public int ExpedienteID { get; set; }
    public string CodigoExpediente { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // DOCUMENTO LEGAL (POLIMÓRFICO)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// N° Certificado SINADEF (si TipoSalida = Familiar)
    /// </summary>
    public string? NumeroCertificadoDefuncion { get; set; }

    /// <summary>
    /// N° Oficio Legal (si TipoSalida = AutoridadLegal)
    /// </summary>
    public string? NumeroOficioLegal { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DEL FALLECIDO
    // ═══════════════════════════════════════════════════════════
    public string NombreCompletoFallecido { get; set; } = string.Empty;
    public string HistoriaClinica { get; set; } = string.Empty;
    public string TipoDocumentoFallecido { get; set; } = string.Empty;
    public string NumeroDocumentoFallecido { get; set; } = string.Empty;
    public string ServicioFallecimiento { get; set; } = string.Empty;
    public DateTime FechaHoraFallecimiento { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MÉDICO CERTIFICANTE
    // ═══════════════════════════════════════════════════════════
    public string MedicoCertificaNombre { get; set; } = string.Empty;
    public string MedicoCMP { get; set; } = string.Empty;
    public string? MedicoRNE { get; set; }

    // ═══════════════════════════════════════════════════════════
    // JEFE DE GUARDIA
    // ═══════════════════════════════════════════════════════════
    public string JefeGuardiaNombre { get; set; } = string.Empty;
    public string JefeGuardiaCMP { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // TIPO DE SALIDA
    // ═══════════════════════════════════════════════════════════
    public string TipoSalida { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE - FAMILIAR
    // ═══════════════════════════════════════════════════════════
    public string? FamiliarApellidoPaterno { get; set; }
    public string? FamiliarApellidoMaterno { get; set; }
    public string? FamiliarNombres { get; set; }
    public string? FamiliarNombreCompleto { get; set; }
    public string? FamiliarTipoDocumento { get; set; }
    public string? FamiliarNumeroDocumento { get; set; }
    public string? FamiliarParentesco { get; set; }
    public string? FamiliarTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE - AUTORIDAD LEGAL
    // ═══════════════════════════════════════════════════════════
    public string? AutoridadApellidoPaterno { get; set; }
    public string? AutoridadApellidoMaterno { get; set; }
    public string? AutoridadNombres { get; set; }
    public string? AutoridadNombreCompleto { get; set; }
    public string? TipoAutoridad { get; set; }
    public string? AutoridadTipoDocumento { get; set; }
    public string? AutoridadNumeroDocumento { get; set; }
    public string? AutoridadCargo { get; set; }
    public string? AutoridadInstitucion { get; set; }
    public string? AutoridadPlacaVehiculo { get; set; }
    public string? AutoridadTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS ADICIONALES
    // ═══════════════════════════════════════════════════════════
    public string? DatosAdicionales { get; set; }
    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FIRMAS
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Firma del responsable (Familiar O Autoridad)
    /// </summary>
    public bool FirmadoResponsable { get; set; }
    public DateTime? FechaFirmaResponsable { get; set; }

    public bool FirmadoAdmisionista { get; set; }
    public DateTime? FechaFirmaAdmisionista { get; set; }

    public bool FirmadoSupervisorVigilancia { get; set; }
    public DateTime? FechaSupervisorVigilancia { get; set; }

    /// <summary>
    /// Nombre del responsable que firmó (calculado desde backend)
    /// </summary>
    public string? NombreResponsableFirma { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ARCHIVOS PDF
    // ═══════════════════════════════════════════════════════════
    public string? RutaPDFSinFirmar { get; set; }
    public string? NombreArchivoPDFSinFirmar { get; set; }
    public string? TamañoPDFSinFirmarLegible { get; set; }

    public string? RutaPDFFirmado { get; set; }
    public string? NombreArchivoPDFFirmado { get; set; }
    public string? TamañoPDFFirmadoLegible { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════
    public bool EstaCompleta { get; set; }
    public bool TieneTodasLasFirmas { get; set; }
    public bool TienePDFFirmado { get; set; }

    // ═══════════════════════════════════════════════════════════
    // OBSERVACIONES
    // ═══════════════════════════════════════════════════════════
    public string? Observaciones { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUDITORÍA
    // ═══════════════════════════════════════════════════════════
    public string UsuarioAdmisionNombre { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public string? UsuarioSubidaPDFNombre { get; set; }
    public DateTime? FechaSubidaPDF { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RELACIÓN CON SALIDA
    // ═══════════════════════════════════════════════════════════
    public int? SalidaMortuorioID { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
}