using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para validar documentación por Admisión.
    /// Aprueba o rechaza la completitud de los documentos.
    /// </summary>
    public class ValidarDocumentacionAdmisionDTO
    {
        [Required(ErrorMessage = "El ID del expediente legal es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente legal no es válido")]
        public int ExpedienteLegalID { get; set; }

        [Required(ErrorMessage = "El ID del usuario de Admisión es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario de Admisión no es válido")]
        public int UsuarioAdmisionID { get; set; }

        [Required(ErrorMessage = "Debe indicar si la documentación está aprobada")]
        public bool Aprobado { get; set; }

        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? Observaciones { get; set; }
    }
}