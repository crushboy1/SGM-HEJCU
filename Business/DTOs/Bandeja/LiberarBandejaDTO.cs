using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO de entrada (Input) para liberar una bandeja.
    /// Usado internamente por el ISalidaMortuorioService.
    /// </summary>
    public class LiberarBandejaDTO
    {
        [Required(ErrorMessage = "El ID de la bandeja es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la bandeja no es válido")]
        public int BandejaID { get; set; }

        [Required(ErrorMessage = "El ID del usuario que libera es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario no es válido")]
        public int UsuarioLiberaID { get; set; }

        [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }
    }
}