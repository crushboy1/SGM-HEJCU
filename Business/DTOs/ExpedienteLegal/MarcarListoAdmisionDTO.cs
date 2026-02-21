using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para marcar un expediente legal como listo para validación de Admisión.
    /// Usado por Vigilancia cuando ya registró todas las autoridades y documentos.
    /// </summary>
    public class MarcarListoAdmisionDTO
    {
        [Required(ErrorMessage = "El ID del expediente legal es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente legal no es válido")]
        public int ExpedienteLegalID { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }
    }
}