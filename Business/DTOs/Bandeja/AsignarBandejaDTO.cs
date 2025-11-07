using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO de entrada (Input) para asignar un expediente a una bandeja.
    /// </summary>
    public class AsignarBandejaDTO
    {
        [Required(ErrorMessage = "El ID de la bandeja es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la bandeja no es válido")]
        public int BandejaID { get; set; }

        [Required(ErrorMessage = "El ID del expediente es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente no es válido")]
        public int ExpedienteID { get; set; }

        // El ID del usuario que asigna (Téc. Ambulancia) se tomará del Token (Claim)
        // No es necesario enviarlo en el body.

        [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }
    }
}