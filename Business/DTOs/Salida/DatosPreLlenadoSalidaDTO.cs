using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO con datos pre-llenados desde el Acta de Retiro
/// Usado para simplificar el formulario de Registro de Salida
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

    // ═══════════════════════════════════════════════════════════
    // ACTA DE RETIRO (si existe)
    // ═══════════════════════════════════════════════════════════
    public int? ActaRetiroID { get; set; }
    public bool TieneActaFirmada { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO DE SALIDA (desde Acta)
    // ═══════════════════════════════════════════════════════════
    public TipoSalida TipoSalida { get; set; }
    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE (desde Acta)
    // ═══════════════════════════════════════════════════════════
    public string? ResponsableNombre { get; set; }
    public string? ResponsableTipoDocumento { get; set; }
    public string? ResponsableNumeroDocumento { get; set; }
    public string? ResponsableParentesco { get; set; }
    public string? ResponsableTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FUNERARIA (NULL - Vigilante captura)
    // ═══════════════════════════════════════════════════════════
    public string? NombreFuneraria { get; set; }
    public string? FunerariaRUC { get; set; }
    public string? FunerariaTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CAMPOS EDITABLES POR VIGILANTE
    // ═══════════════════════════════════════════════════════════
    public string? ConductorFuneraria { get; set; }
    public string? DNIConductor { get; set; } 
    public string? AyudanteFuneraria { get; set; }
    public string? DNIAyudante { get; set; } 
    public string? PlacaVehiculo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIZACIÓN LEGAL (si AutoridadLegal)
    // ═══════════════════════════════════════════════════════════
    public string? NumeroOficio { get; set; } 

    // ═══════════════════════════════════════════════════════════
    // VALIDACIONES AUTOMÁTICAS (DEUDAS)
    // ═══════════════════════════════════════════════════════════
    public bool DeudaSangreOK { get; set; }
    public string? DeudaSangreMensaje { get; set; } 

    public bool DeudaEconomicaOK { get; set; } 
    public string? DeudaEconomicaMensaje { get; set; }

    public bool PagosOK { get; set; }
    public bool PuedeRegistrarSalida { get; set; }
    public string? MensajeBloqueo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DESTINO
    // ═══════════════════════════════════════════════════════════
    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // LEGACY (mantener por compatibilidad)
    // ═══════════════════════════════════════════════════════════

    public bool DocumentosOK { get; set; }
}