using SisMortuorio.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO para registrar la salida de un cuerpo del mortuorio.
/// Soporta tanto casos internos (Familiar) como externos (AutoridadLegal).
/// 
/// FLUJO:
/// - Admisión registra todos los datos (ActaRetiro o ExpedienteLegal)
/// - Vigilante solo confirma retiro físico y agrega observaciones si necesita
/// </summary>
public class RegistrarSalidaDTO
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICADORES
    // ═══════════════════════════════════════════════════════════

    [Required(ErrorMessage = "El ID del expediente es obligatorio")]
    public int ExpedienteID { get; set; }

    /// <summary>
    /// ID del Acta de Retiro (OBLIGATORIO para TipoSalida = Familiar)
    /// Registrado por Admisión
    /// </summary>
    public int? ActaRetiroID { get; set; }

    /// <summary>
    /// ID del Expediente Legal (OBLIGATORIO para TipoSalida = AutoridadLegal)
    /// Registrado por Admisión
    /// </summary>
    public int? ExpedienteLegalID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO Y RESPONSABLE
    // ═══════════════════════════════════════════════════════════

    [Required(ErrorMessage = "El tipo de salida es obligatorio")]
    public TipoSalida TipoSalida { get; set; }

    [Required(ErrorMessage = "El nombre del responsable es obligatorio")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string ResponsableNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [MaxLength(20)]
    public string ResponsableTipoDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [MaxLength(20)]
    public string ResponsableNumeroDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Parentesco (OBLIGATORIO si TipoSalida = Familiar)
    /// Registrado por Admisión en ActaRetiro
    /// </summary>
    [MaxLength(50)]
    public string? ResponsableParentesco { get; set; }

    [MaxLength(20)]
    public string? ResponsableTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIZACIÓN (SOLO CASOS EXTERNOS)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Número de oficio policial (OBLIGATORIO si TipoSalida = AutoridadLegal)
    /// Ejemplo: "OFICIO N° 1262-2025-REG.POL-LIMA/DIVPOL-SUR-1-CSA-DEINPOL-SIAT"
    /// Registrado por Admisión cuando sube el documento PDF
    /// NULL para casos internos
    /// </summary>
    [MaxLength(150)]
    public string? NumeroOficio { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FUNERARIA (SOLO CASOS INTERNOS)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre de la funeraria (OBLIGATORIO si TipoSalida = Familiar)
    /// Registrado por Admisión
    /// NULL para casos externos
    /// </summary>
    [MaxLength(200)]
    public string? NombreFuneraria { get; set; }

    [MaxLength(11)]
    public string? FunerariaRUC { get; set; }

    [MaxLength(20)]
    public string? FunerariaTelefono { get; set; }

    /// <summary>
    /// Conductor de la funeraria (OBLIGATORIO si TipoSalida = Familiar)
    /// Registrado por Admisión
    /// </summary>
    [MaxLength(200)]
    public string? ConductorFuneraria { get; set; }

    /// <summary>
    /// DNI del conductor (OBLIGATORIO si TipoSalida = Familiar)
    /// Registrado por Admisión
    /// </summary>
    [MaxLength(20)]
    public string? DNIConductor { get; set; }

    /// <summary>
    /// Ayudante de la funeraria (opcional)
    /// Registrado por Admisión
    /// </summary>
    [MaxLength(200)]
    public string? AyudanteFuneraria { get; set; }

    /// <summary>
    /// DNI del ayudante (opcional)
    /// Registrado por Admisión
    /// </summary>
    [MaxLength(20)]
    public string? DNIAyudante { get; set; }

    /// <summary>
    /// Placa del vehículo (OBLIGATORIO para ambos tipos)
    /// - Caso Interno: Placa de vehículo funerario
    /// - Caso Externo: Placa de patrullero (desde AutoridadExterna-Policia)
    /// Registrado por Admisión
    /// Vigilante solo confirma visualmente
    /// </summary>
    [MaxLength(20)]
    public string? PlacaVehiculo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DESTINO Y OBSERVACIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Destino final del cuerpo
    /// - Caso Interno: Cementerio/Crematorio
    /// - Caso Externo: "Morgue Central"
    /// Registrado por Admisión
    /// </summary>
    [MaxLength(200)]
    public string? Destino { get; set; }

    /// <summary>
    /// Observaciones adicionales
    /// Puede ser agregado/modificado por Vigilante si detecta inconsistencias
    /// Ejemplo: "Placa no coincide con registro", "Retiro urgente"
    /// </summary>
    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}