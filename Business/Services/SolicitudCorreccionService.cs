using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Solicitud;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de Solicitud de Corrección.
    /// </summary>
    public class SolicitudCorreccionService : ISolicitudCorreccionService
    {
        private readonly ISolicitudCorreccionRepository _solicitudRepo;
        private readonly IExpedienteRepository _expedienteRepo;
        private readonly IStateMachineService _stateMachine;
        private readonly ILogger<SolicitudCorreccionService> _logger;

        public SolicitudCorreccionService(
            ISolicitudCorreccionRepository solicitudRepo,
            IExpedienteRepository expedienteRepo,
            IStateMachineService stateMachine,
            ILogger<SolicitudCorreccionService> logger)
        {
            _solicitudRepo = solicitudRepo;
            _expedienteRepo = expedienteRepo;
            _stateMachine = stateMachine;
            _logger = logger;
        }

        public async Task<SolicitudCorreccionDTO> ResolverSolicitudAsync(int solicitudId, ResolverSolicitudDTO dto, int usuarioId)
        {
            // 1. Obtener la solicitud (incluyendo el expediente)
            var solicitud = await _solicitudRepo.GetByIdAsync(solicitudId);
            if (solicitud == null)
                throw new InvalidOperationException($"Solicitud de corrección ID {solicitudId} no encontrada.");

            // 2. Validar estado de la solicitud
            if (solicitud.Resuelta)
                throw new InvalidOperationException($"La solicitud {solicitudId} ya fue resuelta anteriormente.");

            // 3. Obtener el expediente asociado
            var expediente = await _expedienteRepo.GetByIdAsync(solicitud.ExpedienteID);
            if (expediente == null)
            {
                _logger.LogCritical("INCONSISTENCIA DE DATOS: Solicitud ID {SolicitudID} apunta a Expediente ID {ExpedienteID} que no existe.", solicitudId, solicitud.ExpedienteID);
                throw new InvalidOperationException("Error crítico: El expediente asociado a esta solicitud no existe.");
            }

            // 4. Validar Máquina de Estados
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.CorregirDatos))
            {
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser corregido (se esperaba 'VerificacionRechazadaMortuorio').");
            }

            var estadoAnterior = expediente.EstadoActual;

            // 5. Ejecutar Transacción
            // a. Actualizar la solicitud (marcarla como resuelta)
            solicitud.Resolver(dto.DescripcionResolucion, dto.BrazaleteReimpreso, dto.ObservacionesResolucion);
            await _solicitudRepo.UpdateAsync(solicitud);

            // b. Disparar State Machine (Expediente vuelve a 'EnTrasladoMortuorio')
            await _stateMachine.FireAsync(expediente, TriggerExpediente.CorregirDatos);
            await _expedienteRepo.UpdateAsync(expediente);

            _logger.LogInformation("Solicitud de Corrección ID {SolicitudID} resuelta por Usuario ID {UsuarioID}. Expediente {CodigoExpediente} cambió de estado: {EstadoAnterior} -> {EstadoNuevo}",
                solicitudId, usuarioId, expediente.CodigoExpediente, estadoAnterior, expediente.EstadoActual);

            // 6. Devolver DTO actualizado
            return MapToSolicitudDTO(solicitud);
        }

        public async Task<SolicitudCorreccionDTO?> GetByIdAsync(int solicitudId)
        {
            var solicitud = await _solicitudRepo.GetByIdAsync(solicitudId);
            return solicitud == null ? null : MapToSolicitudDTO(solicitud);
        }

        public async Task<List<SolicitudCorreccionDTO>> GetPendientesAsync()
        {
            var solicitudes = await _solicitudRepo.GetPendientesAsync();
            return solicitudes.Select(MapToSolicitudDTO).ToList();
        }

        public async Task<List<SolicitudCorreccionDTO>> GetPendientesByServicioAsync(string servicio)
        {
            var solicitudes = await _solicitudRepo.GetPendientesByServicioAsync(servicio);
            return solicitudes.Select(MapToSolicitudDTO).ToList();
        }

        public async Task<List<SolicitudCorreccionDTO>> GetHistorialByExpedienteIdAsync(int expedienteId)
        {
            var solicitudes = await _solicitudRepo.GetHistorialByExpedienteIdAsync(expedienteId);
            return solicitudes.Select(MapToSolicitudDTO).ToList();
        }

        public async Task<EstadisticasSolicitudDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var stats = await _solicitudRepo.GetEstadisticasAsync(fechaInicio, fechaFin);

            return new EstadisticasSolicitudDTO
            {
                TotalSolicitudes = stats.TotalSolicitudes,
                Pendientes = stats.Pendientes,
                Resueltas = stats.Resueltas,
                ConAlerta = stats.ConAlerta,
                TiempoPromedioResolucionHoras = stats.TiempoPromedioResolucionHoras
            };
        }


        // --- Métodos Privados de Mapeo ---

        private SolicitudCorreccionDTO MapToSolicitudDTO(Data.Entities.SolicitudCorreccionExpediente solicitud)
        {
            var tiempo = solicitud.TiempoTranscurrido();
            var tiempoTexto = $"{(int)tiempo.TotalHours}h {tiempo.Minutes}m";

            return new SolicitudCorreccionDTO
            {
                SolicitudID = solicitud.SolicitudID,
                ExpedienteID = solicitud.ExpedienteID,
                CodigoExpediente = solicitud.Expediente?.CodigoExpediente ?? "N/A",
                FechaHoraSolicitud = solicitud.FechaHoraSolicitud,
                UsuarioSolicitaNombre = solicitud.UsuarioSolicita?.NombreCompleto ?? "N/A",
                UsuarioResponsableNombre = solicitud.UsuarioResponsable?.NombreCompleto ?? "N/A",
                DescripcionProblema = solicitud.DescripcionProblema,
                DatosIncorrectos = solicitud.DatosIncorrectos,
                ObservacionesSolicitud = solicitud.ObservacionesSolicitud,
                Resuelta = solicitud.Resuelta,
                FechaHoraResolucion = solicitud.FechaHoraResolucion,
                DescripcionResolucion = solicitud.DescripcionResolucion,
                BrazaleteReimpreso = solicitud.BrazaleteReimpreso,
                TiempoTranscurrido = tiempoTexto,
                SuperaTiempoAlerta = solicitud.SuperaTiempoAlerta()
            };
        }
    }
}