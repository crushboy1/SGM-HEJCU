using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/salidas")]
    [Authorize]
    public class SalidasController : ControllerBase
    {
        private readonly ISalidaMortuorioService _salidaService;
        private readonly ILogger<SalidasController> _logger;

        public SalidasController(
            ISalidaMortuorioService salidaService,
            ILogger<SalidasController> logger)
        {
            _salidaService = salidaService;
            _logger = logger;
        }

        /// <summary>
        /// Registra la salida física de un cuerpo del mortuorio.
        /// (Vigilante registra datos de funeraria/familiar).
        /// Cambia estado a 'Retirado' y libera la bandeja.
        /// </summary>
        [HttpPost("registrar")]
        [Authorize(Roles = "VigilanteSupervisor,VigilanciaMortuorio,Administrador")]
        [ProducesResponseType(typeof(SalidaDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RegistrarSalida([FromBody] RegistrarSalidaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var vigilanteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _salidaService.RegistrarSalidaAsync(dto, vigilanteId);

                _logger.LogInformation("Salida registrada para Expediente {ExpedienteID} por Vigilante {VigilanteID}",
                    dto.ExpedienteID, vigilanteId);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al registrar salida. Expediente: {ExpedienteID}",
                    dto.ExpedienteID);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno al registrar salida. Expediente: {ExpedienteID}", dto.ExpedienteID);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el registro de salida de un expediente.
        /// </summary>
        [HttpGet("expediente/{expedienteId}")]
        [ProducesResponseType(typeof(SalidaDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByExpedienteId(int expedienteId)
        {
            try
            {
                var salida = await _salidaService.GetByExpedienteIdAsync(expedienteId);
                if (salida == null)
                    return NotFound(new { message = "No se encontró registro de salida para este expediente" });

                return Ok(salida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registro de salida para Expediente ID {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un historial de salidas por rango de fechas.
        /// </summary>
        [HttpGet("historial")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(List<SalidaDTO>), 200)]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                var historial = await _salidaService.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de salidas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de todas las salidas.
        /// </summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(EstadisticasSalidaDTO), 200)]
        public async Task<IActionResult> GetEstadisticas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var stats = await _salidaService.GetEstadisticasAsync(fechaInicio, fechaFin);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de salidas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}