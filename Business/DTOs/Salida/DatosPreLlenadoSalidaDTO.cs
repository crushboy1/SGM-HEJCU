using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO de respuesta con datos pre-llenados para el formulario de Registro de Salida.
/// El Vigilante recibe este DTO al buscar un expediente — los datos del responsable
/// vienen solo lectura desde ActaRetiro. Solo los datos de funeraria son editables.
/// </summary>
public class DatosPreLlenadoSalidaDTO
{
    // ═══════════════════════════════════════════════════════════
    // EXPEDIENTE
    // ═══════════════════════════════════════════════════════════
    public int ExpedienteID { get; set; }
    public string CodigoExpediente { get; set; } = string.Empty;
    public string NombrePaciente { get; set; } = string.Empty;
    public string HC { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;

    /// <summary>
    /// Bandeja asignada actualmente al expediente.
    /// Ejemplo: "B-03"
    /// </summary>
    public string? BandejaAsignada { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ACTA DE RETIRO
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// ID del Acta de Retiro. Siempre presente — maneja ambos tipos de salida.
    /// </summary>
    public int ActaRetiroID { get; set; }

    /// <summary>
    /// Indica si el acta tiene PDF firmado cargado.
    /// </summary>
    public bool TieneActaFirmada { get; set; }

    /// <summary>
    /// Indica si el expediente está listo para que el Vigilante registre la salida.
    /// Requiere: acta firmada + expediente en estado PendienteRetiro.
    /// </summary>
    public bool ActaListaParaSalida => TieneActaFirmada && PuedeRegistrarSalida;

    // ═══════════════════════════════════════════════════════════
    // TIPO DE SALIDA — leído desde ActaRetiro (solo lectura)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Tipo de salida mapeado desde ActaRetiro.
    /// Determina qué secciones muestra el formulario del Vigilante.
    /// </summary>
    public TipoSalida TipoSalida { get; set; }

    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE — mapeado desde ActaRetiro (solo lectura en frontend)
    // ═══════════════════════════════════════════════════════════
    public string? ResponsableApellidoPaterno { get; set; }
    public string? ResponsableApellidoMaterno { get; set; }
    public string? ResponsableNombres { get; set; }

    /// <summary>
    /// Nombre completo concatenado para mostrar en frontend (readonly).
    /// Formato: "APELLIDO PATERNO APELLIDO MATERNO, Nombres"
    /// </summary>
    public string ResponsableNombreCompleto =>
        !string.IsNullOrWhiteSpace(ResponsableApellidoPaterno)
            ? $"{ResponsableApellidoPaterno} {ResponsableApellidoMaterno}, {ResponsableNombres}".Trim()
            : string.Empty;

    public string? ResponsableTipoDocumento { get; set; }
    public string? ResponsableNumeroDocumento { get; set; }

    /// <summary>
    /// Parentesco con el fallecido. Solo si TipoSalida = Familiar.
    /// </summary>
    public string? ResponsableParentesco { get; set; }

    public string? ResponsableTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIDAD LEGAL — solo si TipoSalida = AutoridadLegal (solo lectura)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Tipo de autoridad. Ejemplo: "Policía Nacional del Perú (PNP)"
    /// </summary>
    public string? TipoAutoridad { get; set; }

    public string? AutoridadInstitucion { get; set; }
    public string? AutoridadCargo { get; set; }

    /// <summary>
    /// Número de oficio legal. Ejemplo: "OF-2025-001234"
    /// </summary>
    public string? NumeroOficioLegal { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FUNERARIA — campos editables por el Vigilante
    // Vienen null desde el backend, el Vigilante los completa
    // ═══════════════════════════════════════════════════════════
    public string? NombreFuneraria { get; set; }
    public string? FunerariaRUC { get; set; }
    public string? FunerariaTelefono { get; set; }
    public string? ConductorFuneraria { get; set; }
    public string? DNIConductor { get; set; }
    public string? AyudanteFuneraria { get; set; }
    public string? DNIAyudante { get; set; }
    public string? PlacaVehiculo { get; set; }
    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VALIDACIONES — estado de deudas y bloqueos
    // ═══════════════════════════════════════════════════════════
    public bool DeudaSangreOK { get; set; }
    public string? DeudaSangreMensaje { get; set; }

    public bool DeudaEconomicaOK { get; set; }
    public string? DeudaEconomicaMensaje { get; set; }

    /// <summary>
    /// Indica si el expediente puede proceder a salida física.
    /// False si: deudas pendientes, acta sin firmar o estado incorrecto.
    /// </summary>
    public bool PuedeRegistrarSalida { get; set; }

    /// <summary>
    /// Mensaje explicativo si PuedeRegistrarSalida = false.
    /// </summary>
    public string? MensajeBloqueo { get; set; }
}