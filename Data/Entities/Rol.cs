using Microsoft.AspNetCore.Identity;

namespace SisMortuorio.Data.Entities
{
    public class Rol : IdentityRole<int>
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        // Relaciones
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}