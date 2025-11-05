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
    public class CustodiaController : ControllerBase
    {
        private readonly ICustodiaService _custodiaService;
        private readonly ILogger<CustodiaController> _logger;

        public CustodiaController(
            ICustodiaService custodiaService,
            ILogger<CustodiaController> logger)
        {
            _custodiaService = custodiaService;
            _logger = logger;
        }

        /// <summary>
        /// Realizar traspaso de custodia (Enfermería → Ambulancia)
        /// El técnico de ambulancia escanea el QR y recibe la custodia
        /// </summary>
        [HttpPost("traspasos")]
        [Authorize(Roles = "Ambulancia")]
        [ProducesResponseType(typeof(TraspasoRealizadoDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RealizarTraspaso([FromBody] RealizarTraspasoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Obtener ID del usuario autenticado (técnico de ambulancia)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Usuario no autenticado" });

                var usuarioDestinoId = int.Parse(userIdClaim);
                var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation(
                    "Usuario {UsuarioNombre} (ID: {UsuarioId}) solicita recibir custodia. QR escaneado: {CodigoQR}",
                    usuarioNombre,
                    usuarioDestinoId,
                    dto.CodigoQR);

                var resultado = await _custodiaService.RealizarTraspasoAsync(dto, usuarioDestinoId);

                _logger.LogInformation(
                    "Traspaso de custodia exitoso: Expediente {CodigoExpediente}. " +
                    "{UsuarioOrigen} ({RolOrigen}) → {UsuarioDestino} ({RolDestino}). " +
                    "Estado: {EstadoAnterior} → {EstadoNuevo}",
                    resultado.CodigoExpediente,
                    resultado.UsuarioOrigen,
                    resultado.RolOrigen,
                    resultado.UsuarioDestino,
                    resultado.RolDestino,
                    resultado.EstadoAnterior,
                    resultado.EstadoNuevo);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación en traspaso de custodia. QR: {CodigoQR}", dto.CodigoQR);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar traspaso de custodia. QR: {CodigoQR}", dto.CodigoQR);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener historial completo de custodia de un expediente
        /// Muestra todas las transferencias en orden cronológico
        /// </summary>
        [HttpGet("expediente/{expedienteId}/historial")]
        [ProducesResponseType(typeof(List<CustodiaTransferenciaDTO>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetHistorial(int expedienteId)
        {
            try
            {
                var historial = await _custodiaService.GetHistorialCustodiaAsync(expedienteId);

                _logger.LogInformation(
                    "Historial de custodia consultado para expediente {ExpedienteId}. Total transferencias: {Total}",
                    expedienteId,
                    historial.Count);

                return Ok(historial);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Expediente no encontrado: {ExpedienteId}", expedienteId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de custodia. ExpedienteId: {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener la custodia actual de un expediente
        /// Muestra quién tiene el cuerpo actualmente (información simplificada)
        /// </summary>
        [HttpGet("expediente/{expedienteId}/actual")]
        [ProducesResponseType(typeof(CustodiaActualDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCustodiaActual(int expedienteId)
        {
            try
            {
                var custodia = await _custodiaService.GetUltimaCustodiaAsync(expedienteId);

                if (custodia == null)
                {
                    _logger.LogInformation(
                        "No se encontró custodia para expediente {ExpedienteId}",
                        expedienteId);
                    return NotFound(new { message = "No se encontró custodia para este expediente" });
                }

                _logger.LogInformation(
                    "Custodia actual consultada para expediente {ExpedienteId}. En poder de: {UsuarioActual}",
                    expedienteId,
                    custodia.UsuarioActualNombre);

                return Ok(custodia);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Expediente no encontrado: {ExpedienteId}", expedienteId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener custodia actual. ExpedienteId: {ExpedienteId}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}