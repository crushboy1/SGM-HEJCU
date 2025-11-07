using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio para VerificacionMortuorio.
    /// </summary>
    public class VerificacionMortuorioRepository : IVerificacionMortuorioRepository
    {
        private readonly ApplicationDbContext _context;

        public VerificacionMortuorioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VerificacionMortuorio> CreateAsync(VerificacionMortuorio verificacion)
        {
            _context.VerificacionesMortuorio.Add(verificacion);
            await _context.SaveChangesAsync();

            // Recargar con relaciones para DTOs y logs
            var verificacionCreada = await _context.VerificacionesMortuorio
                .Include(v => v.Expediente)
                .Include(v => v.Vigilante)
                    .ThenInclude(u => u.Rol)
                .Include(v => v.TecnicoAmbulancia)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(v => v.VerificacionID == verificacion.VerificacionID);

            return verificacionCreada ?? verificacion;
        }

        public async Task<VerificacionMortuorio?> GetByIdAsync(int verificacionId)
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Expediente)
                .Include(v => v.Vigilante)
                    .ThenInclude(u => u.Rol)
                .Include(v => v.TecnicoAmbulancia)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(v => v.VerificacionID == verificacionId);
        }

        public async Task<List<VerificacionMortuorio>> GetHistorialByExpedienteIdAsync(int expedienteId)
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Vigilante)
                .Include(v => v.TecnicoAmbulancia)
                .Where(v => v.ExpedienteID == expedienteId)
                .OrderByDescending(v => v.FechaHoraVerificacion)
                .ToListAsync();
        }

        public async Task<VerificacionMortuorio?> GetUltimaVerificacionAsync(int expedienteId)
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Vigilante)
                .Include(v => v.TecnicoAmbulancia)
                .Where(v => v.ExpedienteID == expedienteId)
                .OrderByDescending(v => v.FechaHoraVerificacion)
                .FirstOrDefaultAsync();
        }

        public async Task<List<VerificacionMortuorio>> GetByVigilanteIdAsync(int vigilanteId)
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Expediente)
                .Include(v => v.TecnicoAmbulancia)
                .Where(v => v.VigilanteID == vigilanteId)
                .OrderByDescending(v => v.FechaHoraVerificacion)
                .ToListAsync();
        }

        public async Task<List<VerificacionMortuorio>> GetVerificacionesRechazadasAsync()
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Expediente)
                .Include(v => v.Vigilante)
                .Include(v => v.TecnicoAmbulancia)
                .Where(v => !v.Aprobada)
                .OrderByDescending(v => v.FechaHoraVerificacion)
                .ToListAsync();
        }

        public async Task<List<VerificacionMortuorio>> GetVerificacionesPorRangoFechasAsync(
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            return await _context.VerificacionesMortuorio
                .Include(v => v.Expediente)
                .Include(v => v.Vigilante)
                .Include(v => v.TecnicoAmbulancia)
                .Where(v => v.FechaHoraVerificacion >= fechaInicio &&
                            v.FechaHoraVerificacion <= fechaFin)
                .OrderByDescending(v => v.FechaHoraVerificacion)
                .ToListAsync();
        }

        public async Task<VerificacionEstadisticas> GetEstadisticasAsync(
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            var query = _context.VerificacionesMortuorio.AsQueryable();

            // Aplicar filtro de fechas si se proporcionan
            if (fechaInicio.HasValue)
                query = query.Where(v => v.FechaHoraVerificacion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(v => v.FechaHoraVerificacion <= fechaFin.Value);

            var total = await query.CountAsync();
            var aprobadas = await query.CountAsync(v => v.Aprobada);
            var rechazadas = total - aprobadas;

            // Contar discrepancias específicas (solo en rechazadas)
            var conDiscrepanciaHC = await query.CountAsync(v => !v.Aprobada && !v.HCCoincide);
            var conDiscrepanciaDNI = await query.CountAsync(v => !v.Aprobada && !v.DNICoincide);
            var conDiscrepanciaNombre = await query.CountAsync(v => !v.Aprobada && !v.NombreCoincide);

            return new VerificacionEstadisticas
            {
                TotalVerificaciones = total,
                Aprobadas = aprobadas,
                Rechazadas = rechazadas,
                PorcentajeAprobacion = total > 0 ? (aprobadas * 100.0 / total) : 0,
                ConDiscrepanciaHC = conDiscrepanciaHC,
                ConDiscrepanciaDNI = conDiscrepanciaDNI,
                ConDiscrepanciaNombre = conDiscrepanciaNombre
            };
        }
    }
}