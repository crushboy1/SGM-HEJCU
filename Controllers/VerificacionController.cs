using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Verificacion;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/verificacion")]
    [Authorize]
    public class VerificacionController : ControllerBase
    {
        private readonly IVerificacionService _verificacionService;
        private readonly ILogger<VerificacionController> _logger;

        public VerificacionController(
            IVerificacionService verificacionService,
            ILogger<VerificacionController> logger)
        {
            _verificacionService = verificacionService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa un intento de verificación de ingreso al mortuorio.
        /// (Vigilante escanea brazalete).
        /// Maneja aprobación (Happy Path) y rechazo (Sad Path) atómicamente.
        /// </summary>
        [HttpPost("ingreso")]
        [Authorize(Roles = "VigilanteSupervisor,VigilanciaMortuorio")]
        [ProducesResponseType(typeof(VerificacionResultadoDTO), 200)]
        [ProducesResponseType(typeof(VerificacionResultadoDTO), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> VerificarIngreso([FromBody] VerificacionRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var vigilanteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _verificacionService.VerificarIngresoAsync(dto, vigilanteId);

                if (!resultado.Aprobada)
                {
                    _logger.LogWarning("Verificación Rechazada. Expediente QR: {QR}, Motivo: {Motivo}", dto.CodigoExpedienteBrazalete, resultado.MotivoRechazo);
                    // Aún si es rechazada, la operación fue exitosa (200 OK)
                    // El frontend debe interpretar el booleano 'Aprobada'.
                    return Ok(resultado);
                }

                _logger.LogInformation("Verificación Aprobada. Expediente QR: {QR}", dto.CodigoExpedienteBrazalete);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al verificar ingreso. QR: {QR}", dto.CodigoExpedienteBrazalete);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno al verificar ingreso. QR: {QR}", dto.CodigoExpedienteBrazalete);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el historial de intentos de verificación para un expediente.
        /// </summary>
        [HttpGet("expediente/{expedienteId}/historial")]
        [ProducesResponseType(typeof(List<VerificacionHistorialDTO>), 200)]
        public async Task<IActionResult> GetHistorialByExpedienteId(int expedienteId)
        {
            try
            {
                var historial = await _verificacionService.GetHistorialByExpedienteIdAsync(expedienteId);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de verificación para Expediente ID {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de verificaciones (aprobadas vs. rechazadas).
        /// </summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(EstadisticasVerificacionDTO), 200)]
        public async Task<IActionResult> GetEstadisticas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var stats = await _verificacionService.GetEstadisticasAsync(fechaInicio, fechaFin);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de verificación");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}