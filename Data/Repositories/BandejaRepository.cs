using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio para Bandejas.
    /// </summary>
    public class BandejaRepository : IBandejaRepository
    {
        private readonly ApplicationDbContext _context;

        public BandejaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Bandeja>> GetAllAsync()
        {
            return await _context.Bandejas
                .Include(b => b.Expediente)
                .Include(b => b.UsuarioAsigna)
                .Where(b => !b.Eliminado)
                .OrderBy(b => b.Codigo)
                .ToListAsync();
        }

        public async Task<Bandeja?> GetByIdAsync(int id)
        {
            return await _context.Bandejas
                .Include(b => b.Expediente)
                .Include(b => b.UsuarioAsigna)
                .Include(b => b.UsuarioLibera)
                .FirstOrDefaultAsync(b => b.BandejaID == id && !b.Eliminado);
        }

        public async Task<Bandeja?> GetByCodigoAsync(string codigo)
        {
            return await _context.Bandejas
                .Include(b => b.Expediente)
                .Include(b => b.UsuarioAsigna)
                .FirstOrDefaultAsync(b => b.Codigo == codigo && !b.Eliminado);
        }

        public async Task<List<Bandeja>> GetDisponiblesAsync()
        {
            return await _context.Bandejas
                .Where(b => b.Estado == EstadoBandeja.Disponible && !b.Eliminado)
                .OrderBy(b => b.Codigo)
                .ToListAsync();
        }

        public async Task<List<Bandeja>> GetByEstadoAsync(EstadoBandeja estado)
        {
            return await _context.Bandejas
                .Include(b => b.Expediente)
                .Include(b => b.UsuarioAsigna)
                .Where(b => b.Estado == estado && !b.Eliminado)
                .OrderBy(b => b.Codigo)
                .ToListAsync();
        }

        public async Task<Bandeja?> GetByExpedienteIdAsync(int expedienteId)
        {
            return await _context.Bandejas
                .Include(b => b.UsuarioAsigna)
                .Include(b => b.Expediente)
                .FirstOrDefaultAsync(b => b.ExpedienteID == expedienteId &&
                                          b.Estado == EstadoBandeja.Ocupada &&
                                          !b.Eliminado);
        }

        public async Task<List<Bandeja>> GetBandejasConAlertaAsync(int horasAlerta = 24)
        {
            var fechaLimite = DateTime.Now.AddHours(-horasAlerta);

            return await _context.Bandejas
                .Include(b => b.Expediente)
                .Include(b => b.UsuarioAsigna)
                .Where(b => b.Estado == EstadoBandeja.Ocupada &&
                            b.FechaHoraAsignacion.HasValue &&
                            b.FechaHoraAsignacion.Value <= fechaLimite &&
                            !b.Eliminado)
                .OrderBy(b => b.FechaHoraAsignacion)
                .ToListAsync();
        }

        public async Task<BandejaEstadisticas> GetEstadisticasAsync()
        {
            var total = await _context.Bandejas
                .CountAsync(b => !b.Eliminado);

            var disponibles = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.Disponible && !b.Eliminado);

            var ocupadas = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.Ocupada && !b.Eliminado);

            var enMantenimiento = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.Mantenimiento && !b.Eliminado);

            var fueraDeServicio = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.FueraDeServicio && !b.Eliminado);

            // Contar bandejas con alerta
            var fechaLimite24h = DateTime.Now.AddHours(-24);
            var fechaLimite48h = DateTime.Now.AddHours(-48);

            var conAlerta24h = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.Ocupada &&
                                 b.FechaHoraAsignacion.HasValue &&
                                 b.FechaHoraAsignacion.Value <= fechaLimite24h &&
                                 !b.Eliminado);

            var conAlerta48h = await _context.Bandejas
                .CountAsync(b => b.Estado == EstadoBandeja.Ocupada &&
                                 b.FechaHoraAsignacion.HasValue &&
                                 b.FechaHoraAsignacion.Value <= fechaLimite48h &&
                                 !b.Eliminado);

            return new BandejaEstadisticas
            {
                Total = total,
                Disponibles = disponibles,
                Ocupadas = ocupadas,
                EnMantenimiento = enMantenimiento,
                FueraDeServicio = fueraDeServicio,
                PorcentajeOcupacion = total > 0 ? (ocupadas * 100.0 / total) : 0,
                ConAlerta24h = conAlerta24h,
                ConAlerta48h = conAlerta48h
            };
        }

        public async Task UpdateAsync(Bandeja bandeja)
        {
            bandeja.FechaModificacion = DateTime.Now;
            _context.Bandejas.Update(bandeja);
            await _context.SaveChangesAsync();
        }
    }
}