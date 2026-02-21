using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para subir un nuevo documento legal.
    /// Usado por Admisión o Vigilancia al escanear documentos físicos.
    /// </summary>
    public class CreateDocumentoLegalDTO
    {
        [Required(ErrorMessage = "El ID del expediente legal es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente legal no es válido")]
        public int ExpedienteLegalID { get; set; }

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo de documento no puede exceder los 50 caracteres")]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del archivo es obligatorio")]
        [MaxLength(255, ErrorMessage = "El nombre del archivo no puede exceder los 255 caracteres")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ruta del archivo es obligatoria")]
        [MaxLength(500, ErrorMessage = "La ruta del archivo no puede exceder los 500 caracteres")]
        public string RutaArchivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tamaño del archivo es obligatorio")]
        [Range(1, long.MaxValue, ErrorMessage = "El tamaño del archivo debe ser mayor a 0")]
        public long TamañoArchivo { get; set; }

        [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "El ID del usuario que sube es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario no es válido")]
        public int UsuarioSubeID { get; set; }
    }
}