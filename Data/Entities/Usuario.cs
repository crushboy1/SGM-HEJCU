using Microsoft.AspNetCore.Identity;

namespace SisMortuorio.Data.Entities
{
    public class Usuario : IdentityUser<int>
    {
        public TipoDocumentoIdentidad TipoDocumento { get; set; } = TipoDocumentoIdentidad.DNI;
        public string NumeroDocumento { get; set; } = string.Empty;  // ← Era "DNI"
        public string NombreCompleto { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? UltimoAcceso { get; set; }

        // Relaciones
        public int RolID { get; set; }
        public Rol Rol { get; set; } = null!;
    }
}