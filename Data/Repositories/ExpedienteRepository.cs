using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using System.Reflection.Metadata.Ecma335;

namespace SisMortuorio.Data.Repositories
{
    public class ExpedienteRepository : IExpedienteRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpedienteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Expediente?> GetByIdAsync(int id)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.CustodiaTransferencias)
                .Include(e => e.BandejaActual)
               
                .FirstOrDefaultAsync(e => e.ExpedienteID == id && !e.Eliminado);
        }

        public async Task<Expediente?> GetByCodigoAsync(string codigoExpediente)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .FirstOrDefaultAsync(e => e.CodigoExpediente == codigoExpediente && !e.Eliminado);
        }

        public async Task<Expediente?> GetByHCAsync(string hc)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .FirstOrDefaultAsync(e => e.HC == hc && !e.Eliminado);
        }

        public async Task<List<Expediente>> GetAllAsync()
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .Where(e => !e.Eliminado)
                .OrderByDescending(e => e.FechaCreacion)
                .ToListAsync();
        }

        public async Task<List<Expediente>> GetByFiltrosAsync(
            string? hc,
            string? dni,
            string? servicio,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            EstadoExpediente? estado)
        {
            var query = _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .Where(e => !e.Eliminado)
                .AsQueryable();

            if (!string.IsNullOrEmpty(hc))
                query = query.Where(e => e.HC == hc);

            if (!string.IsNullOrEmpty(dni))
                query = query.Where(e => e.NumeroDocumento == dni);

            if (!string.IsNullOrEmpty(servicio))
                query = query.Where(e => e.ServicioFallecimiento.Contains(servicio));

            if (fechaDesde.HasValue)
                query = query.Where(e => e.FechaHoraFallecimiento >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(e => e.FechaHoraFallecimiento <= fechaHasta.Value);

            if (estado.HasValue)
                query = query.Where(e => e.EstadoActual == estado.Value);

            return await query
                .OrderByDescending(e => e.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Expediente> CreateAsync(Expediente expediente)
        {
            _context.Expedientes.Add(expediente);
            await _context.SaveChangesAsync();

            // Recargar con relaciones
            var expedienteCreado = await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .FirstOrDefaultAsync(e => e.ExpedienteID == expediente.ExpedienteID);

            return expedienteCreado ?? expediente;
        }

        public async Task UpdateAsync(Expediente expediente)
        {
            expediente.FechaModificacion = DateTime.Now;
            _context.Expedientes.Update(expediente);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsHCAsync(string hc)
        {
            return await _context.Expedientes
                .AnyAsync(e => e.HC == hc && !e.Eliminado);
        }

        public async Task<bool> ExistsCertificadoSINADEFAsync(string certificado)
        {
            return await _context.Expedientes
                .AnyAsync(e => e.NumeroCertificadoSINADEF == certificado && !e.Eliminado);
        }

        public async Task<int> GetCountByServicioAsync(string servicio)
        {
            return await _context.Expedientes
                .CountAsync(e => e.ServicioFallecimiento == servicio && !e.Eliminado);
        }
        public async Task<Expediente?> GetUltimoExpedienteDelAñoAsync(int año)
        {
            var añoStr = año.ToString();
            var prefijo = $"SGM-{añoStr}-";

            return await _context.Expedientes
                .Where(e => !e.Eliminado && e.CodigoExpediente.StartsWith(prefijo))
                .OrderByDescending(e => e.ExpedienteID) // Usar ID en lugar de código
                .FirstOrDefaultAsync();
        }
        public async Task<Expediente?> GetByCodigoQRAsync(string codigoQR)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .FirstOrDefaultAsync(e => e.CodigoQR == codigoQR && !e.Eliminado);
        }
        public async Task<List<Expediente>> GetPendientesValidacionAdmisionAsync()
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .Where(e => !e.Eliminado &&
                            (e.EstadoActual == EstadoExpediente.EnBandeja ||
                             e.EstadoActual == EstadoExpediente.PendienteRetiro ||
                             e.EstadoActual == EstadoExpediente.Retirado))
                .OrderByDescending(e => e.FechaCreacion)
                .ToListAsync();
        }
        public async Task<List<Expediente>> GetPendientesRecojoAsync()
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.DeudaSangre)
                .Include(e => e.DeudaEconomica)
                .Include(e => e.BandejaActual)
                .Where(e =>
                    e.EstadoActual == EstadoExpediente.EnPiso ||
                    e.EstadoActual == EstadoExpediente.PendienteDeRecojo ||
                    e.EstadoActual == EstadoExpediente.EnTrasladoMortuorio ||
                    e.EstadoActual == EstadoExpediente.PendienteAsignacionBandeja) 
                .OrderBy(e => e.FechaHoraFallecimiento)
                .ToListAsync();
        }
        // ===================================================================
        // BÚSQUEDA SIMPLE (para módulos de deudas)
        // ===================================================================

        public async Task<Expediente?> GetByHCMasRecienteAsync(string hc)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .Where(e => e.HC == hc && !e.Eliminado)
                .OrderByDescending(e => e.FechaCreacion)
                .FirstOrDefaultAsync();
        }

        public async Task<Expediente?> GetByDNIMasRecienteAsync(string dni)
        {
            return await _context.Expedientes
                .Include(e => e.UsuarioCreador)
                .Include(e => e.Pertenencias)
                .Include(e => e.BandejaActual)
                .Where(e => e.NumeroDocumento == dni && !e.Eliminado)
                .OrderByDescending(e => e.FechaCreacion)
                .FirstOrDefaultAsync();
        }
    }

}