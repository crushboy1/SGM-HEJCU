namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO de respuesta que representa un registro de salida completado.
/// Incluye información del expediente, responsable y métricas.
/// </summary>
public class SalidaDTO
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICADORES Y REFERENCIAS
    // ═══════════════════════════════════════════════════════════

    public int SalidaID { get; set; }
    public int ExpedienteID { get; set; }
    public string CodigoExpediente { get; set; } = string.Empty;
    public string NombrePaciente { get; set; } = string.Empty;

    /// <summary>
    /// ID del Acta de Retiro (solo si TipoSalida = Familiar)
    /// </summary>
    public int? ActaRetiroID { get; set; }

    /// <summary>
    /// ID del Expediente Legal (solo si TipoSalida = AutoridadLegal)
    /// </summary>
    public int? ExpedienteLegalID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO Y FECHA
    // ═══════════════════════════════════════════════════════════

    public DateTime FechaHoraSalida { get; set; }

    /// <summary>
    /// Tipo de salida: "Familiar", "AutoridadLegal", "TrasladoHospital", "Otro"
    /// </summary>
    public string TipoSalida { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE QUE RETIRA
    // ═══════════════════════════════════════════════════════════

    public string ResponsableNombre { get; set; } = string.Empty;

    /// <summary>
    /// Documento concatenado para mostrar: "DNI 12345678"
    /// </summary>
    public string ResponsableDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Parentesco (solo casos internos)
    /// </summary>
    public string? ResponsableParentesco { get; set; }

    /// <summary>
    /// Teléfono del responsable
    /// </summary>
    public string? ResponsableTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIZACIÓN (CASOS EXTERNOS)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Número de oficio policial (solo casos externos)
    /// Ejemplo: "OFICIO N° 1262-2025-REG.POL-LIMA/..."
    /// </summary>
    public string? NumeroOficio { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FUNERARIA (CASOS INTERNOS)
    // ═══════════════════════════════════════════════════════════

    public string? NombreFuneraria { get; set; }
    public string? FunerariaRUC { get; set; }
    public string? FunerariaTelefono { get; set; }
    public string? ConductorFuneraria { get; set; }
    public string? DNIConductor { get; set; }
    public string? AyudanteFuneraria { get; set; }
    public string? DNIAyudante { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VEHÍCULO Y DESTINO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Placa del vehículo (funeraria o patrullero)
    /// </summary>
    public string? PlacaVehiculo { get; set; }

    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VIGILANTE Y OBSERVACIONES
    // ═══════════════════════════════════════════════════════════

    public string VigilanteNombre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    // ═══════════════════════════════════════════════════════════
    // INCIDENTES
    // ═══════════════════════════════════════════════════════════

    public bool IncidenteRegistrado { get; set; }
    public string? DetalleIncidente { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MÉTRICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tiempo total de permanencia en el mortuorio
    /// </summary>
    public TimeSpan? TiempoPermanencia { get; set; }

    /// <summary>
    /// Tiempo de permanencia en formato legible
    /// Ejemplo: "2 días 5 horas", "18 horas"
    /// </summary>
    public string? TiempoPermanenciaLegible { get; set; }

    /// <summary>
    /// Indica si excedió las 48 horas de permanencia
    /// </summary>
    public bool ExcedioLimite { get; set; }
}