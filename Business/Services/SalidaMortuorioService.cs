using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de Salida de Mortuorio.
    /// </summary>
    public class SalidaMortuorioService : ISalidaMortuorioService
    {
        private readonly ISalidaMortuorioRepository _salidaRepo;
        private readonly IExpedienteRepository _expedienteRepo;
        private readonly IBandejaService _bandejaService; // Dependencia clave
        private readonly IStateMachineService _stateMachine;
        private readonly ILogger<SalidaMortuorioService> _logger;

        public SalidaMortuorioService(
            ISalidaMortuorioRepository salidaRepo,
            IExpedienteRepository expedienteRepo,
            IBandejaService bandejaService,
            IStateMachineService stateMachine,
            ILogger<SalidaMortuorioService> logger)
        {
            _salidaRepo = salidaRepo;
            _expedienteRepo = expedienteRepo;
            _bandejaService = bandejaService;
            _stateMachine = stateMachine;
            _logger = logger;
        }

        public async Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int vigilanteId)
        {
            // 1. Validar Entidades
            var expediente = await _expedienteRepo.GetByIdAsync(dto.ExpedienteID);
            if (expediente == null)
                throw new InvalidOperationException($"Expediente ID {dto.ExpedienteID} no encontrado.");

            // 2. Validar Máquina de Estados
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.RegistrarSalida))
            {
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede registrarse su salida.");
            }

            var estadoAnterior = expediente.EstadoActual;

            // 3. Mapear DTO a Entidad
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

            // 4. Guardar registro de salida
            var salidaCreada = await _salidaRepo.CreateAsync(salida);

            // 5. Disparar State Machine
            await _stateMachine.FireAsync(expediente, TriggerExpediente.RegistrarSalida);
            await _expedienteRepo.UpdateAsync(expediente);

            // 6. Liberar la bandeja (RN-34)
            // Esta es la dependencia cruzada clave
            await _bandejaService.LiberarBandejaAsync(expediente.ExpedienteID, vigilanteId);

            _logger.LogInformation("Salida registrada para Expediente {CodigoExpediente} por Usuario ID {UsuarioID}. Estado: {EstadoAnterior} -> {EstadoNuevo}. Bandeja liberada.",
                expediente.CodigoExpediente, vigilanteId, estadoAnterior, expediente.EstadoActual);

            // 7. Devolver DTO de respuesta
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


        // --- Métodos Privados de Mapeo ---

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
                ResponsableDocumento = $"{salida.ResponsableTipoDocumento} {salida.ResponsableNumeroDocumento}",
                VigilanteNombre = salida.Vigilante?.NombreCompleto ?? "N/A",
                NombreFuneraria = salida.NombreFuneraria,
                Destino = salida.Destino,
                IncidenteRegistrado = salida.IncidenteRegistrado,
                DetalleIncidente = salida.DetalleIncidente
            };
        }
    }
}