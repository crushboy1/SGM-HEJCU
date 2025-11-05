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
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<QRService> _logger;
        private readonly IStateMachineService _stateMachineService;
        private readonly IExpedienteMapperService _mapper; // <-- 1. Inyectar el Mapper

        /// <summary>
        /// Constructor para inyectar todas las dependencias necesarias.
        /// </summary>
        public QRService(
            IExpedienteRepository expedienteRepository,
            IWebHostEnvironment environment,
            ILogger<QRService> logger,
            IStateMachineService stateMachineService,
            IExpedienteMapperService mapper) // <-- 2. Recibir el Mapper
        {
            _expedienteRepository = expedienteRepository;
            _environment = environment;
            _logger = logger;
            _stateMachineService = stateMachineService;
            _mapper = mapper; // <-- 3. Asignar el Mapper
        }

        /// <summary>
        /// Genera un Código QR por primera vez para un expediente.
        /// Actualiza el estado del expediente a "PendienteDeRecojo".
        /// </summary>
        public async Task<QRGeneradoDTO> GenerarQRAsync(int expedienteId)
        {
            // 1. Obtener expediente
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);

            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            // 2. Validar que NO tenga QR previamente generado
            if (!string.IsNullOrEmpty(expediente.CodigoQR))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} ya tiene QR generado. " +
                    "Use el endpoint de reimpresión si necesita volver a imprimir el brazalete.");

            // 3. Validar estado actual usando la StateMachine
            if (!_stateMachineService.CanFire(expediente, TriggerExpediente.GenerarQR))
            {
                var triggers = await _stateMachineService.GetPermittedTriggersAsync(expediente);
                throw new InvalidOperationException(
                    $"El expediente está en estado '{expediente.EstadoActual}'. " +
                    $"No se puede ejecutar la acción '{TriggerExpediente.GenerarQR}'. " +
                    $"Acciones válidas: {string.Join(", ", triggers)}");
            }

            // Guardar estado anterior ANTES de la transición
            var estadoAnterior = expediente.EstadoActual;

            // 4. Generar código QR (usa el CodigoExpediente como contenido)
            var codigoQR = expediente.CodigoExpediente;

            // 5. Crear imagen QR usando QRCoder
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(codigoQR, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20); // 20 pixeles por módulo

            // 6. Guardar imagen en disco
            var rutaImagenQR = await GuardarImagenQRAsync(expediente.CodigoExpediente, qrBytes);

            // 7. Actualizar expediente en BD

            // Dispara la máquina de estados. Esto actualiza 'expediente.EstadoActual' en memoria.
            await _stateMachineService.FireAsync(expediente, TriggerExpediente.GenerarQR);

            expediente.CodigoQR = codigoQR;
            expediente.FechaGeneracionQR = DateTime.Now;
            // El estado ya fue actualizado por la máquina, solo guardamos los cambios.
            await _expedienteRepository.UpdateAsync(expediente);

            _logger.LogInformation(
                "QR generado para expediente {CodigoExpediente}. Estado: {EstadoAnterior} → {EstadoNuevo}",
                expediente.CodigoExpediente,
                estadoAnterior,
                expediente.EstadoActual);

            // 8. Devolver DTO
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
            // 1. Obtener expediente
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);

            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            // 2. Validar que SÍ tenga QR generado
            if (string.IsNullOrEmpty(expediente.CodigoQR))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} no tiene QR generado. " +
                    "Debe generar el QR primero.");

            // 3. Construir ruta de la imagen
            var rutaImagenQR = $"/qr/{expediente.CodigoExpediente}.png";

            // 4. Verificar que el archivo existe físicamente
            var rutaFisica = Path.Combine(_environment.WebRootPath, "qr", $"{expediente.CodigoExpediente}.png");
            if (!File.Exists(rutaFisica))
            {
                _logger.LogWarning(
                    "Archivo QR no encontrado para expediente {CodigoExpediente}. Se regenerará.",
                    expediente.CodigoExpediente);

                // Regenerar la imagen (pero NO cambiar el código ni el estado)
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(expediente.CodigoQR, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrBytes = qrCode.GetGraphic(20);

                rutaImagenQR = await GuardarImagenQRAsync(expediente.CodigoExpediente, qrBytes);
            }

            _logger.LogInformation(
                "QR existente obtenido para expediente {CodigoExpediente}",
                expediente.CodigoExpediente);

            // 5. Devolver DTO
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
        /// Consulta un expediente completo usando su código QR.
        /// (Usado por la app móvil de Ambulancia al escanear).
        /// </summary>
        public async Task<ExpedienteDTO> ConsultarPorQRAsync(string codigoQR)
        {
            // 1. Buscar expediente por código QR
            var expediente = await _expedienteRepository.GetByCodigoQRAsync(codigoQR);

            if (expediente == null)
                throw new InvalidOperationException($"No se encontró expediente con código QR: {codigoQR}");

            _logger.LogInformation(
                "Expediente {CodigoExpediente} consultado por QR",
                expediente.CodigoExpediente);

            // 2. Mapear a DTO
            return _mapper.MapToExpedienteDTO(expediente)!;
        }

        /// <summary>
        /// Método auxiliar privado para guardar la imagen QR en el servidor.
        /// </summary>
        private async Task<string> GuardarImagenQRAsync(string codigoExpediente, byte[] qrBytes)
        {
            // Usar WebRootPath para encontrar la carpeta 'wwwroot'
            var directorioQR = Path.Combine(_environment.WebRootPath, "qr");
            if (!Directory.Exists(directorioQR))
            {
                Directory.CreateDirectory(directorioQR);
            }

            // Guardar archivo
            var nombreArchivo = $"{codigoExpediente}.png";
            var rutaCompleta = Path.Combine(directorioQR, nombreArchivo);

            await File.WriteAllBytesAsync(rutaCompleta, qrBytes);

            // Devolver ruta relativa para usar en URL
            return $"/qr/{nombreArchivo}";
        }
    }
}