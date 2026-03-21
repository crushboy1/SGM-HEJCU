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
            // 1. Obtener expediente por código QR
            var expediente = await _expedienteRepo.GetByCodigoQRAsync(dto.CodigoExpedienteBrazalete);
            if (expediente == null)
            {
                _logger.LogWarning("Verificación Rechazada: QR no válido {QR}", dto.CodigoExpedienteBrazalete);
                throw new InvalidOperationException($"QR inválido. No se encontró expediente con código: {dto.CodigoExpedienteBrazalete}");
            }

            _logger.LogInformation("Iniciando verificación para Expediente {CodigoExpediente}", expediente.CodigoExpediente);

            // 2. Obtener ID del Técnico de Ambulancia desde la última custodia
            var ultimaCustodia = await _custodiaRepo.GetUltimaTransferenciaAsync(expediente.ExpedienteID);
            if (ultimaCustodia == null || ultimaCustodia.UsuarioDestino.Rol.Name != "Ambulancia")
            {
                _logger.LogWarning("Verificación Rechazada: No hay custodia previa de Ambulancia para Exp {ExpedienteID}", expediente.ExpedienteID);
                throw new InvalidOperationException($"No se puede verificar el ingreso. El expediente no registra custodia de un Téc. de Ambulancia (Estado: {expediente.EstadoActual})");
            }

            int tecnicoAmbulanciaId = ultimaCustodia.UsuarioDestinoID;

            // 3. Calcular coincidencias (para auditoría — el frontend ya envía datos de la BD,
            //    por lo que normalmente siempre serán true. Se guardan para trazabilidad.)
            bool documentoCoincide =
                expediente.NumeroDocumento == dto.NumeroDocumentoBrazalete &&
                expediente.TipoDocumento.ToString().Equals(dto.TipoDocumentoBrazalete, StringComparison.OrdinalIgnoreCase);

            // 4. Construir entidad de verificación con log de auditoría completo
            var verificacion = new VerificacionMortuorio
            {
                ExpedienteID = expediente.ExpedienteID,
                VigilanteID = vigilanteId,
                TecnicoAmbulanciaID = tecnicoAmbulanciaId,
                FechaHoraVerificacion = DateTime.Now,

                // Datos del brazalete enviados por el frontend (auditoría)
                CodigoExpedienteBrazalete = dto.CodigoExpedienteBrazalete,
                HCBrazalete = dto.HCBrazalete,
                TipoDocumentoBrazalete = dto.TipoDocumentoBrazalete,
                NumeroDocumentoBrazalete = dto.NumeroDocumentoBrazalete,
                NombreCompletoBrazalete = dto.NombreCompletoBrazalete,
                ServicioBrazalete = dto.ServicioBrazalete,

                // Flags de coincidencia (calculados aquí, guardados para historial)
                CodigoExpedienteCoincide = expediente.CodigoExpediente == dto.CodigoExpedienteBrazalete,
                HCCoincide = expediente.HC == dto.HCBrazalete,
                DocumentoCoincide = documentoCoincide,
                NombreCoincide = expediente.NombreCompleto.Equals(dto.NombreCompletoBrazalete, StringComparison.OrdinalIgnoreCase),
                ServicioCoincide = expediente.ServicioFallecimiento == dto.ServicioBrazalete,

                Observaciones = dto.Observaciones
            };

            // 5. Happy path / Sad path según confirmación manual del brazalete físico
            //    BrazaletePresente es la única validación que el sistema no puede hacer por sí solo.
            if (dto.BrazaletePresente)
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
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.VerificarIngresoMortuorio))
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser verificado.");

            var estadoAnterior = expediente.EstadoActual;

            // 1. Aprobar y guardar log
            verificacion.Aprobar("Verificación aprobada. Brazalete físico confirmado por el Vigilante.");
            var verificacionCreada = await _verificacionRepo.CreateAsync(verificacion);

            // 2. Traspaso de custodia: Ambulancia → Vigilante
            await _custodiaRepo.CreateAsync(new CustodiaTransferencia
            {
                ExpedienteID = expediente.ExpedienteID,
                UsuarioOrigenID = verificacion.TecnicoAmbulanciaID,
                UsuarioDestinoID = verificacion.VigilanteID,
                UbicacionOrigen = "Tránsito (Camilla)",
                UbicacionDestino = "Mortuorio (Puerta de Ingreso)",
                Observaciones = "Custodia entregada en puerta de mortuorio tras verificación."
            });

            // 3. Disparar estado
            await _stateMachine.FireAsync(expediente, TriggerExpediente.VerificarIngresoMortuorio);
            await _expedienteRepo.UpdateAsync(expediente);

            _logger.LogInformation("Verificación APROBADA — Expediente {CodigoExpediente}. Estado: {EstadoAnterior} → {EstadoNuevo}",
                expediente.CodigoExpediente, estadoAnterior, expediente.EstadoActual);

            return new VerificacionResultadoDTO
            {
                VerificacionID = verificacionCreada.VerificacionID,
                FechaHoraVerificacion = verificacionCreada.FechaHoraVerificacion,
                Aprobada = true,
                MensajeResultado = "Verificación exitosa. El expediente puede ingresar al mortuorio.",
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
            if (!_stateMachine.CanFire(expediente, TriggerExpediente.RechazarVerificacion))
                throw new InvalidOperationException($"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' y no puede ser rechazado.");

            var estadoAnterior = expediente.EstadoActual;

            // El motivo de rechazo es por brazalete físico ausente/incorrecto
            var motivoRechazo = string.IsNullOrWhiteSpace(verificacion.Observaciones)
                ? "El Vigilante indicó que el brazalete físico no está presente o no coincide con el cuerpo."
                : $"Brazalete físico no confirmado. Observación del Vigilante: {verificacion.Observaciones}";

            // 1. Rechazar y guardar log
            verificacion.Rechazar(motivoRechazo, "Brazalete físico no confirmado por el Vigilante.");
            var verificacionCreada = await _verificacionRepo.CreateAsync(verificacion);

            // 2. Disparar estado
            await _stateMachine.FireAsync(expediente, TriggerExpediente.RechazarVerificacion);
            await _expedienteRepo.UpdateAsync(expediente);

            // 3. Crear solicitud de corrección para Enfermería
            var datosJson = JsonSerializer.Serialize(new
            {
                MotivoFisico = motivoRechazo,
                Expediente = new
                {
                    expediente.HC,
                    Documento = new { Tipo = expediente.TipoDocumento.ToString(), Num = expediente.NumeroDocumento },
                    expediente.NombreCompleto,
                    expediente.ServicioFallecimiento
                }
            });

            var solicitud = new SolicitudCorreccionExpediente
            {
                ExpedienteID = expediente.ExpedienteID,
                UsuarioSolicitaID = verificacion.VigilanteID,
                UsuarioResponsableID = expediente.UsuarioCreadorID,
                DatosIncorrectos = datosJson,
                DescripcionProblema = motivoRechazo,
                ObservacionesSolicitud = verificacion.Observaciones,
                Resuelta = false
            };
            var solicitudCreada = await _solicitudRepo.CreateAsync(solicitud);

            _logger.LogWarning("Verificación RECHAZADA — Expediente {CodigoExpediente}. Motivo: {Motivo}. Estado: {EstadoAnterior} → {EstadoNuevo}. Solicitud corrección ID: {SolicitudID}",
                expediente.CodigoExpediente, motivoRechazo, estadoAnterior, expediente.EstadoActual, solicitudCreada.SolicitudID);

            return new VerificacionResultadoDTO
            {
                VerificacionID = verificacionCreada.VerificacionID,
                FechaHoraVerificacion = verificacionCreada.FechaHoraVerificacion,
                Aprobada = false,
                MensajeResultado = "Verificación rechazada. Se notificó a Enfermería para revisar el brazalete.",
                EstadoExpedienteNuevo = expediente.EstadoActual.ToString(),
                HCCoincide = verificacion.HCCoincide,
                DNICoincide = verificacion.DocumentoCoincide,
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

            return new EstadisticasVerificacionDTO
            {
                TotalVerificaciones = stats.TotalVerificaciones,
                Aprobadas = stats.Aprobadas,
                Rechazadas = stats.Rechazadas,
                PorcentajeAprobacion = stats.PorcentajeAprobacion,
                ConDiscrepanciaHC = stats.ConDiscrepanciaHC,
                ConDiscrepanciaDocumento = stats.ConDiscrepanciaDocumento,
                ConDiscrepanciaNombre = stats.ConDiscrepanciaNombre
            };
        }
    }
}