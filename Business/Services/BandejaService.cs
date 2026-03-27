using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de gestión de Bandejas del mortuorio.
    ///
    /// CAMBIOS v2 (Fase 5 — Modal Mantenimiento):
    /// - IniciarMantenimientoAsync: acepta IniciarMantenimientoDTO completo
    ///   + crea registro en BandejaHistorial (AccionBandeja.InicioMantenimiento)
    /// - FinalizarMantenimientoAsync: crea registro en BandejaHistorial (AccionBandeja.FinMantenimiento)
    /// - MapToBandejaDTO: mapea los 6 campos nuevos de mantenimiento
    /// </summary>
    public class BandejaService(
        IBandejaRepository bandejaRepo,
        IBandejaHistorialRepository ocupacionRepo,
        IExpedienteRepository expedienteRepo,
        IStateMachineService stateMachine,
        INotificacionBandejaService notificacionService,
        ILogger<BandejaService> logger,
        ApplicationDbContext context) : IBandejaService
    {
        private readonly IBandejaRepository _bandejaRepo = bandejaRepo;
        private readonly IBandejaHistorialRepository _ocupacionRepo = ocupacionRepo;
        private readonly IExpedienteRepository _expedienteRepo = expedienteRepo;
        private readonly IStateMachineService _stateMachine = stateMachine;
        private readonly INotificacionBandejaService _notificacionService = notificacionService;
        private readonly ILogger<BandejaService> _logger = logger;
        private readonly ApplicationDbContext _context = context;

        // ─── Sin cambios: GetOcupacionDashboardAsync, GetByIdAsync,
        //                  GetDisponiblesAsync, AsignarBandejaAsync,
        //                  LiberarBandejaAsync, GetEstadisticasAsync,
        //                  LiberarManualmenteAsync ────────────────────

        public async Task<List<BandejaDTO>> GetOcupacionDashboardAsync()
        {
            var bandejas = await _bandejaRepo.GetAllAsync();
            return bandejas.Select(MapToBandejaDTO).ToList();
        }

        public async Task<BandejaDTO?> GetByIdAsync(int id)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(id);
            return bandeja == null ? null : MapToBandejaDTO(bandeja);
        }

        public async Task<List<BandejaDisponibleDTO>> GetDisponiblesAsync()
        {
            var bandejas = await _bandejaRepo.GetDisponiblesAsync();
            return [.. bandejas.Select(b => new BandejaDisponibleDTO
            {
                BandejaID = b.BandejaID,
                Codigo = b.Codigo
            })];
        }

        public async Task<BandejaDTO> AsignarBandejaAsync(AsignarBandejaDTO dto, int usuarioAsignaId)
        {
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID)
                ?? throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");
            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID)
                ?? throw new InvalidOperationException($"Bandeja ID {dto.BandejaID} no encontrada.");

            if (!bandeja.EstaDisponible())
                throw new InvalidOperationException(
                    $"La bandeja {bandeja.Codigo} no está disponible. Estado actual: {bandeja.Estado}");

            if (!_stateMachine.CanFire(expediente, TriggerExpediente.AsignarBandeja))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} no puede ser asignado a bandeja " +
                    $"desde el estado '{expediente.EstadoActual}'.");

            var estadoAnterior = expediente.EstadoActual;

            bandeja.Ocupar(expediente.ExpedienteID, usuarioAsignaId);
            await _bandejaRepo.UpdateAsync(bandeja);

            await _stateMachine.FireAsync(expediente, TriggerExpediente.AsignarBandeja);
            expediente.BandejaActualID = bandeja.BandejaID;
            _context.Entry(expediente).Property(e => e.BandejaActualID).IsModified = true;
            await _expedienteRepo.UpdateAsync(expediente);

            var ocupacion = new BandejaHistorial
            {
                BandejaID = bandeja.BandejaID,
                ExpedienteID = expediente.ExpedienteID,
                UsuarioAsignadorID = usuarioAsignaId,
                Accion = AccionBandeja.Asignacion,
                Observaciones = dto.Observaciones,
                FechaHoraIngreso = bandeja.FechaHoraAsignacion ?? DateTime.Now
            };
            await _ocupacionRepo.CreateAsync(ocupacion);

            _logger.LogInformation(
                "Bandeja {Codigo} asignada a Expediente {CodigoExp} por Usuario {UID}. " +
                "Estado: {Antes} → {Despues}",
                bandeja.Codigo, expediente.CodigoExpediente, usuarioAsignaId,
                estadoAnterior, expediente.EstadoActual);

            await RegistrarAuditoriaAsync("AsignarBandeja", usuarioAsignaId, expediente.ExpedienteID,
                new { EstadoExpediente = estadoAnterior.ToString(), BandejaID = (int?)null },
                new
                {
                    EstadoExpediente = expediente.EstadoActual.ToString(),
                    BandejaID = bandeja.BandejaID,
                    Codigo = bandeja.Codigo
                });

            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaAsignadaAsync(bandejaDTO);

            var estadisticas = await GetEstadisticasAsync();
            await _notificacionService.VerificarYNotificarOcupacionCriticaAsync(estadisticas);

            return bandejaDTO;
        }

        public async Task LiberarBandejaAsync(int expedienteId, int usuarioLiberaId)
        {
            var bandeja = await _bandejaRepo.GetByExpedienteIdAsync(expedienteId);
            if (bandeja == null)
            {
                _logger.LogWarning("No se encontró bandeja ocupada por Expediente {ID}.", expedienteId);
                return;
            }

            var ocupacion = await _ocupacionRepo.GetActualByExpedienteIdAsync(expedienteId);

            bandeja.Liberar(usuarioLiberaId);
            await _bandejaRepo.UpdateAsync(bandeja);

            var expediente = await _expedienteRepo.GetByIdAsync(expedienteId);
            if (expediente != null)
            {
                expediente.BandejaActualID = null;
                await _expedienteRepo.UpdateAsync(expediente);
            }

            if (ocupacion != null)
            {
                ocupacion.RegistrarSalida(usuarioLiberaId, "Salida registrada por Vigilante");
                await _ocupacionRepo.UpdateAsync(ocupacion);
            }

            await RegistrarAuditoriaAsync("LiberarBandeja", usuarioLiberaId, expedienteId,
                new { Codigo = bandeja.Codigo, Estado = "Ocupada" },
                new { Codigo = bandeja.Codigo, Estado = "Disponible" });

            await _notificacionService.NotificarBandejaLiberadaAsync(MapToBandejaDTO(bandeja));
        }

        public async Task<EstadisticasBandejaDTO> GetEstadisticasAsync()
        {
            var stats = await _bandejaRepo.GetEstadisticasAsync();
            return new EstadisticasBandejaDTO
            {
                Total = stats.Total,
                Disponibles = stats.Disponibles,
                Ocupadas = stats.Ocupadas,
                EnMantenimiento = stats.EnMantenimiento,
                FueraDeServicio = stats.FueraDeServicio,
                PorcentajeOcupacion = stats.PorcentajeOcupacion,
                ConAlerta24h = stats.ConAlerta24h,
                ConAlerta48h = stats.ConAlerta48h
            };
        }

        // ═══════════════════════════════════════════════════════════
        // MANTENIMIENTO — ACTUALIZADO v2
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Inicia el mantenimiento de una bandeja con datos completos del modal SGM.
        /// CAMBIOS v2:
        /// - Acepta IniciarMantenimientoDTO en lugar de string
        /// - Crea registro en BandejaHistorial (AccionBandeja.InicioMantenimiento)
        /// </summary>
        public async Task<BandejaDTO> IniciarMantenimientoAsync(
            int bandejaId,
            IniciarMantenimientoDTO dto,
            int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId)
                ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            var estadoAnterior = bandeja.Estado;

            // Llamar al método actualizado de la entidad
            bandeja.IniciarMantenimiento(
                motivo: dto.Motivo,
                detalle: dto.Detalle,
                fechaInicio: dto.FechaInicio,
                fechaEstimadaFin: dto.FechaEstimadaFin,
                responsableExterno: dto.ResponsableExterno,
                usuarioRegistraId: usuarioId);

            await _bandejaRepo.UpdateAsync(bandeja);

            // Registrar en BandejaHistorial para auditoría completa
            var historial = new BandejaHistorial
            {
                BandejaID = bandeja.BandejaID,
                ExpedienteID = null, // Mantenimiento no tiene expediente
                UsuarioAsignadorID = usuarioId,
                Accion = AccionBandeja.InicioMantenimiento,
                Observaciones = $"[{dto.Motivo}] {dto.Detalle} | " +
                                     $"Responsable: {dto.ResponsableExterno ?? "No especificado"} | " +
                                     $"Estimado fin: {dto.FechaEstimadaFin?.ToString("dd/MM/yyyy HH:mm") ?? "No indicado"}",
                FechaHoraIngreso = bandeja.FechaInicioMantenimiento ?? DateTime.Now
            };
            await _ocupacionRepo.CreateAsync(historial);

            _logger.LogInformation(
                "Bandeja {Codigo} → Mantenimiento. Motivo: {Motivo}. Usuario: {UID}",
                bandeja.Codigo, dto.Motivo, usuarioId);

            await RegistrarAuditoriaAsync("IniciarMantenimiento", usuarioId, null,
                new { Codigo = bandeja.Codigo, Estado = estadoAnterior.ToString() },
                new
                {
                    Codigo = bandeja.Codigo,
                    Estado = bandeja.Estado.ToString(),
                    Motivo = dto.Motivo,
                    Detalle = dto.Detalle,
                    FechaInicio = bandeja.FechaInicioMantenimiento,
                    FechaEstimadaFin = dto.FechaEstimadaFin,
                    ResponsableExterno = dto.ResponsableExterno
                });

            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaEnMantenimientoAsync(bandejaDTO);

            return bandejaDTO;
        }

        /// <summary>
        /// Finaliza el mantenimiento de una bandeja.
        /// CAMBIOS v2: Crea registro en BandejaHistorial (AccionBandeja.FinMantenimiento).
        /// </summary>
        public async Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId)
                ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            var estadoAnterior = bandeja.Estado;
            var motivoAnterior = bandeja.MotivoMantenimiento;
            var detalleAnterior = bandeja.DetalleMantenimiento;
            var inicioAnterior = bandeja.FechaInicioMantenimiento;
            var responsableAnterior = bandeja.ResponsableMantenimiento;

            bandeja.FinalizarMantenimiento();
            await _bandejaRepo.UpdateAsync(bandeja);

            // Registrar en BandejaHistorial
            var historial = new BandejaHistorial
            {
                BandejaID = bandeja.BandejaID,
                ExpedienteID = null,
                UsuarioAsignadorID = usuarioId,
                Accion = AccionBandeja.FinMantenimiento,
                Observaciones = $"Mantenimiento finalizado. Motivo original: [{motivoAnterior}] {detalleAnterior}",
                FechaHoraIngreso = inicioAnterior ?? DateTime.Now,
                // Registrar la salida como ahora
                FechaHoraSalida = DateTime.Now
            };
            await _ocupacionRepo.CreateAsync(historial);

            _logger.LogInformation(
                "Mantenimiento de Bandeja {Codigo} finalizado por Usuario {UID}",
                bandeja.Codigo, usuarioId);

            await RegistrarAuditoriaAsync("FinalizarMantenimiento", usuarioId, null,
                new
                {
                    Codigo = bandeja.Codigo,
                    Estado = estadoAnterior.ToString(),
                    Motivo = motivoAnterior,
                    Detalle = detalleAnterior,
                    FechaInicio = inicioAnterior,
                    ResponsableExterno = responsableAnterior
                },
                new { Codigo = bandeja.Codigo, Estado = bandeja.Estado.ToString() });

            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaSalidaMantenimientoAsync(bandejaDTO);

            return bandejaDTO;
        }

        public async Task<BandejaDTO> LiberarManualmenteAsync(LiberarBandejaManualDTO dto)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID)
                ?? throw new KeyNotFoundException($"Bandeja con ID {dto.BandejaID} no encontrada.");

            if (bandeja.Estado != EstadoBandeja.Ocupada)
                throw new InvalidOperationException(
                    $"La bandeja {bandeja.Codigo} no está ocupada. Estado actual: {bandeja.Estado}");

            _logger.LogWarning(
                "Liberación MANUAL de Bandeja {Codigo}. Usuario: {UID}. Motivo: {Motivo}",
                bandeja.Codigo, dto.UsuarioLiberaID, dto.MotivoLiberacion);

            var expedienteID = bandeja.ExpedienteID!.Value;

            bandeja.Liberar(dto.UsuarioLiberaID);
            bandeja.Observaciones =
                $"[LIBERACIÓN MANUAL] Motivo: {dto.MotivoLiberacion}. {dto.Observaciones}";
            await _bandejaRepo.UpdateAsync(bandeja);

            var expediente = await _expedienteRepo.GetByIdAsync(expedienteID);
            if (expediente != null)
            {
                expediente.BandejaActualID = null;
                await _expedienteRepo.UpdateAsync(expediente);
            }

            var ocupacionActiva = await _ocupacionRepo.GetActualByExpedienteIdAsync(expedienteID);
            if (ocupacionActiva != null)
            {
                ocupacionActiva.RegistrarSalida(
                    dto.UsuarioLiberaID,
                    $"[LIBERACIÓN MANUAL] Motivo: {dto.MotivoLiberacion}. {dto.Observaciones}");
                ocupacionActiva.Accion = AccionBandeja.LiberacionManual;
                await _ocupacionRepo.UpdateAsync(ocupacionActiva);
            }

            return MapToBandejaDTO(bandeja);
        }

        // ═══════════════════════════════════════════════════════════
        // MAPEO — ACTUALIZADO v2
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Mapea Bandeja → BandejaDTO.
        /// CAMBIOS v2: incluye campos de mantenimiento y formato de tiempo mejorado.
        /// </summary>
        private static BandejaDTO MapToBandejaDTO(Bandeja bandeja)
        {
            var dto = new BandejaDTO
            {
                BandejaID = bandeja.BandejaID,
                Codigo = bandeja.Codigo,
                Estado = bandeja.Estado.ToString(),
                Observaciones = bandeja.Observaciones,
                FechaCreacion = bandeja.FechaCreacion,
                FechaModificacion = bandeja.FechaModificacion,
                UsuarioLiberaNombre = bandeja.UsuarioLibera?.NombreCompleto,
                FechaHoraLiberacion = bandeja.FechaHoraLiberacion,

                // Campos de mantenimiento (nuevos v2)
                MotivoMantenimiento = bandeja.MotivoMantenimiento,
                DetalleMantenimiento = bandeja.DetalleMantenimiento,
                FechaInicioMantenimiento = bandeja.FechaInicioMantenimiento,
                FechaEstimadaFinMantenimiento = bandeja.FechaEstimadaFinMantenimiento,
                ResponsableMantenimiento = bandeja.ResponsableMantenimiento,
                UsuarioRegistraMantenimientoNombre =
                    bandeja.UsuarioRegistraMantenimiento?.NombreCompleto
            };

            if (bandeja.EstaOcupada())
            {
                dto.ExpedienteID = bandeja.ExpedienteID;
                dto.CodigoExpediente = bandeja.Expediente?.CodigoExpediente;
                dto.NombrePaciente = bandeja.Expediente?.NombreCompleto;
                dto.UsuarioAsignaNombre = bandeja.UsuarioAsigna?.NombreCompleto;
                dto.FechaHoraAsignacion = bandeja.FechaHoraAsignacion;

                var t = bandeja.TiempoOcupada();
                if (t.HasValue)
                {
                    dto.TiempoOcupada = FormatearTiempo(t.Value);
                    dto.TieneAlerta = t.Value.TotalHours >= 24;
                }
            }

            return dto;
        }

        /// <summary>
        /// Formatea un TimeSpan en formato legible para el frontend.
        /// Si es menor a 24h: "3h 20m"
        /// Si es mayor o igual a 24h: "2d 5h 10m"
        /// Fix del bug anterior que mostraba "433h 12m".
        /// </summary>
        private static string FormatearTiempo(TimeSpan t)
        {
            if (t.TotalHours < 24)
                return $"{(int)t.TotalHours}h {t.Minutes}m";

            int dias = (int)t.TotalDays;
            int horas = t.Hours;
            int minutos = t.Minutes;

            return minutos > 0
                ? $"{dias}d {horas}h {minutos}m"
                : $"{dias}d {horas}h";
        }

        private async Task RegistrarAuditoriaAsync(
            string accion, int usuarioId, int? expedienteId,
            object datosAntes, object datosDespues)
        {
            try
            {
                var log = AuditLog.CrearLogPersonalizado(
                    "Bandeja", accion, usuarioId, expedienteId,
                    datosAntes, datosDespues, null);
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría para acción {Accion}", accion);
            }
        }
    }
}