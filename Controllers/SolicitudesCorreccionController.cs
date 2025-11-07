using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Solicitud;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/solicitudes-correccion")]
    [Authorize]
    public class SolicitudesCorreccionController : ControllerBase
    {
        private readonly ISolicitudCorreccionService _solicitudService;
        private readonly ILogger<SolicitudesCorreccionController> _logger;

        public SolicitudesCorreccionController(
            ISolicitudCorreccionService solicitudService,
            ILogger<SolicitudesCorreccionController> logger)
        {
            _solicitudService = solicitudService;
            _logger = logger;
        }

        /// <summary>
        /// Resuelve una solicitud de corrección pendiente (Rol: Enfermería).
        /// Cambia estado del expediente a 'EnTrasladoMortuorio'
        /// </summary>
        [HttpPost("{solicitudId}/resolver")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria")]
        [ProducesResponseType(typeof(SolicitudCorreccionDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResolverSolicitud(int solicitudId, [FromBody] ResolverSolicitudDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _solicitudService.ResolverSolicitudAsync(solicitudId, dto, usuarioId);

                _logger.LogInformation("Solicitud {SolicitudID} resuelta por Usuario {UsuarioID}",
                    solicitudId, usuarioId);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al resolver Solicitud {SolicitudID}", solicitudId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno al resolver Solicitud {SolicitudID}", solicitudId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene todas las solicitudes de corrección pendientes.
        /// </summary>
        [HttpGet("pendientes")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(List<SolicitudCorreccionDTO>), 200)]
        public async Task<IActionResult> GetPendientes()
        {
            try
            {
                var solicitudes = await _solicitudService.GetPendientesAsync();
                return Ok(solicitudes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener solicitudes pendientes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene las solicitudes pendientes para un servicio específico.
        /// </summary>
        [HttpGet("pendientes/servicio/{servicio}")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(List<SolicitudCorreccionDTO>), 200)]
        public async Task<IActionResult> GetPendientesPorServicio(string servicio)
        {
            try
            {
                var solicitudes = await _solicitudService.GetPendientesByServicioAsync(servicio);
                return Ok(solicitudes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener solicitudes pendientes para servicio {Servicio}", servicio);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el historial de solicitudes para un expediente.
        /// </summary>
        [HttpGet("expediente/{expedienteId}/historial")]
        [ProducesResponseType(typeof(List<SolicitudCorreccionDTO>), 200)]
        public async Task<IActionResult> GetHistorialPorExpediente(int expedienteId)
        {
            try
            {
                var historial = await _solicitudService.GetHistorialByExpedienteIdAsync(expedienteId);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de solicitudes para Expediente ID {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una solicitud específica por su ID.
        /// </summary>
        [HttpGet("{solicitudId}")]
        [ProducesResponseType(typeof(SolicitudCorreccionDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int solicitudId)
        {
            try
            {
                var solicitud = await _solicitudService.GetByIdAsync(solicitudId);
                if (solicitud == null)
                    return NotFound(new { message = $"Solicitud ID {solicitudId} no encontrada" });

                return Ok(solicitud);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener Solicitud ID {SolicitudID}", solicitudId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas sobre las solicitudes de corrección.
        /// </summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador, JefeGuardia, SupervisoraEnfermeria")]
        [ProducesResponseType(typeof(EstadisticasSolicitudDTO), 200)]
        public async Task<IActionResult> GetEstadisticas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var stats = await _solicitudService.GetEstadisticasAsync(fechaInicio, fechaFin);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de solicitudes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}