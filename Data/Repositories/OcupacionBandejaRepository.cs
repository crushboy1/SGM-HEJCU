using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio para OcupacionBandeja.
    /// </summary>
    public class OcupacionBandejaRepository : IOcupacionBandejaRepository
    {
        private readonly ApplicationDbContext _context;

        public OcupacionBandejaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OcupacionBandeja> CreateAsync(OcupacionBandeja ocupacion)
        {
            _context.OcupacionesBandejas.Add(ocupacion);
            await _context.SaveChangesAsync();

            // Recargar con relaciones para devolver objeto completo al servicio
            var ocupacionCreada = await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.Expediente)
                .Include(o => o.UsuarioAsignador)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(o => o.OcupacionID == ocupacion.OcupacionID);

            return ocupacionCreada ?? ocupacion;
        }

        public async Task UpdateAsync(OcupacionBandeja ocupacion)
        {
            _context.OcupacionesBandejas.Update(ocupacion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<OcupacionBandeja>> GetHistorialByExpedienteIdAsync(int expedienteId)
        {
            return await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.UsuarioAsignador)
                .Include(o => o.UsuarioLibera)
                .Where(o => o.ExpedienteID == expedienteId)
                .OrderBy(o => o.FechaHoraIngreso)
                .ToListAsync();
        }

        public async Task<OcupacionBandeja?> GetActualByExpedienteIdAsync(int expedienteId)
        {
            return await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.UsuarioAsignador)
                .Where(o => o.ExpedienteID == expedienteId && !o.FechaHoraSalida.HasValue)
                .OrderByDescending(o => o.FechaHoraIngreso)
                .FirstOrDefaultAsync();
        }

        public async Task<List<OcupacionBandeja>> GetHistorialByBandejaIdAsync(int bandejaId)
        {
            return await _context.OcupacionesBandejas
                .Include(o => o.Expediente)
                .Include(o => o.UsuarioAsignador)
                .Include(o => o.UsuarioLibera)
                .Where(o => o.BandejaID == bandejaId)
                .OrderByDescending(o => o.FechaHoraIngreso)
                .ToListAsync();
        }

        public async Task<List<OcupacionBandeja>> GetOcupacionesActivasAsync()
        {
            return await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.Expediente)
                .Include(o => o.UsuarioAsignador)
                .Where(o => !o.FechaHoraSalida.HasValue)
                .OrderBy(o => o.FechaHoraIngreso)
                .ToListAsync();
        }

        public async Task<List<OcupacionBandeja>> GetOcupacionesConAlertaAsync(int horasAlerta = 24)
        {
            var fechaLimite = DateTime.Now.AddHours(-horasAlerta);

            return await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.Expediente)
                .Include(o => o.UsuarioAsignador)
                .Where(o => !o.FechaHoraSalida.HasValue &&
                            o.FechaHoraIngreso <= fechaLimite)
                .OrderBy(o => o.FechaHoraIngreso)
                .ToListAsync();
        }

        public async Task<List<OcupacionBandeja>> GetOcupacionesPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.OcupacionesBandejas
                .Include(o => o.Bandeja)
                .Include(o => o.Expediente)
                .Include(o => o.UsuarioAsignador)
                .Include(o => o.UsuarioLibera)
                .Where(o => o.FechaHoraIngreso >= fechaInicio &&
                            o.FechaHoraIngreso <= fechaFin)
                .OrderByDescending(o => o.FechaHoraIngreso)
                .ToListAsync();
        }
    }
}