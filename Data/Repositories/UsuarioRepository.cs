using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public UsuarioRepository(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario?> GetByNumeroDocumentoAsync(string numeroDocumento)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.NumeroDocumento == numeroDocumento);
        }

        public async Task<List<Usuario>> GetAllActivosAsync()
        {
            return await _context.Users
                .Include(u => u.Rol)
                .Where(u => u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();
        }

        public async Task<List<Usuario>> GetByRolAsync(string nombreRol)
        {
            // Obtener el rol por nombre
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Name == nombreRol);

            if (rol == null)
                return new List<Usuario>();

            return await _context.Users
                .Include(u => u.Rol)
                .Where(u => u.RolID == rol.Id && u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();
        }
    }
}