using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs.Salida;
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
        public async Task<List<SalidaMortuorio>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Where(s => s.TiempoPermanencia != null && s.TiempoPermanencia.Value.TotalHours > 48);

            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

            return await query
                .OrderByDescending(s => s.TiempoPermanencia)
                .ToListAsync();
        }

        public async Task<List<SalidaMortuorio>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.SalidasMortuorio
                .Include(s => s.Expediente)
                .Include(s => s.Vigilante)
                .Include(s => s.ActaRetiro)
                .Include(s => s.ExpedienteLegal)
                .Where(s => s.TipoSalida == tipo);

            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

            return await query
                .OrderByDescending(s => s.FechaHoraSalida)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene datos del expediente y acta para pre-llenar el formulario de salida
        /// </summary>
        public async Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId)
        {
            var expediente = await _context.Expedientes
                .Include(e => e.ActaRetiro)
                .Include(e => e.DeudaSangre)
                .Include(e => e.DeudaEconomica)
                .FirstOrDefaultAsync(e => e.ExpedienteID == expedienteId)
                ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

            var acta = expediente.ActaRetiro
                ?? throw new InvalidOperationException($"El expediente {expedienteId} no tiene Acta de Retiro");

            // Validar estado del expediente
            if (expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
                throw new InvalidOperationException($"El expediente debe estar en PendienteRetiro. Estado actual: {expediente.EstadoActual}");

            // Validar que el acta esté firmada
            if (!acta.TieneTodasLasFirmas())
                throw new InvalidOperationException("El acta debe estar firmada por las 3 partes antes de registrar la salida");

            // Evaluar deudas
            bool tieneDeudaSangre = expediente.DeudaSangre?.BloqueaRetiro() ?? false;
            bool tieneDeudaEconomica = expediente.DeudaEconomica?.BloqueaRetiro() ?? false;
            bool pagosOK = !tieneDeudaSangre && !tieneDeudaEconomica;

            return new DatosPreLlenadoSalidaDTO
            {
                // Tipo de salida (determina flujo)
                TipoSalida = acta.TipoSalida,

                // Responsable (Familiar O Autoridad según TipoSalida)
                ResponsableNombre = acta.TipoSalida == TipoSalida.Familiar
                    ? acta.FamiliarNombreCompleto
                    : acta.AutoridadNombreCompleto,

                ResponsableTipoDocumento = acta.TipoSalida == TipoSalida.Familiar
                    ? acta.FamiliarTipoDocumento?.ToString()
                    : acta.AutoridadTipoDocumento?.ToString(),

                ResponsableNumeroDocumento = acta.TipoSalida == TipoSalida.Familiar
                    ? acta.FamiliarNumeroDocumento
                    : acta.AutoridadNumeroDocumento,

                ResponsableParentesco = acta.FamiliarParentesco,

                ResponsableTelefono = acta.TipoSalida == TipoSalida.Familiar
                    ? acta.FamiliarTelefono
                    : acta.AutoridadTelefono,

                // Funeraria (NULL - Vigilante captura)
                NombreFuneraria = null,
                FunerariaRUC = null,
                FunerariaTelefono = null,
                ConductorFuneraria = null,
                DNIConductor = null,
                AyudanteFuneraria = null,
                DNIAyudante = null,
                PlacaVehiculo = null,

                // Autorización Legal
                NumeroOficio = acta.TipoSalida == TipoSalida.AutoridadLegal
                    ? acta.NumeroOficioLegal
                    : null,

                // Destino
                Destino = acta.TipoSalida == TipoSalida.AutoridadLegal
                    ? "Morgue Central de Lima"
                    : null,

                // Validaciones automáticas
                DeudaSangreOK = !tieneDeudaSangre,
                DeudaSangreMensaje = tieneDeudaSangre
                    ? "Pendiente firma de compromiso"
                    : null,

                DeudaEconomicaOK = !tieneDeudaEconomica,
                DeudaEconomicaMensaje = tieneDeudaEconomica
                    ? "Pendiente liquidación económica"
                    : null,

                PagosOK = pagosOK,
                PuedeRegistrarSalida = pagosOK,
                MensajeBloqueo = !pagosOK
                    ? "No se puede registrar salida: Deudas pendientes"
                    : null
            };
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
            var conIncidentes = await query.CountAsync(s => s.IncidenteRegistrado);
            var conFuneraria = await query.CountAsync(s => !string.IsNullOrEmpty(s.NombreFuneraria));

            return new SalidaEstadisticas
            {
                TotalSalidas = total,
                SalidasFamiliar = familiar,
                SalidasAutoridadLegal = autoridadLegal,
                ConIncidentes = conIncidentes,
                ConFuneraria = conFuneraria,
                PorcentajeIncidentes = total > 0 ? (conIncidentes * 100.0 / total) : 0
            };
        }
    }
}