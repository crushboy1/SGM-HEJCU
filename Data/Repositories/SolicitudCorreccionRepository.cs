using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using static SisMortuorio.Data.Repositories.ISolicitudCorreccionRepository;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Implementación del repositorio para SolicitudCorreccionExpediente.
    /// </summary>
    public class SolicitudCorreccionRepository : ISolicitudCorreccionRepository
    {
        private readonly ApplicationDbContext _context;

        public SolicitudCorreccionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SolicitudCorreccionExpediente> CreateAsync(SolicitudCorreccionExpediente solicitud)
        {
            _context.SolicitudesCorreccion.Add(solicitud);
            await _context.SaveChangesAsync();

            // Recargar con relaciones para DTOs y notificaciones
            var solicitudCreada = await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita) // Vigilante
                    .ThenInclude(u => u.Rol)
                .Include(sc => sc.UsuarioResponsable) // Enfermera
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(sc => sc.SolicitudID == solicitud.SolicitudID);

            return solicitudCreada ?? solicitud;
        }

        public async Task UpdateAsync(SolicitudCorreccionExpediente solicitud)
        {
            _context.SolicitudesCorreccion.Update(solicitud);
            await _context.SaveChangesAsync();
        }

        public async Task<SolicitudCorreccionExpediente?> GetByIdAsync(int solicitudId)
        {
            return await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita)
                .Include(sc => sc.UsuarioResponsable)
                .FirstOrDefaultAsync(sc => sc.SolicitudID == solicitudId);
        }

        public async Task<List<SolicitudCorreccionExpediente>> GetPendientesAsync()
        {
            return await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita)
                .Where(sc => !sc.Resuelta)
                .OrderBy(sc => sc.FechaHoraSolicitud)
                .ToListAsync();
        }

        public async Task<List<SolicitudCorreccionExpediente>> GetPendientesByServicioAsync(string servicio)
        {
            return await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita)
                .Where(sc => !sc.Resuelta &&
                             sc.Expediente.ServicioFallecimiento.Contains(servicio))
                .OrderBy(sc => sc.FechaHoraSolicitud)
                .ToListAsync();
        }

        public async Task<List<SolicitudCorreccionExpediente>> GetHistorialByExpedienteIdAsync(int expedienteId)
        {
            return await _context.SolicitudesCorreccion
                .Include(sc => sc.UsuarioSolicita)
                .Include(sc => sc.UsuarioResponsable)
                .Where(sc => sc.ExpedienteID == expedienteId)
                .OrderBy(sc => sc.FechaHoraSolicitud)
                .ToListAsync();
        }
        public async Task<List<SolicitudCorreccionExpediente>> GetSolicitudesConAlertaAsync(int horasAlerta = 2)
        {
            var fechaLimite = DateTime.Now.AddHours(-horasAlerta);

            return await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita)
                .Include(sc => sc.UsuarioResponsable)
                .Where(sc => !sc.Resuelta && sc.FechaHoraSolicitud <= fechaLimite)
                .OrderBy(sc => sc.FechaHoraSolicitud)
                .ToListAsync();
        }

        public async Task<List<SolicitudCorreccionExpediente>> GetSolicitudesPorRangoFechasAsync(
            DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.SolicitudesCorreccion
                .Include(sc => sc.Expediente)
                .Include(sc => sc.UsuarioSolicita)
                .Include(sc => sc.UsuarioResponsable)
                .Where(sc => sc.FechaHoraSolicitud >= fechaInicio && sc.FechaHoraSolicitud <= fechaFin)
                .OrderByDescending(sc => sc.FechaHoraSolicitud)
                .ToListAsync();
        }

        public async Task<SolicitudEstadisticas> GetEstadisticasAsync(
            DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = _context.SolicitudesCorreccion.AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(sc => sc.FechaHoraSolicitud >= fechaInicio.Value);
            if (fechaFin.HasValue)
                query = query.Where(sc => sc.FechaHoraSolicitud <= fechaFin.Value);

            var total = await query.CountAsync();
            var pendientes = await query.CountAsync(sc => !sc.Resuelta);
            var resueltas = total - pendientes;
            var fechaLimite = DateTime.Now.AddHours(-2);
            var conAlerta = await query.CountAsync(sc => !sc.Resuelta && sc.FechaHoraSolicitud <= fechaLimite);

            var resueltasConTiempo = await query
                .Where(sc => sc.Resuelta && sc.FechaHoraResolucion.HasValue)
                .Select(sc => EF.Functions.DateDiffHour(sc.FechaHoraSolicitud, sc.FechaHoraResolucion!.Value))
                .ToListAsync();

            var tiempoPromedio = resueltasConTiempo.Any() ? resueltasConTiempo.Average() : 0.0;

            return new SolicitudEstadisticas
            {
                TotalSolicitudes = total,
                Pendientes = pendientes,
                Resueltas = resueltas,
                ConAlerta = conAlerta,
                TiempoPromedioResolucionHoras = tiempoPromedio
            };
        }
    }
}