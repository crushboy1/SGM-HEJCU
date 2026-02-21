using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ActaRetiro;

/// <summary>
/// DTO para subir el PDF firmado del Acta de Retiro
/// </summary>
public class UpdateActaRetiroPDFDTO
{
    /// <summary>
    /// ID del acta a actualizar
    /// </summary>
    [Required]
    public int ActaRetiroID { get; set; }

    /// <summary>
    /// Ruta del PDF firmado escaneado
    /// </summary>
    [Required]
    [StringLength(500)]
    public string RutaPDFFirmado { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del archivo PDF firmado
    /// </summary>
    [Required]
    [StringLength(255)]
    public string NombreArchivoPDFFirmado { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes
    /// </summary>
    [Required]
    public long TamañoPDFFirmado { get; set; }

    /// <summary>
    /// Usuario que sube el PDF firmado
    /// </summary>
    [Required]
    public int UsuarioSubidaPDFID { get; set; }

    /// <summary>
    /// Observaciones opcionales
    /// </summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}