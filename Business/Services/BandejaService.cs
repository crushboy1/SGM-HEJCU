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
    /// Responsabilidades:
    /// 1. Asignar/liberar bandejas a expedientes
    /// 2. Gestionar mantenimiento de bandejas
    /// 3. Calcular estadísticas de ocupación
    /// 4. Registrar auditoría completa en AuditLog
    /// 
    /// Notificaciones SignalR delegadas a INotificacionBandejaService:
    /// - NotificarBandejaAsignadaAsync
    /// - NotificarBandejaLiberadaAsync
    /// - NotificarBandejaEnMantenimientoAsync
    /// - NotificarBandejaSalidaMantenimientoAsync
    /// - VerificarYNotificarOcupacionCriticaAsync
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

        public async Task<List<BandejaDTO>> GetOcupacionDashboardAsync()
        {
            var bandejas = await _bandejaRepo.GetAllAsync();
            var dtos = bandejas.Select(MapToBandejaDTO).ToList();
            return dtos;
        }

        public async Task<BandejaDTO?> GetByIdAsync(int id)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(id);
            if (bandeja == null) return null;
            return MapToBandejaDTO(bandeja);
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
            // 1. Validar Entidades
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID)
                ?? throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");
            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID)
                ?? throw new InvalidOperationException($"Bandeja ID {dto.BandejaID} no encontrada.");

            // 2. Validar Lógica de Negocio
            if (!bandeja.EstaDisponible())
                throw new InvalidOperationException($"La bandeja {bandeja.Codigo} no está disponible. Estado actual: {bandeja.Estado}");

            // 3. Validar Máquina de Estados
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.AsignarBandeja))
            {
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser asignado a bandeja.");
            }

            var estadoAnterior = expediente.EstadoActual;

            // 4. Ejecutar Transacción
            // a. Ocupar la bandeja
            bandeja.Ocupar(expediente.ExpedienteID, usuarioAsignaId);
            await _bandejaRepo.UpdateAsync(bandeja);

            // b. Cambiar estado del expediente
            await _stateMachine.FireAsync(expediente, TriggerExpediente.AsignarBandeja);
            expediente.BandejaActualID = bandeja.BandejaID;
            _context.Entry(expediente).Property(e => e.BandejaActualID).IsModified = true;
            await _expedienteRepo.UpdateAsync(expediente);

            // c. Crear registro de auditoría en BandejaHistorial
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
                "Bandeja {CodigoBandeja} asignada a Expediente {CodigoExpediente} por Usuario ID {UsuarioID}. Estado: {EstadoAnterior} -> {EstadoNuevo}",
                bandeja.Codigo, expediente.CodigoExpediente, usuarioAsignaId, estadoAnterior, expediente.EstadoActual
            );

            // d. Registrar en AuditLog
            await RegistrarAuditoriaAsync(
                accion: "AsignarBandeja",
                usuarioId: usuarioAsignaId,
                expedienteId: expediente.ExpedienteID,
                datosAntes: new
                {
                    EstadoExpediente = estadoAnterior.ToString(),
                    BandejaID = (int?)null,
                    CodigoBandeja = (string?)null
                },
                datosDespues: new
                {
                    EstadoExpediente = expediente.EstadoActual.ToString(),
                    BandejaID = bandeja.BandejaID,
                    CodigoBandeja = bandeja.Codigo,
                    Observaciones = dto.Observaciones
                }
            );

            // 5. NOTIFICACIONES SIGNALR (delegadas al servicio centralizado)
            var bandejaDTO = MapToBandejaDTO(bandeja);

            await _notificacionService.NotificarBandejaAsignadaAsync(bandejaDTO);

            // Verificar y enviar alerta de ocupación si supera 70%
            var estadisticas = await GetEstadisticasAsync();
            await _notificacionService.VerificarYNotificarOcupacionCriticaAsync(estadisticas);

            // 6. Devolver DTO
            return bandejaDTO;
        }

        public async Task LiberarBandejaAsync(int expedienteId, int usuarioLiberaId)
        {
            _logger.LogInformation("Iniciando liberación de bandeja para Expediente ID {ExpedienteID}", expedienteId);

            // 1. Buscar la bandeja ocupada por este expediente
            var bandeja = await _bandejaRepo.GetByExpedienteIdAsync(expedienteId);
            if (bandeja == null)
            {
                _logger.LogWarning("No se encontró bandeja ocupada por Expediente ID {ExpedienteID}. La liberación no es necesaria.", expedienteId);
                return;
            }

            var codigoBandeja = bandeja.Codigo;
            var bandejaId = bandeja.BandejaID;

            // 2. Buscar el registro de ocupación activo
            var ocupacion = await _ocupacionRepo.GetActualByExpedienteIdAsync(expedienteId);
            if (ocupacion == null)
            {
                _logger.LogError(
                    "INCONSISTENCIA DE DATOS: La bandeja {CodigoBandeja} figura ocupada por Exp {ExpedienteID} pero no existe registro de ocupación activa.",
                    bandeja.Codigo, expedienteId
                );
            }

            // 3. Ejecutar Transacción
            // a. Liberar la bandeja
            bandeja.Liberar(usuarioLiberaId);
            await _bandejaRepo.UpdateAsync(bandeja);

            // b. Cerrar el registro de ocupación

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

            _logger.LogInformation(
                "Bandeja {CodigoBandeja} liberada exitosamente por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioLiberaId
            );

            // c. Registrar en AuditLog
            await RegistrarAuditoriaAsync(
                accion: "LiberarBandeja",
                usuarioId: usuarioLiberaId,
                expedienteId: expedienteId,
                datosAntes: new
                {
                    BandejaID = bandejaId,
                    CodigoBandeja = codigoBandeja,
                    Estado = "Ocupada",
                    ExpedienteID = expedienteId
                },
                datosDespues: new
                {
                    BandejaID = bandejaId,
                    CodigoBandeja = codigoBandeja,
                    Estado = "Disponible",
                    ExpedienteID = (int?)null
                }
            );

            // 4. NOTIFICACIONES SIGNALR (delegadas al servicio centralizado)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaLiberadaAsync(bandejaDTO);
        }

        public async Task<EstadisticasBandejaDTO> GetEstadisticasAsync()
        {
            var stats = await _bandejaRepo.GetEstadisticasAsync();

            // Mapeo 1:1
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

        public async Task<BandejaDTO> IniciarMantenimientoAsync(int bandejaId, string observaciones, int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId)
                ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            var estadoAnterior = bandeja.Estado;

            bandeja.IniciarMantenimiento(observaciones);
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation(
                "Bandeja {CodigoBandeja} puesta en Mantenimiento por Usuario ID {UsuarioID}. Motivo: {Motivo}",
                bandeja.Codigo, usuarioId, observaciones
            );

            // Registrar en AuditLog
            await RegistrarAuditoriaAsync(
                accion: "IniciarMantenimiento",
                usuarioId: usuarioId,
                expedienteId: null,
                datosAntes: new
                {
                    BandejaID = bandejaId,
                    Codigo = bandeja.Codigo,
                    Estado = estadoAnterior.ToString()
                },
                datosDespues: new
                {
                    BandejaID = bandejaId,
                    Codigo = bandeja.Codigo,
                    Estado = bandeja.Estado.ToString(),
                    Observaciones = observaciones
                }
            );

            // NOTIFICACIONES SIGNALR (delegadas al servicio centralizado)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaEnMantenimientoAsync(bandejaDTO);

            return bandejaDTO;
        }

        public async Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId)
                ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            var estadoAnterior = bandeja.Estado;

            bandeja.FinalizarMantenimiento();
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation(
                "Mantenimiento de Bandeja {CodigoBandeja} finalizado por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioId
            );

            // Registrar en AuditLog
            await RegistrarAuditoriaAsync(
                accion: "FinalizarMantenimiento",
                usuarioId: usuarioId,
                expedienteId: null,
                datosAntes: new
                {
                    BandejaID = bandejaId,
                    Codigo = bandeja.Codigo,
                    Estado = estadoAnterior.ToString()
                },
                datosDespues: new
                {
                    BandejaID = bandejaId,
                    Codigo = bandeja.Codigo,
                    Estado = bandeja.Estado.ToString()
                }
            );

            // NOTIFICACIONES SIGNALR (delegadas al servicio centralizado)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _notificacionService.NotificarBandejaSalidaMantenimientoAsync(bandejaDTO);

            return bandejaDTO;
        }
        public async Task<BandejaDTO> LiberarManualmenteAsync(LiberarBandejaManualDTO dto)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID)
                ?? throw new KeyNotFoundException($"Bandeja con ID {dto.BandejaID} no encontrada");

            if (bandeja.Estado != EstadoBandeja.Ocupada)
                throw new InvalidOperationException($"La bandeja {bandeja.Codigo} no está ocupada. Estado actual: {bandeja.Estado}");

            _logger.LogWarning("Liberación MANUAL de bandeja {Codigo}. Usuario: {UsuarioID}, Motivo: {Motivo}",
                bandeja.Codigo, dto.UsuarioLiberaID, dto.MotivoLiberacion);

            // Guardar datos antes de liberar (para historial y auditoría)
            var expedienteID = bandeja.ExpedienteID!.Value;

            // Liberar la bandeja
            bandeja.Liberar(dto.UsuarioLiberaID);
            bandeja.Observaciones = $"[LIBERACIÓN MANUAL] Motivo: {dto.MotivoLiberacion}. {dto.Observaciones}";
            await _bandejaRepo.UpdateAsync(bandeja);

            // Limpiar BandejaActualID del expediente
            var expediente = await _expedienteRepo.GetByIdAsync(expedienteID);
            if (expediente != null)
            {
                expediente.BandejaActualID = null;
                await _expedienteRepo.UpdateAsync(expediente);
            }

            // Registrar en historial
            var ocupacionActiva = await _ocupacionRepo.GetActualByExpedienteIdAsync(expedienteID);
            if (ocupacionActiva != null)
            {
                ocupacionActiva.RegistrarSalida(
                    dto.UsuarioLiberaID,
                    $"[LIBERACIÓN MANUAL] Motivo: {dto.MotivoLiberacion}. {dto.Observaciones}"
                );
                ocupacionActiva.Accion = AccionBandeja.LiberacionManual;
                await _ocupacionRepo.UpdateAsync(ocupacionActiva);
            }

            _logger.LogInformation("Bandeja {Codigo} liberada manualmente. Expediente: {ExpedienteID}",
                bandeja.Codigo, expedienteID);

            // Mapear y retornar
            return MapToBandejaDTO(bandeja);
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS PRIVADOS - MAPEO Y AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Mapea una entidad Bandeja a su DTO correspondiente.
        /// </summary>
        private static BandejaDTO MapToBandejaDTO(Bandeja bandeja)
        {
            var dto = new BandejaDTO
            {
                BandejaID = bandeja.BandejaID,
                Codigo = bandeja.Codigo,
                Estado = bandeja.Estado.ToString(),
                Observaciones = bandeja.Observaciones,

                // Auditoría
                FechaCreacion = bandeja.FechaCreacion,
                FechaModificacion = bandeja.FechaModificacion,
                UsuarioLiberaNombre = bandeja.UsuarioLibera?.NombreCompleto,
                FechaHoraLiberacion = bandeja.FechaHoraLiberacion
            };

            if (bandeja.EstaOcupada())
            {
                dto.ExpedienteID = bandeja.ExpedienteID;
                dto.CodigoExpediente = bandeja.Expediente?.CodigoExpediente;
                dto.NombrePaciente = bandeja.Expediente?.NombreCompleto;
                dto.UsuarioAsignaNombre = bandeja.UsuarioAsigna?.NombreCompleto;
                dto.FechaHoraAsignacion = bandeja.FechaHoraAsignacion;

                var tiempoOcupada = bandeja.TiempoOcupada();
                if (tiempoOcupada.HasValue)
                {
                    dto.TiempoOcupada = $"{(int)tiempoOcupada.Value.TotalHours}h {tiempoOcupada.Value.Minutes}m";
                    dto.TieneAlerta = tiempoOcupada.Value.TotalHours >= 24;
                }
            }

            return dto;
        }

        /// <summary>
        /// Registra operación en AuditLog.
        /// </summary>
        private async Task RegistrarAuditoriaAsync(string accion, int usuarioId, int? expedienteId, object datosAntes, object datosDespues)
        {
            try
            {
                var log = AuditLog.CrearLogPersonalizado("Bandeja", accion, usuarioId, expedienteId, datosAntes, datosDespues, null);
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