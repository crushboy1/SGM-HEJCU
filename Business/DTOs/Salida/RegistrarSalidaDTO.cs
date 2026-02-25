using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO de entrada para registrar la salida física de un cuerpo del mortuorio.
///
/// DECISIÓN ARQUITECTÓNICA v2.0:
/// ActaRetiroID eliminado del DTO. El backend resuelve el ActaRetiro
/// desde ExpedienteID (relación 1-1 garantizada). El frontend no decide
/// qué acta usar — evita ID=0, inconsistencias y violaciones de dominio.
public class RegistrarSalidaDTO
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICADORES
    // ═══════════════════════════════════════════════════════════



    /// <summary>ID del expediente que está siendo retirado.</summary>
    [Required(ErrorMessage = "El ID del expediente es obligatorio")]
    public int ExpedienteID { get; set; }

    /// <summary>
    /// ID del Expediente Legal Digital. OPCIONAL.
    /// Referencia al archivador digital de Vigilancia.
    /// No tiene relación con el flujo de validación del acta.
    /// </summary>
    public int? ExpedienteLegalID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // DATOS DE LA FUNERARIA
    // Capturados por el Vigilante al momento del retiro físico.
    // Obligatorios si TipoSalida = Familiar (leído desde ActaRetiro).
    // Para AutoridadLegal estos campos son opcionales.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Nombre de la funeraria que retira el cuerpo.
    /// Obligatorio si TipoSalida = Familiar.
    /// </summary>
    [MaxLength(200, ErrorMessage = "El nombre de la funeraria no puede exceder 200 caracteres")]
    public string? NombreFuneraria { get; set; }

    /// <summary>
    /// RUC de la funeraria. Opcional. Formato: 11 dígitos numéricos.
    /// </summary>
    [MaxLength(11, ErrorMessage = "El RUC no puede exceder 11 caracteres")]
    public string? FunerariaRUC { get; set; }

    /// <summary>
    /// Teléfono de contacto de la funeraria. Opcional.
    /// </summary>
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? FunerariaTelefono { get; set; }

    /// <summary>
    /// Nombre completo del conductor que retira el cuerpo.
    /// Obligatorio si TipoSalida = Familiar.
    /// </summary>
    [MaxLength(200, ErrorMessage = "El nombre del conductor no puede exceder 200 caracteres")]
    public string? ConductorFuneraria { get; set; }

    /// <summary>
    /// DNI del conductor. Obligatorio si TipoSalida = Familiar.
    /// </summary>
    [MaxLength(20, ErrorMessage = "El DNI del conductor no puede exceder 20 caracteres")]
    public string? DNIConductor { get; set; }

    /// <summary>
    /// Nombre completo del ayudante de la funeraria. Opcional.
    /// </summary>
    [MaxLength(200, ErrorMessage = "El nombre del ayudante no puede exceder 200 caracteres")]
    public string? AyudanteFuneraria { get; set; }

    /// <summary>
    /// DNI del ayudante. Opcional.
    /// </summary>
    [MaxLength(20, ErrorMessage = "El DNI del ayudante no puede exceder 20 caracteres")]
    public string? DNIAyudante { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VEHÍCULO Y DESTINO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Placa del vehículo. Obligatorio para ambos tipos de salida.
    /// - Familiar: placa del vehículo funerario
    /// - AutoridadLegal: placa del patrullero o vehículo oficial
    /// </summary>
    [MaxLength(20, ErrorMessage = "La placa no puede exceder 20 caracteres")]
    public string? PlacaVehiculo { get; set; }

    /// <summary>
    /// Destino final del cuerpo. Opcional.
    /// Ejemplos: "Cementerio El Ángel", "Crematorio", "Morgue Central"
    /// </summary>
    [MaxLength(200, ErrorMessage = "El destino no puede exceder 200 caracteres")]
    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // OBSERVACIONES E INCIDENTES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Observaciones adicionales registradas por el Vigilante.
    /// Ejemplos: "Placa no coincide con registro", "Retiro urgente autorizado"
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres")]
    public string? Observaciones { get; set; }
}