using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs
{
    public class UpdateExpedienteDTO
    {
        public string? NumeroCama { get; set; }

        [StringLength(500)]
        public string? CausaMuerte { get; set; }

        public string? MedicoRNE { get; set; }

        public string? NumeroCertificadoSINADEF { get; set; }

        public string? Observaciones { get; set; }
    }
}