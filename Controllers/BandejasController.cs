using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BandejasController : ControllerBase
    {
        private readonly IBandejaService _bandejaService;
        private readonly ILogger<BandejasController> _logger;

        public BandejasController(
            IBandejaService bandejaService,
            ILogger<BandejasController> logger)
        {
            _bandejaService = bandejaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el estado actual de todas las bandejas (mapa visual).
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(List<BandejaDTO>), 200)]
        public async Task<IActionResult> GetOcupacionDashboard()
        {
            try
            {
                var dashboard = await _bandejaService.GetOcupacionDashboardAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard de bandejas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una lista de bandejas disponibles (para dropdown).
        /// </summary>
        [HttpGet("disponibles")]
        [Authorize(Roles = "Ambulancia, Administrador")]
        [ProducesResponseType(typeof(List<BandejaDisponibleDTO>), 200)]
        public async Task<IActionResult> GetDisponibles()
        {
            try
            {
                var disponibles = await _bandejaService.GetDisponiblesAsync();
                return Ok(disponibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bandejas disponibles");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de ocupación del mortuorio.
        /// </summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(EstadisticasBandejaDTO), 200)]
        public async Task<IActionResult> GetEstadisticas()
        {
            try
            {
                var stats = await _bandejaService.GetEstadisticasAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de bandejas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Asigna un expediente a una bandeja (Técnico de Ambulancia).
        /// </summary>
        [HttpPost("asignar")]
        [Authorize(Roles = "Ambulancia")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AsignarBandeja([FromBody] AsignarBandejaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _bandejaService.AsignarBandejaAsync(dto, usuarioId);

                _logger.LogInformation("Usuario {UsuarioID} asignó Expediente {ExpedienteID} a Bandeja {BandejaID}",
                    usuarioId, dto.ExpedienteID, dto.BandejaID);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al asignar bandeja. Expediente: {ExpedienteID}, Bandeja: {BandejaID}",
                    dto.ExpedienteID, dto.BandejaID);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar bandeja. Expediente: {ExpedienteID}", dto.ExpedienteID);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Pone una bandeja en estado de Mantenimiento.
        /// </summary>
        [HttpPut("{bandejaId}/mantenimiento/iniciar")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IniciarMantenimiento(int bandejaId, [FromBody] string observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones))
                return BadRequest(new { message = "Se requiere un motivo/observaciones para iniciar mantenimiento." });

            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _bandejaService.IniciarMantenimientoAsync(bandejaId, observaciones, usuarioId);

                _logger.LogInformation("Usuario {UsuarioID} puso Bandeja {BandejaID} en mantenimiento", usuarioId, bandejaId);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al iniciar mantenimiento de Bandeja {BandejaID}", bandejaId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar mantenimiento de Bandeja {BandejaID}", bandejaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Saca una bandeja de Mantenimiento y la pone Disponible.
        /// </summary>
        [HttpPut("{bandejaId}/mantenimiento/finalizar")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> FinalizarMantenimiento(int bandejaId)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resultado = await _bandejaService.FinalizarMantenimientoAsync(bandejaId, usuarioId);

                _logger.LogInformation("Usuario {UsuarioID} finalizó mantenimiento de Bandeja {BandejaID}", usuarioId, bandejaId);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al finalizar mantenimiento de Bandeja {BandejaID}", bandejaId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al finalizar mantenimiento de Bandeja {BandejaID}", bandejaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}