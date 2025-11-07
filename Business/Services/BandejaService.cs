using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de Bandejas.
    /// </summary>
    public class BandejaService : IBandejaService
    {
        private readonly IBandejaRepository _bandejaRepo;
        private readonly IOcupacionBandejaRepository _ocupacionRepo;
        private readonly IExpedienteRepository _expedienteRepo;
        private readonly IStateMachineService _stateMachine;
        private readonly ILogger<BandejaService> _logger;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext;

        public BandejaService(
            IBandejaRepository bandejaRepo,
            IOcupacionBandejaRepository ocupacionRepo,
            IExpedienteRepository expedienteRepo,
            IStateMachineService stateMachine,
            ILogger<BandejaService> logger,
            IHubContext<SgmHub, ISgmClient> hubContext)
        {
            _bandejaRepo = bandejaRepo;
            _ocupacionRepo = ocupacionRepo;
            _expedienteRepo = expedienteRepo;
            _stateMachine = stateMachine;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<List<BandejaDTO>> GetOcupacionDashboardAsync()
        {
            var bandejas = await _bandejaRepo.GetAllAsync();

            var dtos = bandejas.Select(b => MapToBandejaDTO(b)).ToList();

            return dtos;
        }

        public async Task<List<BandejaDisponibleDTO>> GetDisponiblesAsync()
        {
            var bandejas = await _bandejaRepo.GetDisponiblesAsync();

            return bandejas.Select(b => new BandejaDisponibleDTO
            {
                BandejaID = b.BandejaID,
                Codigo = b.Codigo
            }).ToList();
        }

        public async Task<BandejaDTO> AsignarBandejaAsync(AsignarBandejaDTO dto, int usuarioAsignaId)
        {
            // 1. Validar Entidades
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID);
            if (expediente == null)
                throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");

            var bandeja = await _bandejaRepo.GetByIdAsync(dto.BandejaID);
            if (bandeja == null)
                throw new InvalidOperationException($"Bandeja ID {dto.BandejaID} no encontrada.");

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

            _logger.LogInformation("Bandeja {CodigoBandeja} asignada a Expediente {CodigoExpediente} por Usuario ID {UsuarioID}. Estado: {EstadoAnterior} -> {EstadoNuevo}",
                bandeja.Codigo, expediente.CodigoExpediente, usuarioAsignaId, estadoAnterior, expediente.EstadoActual);

            // 5. DISPARAR ALERTA DE OCUPACIÓN (SI APLICA) 
            await CheckOcupacionAlertAsync();

            // 6. Devolver DTO
            return MapToBandejaDTO(bandeja);
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
                _logger.LogError("INCONSISTENCIA DE DATOS: La bandeja {CodigoBandeja} figura ocupada por Exp {ExpedienteID} pero no existe registro de ocupación activa.",
                    bandeja.Codigo, expedienteId);
                // Forzar liberación de la bandeja de todas formas
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

            _logger.LogInformation("Bandeja {CodigoBandeja} liberada exitosamente por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioLiberaId);
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
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId);
            if (bandeja == null)
                throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            bandeja.IniciarMantenimiento(observaciones);
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation("Bandeja {CodigoBandeja} puesta en Mantenimiento por Usuario ID {UsuarioID}. Motivo: {Motivo}",
                bandeja.Codigo, usuarioId, observaciones);

            return MapToBandejaDTO(bandeja);
        }

        public async Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId)
        {
            var bandeja = await _bandejaRepo.GetByIdAsync(bandejaId);
            if (bandeja == null)
                throw new InvalidOperationException($"Bandeja ID {bandejaId} no encontrada.");

            bandeja.FinalizarMantenimiento();
            await _bandejaRepo.UpdateAsync(bandeja);

            _logger.LogInformation("Mantenimiento de Bandeja {CodigoBandeja} finalizado por Usuario ID {UsuarioID}",
                bandeja.Codigo, usuarioId);

            return MapToBandejaDTO(bandeja);
        }


        // --- Métodos Privados de Mapeo y Alertas ---

        private BandejaDTO MapToBandejaDTO(Bandeja bandeja)
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
        /// Comprueba las estadísticas de ocupación y envía una alerta
        /// a través de SignalR si supera el 70%.
        /// </summary>
        private async Task CheckOcupacionAlertAsync() // <-- 6. NUEVO MÉTODO PRIVADO
        {
            try
            {
                var statsDTO = await GetEstadisticasAsync();

                if (statsDTO.PorcentajeOcupacion > 70)
                {
                    _logger.LogWarning("ALERTA DE OCUPACIÓN: El mortuorio ha superado el 70% de capacidad. Ocupación actual: {PorcentajeOcupacion}%",
                        statsDTO.PorcentajeOcupacion.ToString("F2"));

                    // Enviar notificación a TODOS los clientes conectados
                    await _hubContext.Clients.All.RecibirAlertaOcupacion(statsDTO);
                }
            }
            catch (Exception ex)
            {
                // Importante: No relanzar la excepción.
                // Un fallo en SignalR NO debe detener la transacción principal.
                _logger.LogError(ex, "Error al intentar enviar alerta de ocupación por SignalR");
            }
        }
    }
}