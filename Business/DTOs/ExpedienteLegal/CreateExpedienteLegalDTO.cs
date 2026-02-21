using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para crear un nuevo expediente legal.
    /// Usado por Admisión cuando el caso requiere intervención policial/fiscal.
    /// </summary>
    public class CreateExpedienteLegalDTO
    {
        [Required(ErrorMessage = "El ID del expediente es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente no es válido")]
        public int ExpedienteID { get; set; }

        [MaxLength(100, ErrorMessage = "El número de acta policial no puede exceder los 100 caracteres")]
        public string? NumeroActaPolicial { get; set; }

        [MaxLength(100, ErrorMessage = "El número de oficio de fiscalía no puede exceder los 100 caracteres")]
        public string? NumeroOficioFiscalia { get; set; }

        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "El ID del usuario que registra es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario no es válido")]
        public int UsuarioRegistroID { get; set; }
    }
}