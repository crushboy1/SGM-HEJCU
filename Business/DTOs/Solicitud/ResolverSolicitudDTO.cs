using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.Solicitud
{
    /// <summary>
    /// DTO de entrada (Input) para que Enfermería resuelva
    /// una solicitud de corrección pendiente.
    /// </summary>
    public class ResolverSolicitudDTO
    {
        // El SolicitudID vendrá de la ruta del endpoint (ej. /api/solicitudes/123/resolver)
        // El UsuarioID (Enfermera) vendrá del Token (Claim)

        [Required(ErrorMessage = "La descripción de la resolución es obligatoria")]
        [MaxLength(1000, ErrorMessage = "La descripción no puede exceder los 1000 caracteres")]
        public string DescripcionResolucion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar si el brazalete fue reimpreso")]
        public bool BrazaleteReimpreso { get; set; } = false;

        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder los 1000 caracteres")]
        public string? ObservacionesResolucion { get; set; }
    }
}