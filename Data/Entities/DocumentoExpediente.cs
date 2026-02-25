using System.ComponentModel.DataAnnotations;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Entities;

/// <summary>
/// Documento digitalizado adjunto al expediente mortuorio.
/// Reemplaza los "juegos de copias físicas" del proceso manual.
/// 
/// FLUJO:
/// 1. Admisionista sube el archivo escaneado (Estado: PendienteVerificacion)
/// 2. Admisionista verifica físicamente contra el original presentado
/// 3. Admisionista marca como verificado (Estado: Verificado)
/// 4. Sistema calcula Expediente.DocumentacionCompleta automáticamente
/// 
/// DOCUMENTOS REQUERIDOS según TipoSalida:
/// - Familiar → DNI_Familiar + DNI_Fallecido + CertificadoDefuncion
/// - AutoridadLegal → OficioLegal
/// </summary>
public class DocumentoExpediente
{
    /// <summary>
    /// Identificador único del documento
    /// </summary>
    [Key]
    public int DocumentoExpedienteID { get; set; }

    // ===================================================================
    // RELACIÓN CON EXPEDIENTE
    // ===================================================================

    /// <summary>
    /// ID del expediente al que pertenece este documento
    /// </summary>
    [Required]
    public int ExpedienteID { get; set; }

    /// <summary>
    /// Navegación al expediente
    /// </summary>
    public virtual Expediente Expediente { get; set; } = null!;

    // ===================================================================
    // CLASIFICACIÓN DEL DOCUMENTO
    // ===================================================================

    /// <summary>
    /// Tipo de documento según catálogo
    /// Determina si es obligatorio según TipoSalida del ActaRetiro
    /// </summary>
    [Required]
    public TipoDocumentoExpediente TipoDocumento { get; set; }

    /// <summary>
    /// Estado de verificación del documento
    /// PendienteVerificacion → Verificado → (o) Rechazado
    /// </summary>
    [Required]
    public EstadoDocumentoExpediente Estado { get; set; } = EstadoDocumentoExpediente.PendienteVerificacion;

    // ===================================================================
    // ARCHIVO
    // ===================================================================

    /// <summary>
    /// Ruta relativa del archivo en el servidor
    /// Formato: "documentos-expedientes/2025/01/SGM-2025-00001_DNI_Familiar_20250115.pdf"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string RutaArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre original del archivo subido por el usuario
    /// Ejemplo: "dni_juan_perez.jpg"
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Extensión del archivo en minúsculas
    /// Valores permitidos: .pdf, .jpg, .jpeg, .png
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string ExtensionArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes
    /// Máximo permitido: 5 MB (5,242,880 bytes)
    /// </summary>
    [Required]
    public long TamañoBytes { get; set; }

    // ===================================================================
    // AUDITORÍA - SUBIDA
    // ===================================================================

    /// <summary>
    /// ID del usuario que subió el documento
    /// Típicamente el Admisionista
    /// </summary>
    [Required]
    public int UsuarioSubioID { get; set; }

    /// <summary>
    /// Navegación al usuario que subió el documento
    /// </summary>
    public virtual Usuario UsuarioSubio { get; set; } = null!;

    /// <summary>
    /// Fecha y hora en que se subió el documento
    /// </summary>
    [Required]
    public DateTime FechaHoraSubida { get; set; } = DateTime.Now;

    // ===================================================================
    // AUDITORÍA - VERIFICACIÓN
    // ===================================================================

    /// <summary>
    /// ID del usuario que verificó el documento contra el original físico
    /// Típicamente el mismo Admisionista que lo subió
    /// Solo tiene valor cuando Estado == Verificado o Rechazado
    /// </summary>
    public int? UsuarioVerificoID { get; set; }

    /// <summary>
    /// Navegación al usuario que verificó el documento
    /// </summary>
    public virtual Usuario? UsuarioVerifico { get; set; }

    /// <summary>
    /// Fecha y hora en que se verificó o rechazó el documento
    /// </summary>
    public DateTime? FechaHoraVerificacion { get; set; }

    // ===================================================================
    // OBSERVACIONES
    // ===================================================================

    /// <summary>
    /// Observaciones del Admisionista al subir o verificar el documento
    /// Ejemplos:
    /// - "DNI con fecha de vencimiento próxima, se aceptó"
    /// - "Imagen legible, coincide con original presentado"
    /// - "RECHAZADO: archivo borroso, solicitar nuevo escaneo"
    /// </summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    // ===================================================================
    // MÉTODOS DE LÓGICA DE NEGOCIO
    // ===================================================================

    /// <summary>
    /// Obtiene el tamaño del archivo en formato legible
    /// Ejemplo: "1.23 MB", "450 KB"
    /// </summary>
    public string ObtenerTamañoLegible()
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = TamañoBytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Verifica si el documento está verificado y válido
    /// </summary>
    public bool EsValido() => Estado == EstadoDocumentoExpediente.Verificado;

    /// <summary>
    /// Marca el documento como verificado por el admisionista
    /// </summary>
    /// <param name="usuarioID">ID del usuario que verifica</param>
    /// <param name="observaciones">Observaciones opcionales de la verificación</param>
    public void MarcarVerificado(int usuarioID, string? observaciones = null)
    {
        Estado = EstadoDocumentoExpediente.Verificado;
        UsuarioVerificoID = usuarioID;
        FechaHoraVerificacion = DateTime.Now;
        Observaciones = observaciones ?? Observaciones;
    }

    /// <summary>
    /// Marca el documento como rechazado indicando el motivo
    /// El familiar deberá presentar nuevamente el documento
    /// </summary>
    /// <param name="usuarioID">ID del usuario que rechaza</param>
    /// <param name="motivo">Motivo del rechazo (obligatorio para trazabilidad)</param>
    public void MarcarRechazado(int usuarioID, string motivo)
    {
        Estado = EstadoDocumentoExpediente.Rechazado;
        UsuarioVerificoID = usuarioID;
        FechaHoraVerificacion = DateTime.Now;
        Observaciones = $"RECHAZADO: {motivo}";
    }
}