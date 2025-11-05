using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    public class CustodiaRepository : ICustodiaRepository
    {
        private readonly ApplicationDbContext _context;

        public CustodiaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustodiaTransferencia> CreateAsync(CustodiaTransferencia transferencia)
        {
            _context.CustodiaTransferencias.Add(transferencia);
            await _context.SaveChangesAsync();

            var transferenciaCreada = await _context.CustodiaTransferencias
                .Include(ct => ct.Expediente)
                .Include(ct => ct.UsuarioOrigen)
                    .ThenInclude(u => u.Rol)
                .Include(ct => ct.UsuarioDestino)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(ct => ct.TransferenciaID == transferencia.TransferenciaID);

            return transferenciaCreada ?? transferencia;
        }

        public async Task<List<CustodiaTransferencia>> GetHistorialByExpedienteAsync(int expedienteId)
        {
            return await _context.CustodiaTransferencias
                .Include(ct => ct.UsuarioOrigen)
                    .ThenInclude(u => u.Rol)
                .Include(ct => ct.UsuarioDestino)
                    .ThenInclude(u => u.Rol)
                .Where(ct => ct.ExpedienteID == expedienteId)
                .OrderBy(ct => ct.FechaHoraTransferencia)
                .ToListAsync();
        }

        public async Task<CustodiaTransferencia?> GetUltimaTransferenciaAsync(int expedienteId)
        {
            return await _context.CustodiaTransferencias
                .Include(ct => ct.UsuarioOrigen)
                    .ThenInclude(u => u.Rol) 
                .Include(ct => ct.UsuarioDestino)
                    .ThenInclude(u => u.Rol) 
                .Where(ct => ct.ExpedienteID == expedienteId)
                .OrderByDescending(ct => ct.FechaHoraTransferencia)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExisteTransferenciaRecienteAsync(int expedienteId, int usuarioDestinoId, int minutosMargen = 5)
        {
            var fechaLimite = DateTime.Now.AddMinutes(-minutosMargen);

            return await _context.CustodiaTransferencias
                .AnyAsync(ct =>
                    ct.ExpedienteID == expedienteId &&
                    ct.UsuarioDestinoID == usuarioDestinoId &&
                    ct.FechaHoraTransferencia >= fechaLimite);
        }
    }
}