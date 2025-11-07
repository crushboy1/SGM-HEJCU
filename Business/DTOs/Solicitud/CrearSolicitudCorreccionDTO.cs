using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Solicitud
{
    /// <summary>
    /// DTO de uso interno para crear una solicitud de corrección.
    /// Es instanciado por el IVerificacionService cuando una verificación es rechazada.
    /// </summary>
    public class CrearSolicitudCorreccionDTO
    {
        [Required]
        public int ExpedienteID { get; set; }

        [Required]
        public int UsuarioSolicitaID { get; set; } // Vigilante

        [Required]
        public int UsuarioResponsableID { get; set; } // Enfermera creadora del expediente

        [Required]
        public string DatosIncorrectos { get; set; } = string.Empty; // JSON

        [Required]
        public string DescripcionProblema { get; set; } = string.Empty;

        public string? ObservacionesSolicitud { get; set; }
    }
}