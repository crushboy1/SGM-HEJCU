using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;


namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de Salida de Mortuorio.
    /// Gestiona el registro de salida de cuerpos y la liberación automática de bandejas.
    /// 
    /// Responsabilidades:
    /// - Registrar salida según tipo (Familiar, Autoridad Legal, Traslado Hospital, Otro)
    /// - Validar documentación y pagos
    /// - Transicionar estado del expediente (PendienteRetiro → Retirado)
    /// - Liberar bandeja automáticamente (RN-34)
    /// - Notificar cambios vía SignalR
    /// </summary>
    public class SalidaMortuorioService : ISalidaMortuorioService
    {
        private readonly ISalidaMortuorioRepository _salidaRepo;
        private readonly IExpedienteRepository _expedienteRepo;
        private readonly IBandejaService _bandejaService;
        private readonly IStateMachineService _stateMachine;
        private readonly IHubContext<SgmHub> _hubContext;
        private readonly ILogger<SalidaMortuorioService> _logger;

        public SalidaMortuorioService(
            ISalidaMortuorioRepository salidaRepo,
            IExpedienteRepository expedienteRepo,
            IBandejaService bandejaService,
            IStateMachineService stateMachine,
            IHubContext<SgmHub> hubContext,
            ILogger<SalidaMortuorioService> logger)
        {
            _salidaRepo = salidaRepo;
            _expedienteRepo = expedienteRepo;
            _bandejaService = bandejaService;
            _stateMachine = stateMachine;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Registra la salida de un cuerpo del mortuorio.
        /// Incluye liberación automática de bandeja y notificación SignalR.
        /// </summary>
        public async Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int vigilanteId)
        {
            // 1. Validar Entidades
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID);
            if (expediente == null)
                throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");

            // 2. Validación explícita de estado (debe estar en PendienteRetiro)
            if (expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
            {
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} debe estar en estado 'Pendiente Retiro'. " +
                    $"Estado actual: {expediente.EstadoActual}"
                );
            }

            // 3. Validar Máquina de Estados
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.RegistrarSalida))
            {
                throw new InvalidOperationException(
                    $"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' " +
                    $"y no puede registrarse su salida."
                );
            }

            var estadoAnterior = expediente.EstadoActual;

            // 4. Mapear DTO a Entidad
            var salida = new SalidaMortuorio
            {
                ExpedienteID = dto.ExpedienteID,
                VigilanteID = vigilanteId,
                FechaHoraSalida = DateTime.Now,
                TipoSalida = dto.TipoSalida,
                ResponsableNombre = dto.ResponsableNombre,
                ResponsableTipoDocumento = dto.ResponsableTipoDocumento,
                ResponsableNumeroDocumento = dto.ResponsableNumeroDocumento,
                ResponsableParentesco = dto.ResponsableParentesco,
                ResponsableTelefono = dto.ResponsableTelefono,
                NumeroAutorizacion = dto.NumeroAutorizacion,
                EntidadAutorizante = dto.EntidadAutorizante,
                DocumentacionVerificada = dto.DocumentacionVerificada,
                PagoRealizado = dto.PagoRealizado,
                NumeroRecibo = dto.NumeroRecibo,
                NombreFuneraria = dto.NombreFuneraria,
                ConductorFuneraria = dto.ConductorFuneraria,
                DNIConductor = dto.DNIConductor,
                PlacaVehiculo = dto.PlacaVehiculo,
                Destino = dto.Destino,
                Observaciones = dto.Observaciones,
                IncidenteRegistrado = false // Por defecto
            };

            // 5. Guardar registro de salida
            var salidaCreada = await _salidaRepo.CreateAsync(salida);

            // 6. Disparar State Machine (PendienteRetiro → Retirado)
            await _stateMachine.FireAsync(expediente, TriggerExpediente.RegistrarSalida);
            await _expedienteRepo.UpdateAsync(expediente);

            // 7. Liberar la bandeja automáticamente (RN-34)
            await _bandejaService.LiberarBandejaAsync(expediente.ExpedienteID, vigilanteId);

            _logger.LogInformation(
                "Salida registrada para Expediente {CodigoExpediente} por Usuario ID {UsuarioID}. " +
                "Estado: {EstadoAnterior} -> {EstadoNuevo}. Bandeja liberada.",
                expediente.CodigoExpediente, vigilanteId, estadoAnterior, expediente.EstadoActual
            );

            // 8. Notificar cambio de estado vía SignalR
            await NotificarSalidaRegistradaAsync(expediente, estadoAnterior);

            // 9. Devolver DTO de respuesta
            return MapToSalidaDTO(salidaCreada);
        }

        public async Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId)
        {
            var salida = await _salidaRepo.GetByExpedienteIdAsync(expedienteId);
            if (salida == null) return null;

            return MapToSalidaDTO(salida);
        }

        public async Task<EstadisticasSalidaDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var stats = await _salidaRepo.GetEstadisticasAsync(fechaInicio, fechaFin);

            // Mapeo 1:1
            return new EstadisticasSalidaDTO
            {
                TotalSalidas = stats.TotalSalidas,
                SalidasFamiliar = stats.SalidasFamiliar,
                SalidasAutoridadLegal = stats.SalidasAutoridadLegal,
                SalidasTrasladoHospital = stats.SalidasTrasladoHospital,
                SalidasOtro = stats.SalidasOtro,
                ConIncidentes = stats.ConIncidentes,
                ConFuneraria = stats.ConFuneraria,
                PorcentajeIncidentes = stats.PorcentajeIncidentes
            };
        }

        public async Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var salidas = await _salidaRepo.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);
            return salidas.Select(MapToSalidaDTO).ToList();
        }

        // ===================================================================
        // MÉTODOS PRIVADOS
        // ===================================================================

        /// <summary>
        /// Notifica vía SignalR que se registró una salida del mortuorio.
        /// Envía notificación al dashboard y a roles relevantes (Jefatura, Vigilancia).
        /// </summary>
        private async Task NotificarSalidaRegistradaAsync(Expediente expediente, EstadoExpediente estadoAnterior)
        {
            try
            {
                var notificacion = new NotificacionDTO
                {
                    Titulo = "Salida Registrada",
                    Mensaje = $"El expediente {expediente.CodigoExpediente} ha sido retirado del mortuorio.",
                    Tipo = "success",
                    CategoriaNotificacion = "expediente_actualizado",
                    ExpedienteId = expediente.ExpedienteID,
                    CodigoExpediente = expediente.CodigoExpediente,
                    EstadoAnterior = estadoAnterior.ToString(),
                    EstadoNuevo = expediente.EstadoActual.ToString(),
                    RolesDestino = "JefeGuardia,VigilanteSupervisor",
                    RequiereAccion = false,
                    FechaExpiracion = DateTime.Now.AddHours(24)
                };

                // Enviar a grupo específico
                await _hubContext.Clients
                    .Group("JefeGuardia")
                    .SendAsync("RecibirNotificacion", notificacion);

                await _hubContext.Clients
                    .Group("VigilanteSupervisor")
                    .SendAsync("RecibirNotificacion", notificacion);

                _logger.LogInformation(
                    "Notificación SignalR enviada: Salida registrada para expediente {CodigoExpediente}",
                    expediente.CodigoExpediente
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al enviar notificación SignalR para salida del expediente {CodigoExpediente}",
                    expediente.CodigoExpediente
                );
                // No lanzar excepción - la notificación es opcional
            }
        }

        /// <summary>
        /// Mapea una entidad SalidaMortuorio a su DTO correspondiente.
        /// </summary>
        private SalidaDTO MapToSalidaDTO(SalidaMortuorio salida)
        {
            return new SalidaDTO
            {
                SalidaID = salida.SalidaID,
                ExpedienteID = salida.ExpedienteID,
                CodigoExpediente = salida.Expediente?.CodigoExpediente ?? "N/A",
                NombrePaciente = salida.Expediente?.NombreCompleto ?? "N/A",
                FechaHoraSalida = salida.FechaHoraSalida,
                TipoSalida = salida.TipoSalida.ToString(),
                ResponsableNombre = salida.ResponsableNombre,
                ResponsableDocumento = $"{salida.ResponsableTipoDocumento ?? "N/A"} {salida.ResponsableNumeroDocumento ?? "N/A"}",
                VigilanteNombre = salida.Vigilante?.NombreCompleto ?? "N/A",
                NombreFuneraria = salida.NombreFuneraria,
                Destino = salida.Destino,
                IncidenteRegistrado = salida.IncidenteRegistrado,
                DetalleIncidente = salida.DetalleIncidente
            };
        }
    }
}