using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Vigilancia;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Consulta de expedientes para Supervisor de Vigilancia.
    /// </summary>
    public class VigilanteSupervisorService(
        ApplicationDbContext context,
        ILogger<VigilanteSupervisorService> logger) : IVigilanteSupervisorService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<VigilanteSupervisorService> _logger = logger;

        /// <inheritdoc/>
        public async Task<List<ExpedienteVigilanciaDTO>> ObtenerExpedientesAsync(string? busqueda)
        {
            var query = _context.Expedientes
                .Include(e => e.DeudaSangre)
                .Include(e => e.DeudaEconomica)
                .Include(e => e.ActaRetiro)
                .Include(e => e.BandejaActual)
                .Where(e => !e.Eliminado)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var t = busqueda.Trim();
                query = query.Where(e =>
                    e.HC.Contains(t) ||
                    e.NumeroDocumento.Contains(t) ||
                    e.NombreCompleto.Contains(t));
            }

            var expedientes = await query
                .OrderByDescending(e => e.FechaCreacion)
                .ToListAsync();

            return expedientes.Select(MapToVigilanciaDTO).ToList();
        }

        /// <inheritdoc/>
        public async Task<DetalleVigilanciaDTO?> ObtenerDetalleAsync(int expedienteId)
        {
            var expediente = await _context.Expedientes
                .Include(e => e.DeudaSangre)
                .Include(e => e.DeudaEconomica)
                .Include(e => e.ActaRetiro)
                .Include(e => e.BandejaActual)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExpedienteID == expedienteId && !e.Eliminado);

            return expediente is null ? null : MapToDetalleDTO(expediente);
        }

        // ── Mapeo ────────────────────────────────────────────────────

        private static ExpedienteVigilanciaDTO MapToVigilanciaDTO(Expediente e)
        {
            var dto = new ExpedienteVigilanciaDTO
            {
                ExpedienteID = e.ExpedienteID,
                CodigoExpediente = e.CodigoExpediente,
                HC = e.HC,
                NombreCompleto = e.NombreCompleto,
                TipoDocumento = e.TipoDocumento.ToString(),
                NumeroDocumento = e.NumeroDocumento,
                ServicioFallecimiento = e.ServicioFallecimiento,
                FechaHoraFallecimiento = e.FechaHoraFallecimiento,
                TipoExpediente = e.TipoExpediente.ToString(),
                EstadoActual = e.EstadoActual.ToString(),
                CodigoBandeja = e.BandejaActual?.Codigo,
                FechaIngresoBandeja = e.BandejaActual?.FechaHoraAsignacion,
                TiempoEnMortuorio = FormatearTiempo(e.BandejaActual?.FechaHoraAsignacion),
                BypassDeudaAutorizado = e.BypassDeudaAutorizado,
                BypassDeudaJustificacion = e.BypassDeudaJustificacion,
                TieneActa = e.ActaRetiro != null,
                TipoSalida = e.ActaRetiro?.TipoSalida.ToString(),
            };

            (dto.BloqueaSangre, dto.DescripcionSangre) = CalcSemaforoSangre(e);
            (dto.BloqueaEconomica, dto.DescripcionEconomica) = CalcSemaforoEconomica(e);

            // Override semáforo si bypass activo
            if (e.BypassDeudaAutorizado)
            {
                dto.BloqueaSangre = false;
                dto.BloqueaEconomica = false;
                dto.DescripcionSangre = "Bypass autorizado";
                dto.DescripcionEconomica = "Bypass autorizado";
            }

            return dto;
        }

        private static DetalleVigilanciaDTO MapToDetalleDTO(Expediente e)
        {
            var base_ = MapToVigilanciaDTO(e);

            var dto = new DetalleVigilanciaDTO
            {
                // Herencia del DTO base
                ExpedienteID = base_.ExpedienteID,
                CodigoExpediente = base_.CodigoExpediente,
                HC = base_.HC,
                NombreCompleto = base_.NombreCompleto,
                TipoDocumento = base_.TipoDocumento,
                NumeroDocumento = base_.NumeroDocumento,
                ServicioFallecimiento = base_.ServicioFallecimiento,
                FechaHoraFallecimiento = base_.FechaHoraFallecimiento,
                TipoExpediente = base_.TipoExpediente,
                EstadoActual = base_.EstadoActual,
                CodigoBandeja = base_.CodigoBandeja,
                FechaIngresoBandeja = base_.FechaIngresoBandeja,
                TiempoEnMortuorio = base_.TiempoEnMortuorio,
                BloqueaSangre = base_.BloqueaSangre,
                BloqueaEconomica = base_.BloqueaEconomica,
                DescripcionSangre = base_.DescripcionSangre,
                DescripcionEconomica = base_.DescripcionEconomica,
                BypassDeudaAutorizado = base_.BypassDeudaAutorizado,
                BypassDeudaJustificacion = base_.BypassDeudaJustificacion,
                TieneActa = base_.TieneActa,
                TipoSalida = base_.TipoSalida,

                // Campos adicionales del detalle
                DiagnosticoFinal = e.DiagnosticoFinal,
                CausaViolentaODudosa = e.CausaViolentaODudosa,
                EsNN = e.EsNN,
                FechaNacimiento = e.FechaNacimiento,
                Sexo = e.Sexo,
                FuenteFinanciamiento = e.FuenteFinanciamiento.ToString(),
                DocumentacionCompleta = e.DocumentacionCompleta,

                // Semáforo expandido
                EstadoSangre = e.DeudaSangre?.Estado.ToString() ?? "Sin registro",
                DetalleSangre = e.DeudaSangre?.ObtenerSemaforo(),
                EstadoEconomica = e.DeudaEconomica?.Estado.ToString() ?? "Sin registro",
                MensajeEconomica = e.DeudaEconomica?.ObtenerMensajeEstado() ?? "Sin deuda registrada",

                // Retiro
                ResponsableRetiro = e.ActaRetiro?.ObtenerNombreResponsableFirma(),
                ParentescoOCargo = e.ActaRetiro?.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar
                    ? e.ActaRetiro.FamiliarParentesco
                    : e.ActaRetiro?.AutoridadCargo,
                Destino = e.ActaRetiro?.Destino,

                ActaRetiroID = e.ActaRetiro?.ActaRetiroID,

                // Jefe de Guardia
                JefeGuardiaNombre = e.ActaRetiro?.JefeGuardiaNombre,
                JefeGuardiaCMP = e.ActaRetiro?.JefeGuardiaCMP,
            };

            // Override bypass en detalle
            if (e.BypassDeudaAutorizado)
            {
                dto.BloqueaSangre = false;
                dto.BloqueaEconomica = false;
                dto.DescripcionSangre = "Bypass autorizado";
                dto.DescripcionEconomica = "Bypass autorizado";
                // MensajeEconomica lo sobreescribe el frontend si estado=Pendiente
            }

            return dto;
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>null=sin registro, true=bloquea, false=libre.</summary>
        private static (bool? bloquea, string descripcion) CalcSemaforoSangre(Expediente e)
        {
            if (e.DeudaSangre is null) return (null, "Sin deuda registrada");
            return (e.DeudaSangre.BloqueaRetiro(), e.DeudaSangre.ObtenerSemaforo());
        }

        /// <summary>null=sin registro, true=bloquea, false=libre.</summary>
        private static (bool? bloquea, string descripcion) CalcSemaforoEconomica(Expediente e)
        {
            if (e.DeudaEconomica is null) return (null, "Sin deuda registrada");
            return (e.DeudaEconomica.BloqueaRetiro(), e.DeudaEconomica.ObtenerMensajeEstado());
        }

        /// <summary>
        /// Formatea tiempo desde una fecha hasta ahora.
        /// Menos de 24h → "3h 20m". 24h o más → "2d 5h 10m".
        /// </summary>
        private static string? FormatearTiempo(DateTime? fechaIngreso)
        {
            if (!fechaIngreso.HasValue) return null;
            var t = DateTime.Now - fechaIngreso.Value;
            if (t.TotalHours < 24) return $"{(int)t.TotalHours}h {t.Minutes}m";
            var d = (int)t.TotalDays;
            return t.Minutes > 0 ? $"{d}d {t.Hours}h {t.Minutes}m" : $"{d}d {t.Hours}h";
        }
    }
}