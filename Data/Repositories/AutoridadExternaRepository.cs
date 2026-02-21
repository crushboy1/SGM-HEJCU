using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio de autoridades externas.
    /// </summary>
    public class AutoridadExternaRepository : IAutoridadExternaRepository
    {
        private readonly ApplicationDbContext _context;

        public AutoridadExternaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AutoridadExterna> CreateAsync(AutoridadExterna autoridad)
        {
            autoridad.FechaRegistro = DateTime.Now;
            _context.AutoridadesExternas.Add(autoridad);
            await _context.SaveChangesAsync();

            // Recargar con relaciones
            var autoridadCreada = await _context.AutoridadesExternas
                .Include(a => a.ExpedienteLegal)
                .Include(a => a.UsuarioRegistro)
                .FirstOrDefaultAsync(a => a.AutoridadID == autoridad.AutoridadID);

            return autoridadCreada ?? autoridad;
        }

        public async Task<AutoridadExterna?> GetByIdAsync(int autoridadId)
        {
            return await _context.AutoridadesExternas
                .Include(a => a.ExpedienteLegal)
                .Include(a => a.UsuarioRegistro)
                .FirstOrDefaultAsync(a => a.AutoridadID == autoridadId);
        }

        public async Task<List<AutoridadExterna>> GetByExpedienteLegalIdAsync(int expedienteLegalId)
        {
            return await _context.AutoridadesExternas
                .Include(a => a.UsuarioRegistro)
                .Where(a => a.ExpedienteLegalID == expedienteLegalId)
                .OrderBy(a => a.FechaRegistro)
                .ToListAsync();
        }

        public async Task DeleteAsync(int autoridadId)
        {
            var autoridad = await _context.AutoridadesExternas
                .FindAsync(autoridadId);

            if (autoridad != null)
            {
                _context.AutoridadesExternas.Remove(autoridad);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByDocumentoAsync(string numeroDocumento, int expedienteLegalId)
        {
            return await _context.AutoridadesExternas
                .AnyAsync(a => a.NumeroDocumento == numeroDocumento &&
                              a.ExpedienteLegalID == expedienteLegalId);
        }
    }
}