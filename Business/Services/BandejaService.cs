using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;
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
    /// 4. Enviar alertas en tiempo real por SignalR cuando:
    ///    - Ocupación supera 70%
    ///    - Una bandeja cambia de estado (asignada/liberada)
    /// 
    /// Integración con SignalR:
    /// - RecibirAlertaOcupacion: Alerta específica con estadísticas completas
    /// - RecibirNotificacion: Notificación genérica para dropdown
    /// - RecibirActualizacionBandeja: Actualización de bandeja individual (mapa en vivo)
    /// </summary>
    public class BandejaService(
        IBandejaRepository bandejaRepo,
        IOcupacionBandejaRepository ocupacionRepo,
        IExpedienteRepository expedienteRepo,
        IStateMachineService stateMachine,
        ILogger<BandejaService> logger,
        IHubContext<SgmHub, ISgmClient> hubContext) : IBandejaService
    {
        private readonly IBandejaRepository _bandejaRepo = bandejaRepo;
        private readonly IOcupacionBandejaRepository _ocupacionRepo = ocupacionRepo;
        private readonly IExpedienteRepository _expedienteRepo = expedienteRepo;
        private readonly IStateMachineService _stateMachine = stateMachine;
        private readonly ILogger<BandejaService> _logger = logger;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext = hubContext;

        // Roles que deben recibir alertas de ocupación
        private static readonly string[] RolesAlertaOcupacion = ["Admision","JefeGuardia","Administrador","VigilanteSupervisor"];

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
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID) ?? throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");
            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID) ?? throw new InvalidOperationException($"Bandeja ID {dto.BandejaID} no encontrada.");

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
            await _expedienteRepo.UpdateAsync(expediente);

            // c. Crear registro de auditoría en OcupacionBandeja
            var ocupacion = new OcupacionBandeja
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

            // 5. NOTIFICACIONES SIGNALR

            // 5a. Enviar actualización de bandeja individual (para actualizar mapa en vivo)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

            _logger.LogDebug(
                "SignalR: Actualización de bandeja {CodigoBandeja} enviada a todos los clientes",
                bandeja.Codigo
            );

            // 5b. Verificar y enviar alerta de ocupación (si supera 70%)
            await CheckOcupacionAlertAsync();

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
            if (ocupacion != null)
            {
                ocupacion.RegistrarSalida(usuarioLiberaId, "Salida registrada por Vigilante");
                await _ocupacionRepo.UpdateAsync(ocupacion);
            }

            _logger.LogInformation(
                "Bandeja {CodigoBandeja} liberada exitosamente por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioLiberaId
            );

            // 4. NOTIFICACIONES SIGNALR

            // 4a. Enviar actualización de bandeja individual (para actualizar mapa en vivo)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

            _logger.LogDebug(
                "SignalR: Actualización de bandeja {CodigoBandeja} (liberada) enviada a todos los clientes",
                bandeja.Codigo
            );
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
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId) ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");
            bandeja.IniciarMantenimiento(observaciones);
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation(
                "Bandeja {CodigoBandeja} puesta en Mantenimiento por Usuario ID {UsuarioID}. Motivo: {Motivo}",
                bandeja.Codigo, usuarioId, observaciones
            );

            // Enviar actualización de bandeja (mapa en vivo)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

            return bandejaDTO;
        }

        public async Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId) ?? throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");
            bandeja.FinalizarMantenimiento();
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation(
                "Mantenimiento de Bandeja {CodigoBandeja} finalizado por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioId
            );

            // Enviar actualización de bandeja (mapa en vivo)
            var bandejaDTO = MapToBandejaDTO(bandeja);
            await _hubContext.Clients.All.RecibirActualizacionBandeja(bandejaDTO);

            return bandejaDTO;
        }

        // --- Métodos Privados de Mapeo y Alertas ---

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
                Observaciones = bandeja.Observaciones
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
        /// Verifica el porcentaje de ocupación y envía alertas por SignalR si supera el 70%.
        /// Envía dos notificaciones:
        /// 1. RecibirAlertaOcupacion: Alerta específica con estadísticas completas (actualiza dashboard)
        /// 2. RecibirNotificacion: Notificación genérica para dropdown del header
        /// </summary>
        private async Task CheckOcupacionAlertAsync()
        {
            try
            {
                var statsDTO = await GetEstadisticasAsync();

                // Umbral de alerta: 70%
                if (statsDTO.PorcentajeOcupacion > 70)
                {
                    _logger.LogWarning(
                        "ALERTA DE OCUPACIÓN: El mortuorio ha superado el 70% de capacidad. Ocupación actual: {PorcentajeOcupacion}%",
                        statsDTO.PorcentajeOcupacion.ToString("F2")
                    );

                    // 1. Enviar alerta específica con estadísticas completas
                    await _hubContext.Clients
                        .Groups(RolesAlertaOcupacion)
                        .RecibirAlertaOcupacion(statsDTO);

                    _logger.LogInformation(
                        "SignalR: Alerta de ocupación enviada a roles: {Roles}",
                        string.Join(", ", RolesAlertaOcupacion)
                    );

                    // 2. Enviar notificación genérica (para dropdown en header)
                    var notificacion = new NotificacionDTO
                    {
                        Titulo = "Capacidad Crítica del Mortuorio",
                        Mensaje = $"El mortuorio ha alcanzado el {statsDTO.PorcentajeOcupacion:F1}% de ocupación ({statsDTO.Ocupadas}/{statsDTO.Total} bandejas). Se recomienda agilizar retiros.",
                        Tipo = "warning",
                        RolesDestino = string.Join(",", RolesAlertaOcupacion),
                        RequiereAccion = true,
                        AccionSugerida = "Ver Mapa del Mortuorio",
                        UrlNavegacion = "/mapa-mortuorio"
                    };

                    await _hubContext.Clients
                        .Groups(RolesAlertaOcupacion)
                        .RecibirNotificacion(notificacion);

                    _logger.LogInformation(
                        "SignalR: Notificación genérica de ocupación enviada. Ocupación: {Porcentaje}%",
                        statsDTO.PorcentajeOcupacion.ToString("F2")
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar enviar alerta de ocupación por SignalR");
                // NO re-throw - la alerta no debe bloquear la asignación de bandeja
            }
        }
    }
}