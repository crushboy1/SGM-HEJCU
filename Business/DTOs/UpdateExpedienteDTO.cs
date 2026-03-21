using System.ComponentModel.DataAnnotations;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// Campos editables del expediente después de su creación.
    /// Solo Admisión o roles autorizados pueden usar este DTO.
    /// </summary>
    public class UpdateExpedienteDTO
    {
        [StringLength(20)]
        public string? NumeroCama { get; set; }

        [StringLength(500)]
        public string? DiagnosticoFinal { get; set; }

        [StringLength(200)]
        public string? MedicoCertificaNombre { get; set; }

        [StringLength(10)]
        public string? MedicoCMP { get; set; }

        [StringLength(10)]
        public string? MedicoRNE { get; set; }

        /// <summary>
        /// Solo válido si TipoExpediente = Externo y CausaViolentaODudosa = false.
        /// </summary>
        [StringLength(200)]
        public string? MedicoExternoNombre { get; set; }

        [StringLength(10)]
        public string? MedicoExternoCMP { get; set; }

        public bool? CausaViolentaODudosa { get; set; }

        public FuenteFinanciamiento? FuenteFinanciamiento { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }
    }
}