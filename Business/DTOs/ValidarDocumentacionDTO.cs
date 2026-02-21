using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO para validar documentación completa por Admisión.
    /// Usado cuando Admisión verifica los 3 juegos de copias.
    /// </summary>
    public class ValidarDocumentacionDTO
    {
        [Required(ErrorMessage = "El ID del expediente es obligatorio")]
        public int ExpedienteID { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}