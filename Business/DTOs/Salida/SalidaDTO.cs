namespace SisMortuorio.Business.DTOs.Salida;

/// <summary>
/// DTO de respuesta que representa un registro de salida completado.
/// Los datos del responsable se mapean desde ActaRetiro en el service.
/// No duplica información — es solo para presentación en frontend.
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
    /// ID del Acta de Retiro. Siempre presente — maneja ambos tipos de salida.
    /// </summary>
    public int ActaRetiroID { get; set; }
    /// <summary>
    /// ID del Expediente Legal Digital. Opcional.
    /// Referencia al archivador digital de Vigilancia.
    /// </summary>
    public int? ExpedienteLegalID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TIPO Y FECHA
    // ═══════════════════════════════════════════════════════════
    public DateTime FechaHoraSalida { get; set; }

    /// <summary>
    /// Tipo de salida mapeado desde ActaRetiro.
    /// Valores: "Familiar", "AutoridadLegal"
    /// </summary>
    public string TipoSalida { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // RESPONSABLE — mapeado desde ActaRetiro (solo lectura)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Nombre completo del responsable, mapeado desde ActaRetiro.
    /// Formato: "APELLIDO PATERNO APELLIDO MATERNO, Nombres"
    /// </summary>
    public string ResponsableNombre { get; set; } = string.Empty;

    /// <summary>
    /// Documento concatenado para mostrar. Ejemplo: "DNI 12345678"
    /// Mapeado desde ActaRetiro.
    /// </summary>
    public string ResponsableDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Parentesco con el fallecido. Solo si TipoSalida = Familiar.
    /// Mapeado desde ActaRetiro.
    /// </summary>
    public string? ResponsableParentesco { get; set; }

    /// <summary>
    /// Teléfono del responsable. Mapeado desde ActaRetiro.
    /// </summary>
    public string? ResponsableTelefono { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUTORIDAD LEGAL — mapeado desde ActaRetiro (solo lectura)
    // Solo presente si TipoSalida = AutoridadLegal
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Número de oficio legal. Mapeado desde ActaRetiro.
    /// Ejemplo: "OF-2025-001234"
    /// </summary>
    public string? NumeroOficio { get; set; }

    /// <summary>
    /// Tipo de autoridad. Mapeado desde ActaRetiro.
    /// Ejemplo: "Policía Nacional del Perú (PNP)"
    /// </summary>
    public string? TipoAutoridad { get; set; }

    /// <summary>
    /// Institución de la autoridad. Mapeado desde ActaRetiro.
    /// </summary>
    public string? AutoridadInstitucion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // FUNERARIA — capturado por Vigilante
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
    /// Placa del vehículo.
    /// - Familiar: vehículo funerario
    /// - AutoridadLegal: patrullero o vehículo oficial
    /// </summary>
    public string? PlacaVehiculo { get; set; }

    public string? Destino { get; set; }

    // ═══════════════════════════════════════════════════════════
    // VIGILANTE Y OBSERVACIONES
    // ═══════════════════════════════════════════════════════════
    public int RegistradoPorID { get; set; }

    public string RegistradoPorNombre { get; set; } = string.Empty;
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
    /// Tiempo total de permanencia en el mortuorio expresado en minutos.
    /// Guardado como int para evitar overflow del tipo TIME de SQL Server.
    /// El frontend formatea: días/horas/minutos según convenga.
    /// </summary>
    public int? TiempoPermanenciaMinutos { get; set; }

    /// <summary>
    /// Tiempo de permanencia en formato legible generado por el backend.
    /// Ejemplos: "53d 7h 23m", "1d 8h 42m", "18h 20m"
    /// </summary>
    public string? TiempoPermanenciaLegible { get; set; }

    /// <summary>
    /// Indica si el cuerpo excedió las 48 horas de permanencia.
    /// </summary>
    public bool ExcedioLimite { get; set; }
}