using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para actualizar observaciones del expediente legal.
    /// </summary>
    public class ActualizarObservacionesDTO
    {
        [Required(ErrorMessage = "Las observaciones son obligatorias")]
        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres")]
        public string Observaciones { get; set; } = string.Empty;
    }
}