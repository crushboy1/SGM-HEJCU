using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para validar un expediente legal.
    /// Usado por Jefe de Guardia para aprobar/rechazar documentación.
    /// </summary>
    public class ValidarExpedienteLegalDTO
    {
        [Required(ErrorMessage = "El ID del expediente legal es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente legal no es válido")]
        public int ExpedienteLegalID { get; set; }

        [Required(ErrorMessage = "El ID del Jefe de Guardia es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del Jefe de Guardia no es válido")]
        public int JefeGuardiaID { get; set; }

        [Required(ErrorMessage = "Debe indicar si el expediente está validado")]
        public bool Validado { get; set; }

        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? ObservacionesValidacion { get; set; }
    }
}