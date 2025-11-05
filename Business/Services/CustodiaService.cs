using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestionar la cadena de custodia y los traspasos del expediente.
    /// </summary>
    public class CustodiaService : ICustodiaService
    {
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly ICustodiaRepository _custodiaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IStateMachineService _stateMachineService; // Inyectar la máquina de estados
        private readonly ILogger<CustodiaService> _logger;

        /// <summary>
        /// Constructor para inyectar todas las dependencias necesarias.
        /// </summary>
        public CustodiaService(
            IExpedienteRepository expedienteRepository,
            ICustodiaRepository custodiaRepository,
            IUsuarioRepository usuarioRepository,
            IStateMachineService stateMachineService, // Recibir la interfaz inyectada
            ILogger<CustodiaService> logger)
        {
            _expedienteRepository = expedienteRepository;
            _custodiaRepository = custodiaRepository;
            _usuarioRepository = usuarioRepository;
            _stateMachineService = stateMachineService; // Asignar el servicio
            _logger = logger;
        }

        /// <summary>
        /// Realiza el traspaso de custodia (Ej. Enfermería -> Ambulancia).
        /// </summary>
        /// <param name="dto">Datos del traspaso (CodigoQR y Observaciones).</param>
        /// <param name="usuarioDestinoId">ID del usuario que recibe la custodia (obtenido del token JWT).</param>
        /// <returns>Un DTO con los detalles de la transferencia realizada.</returns>
        /// <exception cref="InvalidOperationException">Si el expediente no existe, la validación de estado falla, o faltan usuarios.</exception>
        public async Task<TraspasoRealizadoDTO> RealizarTraspasoAsync(RealizarTraspasoDTO dto, int usuarioDestinoId)
        {
            // 1. Buscar expediente por código QR
            var expediente = await _expedienteRepository.GetByCodigoQRAsync(dto.CodigoQR);
            if (expediente == null)
            {
                _logger.LogWarning("Intento de traspaso fallido: No se encontró expediente con QR {CodigoQR}", dto.CodigoQR);
                throw new InvalidOperationException($"No se encontró expediente con código QR: {dto.CodigoQR}");
            }

            // 2. Validar que NO haya traspaso duplicado reciente (ventana de 5 minutos)
            var existeTransferenciaReciente = await _custodiaRepository
                .ExisteTransferenciaRecienteAsync(expediente.ExpedienteID, usuarioDestinoId);

            if (existeTransferenciaReciente)
            {
                _logger.LogWarning("Intento de traspaso duplicado para expediente {ExpedienteID} por usuario {UsuarioID}", expediente.ExpedienteID, usuarioDestinoId);
                throw new InvalidOperationException(
                    "Ya recibiste la custodia de este expediente hace menos de 5 minutos. " +
                    "Evita duplicar el registro.");
            }

            // 3. Validar estado usando la Máquina de Estados
            // Ya no creamos 'new StateMachineService', usamos el servicio inyectado.
            if (!_stateMachineService.CanFire(expediente, TriggerExpediente.AceptarCustodia))
            {
                var triggersValidos = await _stateMachineService.GetPermittedTriggersAsync(expediente);
                var mensajeError = $"El expediente está en estado '{expediente.EstadoActual}'. " +
                                   $"No se puede ejecutar la acción '{TriggerExpediente.AceptarCustodia}'. " +
                                   $"Acciones válidas: {string.Join(", ", triggersValidos)}";
                _logger.LogWarning("Intento de traspaso en estado inválido para expediente {ExpedienteID}: {MensajeError}", expediente.ExpedienteID, mensajeError);
                throw new InvalidOperationException(mensajeError);
            }

            // Guardar estado anterior ANTES de la transición
            var estadoAnterior = expediente.EstadoActual;

            // 4. Obtener usuario origen (quien creó el expediente = enfermera del servicio)
            int usuarioOrigenId = expediente.UsuarioCreadorID;

            // 5. Obtener información completa de ambos usuarios
            var usuarioOrigen = await _usuarioRepository.GetByIdAsync(usuarioOrigenId);
            var usuarioDestino = await _usuarioRepository.GetByIdAsync(usuarioDestinoId);

            if (usuarioOrigen == null)
                throw new InvalidOperationException("No se encontró el usuario origen (creador del expediente)");
            if (usuarioDestino == null)
                throw new InvalidOperationException("No se encontró el usuario destino");

            // 6. Crear registro de transferencia
            var transferencia = new CustodiaTransferencia
            {
                ExpedienteID = expediente.ExpedienteID,
                UsuarioOrigenID = usuarioOrigenId,
                UsuarioDestinoID = usuarioDestinoId,
                FechaHoraTransferencia = DateTime.Now,
                UbicacionOrigen = $"{expediente.ServicioFallecimiento} - Cama {expediente.NumeroCama ?? "S/N"}",
                UbicacionDestino = "Mortuorio", // Destino lógico
                Observaciones = dto.Observaciones
            };

            var transferenciaCreada = await _custodiaRepository.CreateAsync(transferencia);

            // 7. Actualizar estado del expediente usando la Máquina de Estados
            await _stateMachineService.FireAsync(expediente, TriggerExpediente.AceptarCustodia);

            // El estado en 'expediente.EstadoActual' ya fue actualizado a 'EnTrasladoMortuorio' por la máquina.
            expediente.FechaModificacion = DateTime.Now;
            await _expedienteRepository.UpdateAsync(expediente);

            _logger.LogInformation(
                "Traspaso de custodia realizado: Expediente {CodigoExpediente}, " +
                "De: {UsuarioOrigen} ({RolOrigen}) → A: {UsuarioDestino} ({RolDestino}). " +
                "Estado: {EstadoAnterior} → {EstadoNuevo}",
                expediente.CodigoExpediente,
                usuarioOrigen.NombreCompleto,
                usuarioOrigen.Rol?.Name ?? "Sin rol",
                usuarioDestino.NombreCompleto,
                usuarioDestino.Rol?.Name ?? "Sin rol",
                estadoAnterior,
                expediente.EstadoActual);

            // 8. Mapear a DTO de respuesta
            return new TraspasoRealizadoDTO
            {
                TransferenciaID = transferenciaCreada.TransferenciaID,
                ExpedienteID = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                NombreCompleto = expediente.NombreCompleto,

                UsuarioOrigenID = usuarioOrigenId,
                UsuarioOrigen = usuarioOrigen.NombreCompleto,
                RolOrigen = usuarioOrigen.Rol?.Name ?? "Sin rol",

                UsuarioDestinoID = usuarioDestinoId,
                UsuarioDestino = usuarioDestino.NombreCompleto,
                RolDestino = usuarioDestino.Rol?.Name ?? "Sin rol",

                FechaHoraTransferencia = transferenciaCreada.FechaHoraTransferencia,
                UbicacionOrigen = transferenciaCreada.UbicacionOrigen,
                UbicacionDestino = transferenciaCreada.UbicacionDestino,

                EstadoAnterior = estadoAnterior.ToString(),
                EstadoNuevo = expediente.EstadoActual.ToString(),
                Observaciones = transferenciaCreada.Observaciones
            };
        }

        /// <summary>
        /// Obtiene el historial completo de traspasos de custodia de un expediente.
        /// </summary>
        public async Task<List<CustodiaTransferenciaDTO>> GetHistorialCustodiaAsync(int expedienteId)
        {
            // Validar que el expediente existe
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);
            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            var transferencias = await _custodiaRepository.GetHistorialByExpedienteAsync(expedienteId);

            // Mapear la lista
            return transferencias.Select(t => new CustodiaTransferenciaDTO
            {
                TransferenciaID = t.TransferenciaID,
                ExpedienteID = t.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,

                UsuarioOrigenID = t.UsuarioOrigenID,
                UsuarioOrigenNombre = t.UsuarioOrigen?.NombreCompleto ?? "Usuario no disponible",
                UsuarioOrigenRol = t.UsuarioOrigen?.Rol?.Name ?? "Rol no disponible",

                UsuarioDestinoID = t.UsuarioDestinoID,
                UsuarioDestinoNombre = t.UsuarioDestino?.NombreCompleto ?? "Usuario no disponible",
                UsuarioDestinoRol = t.UsuarioDestino?.Rol?.Name ?? "Rol no disponible",

                FechaHoraTransferencia = t.FechaHoraTransferencia,
                UbicacionOrigen = t.UbicacionOrigen,
                UbicacionDestino = t.UbicacionDestino,
                Observaciones = t.Observaciones
            }).ToList();
        }

        /// <summary>
        /// Obtiene la información de custodia actual (quién tiene el cuerpo y dónde está).
        /// </summary>
        public async Task<CustodiaActualDTO?> GetUltimaCustodiaAsync(int expedienteId)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);
            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            var ultima = await _custodiaRepository.GetUltimaTransferenciaAsync(expedienteId);

            // Si no hay transferencias (aún en 'EnPiso' o 'PendienteDeRecojo' sin traspaso)
            if (ultima == null)
            {
                // El creador (Enfermería) tiene la custodia en el servicio
                return new CustodiaActualDTO
                {
                    TransferenciaID = 0,
                    ExpedienteID = expediente.ExpedienteID,
                    CodigoExpediente = expediente.CodigoExpediente,
                    UsuarioActualID = expediente.UsuarioCreadorID,
                    UsuarioActualNombre = expediente.UsuarioCreador?.NombreCompleto ?? "Usuario no disponible",
                    UsuarioActualRol = expediente.UsuarioCreador?.Rol?.Name ?? "Rol no disponible",
                    FechaHoraRecepcion = expediente.FechaCreacion,
                    UbicacionActual = $"{expediente.ServicioFallecimiento} - Cama {expediente.NumeroCama ?? "S/N"}",
                    EstadoActual = expediente.EstadoActual.ToString()
                };
            }

            // CALCULAR UBICACIÓN ACTUAL SEGÚN EL ESTADO
            string ubicacionActual = expediente.EstadoActual switch
            {
                EstadoExpediente.EnPiso => ultima.UbicacionOrigen,
                EstadoExpediente.PendienteDeRecojo => ultima.UbicacionOrigen,
                EstadoExpediente.EnTrasladoMortuorio => "En Tránsito (Camilla)",
                EstadoExpediente.VerificacionRechazadaMortuorio => "En Tránsito (Puerta Mortuorio - Rechazado)",
                EstadoExpediente.PendienteAsignacionBandeja => "Mortuorio (Ingreso)",
                EstadoExpediente.EnBandeja => $"Mortuorio (Bandeja {ultima.UbicacionDestino})", // Asumimos que la última ubicación es la bandeja
                EstadoExpediente.PendienteRetiro => $"Mortuorio (Bandeja {ultima.UbicacionDestino})",
                EstadoExpediente.Retirado => "Retirado",
                _ => ultima.UbicacionDestino // Fallback
            };

            // Mapear DTO
            return new CustodiaActualDTO
            {
                TransferenciaID = ultima.TransferenciaID,
                ExpedienteID = ultima.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,

                UsuarioActualID = ultima.UsuarioDestinoID,
                UsuarioActualNombre = ultima.UsuarioDestino?.NombreCompleto ?? "Usuario no disponible",
                UsuarioActualRol = ultima.UsuarioDestino?.Rol?.Name ?? "Rol no disponible",

                FechaHoraRecepcion = ultima.FechaHoraTransferencia,
                UbicacionActual = ubicacionActual,  // UBICACIÓN CALCULADA
                EstadoActual = expediente.EstadoActual.ToString()
            };
        }
    }
}