using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO para registrar una nueva autoridad externa.
    /// Usado por Admisión o Vigilancia cuando llega policía/fiscal/legista.
    /// </summary>
    public class CreateAutoridadExternaDTO
    {
        [Required(ErrorMessage = "El ID del expediente legal es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del expediente legal no es válido")]
        public int ExpedienteLegalID { get; set; }

        [Required(ErrorMessage = "El ID del usuario que registra es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario no es válido")]
        public int UsuarioRegistroID { get; set; }

        [Required(ErrorMessage = "El tipo de autoridad es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo de autoridad no puede exceder los 50 caracteres")]
        public string TipoAutoridad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        [MaxLength(100, ErrorMessage = "El apellido paterno no puede exceder los 100 caracteres")]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido materno es obligatorio")]
        [MaxLength(100, ErrorMessage = "El apellido materno no puede exceder los 100 caracteres")]
        public string ApellidoMaterno { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [MaxLength(100, ErrorMessage = "Los nombres no pueden exceder los 100 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        // Documento de identidad
        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [MaxLength(20, ErrorMessage = "El tipo de documento no puede exceder los 20 caracteres")]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [MaxLength(20, ErrorMessage = "El número de documento no puede exceder los 20 caracteres")]
        public string NumeroDocumento { get; set; } = string.Empty;

        // Datos de institución
        [MaxLength(200, ErrorMessage = "La institución no puede exceder los 200 caracteres")]
        public string? Institucion { get; set; }

        [MaxLength(100, ErrorMessage = "El cargo no puede exceder los 100 caracteres")]
        public string? Cargo { get; set; }

        // Código especial (CMP, Código Fiscal)
        [MaxLength(50, ErrorMessage = "El código especial no puede exceder los 50 caracteres")]
        public string? CodigoEspecial { get; set; }

        // Contacto
        [MaxLength(20, ErrorMessage = "La placa del vehículo no puede exceder los 20 caracteres")]
        public string? PlacaVehiculo { get; set; }

        [MaxLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres")]
        public string? Telefono { get; set; }

    }
}