using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.Verificacion;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;
using System.Text.Json;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de Verificación de Ingreso al Mortuorio.
    /// </summary>
    public class VerificacionService : IVerificacionService
    {
        private readonly IVerificacionMortuorioRepository _verificacionRepo;
        private readonly IExpedienteRepository _expedienteRepo;
        private readonly ISolicitudCorreccionRepository _solicitudRepo;
        private readonly ICustodiaRepository _custodiaRepo;
        private readonly IStateMachineService _stateMachine;
        private readonly ILogger<VerificacionService> _logger;

        public VerificacionService(
            IVerificacionMortuorioRepository verificacionRepo,
            IExpedienteRepository expedienteRepo,
            ISolicitudCorreccionRepository solicitudRepo,
            ICustodiaRepository custodiaRepo,
            IStateMachineService stateMachine,
            ILogger<VerificacionService> logger)
        {
            _verificacionRepo = verificacionRepo;
            _expedienteRepo = expedienteRepo;
            _solicitudRepo = solicitudRepo;
            _custodiaRepo = custodiaRepo;
            _stateMachine = stateMachine;
            _logger = logger;
        }

        public async Task<VerificacionResultadoDTO> VerificarIngresoAsync(VerificacionRequestDTO dto, int vigilanteId)
        {
            // 1. Obtener el expediente por el CÓDIGO QR (que es el CodigoExpediente)
            var expediente = await _expedienteRepo.GetByCodigoQRAsync(dto.CodigoExpedienteBrazalete);
            if (expediente == null)
            {
                _logger.LogWarning("Verificación Rechazada: QR No válido {QR}", dto.CodigoExpedienteBrazalete);
                throw new InvalidOperationException($"QR Inválido. No se encontró expediente con código: {dto.CodigoExpedienteBrazalete}");
            }

            _logger.LogInformation("Iniciando verificación para Expediente {CodigoExpediente}", expediente.CodigoExpediente);
            // 2. OBTENER ID DEL TÉCNICO DE AMBULANCIA
            var ultimaCustodia = await _custodiaRepo.GetUltimaTransferenciaAsync(expediente.ExpedienteID);
            if (ultimaCustodia == null || ultimaCustodia.UsuarioDestino.Rol.Name != "Ambulancia")
            {
                _logger.LogWarning("Verificación Rechazada: No se encontró la custodia previa de 'Ambulancia' para el Exp {ExpedienteID}", expediente.ExpedienteID);
                throw new InvalidOperationException($"No se puede verificar el ingreso. El expediente no registra la custodia de un Téc. de Ambulancia (Estado: {expediente.EstadoActual})");
            }
            // Obtenemos el ID del técnico (ej. 9) desde la última custodia
            int tecnicoAmbulanciaId = ultimaCustodia.UsuarioDestinoID;
            // 3. Comparar datos del DTO (Brazalete) vs. Entidad (BD)
            var verificacion = new VerificacionMortuorio
            {
                ExpedienteID = expediente.ExpedienteID,
                VigilanteID = vigilanteId,
                TecnicoAmbulanciaID = tecnicoAmbulanciaId,
                FechaHoraVerificacion = DateTime.Now,
                // Datos del Brazalete (leídos por el Vigilante)
                CodigoExpedienteBrazalete = dto.CodigoExpedienteBrazalete,
                HCBrazalete = dto.HCBrazalete,
                DNIBrazalete = dto.DNIBrazalete,
                NombreCompletoBrazalete = dto.NombreCompletoBrazalete,
                ServicioBrazalete = dto.ServicioBrazalete,
                // Comparaciones
                CodigoExpedienteCoincide = expediente.CodigoExpediente == dto.CodigoExpedienteBrazalete,
                HCCoincide = expediente.HC == dto.HCBrazalete,
                DNICoincide = expediente.NumeroDocumento == dto.DNIBrazalete,
                NombreCoincide = expediente.NombreCompleto.Equals(dto.NombreCompletoBrazalete, StringComparison.OrdinalIgnoreCase),
                ServicioCoincide = expediente.ServicioFallecimiento == dto.ServicioBrazalete,
                Observaciones = dto.Observaciones
            };

            // 4. Determinar Happy Path o Sad Path
            if (verificacion.TodosLosCamposCoinciden())
            {
                return await HandleHappyPath(expediente, verificacion);
            }
            else
            {
                return await HandleSadPath(expediente, verificacion);
            }
        }

        private async Task<VerificacionResultadoDTO> HandleHappyPath(Expediente expediente, VerificacionMortuorio verificacion)
        {
            // Validar estado
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.VerificarIngresoMortuorio))
            {
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser verificado.");
            }

            var estadoAnterior = expediente.EstadoActual;

            // 1. Aprobar y guardar log de verificación
            verificacion.Aprobar("Verificación de ingreso aprobada. Todos los datos coinciden.");
            var verificacionCreada = await _verificacionRepo.CreateAsync(verificacion);

            // 2. Crear traspaso final de custodia (Ambulancia -> Vigilante)
            await _custodiaRepo.CreateAsync(new CustodiaTransferencia
            {
                ExpedienteID = expediente.ExpedienteID,
                UsuarioOrigenID = verificacion.TecnicoAmbulanciaID,
                UsuarioDestinoID = verificacion.VigilanteID,
                UbicacionOrigen = "Tránsito (Camilla)",
                UbicacionDestino = "Mortuorio (Puerta de Ingreso)",
                Observaciones = "Custodia entregada en puerta de mortuorio tras verificación."
            });

            // 3. Disparar State Machine
            await _stateMachine.FireAsync(expediente, TriggerExpediente.VerificarIngresoMortuorio);
            await _expedienteRepo.UpdateAsync(expediente);

            _logger.LogInformation("Verificación APROBADA para Expediente {CodigoExpediente}. Estado: {EstadoAnterior} -> {EstadoNuevo}",
                expediente.CodigoExpediente, estadoAnterior, expediente.EstadoActual);

            // 4. Devolver Resultado
            return new VerificacionResultadoDTO
            {
                VerificacionID = verificacionCreada.VerificacionID,
                FechaHoraVerificacion = verificacionCreada.FechaHoraVerificacion,
                Aprobada = true,
                MensajeResultado = "Verificación Exitosa. El expediente puede ingresar.",
                EstadoExpedienteNuevo = expediente.EstadoActual.ToString(),
                HCCoincide = true,
                DNICoincide = true,
                NombreCoincide = true,
                ServicioCoincide = true,
                CodigoExpedienteCoincide = true
            };
        }

        private async Task<VerificacionResultadoDTO> HandleSadPath(Expediente expediente, VerificacionMortuorio verificacion)
        {
            // Validar estado
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.RechazarVerificacion))
            {
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser rechazado.");
            }

            var estadoAnterior = expediente.EstadoActual;
            var motivoRechazo = verificacion.GenerarResumenDiscrepancias();

            // 1. Rechazar y guardar log de verificación
            verificacion.Rechazar(motivoRechazo, "Datos del brazalete no coinciden con la base de datos.");
            var verificacionCreada = await _verificacionRepo.CreateAsync(verificacion);

            // 2. Disparar State Machine
            await _stateMachine.FireAsync(expediente, TriggerExpediente.RechazarVerificacion);
            await _expedienteRepo.UpdateAsync(expediente);

            // 3. Crear Solicitud de Corrección (el "ticket")
            var datosIncorrectosJson = JsonSerializer.Serialize(new
            {
                HC = new { DB = expediente.HC, Brazalete = verificacion.HCBrazalete },
                DNI = new { DB = expediente.NumeroDocumento, Brazalete = verificacion.DNIBrazalete },
                Nombre = new { DB = expediente.NombreCompleto, Brazalete = verificacion.NombreCompletoBrazalete },
                Servicio = new { DB = expediente.ServicioFallecimiento, Brazalete = verificacion.ServicioBrazalete }
            });

            var solicitud = new SolicitudCorreccionExpediente
            {
                ExpedienteID = expediente.ExpedienteID,
                UsuarioSolicitaID = verificacion.VigilanteID,
                UsuarioResponsableID = expediente.UsuarioCreadorID, // Asignado a quien creó el expediente
                DatosIncorrectos = datosIncorrectosJson,
                DescripcionProblema = motivoRechazo,
                ObservacionesSolicitud = verificacion.Observaciones,
                Resuelta = false
            };
            var solicitudCreada = await _solicitudRepo.CreateAsync(solicitud);

            _logger.LogWarning("Verificación RECHAZADA para Expediente {CodigoExpediente}. Motivo: {Motivo}. Estado: {EstadoAnterior} -> {EstadoNuevo}. Creada Solicitud de Corrección ID: {SolicitudID}",
                expediente.CodigoExpediente, motivoRechazo, estadoAnterior, expediente.EstadoActual, solicitudCreada.SolicitudID);

            // 4. Devolver Resultado
            return new VerificacionResultadoDTO
            {
                VerificacionID = verificacionCreada.VerificacionID,
                FechaHoraVerificacion = verificacionCreada.FechaHoraVerificacion,
                Aprobada = false,
                MensajeResultado = "Verificación Rechazada. Se generó una solicitud de corrección a Enfermería.",
                EstadoExpedienteNuevo = expediente.EstadoActual.ToString(),
                HCCoincide = verificacion.HCCoincide,
                DNICoincide = verificacion.DNICoincide,
                NombreCoincide = verificacion.NombreCoincide,
                ServicioCoincide = verificacion.ServicioCoincide,
                CodigoExpedienteCoincide = verificacion.CodigoExpedienteCoincide,
                MotivoRechazo = motivoRechazo,
                SolicitudCorreccionID = solicitudCreada.SolicitudID
            };
        }

        public async Task<List<VerificacionHistorialDTO>> GetHistorialByExpedienteIdAsync(int expedienteId)
        {
            var historial = await _verificacionRepo.GetHistorialByExpedienteIdAsync(expedienteId);

            return historial.Select(v => new VerificacionHistorialDTO
            {
                VerificacionID = v.VerificacionID,
                FechaHora = v.FechaHoraVerificacion,
                VigilanteNombre = v.Vigilante?.NombreCompleto ?? "N/A",
                TecnicoAmbulanciaNombre = v.TecnicoAmbulancia?.NombreCompleto ?? "N/A",
                Aprobada = v.Aprobada,
                MotivoRechazo = v.MotivoRechazo
            }).ToList();
        }

        public async Task<EstadisticasVerificacionDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var stats = await _verificacionRepo.GetEstadisticasAsync(fechaInicio, fechaFin);

            // Mapeo directo
            return new EstadisticasVerificacionDTO
            {
                TotalVerificaciones = stats.TotalVerificaciones,
                Aprobadas = stats.Aprobadas,
                Rechazadas = stats.Rechazadas,
                PorcentajeAprobacion = stats.PorcentajeAprobacion,
                ConDiscrepanciaHC = stats.ConDiscrepanciaHC,
                ConDiscrepanciaDNI = stats.ConDiscrepanciaDNI,
                ConDiscrepanciaNombre = stats.ConDiscrepanciaNombre
            };
        }
    }
}