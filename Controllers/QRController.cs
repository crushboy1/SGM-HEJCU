using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QRController : ControllerBase
    {
        private readonly IQRService _qrService;
        private readonly IBrazaleteService _brazaleteService;
        private readonly ILogger<QRController> _logger;

        public QRController(
            IQRService qrService,
            IBrazaleteService brazaleteService,
            ILogger<QRController> logger)
        {
            _qrService = qrService;
            _brazaleteService = brazaleteService;
            _logger = logger;
        }

        /// <summary>
        /// Genera QR por primera vez para un expediente
        /// Cambia estado a "Pendiente de Recojo"
        /// Solo puede usarse una vez por expediente
        /// </summary>
        [HttpPost("{expedienteId}/generar")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(QRGeneradoDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerarQR(int expedienteId)
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation(
                    "Usuario {UsuarioNombre} (ID: {UsuarioId}) solicita generar QR para expediente {ExpedienteId}",
                    usuarioNombre,
                    usuarioId,
                    expedienteId);

                var resultado = await _qrService.GenerarQRAsync(expedienteId);

                _logger.LogInformation(
                    "QR generado exitosamente para expediente {CodigoExpediente}. Estado: {EstadoAnterior} → {EstadoNuevo}",
                    resultado.CodigoExpediente,
                    resultado.EstadoAnterior,
                    resultado.EstadoNuevo);

                return CreatedAtAction(
                    nameof(ObtenerQR),
                    new { expedienteId = resultado.ExpedienteID },
                    resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al generar QR para expediente {ExpedienteId}", expedienteId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar QR para expediente {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el QR existente de un expediente (para reimpresión)
        /// NO regenera el QR ni cambia estado
        /// </summary>
        [HttpGet("{expedienteId}/obtener")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(QRGeneradoDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ObtenerQR(int expedienteId)
        {
            try
            {
                var resultado = await _qrService.ObtenerQRExistenteAsync(expedienteId);

                _logger.LogInformation(
                    "QR existente obtenido para expediente {CodigoExpediente}",
                    resultado.CodigoExpediente);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al obtener QR para expediente {ExpedienteId}", expedienteId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener QR para expediente {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Consulta expediente completo por código QR escaneado
        /// Usado por Ambulancia al escanear el QR
        /// </summary>
        [HttpGet("consultar/{codigoQR}")]
        [ProducesResponseType(typeof(ExpedienteDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConsultarPorQR(string codigoQR)
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation(
                    "Usuario {UsuarioNombre} (ID: {UsuarioId}) consulta expediente por QR: {CodigoQR}",
                    usuarioNombre,
                    usuarioId,
                    codigoQR);

                var resultado = await _qrService.ConsultarPorQRAsync(codigoQR);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Código QR no encontrado: {CodigoQR}", codigoQR);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar por QR: {CodigoQR}", codigoQR);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Imprime brazalete por primera vez (después de generar QR)
        /// </summary>
        [HttpPost("{expedienteId}/imprimir-brazalete")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ImprimirBrazalete(int expedienteId)
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation(
                    "Usuario {UsuarioNombre} (ID: {UsuarioId}) solicita imprimir brazalete para expediente {ExpedienteId}",
                    usuarioNombre,
                    usuarioId,
                    expedienteId);

                var brazalete = await _brazaleteService.GenerarBrazaleteAsync(expedienteId, esReimpresion: false);

                _logger.LogInformation(
                    "Brazalete impreso para expediente {CodigoExpediente}",
                    brazalete.CodigoExpediente);

                // Devolver PDF como descarga directa desde memoria
                return File(
                    brazalete.PDFBytes!,
                    "application/pdf",
                    $"brazalete-{brazalete.CodigoExpediente}.pdf");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al imprimir brazalete para expediente {ExpedienteId}", expedienteId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al imprimir brazalete para expediente {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Reimprime brazalete con el MISMO QR (si se dañó el brazalete físico)
        /// NO cambia estado ni regenera QR
        /// </summary>
        [HttpGet("{expedienteId}/reimprimir-brazalete")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReimprimirBrazalete(int expedienteId)
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation(
                    "Usuario {UsuarioNombre} (ID: {UsuarioId}) solicita REIMPRIMIR brazalete para expediente {ExpedienteId}",
                    usuarioNombre,
                    usuarioId,
                    expedienteId);

                var brazalete = await _brazaleteService.GenerarBrazaleteAsync(expedienteId, esReimpresion: true);

                _logger.LogInformation(
                    "Brazalete REIMPRESO para expediente {CodigoExpediente}",
                    brazalete.CodigoExpediente);

                // Devolver PDF como descarga directa desde memoria
                return File(
                    brazalete.PDFBytes!,
                    "application/pdf",
                    $"brazalete-{brazalete.CodigoExpediente}-reimpresion.pdf");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al reimprimir brazalete para expediente {ExpedienteId}", expedienteId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reimprimir brazalete para expediente {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}