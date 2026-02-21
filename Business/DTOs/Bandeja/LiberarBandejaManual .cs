using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Bandeja
{
    public class LiberarBandejaManualDTO
    {
        [Required] public int BandejaID { get; set; }

        [Required, MinLength(3), MaxLength(100)]
        public string? MotivoLiberacion { get; set; }

        [Required, MinLength(20), MaxLength(500)]
        public string? Observaciones { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int UsuarioLiberaID { get; set; }

    }
}