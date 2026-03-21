using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestionar la generación, almacenamiento y consulta de Códigos QR.
    /// </summary>
    public class QRService : IQRService
    {
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly IBandejaRepository _bandejaRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<QRService> _logger;
        private readonly IStateMachineService _stateMachineService;
        private readonly IExpedienteMapperService _mapper;

        public QRService(
            IExpedienteRepository expedienteRepository,
            IBandejaRepository bandejaRepository,
            IWebHostEnvironment environment,
            ILogger<QRService> logger,
            IStateMachineService stateMachineService,
            IExpedienteMapperService mapper)
        {
            _expedienteRepository = expedienteRepository;
            _bandejaRepository = bandejaRepository;
            _environment = environment;
            _logger = logger;
            _stateMachineService = stateMachineService;
            _mapper = mapper;
        }

        /// <summary>
        /// Genera un Código QR por primera vez para un expediente.
        /// Actualiza el estado del expediente a "PendienteDeRecojo".
        /// </summary>
        public async Task<QRGeneradoDTO> GenerarQRAsync(int expedienteId)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);

            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            if (!string.IsNullOrEmpty(expediente.CodigoQR))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} ya tiene QR generado. " +
                    "Use el endpoint de reimpresión si necesita volver a imprimir el brazalete.");

            if (!_stateMachineService.CanFire(expediente, TriggerExpediente.GenerarQR))
            {
                var triggers = await _stateMachineService.GetPermittedTriggersAsync(expediente);
                throw new InvalidOperationException(
                    $"El expediente está en estado '{expediente.EstadoActual}'. " +
                    $"No se puede ejecutar la acción '{TriggerExpediente.GenerarQR}'. " +
                    $"Acciones válidas: {string.Join(", ", triggers)}");
            }

            var estadoAnterior = expediente.EstadoActual;
            var codigoQR = expediente.CodigoExpediente;

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(codigoQR, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);

            var rutaImagenQR = await GuardarImagenQRAsync(expediente.CodigoExpediente, qrBytes);

            await _stateMachineService.FireAsync(expediente, TriggerExpediente.GenerarQR);
            expediente.CodigoQR = codigoQR;
            expediente.FechaGeneracionQR = DateTime.Now;
            await _expedienteRepository.UpdateAsync(expediente);

            _logger.LogInformation(
                "QR generado para expediente {CodigoExpediente}. Estado: {EstadoAnterior} → {EstadoNuevo}",
                expediente.CodigoExpediente, estadoAnterior, expediente.EstadoActual);

            return new QRGeneradoDTO
            {
                ExpedienteID = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                CodigoQR = expediente.CodigoQR,
                RutaImagenQR = rutaImagenQR,
                FechaGeneracion = expediente.FechaGeneracionQR!.Value,
                EstadoAnterior = estadoAnterior.ToString(),
                EstadoNuevo = expediente.EstadoActual.ToString(),
                NombreCompleto = expediente.NombreCompleto
            };
        }

        /// <summary>
        /// Obtiene la información de un QR ya generado (para reimpresión).
        /// Si la imagen no existe en disco, la regenera.
        /// </summary>
        public async Task<QRGeneradoDTO> ObtenerQRExistenteAsync(int expedienteId)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);

            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            if (string.IsNullOrEmpty(expediente.CodigoQR))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} no tiene QR generado. " +
                    "Debe generar el QR primero.");

            var rutaImagenQR = $"/qr/{expediente.CodigoExpediente}.png";
            var rutaFisica = Path.Combine(_environment.WebRootPath, "qr", $"{expediente.CodigoExpediente}.png");

            if (!File.Exists(rutaFisica))
            {
                _logger.LogWarning(
                    "Archivo QR no encontrado para expediente {CodigoExpediente}. Se regenerará.",
                    expediente.CodigoExpediente);

                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(expediente.CodigoQR, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrBytes = qrCode.GetGraphic(20);
                rutaImagenQR = await GuardarImagenQRAsync(expediente.CodigoExpediente, qrBytes);
            }

            _logger.LogInformation("QR existente obtenido para expediente {CodigoExpediente}", expediente.CodigoExpediente);

            return new QRGeneradoDTO
            {
                ExpedienteID = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                CodigoQR = expediente.CodigoQR,
                RutaImagenQR = rutaImagenQR,
                FechaGeneracion = expediente.FechaGeneracionQR!.Value,
                EstadoAnterior = expediente.EstadoActual.ToString(),
                EstadoNuevo = expediente.EstadoActual.ToString(),
                NombreCompleto = expediente.NombreCompleto
            };
        }

        /// <summary>
        /// Consulta un expediente por su código QR.
        /// Valida que el estado permita la acción del rol que consulta.
        /// Usado por Ambulancia (AceptarCustodia) y Vigilante (VerificarIngreso).
        /// </summary>
        public async Task<ExpedienteDTO> ConsultarPorQRAsync(string codigoQR)
        {
            var expediente = await _expedienteRepository.GetByCodigoQRAsync(codigoQR);

            if (expediente == null)
                throw new InvalidOperationException($"No se encontró expediente con código QR: {codigoQR}");

            // Validar que el estado permita verificar ingreso al mortuorio.
            // Solo EnTrasladoMortuorio es válido para el Vigilante.
            if (expediente.EstadoActual != EstadoExpediente.EnTrasladoMortuorio)
            {
                var mensaje = expediente.EstadoActual switch
                {
                    EstadoExpediente.EnPiso =>
                        "El expediente aún está en el servicio de origen. No ha sido recogido por Ambulancia.",
                    EstadoExpediente.PendienteDeRecojo =>
                        "El expediente está pendiente de recojo por Ambulancia. Aún no ha sido trasladado.",
                    EstadoExpediente.VerificacionRechazadaMortuorio =>
                        "Este expediente tiene una verificación rechazada. Espere la corrección de Enfermería antes de reintentar.",
                    EstadoExpediente.PendienteAsignacionBandeja =>
                        "Este expediente ya fue ingresado al mortuorio y está pendiente de asignación de bandeja.",
                    EstadoExpediente.EnBandeja =>
                        "Este expediente ya tiene una bandeja asignada dentro del mortuorio.",
                    EstadoExpediente.PendienteRetiro =>
                        "Este expediente ya está autorizado para retiro. No requiere nuevo ingreso.",
                    EstadoExpediente.Retirado =>
                        "Este cuerpo ya fue retirado del mortuorio. El expediente está cerrado.",
                    _ =>
                        $"El expediente está en estado '{expediente.EstadoActual}' y no puede verificarse en este momento."
                };

                _logger.LogWarning(
                    "Verificación bloqueada para Expediente {CodigoExpediente}. Estado: {Estado}",
                    expediente.CodigoExpediente, expediente.EstadoActual);

                throw new InvalidOperationException(mensaje);
            }

            _logger.LogInformation("Expediente {CodigoExpediente} consultado por QR", expediente.CodigoExpediente);

            var dto = _mapper.MapToExpedienteDTO(expediente)!;

            // Incluir bandeja activa si aplica
            if (expediente.EstadoActual == EstadoExpediente.EnBandeja ||
                expediente.EstadoActual == EstadoExpediente.PendienteRetiro)
            {
                var bandeja = await _bandejaRepository.GetByExpedienteIdAsync(expediente.ExpedienteID);
                if (bandeja != null)
                {
                    dto.CodigoBandeja = bandeja.Codigo;
                    dto.BandejaActualID = bandeja.BandejaID;
                }
            }

            return dto;
        }

        private async Task<string> GuardarImagenQRAsync(string codigoExpediente, byte[] qrBytes)
        {
            var directorioQR = Path.Combine(_environment.WebRootPath, "qr");
            if (!Directory.Exists(directorioQR))
                Directory.CreateDirectory(directorioQR);

            var nombreArchivo = $"{codigoExpediente}.png";
            var rutaCompleta = Path.Combine(directorioQR, nombreArchivo);
            await File.WriteAllBytesAsync(rutaCompleta, qrBytes);
            return $"/qr/{nombreArchivo}";
        }
    }
}