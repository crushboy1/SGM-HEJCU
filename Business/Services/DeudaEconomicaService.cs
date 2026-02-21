using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    public class DeudaEconomicaService : IDeudaEconomicaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeudaEconomicaService> _logger;
        private readonly INotificacionDeudaService _notificacionDeudaService;
        private readonly IDeudaEconomicaRepository _repository;

        public DeudaEconomicaService(
            IDeudaEconomicaRepository repository,
            ApplicationDbContext context,
            ILogger<DeudaEconomicaService> logger,
            INotificacionDeudaService notificacionDeudaService)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _notificacionDeudaService = notificacionDeudaService;
        }

        public async Task<DeudaEconomicaDTO> RegistrarDeudaAsync(CreateDeudaEconomicaDTO dto)
        {
            var expediente = await _context.Expedientes
                .FirstOrDefaultAsync(e => e.ExpedienteID == dto.ExpedienteID && !e.Eliminado)
                ?? throw new KeyNotFoundException($"Expediente {dto.ExpedienteID} no encontrado");

            if (await _context.DeudasEconomicas.AnyAsync(d => d.ExpedienteID == dto.ExpedienteID))
                throw new InvalidOperationException($"Ya existe una deuda económica para el expediente {dto.ExpedienteID}");

            if (dto.MontoDeuda <= 0)
                throw new ArgumentException("El monto de la deuda debe ser mayor a cero", nameof(dto.MontoDeuda));

            _ = await _context.Users.FindAsync(dto.UsuarioRegistroID)
                ?? throw new KeyNotFoundException($"Usuario {dto.UsuarioRegistroID} no encontrado");

            var deuda = new DeudaEconomica
            {
                ExpedienteID = dto.ExpedienteID,
                MontoDeuda = dto.MontoDeuda,
                Estado = EstadoDeudaEconomica.Pendiente,
                UsuarioRegistroID = dto.UsuarioRegistroID,
                FechaRegistro = DateTime.Now
            };

            await _repository.CreateAsync(deuda);

            _logger.LogInformation(
                "Deuda económica registrada - ExpedienteID: {ExpedienteID}, Monto: S/ {Monto}",
                dto.ExpedienteID, dto.MontoDeuda
            );

            await _notificacionDeudaService.NotificarDeudaCreadaAsync(
                "Económica",
                dto.ExpedienteID,
                expediente.CodigoExpediente,
                $"S/ {dto.MontoDeuda:N2}"
            );

            await RegistrarAuditoriaAsync("DeudaEconomica", "Crear", dto.UsuarioRegistroID, dto.ExpedienteID, null, deuda);

            return await MapToDTO(deuda);
        }
        public async Task<DeudaEconomicaDTO?> ObtenerPorExpedienteAsync(int expedienteId)
        {
            var deuda = await _repository.GetByExpedienteIdAsync(expedienteId);
            return deuda != null ? await MapToDTO(deuda) : null;
        }

        public async Task<DeudaEconomicaSemaforoDTO> ObtenerSemaforoAsync(int expedienteId)
        {
            var deuda = await _context.DeudasEconomicas
                .Include(d => d.Expediente)
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            if (deuda == null)
            {
                return new DeudaEconomicaSemaforoDTO
                {
                    ExpedienteID = expedienteId,
                    CodigoExpediente = "N/A",
                    TieneDeuda = false,
                    Semaforo = "NO DEBE",
                    Instruccion = "Sin deuda económica registrada"
                };
            }

            return new DeudaEconomicaSemaforoDTO
            {
                ExpedienteID = expedienteId,
                CodigoExpediente = deuda.Expediente?.CodigoExpediente ?? "N/A",
                TieneDeuda = deuda.BloqueaRetiro(),
                Semaforo = deuda.BloqueaRetiro() ? "DEBE" : "NO DEBE",
                Instruccion = deuda.BloqueaRetiro()
                    ? "Dirigirse a Caja o Servicio Social"
                    : $"Deuda resuelta: {deuda.Estado}"
            };
        }

        public async Task<DeudaEconomicaDTO> MarcarSinDeudaAsync(int expedienteId, int usuarioId)
        {
            var deuda = await ObtenerDeudaOThrowAsync(expedienteId);
            var estadoAnterior = deuda.Estado;

            deuda.MarcarSinDeuda(usuarioId);

            await _repository.UpdateAsync(deuda);

            _logger.LogInformation(
                "Deuda económica marcada como Sin Deuda - ExpedienteID: {ExpedienteID}",
                expedienteId
            );

            await RegistrarAuditoriaAsync("DeudaEconomica", "MarcarSinDeuda", usuarioId, expedienteId,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado });

            return await MapToDTO(deuda);
        }

        public async Task<DeudaEconomicaDTO> MarcarLiquidadoAsync(int expedienteId, LiquidarDeudaEconomicaDTO dto)
        {
            var deuda = await ObtenerDeudaOThrowAsync(expedienteId);

            if (deuda.Estado != EstadoDeudaEconomica.Pendiente)
                throw new InvalidOperationException($"Solo se pueden liquidar deudas pendientes. Estado actual: {deuda.Estado}");

            var estadoAnterior = deuda.Estado;

            deuda.MarcarLiquidado(
                dto.NumeroBoleta,
                dto.MontoPagado,
                dto.UsuarioActualizacionID,
                dto.Observaciones
            );

            await _repository.UpdateAsync(deuda);

            var expediente = deuda.Expediente ?? await _context.Expedientes.FindAsync(expedienteId);

            _logger.LogInformation(
                "Deuda económica liquidada - ExpedienteID: {ExpedienteID}, Monto: S/ {Monto}",
                expedienteId, dto.MontoPagado
            );

            await _notificacionDeudaService.NotificarDeudaResueltaAsync(
                "Económica",
                "Liquidada",
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A"
            );

            await VerificarYNotificarDesbloqueoTotalAsync(
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A",
                "Económica"
            );

            await RegistrarAuditoriaAsync("DeudaEconomica", "Liquidar", dto.UsuarioActualizacionID, expedienteId,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado, Monto = dto.MontoPagado });

            return await MapToDTO(deuda);
        }

        public async Task<DeudaEconomicaDTO> AplicarExoneracionAsync(AplicarExoneracionDTO dto)
        {
            var deuda = await ObtenerDeudaOThrowAsync(dto.ExpedienteID);

            if (deuda.Estado == EstadoDeudaEconomica.SinDeuda)
                throw new InvalidOperationException("No se puede exonerar una deuda marcada como Sin Deuda");

            if (dto.MontoExonerado <= 0)
                throw new ArgumentException("El monto de exoneración debe ser mayor a cero", nameof(dto.MontoExonerado));

            _ = await _context.Users.FindAsync(dto.AsistentaSocialID)
                ?? throw new KeyNotFoundException($"Asistente Social {dto.AsistentaSocialID} no encontrado");

            var estadoAnterior = deuda.Estado;

            // Convertir string a enum TipoExoneracion
            if (!Enum.TryParse<TipoExoneracion>(dto.TipoExoneracion, out var tipoExoneracionEnum))
                throw new ArgumentException($"Tipo de exoneración inválido: {dto.TipoExoneracion}", nameof(dto.TipoExoneracion));

            deuda.AplicarExoneracion(
                dto.MontoExonerado,
                tipoExoneracionEnum,
                dto.AsistentaSocialID,
                dto.Observaciones,
                dto.NumeroBoletaExoneracion,
                dto.RutaPDFSustento,
                dto.NombreArchivoSustento,
                dto.TamañoArchivoSustento
            );

            await _repository.UpdateAsync(deuda);

            var expediente = deuda.Expediente ?? await _context.Expedientes.FindAsync(dto.ExpedienteID);

            _logger.LogInformation(
                "Exoneración aplicada - ExpedienteID: {ExpedienteID}, Monto: S/ {Monto}",
                dto.ExpedienteID, dto.MontoExonerado
            );

            await _notificacionDeudaService.NotificarDeudaResueltaAsync(
                "Económica",
                dto.TipoExoneracion,
                dto.ExpedienteID,
                expediente?.CodigoExpediente ?? "N/A"
            );

            await VerificarYNotificarDesbloqueoTotalAsync(
                dto.ExpedienteID,
                expediente?.CodigoExpediente ?? "N/A",
                "Económica"
            );

            await RegistrarAuditoriaAsync("DeudaEconomica", "Exonerar", dto.AsistentaSocialID, dto.ExpedienteID,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado, MontoExonerado = dto.MontoExonerado });

            return await MapToDTO(deuda);
        }

        public async Task<bool> BloqueaRetiroAsync(int expedienteId)
        {
            var deuda = await _context.DeudasEconomicas
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            return deuda?.BloqueaRetiro() ?? false;
        }

        public async Task<List<DeudaEconomicaDTO>> ObtenerDeudasPendientesAsync()
        {
            var deudas = await _repository.GetPendientesAsync();
            var dtos = new List<DeudaEconomicaDTO>();
            foreach (var deuda in deudas)
            {
                dtos.Add(await MapToDTO(deuda));
            }
            return dtos;
        }

        public async Task<List<DeudaEconomicaDTO>> ObtenerDeudasExoneradasAsync()
        {
            var deudas = await _context.DeudasEconomicas
                .Include(d => d.Expediente)
                .Include(d => d.UsuarioRegistro)
                .Include(d => d.AsistentaSocial)
                .Where(d => d.Estado == EstadoDeudaEconomica.Exonerado)
                .OrderByDescending(d => d.FechaExoneracion)
                .ToListAsync();

            var dtos = new List<DeudaEconomicaDTO>();
            foreach (var deuda in deudas)
            {
                dtos.Add(await MapToDTO(deuda));
            }

            return dtos;
        }

        public async Task<List<HistorialDeudaEconomicaDTO>> ObtenerHistorialAsync(int expedienteId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ExpedienteID == expedienteId && a.Modulo == "DeudaEconomica")
                .OrderBy(a => a.FechaHora)
                .Include(a => a.Usuario)
                .ToListAsync();

            return logs.Select(log => new HistorialDeudaEconomicaDTO
            {
                FechaHora = log.FechaHora,
                Accion = log.Accion,
                UsuarioNombre = log.Usuario?.NombreCompleto ?? "Sistema",
                Detalle = log.DatosDespues ?? "Sin detalles",
                IPOrigen = log.IPOrigen
            }).ToList();
        }

        public async Task<EstadisticasDeudaEconomicaDTO> ObtenerEstadisticasAsync()
        {
            var deudas = await _context.DeudasEconomicas.ToListAsync();

            // Calcular porcentaje de exoneración promedio
            var deudasConExoneracion = deudas.Where(d => d.MontoExonerado > 0 && d.MontoDeuda > 0).ToList();
            var promedioExoneracion = deudasConExoneracion.Any()
                ? deudasConExoneracion.Average(d => (d.MontoExonerado / d.MontoDeuda) * 100)
                : 0;

            return new EstadisticasDeudaEconomicaDTO
            {
                TotalDeudas = deudas.Count,
                DeudasPendientes = deudas.Count(d => d.Estado == EstadoDeudaEconomica.Pendiente),
                DeudasLiquidadas = deudas.Count(d => d.Estado == EstadoDeudaEconomica.Liquidado),
                DeudasExoneradas = deudas.Count(d => d.Estado == EstadoDeudaEconomica.Exonerado),
                MontoTotalDeudas = deudas.Sum(d => d.MontoDeuda),
                MontoTotalExonerado = deudas.Sum(d => d.MontoExonerado),
                MontoTotalPagado = deudas.Sum(d => d.MontoPagado),
                MontoTotalPendiente = deudas.Sum(d => d.MontoPendiente),
                PromedioExoneracion = promedioExoneracion
            };
        }

        private async Task<DeudaEconomica> ObtenerDeudaOThrowAsync(int expedienteId)
        {
            return await _context.DeudasEconomicas
                .Include(d => d.Expediente)
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId)
                ?? throw new KeyNotFoundException($"No existe deuda económica para el expediente {expedienteId}");
        }

        private async Task VerificarYNotificarDesbloqueoTotalAsync(int expedienteId, string codigoExpediente, string tipoDeudaResuelto)
        {
            var deudaSangre = await _context.DeudasSangre.FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);
            var deudaEconomica = await _context.DeudasEconomicas.FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            var deudasResueltas = 0;
            var totalDeudas = 0;

            if (deudaSangre != null)
            {
                totalDeudas++;
                if (!deudaSangre.BloqueaRetiro()) deudasResueltas++;
            }

            if (deudaEconomica != null)
            {
                totalDeudas++;
                if (!deudaEconomica.BloqueaRetiro()) deudasResueltas++;
            }

            if (totalDeudas == 0) return;

            if (deudasResueltas == totalDeudas)
            {
                await _notificacionDeudaService.NotificarDesbloqueoTotalAsync(
                    expedienteId,
                    codigoExpediente,
                    deudasResueltas,
                    totalDeudas
                );
            }
            else if (deudasResueltas > 0)
            {
                await _notificacionDeudaService.NotificarDesbloqueoParcialAsync(
                    expedienteId,
                    codigoExpediente,
                    tipoDeudaResuelto,
                    deudasResueltas,
                    totalDeudas
                );
            }
        }

        private async Task<DeudaEconomicaDTO> MapToDTO(DeudaEconomica deuda)
        {
            if (deuda.Expediente == null)
                await _context.Entry(deuda).Reference(d => d.Expediente).LoadAsync();
            if (deuda.UsuarioRegistro == null)
                await _context.Entry(deuda).Reference(d => d.UsuarioRegistro).LoadAsync();
            if (deuda.UsuarioActualizacion == null && deuda.UsuarioActualizacionID.HasValue)
                await _context.Entry(deuda).Reference(d => d.UsuarioActualizacion).LoadAsync();
            if (deuda.AsistentaSocial == null && deuda.AsistentaSocialID.HasValue)
                await _context.Entry(deuda).Reference(d => d.AsistentaSocial).LoadAsync();

            // Calcular porcentaje de exoneración
            var porcentajeExoneracion = deuda.MontoDeuda > 0
                ? (deuda.MontoExonerado / deuda.MontoDeuda) * 100
                : 0;

            return new DeudaEconomicaDTO
            {
                DeudaEconomicaID = deuda.DeudaEconomicaID,
                ExpedienteID = deuda.ExpedienteID,
                CodigoExpediente = deuda.Expediente?.CodigoExpediente ?? "N/A",
                Estado = deuda.Estado.ToString(),
                MontoDeuda = deuda.MontoDeuda,
                MontoExonerado = deuda.MontoExonerado,
                MontoPagado = deuda.MontoPagado,
                MontoPendiente = deuda.MontoPendiente,
                NumeroBoleta = deuda.NumeroBoleta,
                FechaPago = deuda.FechaPago,
                ObservacionesPago = deuda.ObservacionesPago,
                TipoExoneracion = deuda.TipoExoneracion.ToString(),
                NumeroBoletaExoneracion = deuda.NumeroBoletaExoneracion,
                FechaExoneracion = deuda.FechaExoneracion,
                ObservacionesExoneracion = deuda.ObservacionesExoneracion,
                PorcentajeExoneracion = porcentajeExoneracion,
                RutaPDFSustento = deuda.RutaPDFSustento,
                NombreArchivoSustento = deuda.NombreArchivoSustento,
                TamañoArchivoSustento = deuda.TamañoArchivoSustento,
                TamañoArchivoLegible = deuda.TamañoArchivoSustento.HasValue
                    ? FormatearTamañoArchivo(deuda.TamañoArchivoSustento.Value)
                    : null,
                AsistentaSocialID = deuda.AsistentaSocialID,
                AsistentaSocialNombre = deuda.AsistentaSocial?.NombreCompleto,
                UsuarioRegistroID = deuda.UsuarioRegistroID,
                UsuarioRegistroNombre = deuda.UsuarioRegistro?.NombreCompleto ?? "N/A",
                FechaRegistro = deuda.FechaRegistro,
                UsuarioActualizacionID = deuda.UsuarioActualizacionID,
                UsuarioActualizacionNombre = deuda.UsuarioActualizacion?.NombreCompleto,
                FechaActualizacion = deuda.FechaActualizacion,
                BloqueaRetiro = deuda.BloqueaRetiro(),
                SemaforoSupVigilancia = deuda.BloqueaRetiro() ? "DEBE" : "NO DEBE",
                ResumenDetallado = GenerarResumenDetallado(deuda, porcentajeExoneracion),
                ValidacionSustento = ValidarSustento(deuda)
            };
        }

        private static string GenerarResumenDetallado(DeudaEconomica deuda, decimal porcentajeExoneracion)
        {
            return deuda.Estado switch
            {
                EstadoDeudaEconomica.SinDeuda => "Sin deuda registrada",
                EstadoDeudaEconomica.Pendiente => $"Deuda pendiente de S/ {deuda.MontoPendiente:N2}",
                EstadoDeudaEconomica.Liquidado => $"Deuda liquidada con pago de S/ {deuda.MontoPagado:N2}",
                EstadoDeudaEconomica.Exonerado => deuda.MontoExonerado >= deuda.MontoDeuda
                    ? $"Deuda 100% exonerada por {deuda.TipoExoneracion}"
                    : $"Exonerado {porcentajeExoneracion:N0}% (S/ {deuda.MontoExonerado:N2}), pendiente S/ {deuda.MontoPendiente:N2}",
                _ => "Estado desconocido"
            };
        }

        private static string ValidarSustento(DeudaEconomica deuda)
        {
            if (deuda.Estado == EstadoDeudaEconomica.Exonerado)
            {
                return string.IsNullOrEmpty(deuda.RutaPDFSustento)
                    ? "FALTA ADJUNTAR FICHA SOCIOECONÓMICA"
                    : "Sustento adjunto";
            }
            return "N/A";
        }

        private static string FormatearTamañoArchivo(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB"];
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private async Task RegistrarAuditoriaAsync(string modulo, string accion, int usuarioId, int? expedienteId, object? datosAntes, object? datosDespues)
        {
            try
            {
                var log = AuditLog.CrearLogPersonalizado(modulo, accion, usuarioId, expedienteId, datosAntes, datosDespues, null);
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría");
            }
        }
    }
}