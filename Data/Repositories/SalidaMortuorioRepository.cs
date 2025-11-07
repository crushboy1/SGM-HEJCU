using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio para SalidaMortuorio.
    /// </summary>
    public class SalidaMortuorioRepository : ISalidaMortuorioRepository
    {
        private readonly ApplicationDbContext _context;

        public SalidaMortuorioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalidaMortuorio> CreateAsync(SalidaMortuorio salida)
        {
            _context.SalidasMortuorio.Add(salida);
            await _context.SaveChangesAsync();

            // Recargar con relaciones para DTOs y logs
            var salidaCreada = await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(s => s.SalidaID == salida.SalidaID);

            return salidaCreada ?? salida;
        }

        public async Task<SalidaMortuorio?> GetByIdAsync(int salidaId)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(s => s.SalidaID == salidaId);
        }

        public async Task<SalidaMortuorio?> GetByExpedienteIdAsync(int expedienteId)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Vigilante)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(s => s.ExpedienteID == expedienteId);
        }

        public async Task<bool> ExistsByExpedienteIdAsync(int expedienteId)
        {
            return await _context.SalidasMortuorio
                .AnyAsync(s => s.ExpedienteID == expedienteId);
        }

        public async Task<List<SalidaMortuorio>> GetByVigilanteIdAsync(int vigilanteId)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Where(s => s.VigilanteID == vigilanteId)
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        public async Task<List<SalidaMortuorio>> GetByTipoSalidaAsync(TipoSalida tipoSalida)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Where(s => s.TipoSalida == tipoSalida)
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        public async Task<List<SalidaMortuorio>> GetSalidasPorRangoFechasAsync(
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Where(s => s.FechaHoraSalida >= fechaInicio &&
                            s.FechaHoraSalida <= fechaFin)
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        public async Task<List<SalidaMortuorio>> GetSalidasConIncidentesAsync()
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Where(s => s.IncidenteRegistrado)
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        public async Task<List<SalidaMortuorio>> GetByFunerariaAsync(string nombreFuneraria)
        {
            return await _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Where(s => s.NombreFuneraria != null &&
                            s.NombreFuneraria.Contains(nombreFuneraria))
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        public async Task<SalidaEstadisticas> GetEstadisticasAsync(
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            var query = _context.SalidasMortuorio.AsQueryable();

            // Aplicar filtro de fechas si se proporcionan
            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

            var total = await query.CountAsync();
            var familiar = await query.CountAsync(s => s.TipoSalida == TipoSalida.Familiar);
            var autoridadLegal = await query.CountAsync(s => s.TipoSalida == TipoSalida.AutoridadLegal);
            var trasladoHospital = await query.CountAsync(s => s.TipoSalida == TipoSalida.TrasladoHospital);
            var otro = await query.CountAsync(s => s.TipoSalida == TipoSalida.Otro);
            var conIncidentes = await query.CountAsync(s => s.IncidenteRegistrado);
            var conFuneraria = await query.CountAsync(s => !string.IsNullOrEmpty(s.NombreFuneraria));

            return new SalidaEstadisticas
            {
                TotalSalidas = total,
                SalidasFamiliar = familiar,
                SalidasAutoridadLegal = autoridadLegal,
                SalidasTrasladoHospital = trasladoHospital,
                SalidasOtro = otro,
                ConIncidentes = conIncidentes,
                ConFuneraria = conFuneraria,
                PorcentajeIncidentes = total > 0 ? (conIncidentes * 100.0 / total) : 0
            };
        }
    }
}