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
    public class BandejasController(
        IBandejaService bandejaService,
        ILogger<BandejasController> logger) : ControllerBase
    {
        private readonly IBandejaService _bandejaService = bandejaService;
        private readonly ILogger<BandejasController> _logger = logger;

        private int UsuarioActualId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Todas las bandejas con estado actual (mapa visual).</summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(List<BandejaDTO>), 200)]
        public async Task<IActionResult> GetOcupacionDashboard()
        {
            try
            {
                return Ok(await _bandejaService.GetOcupacionDashboardAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard de bandejas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Detalle de una bandeja por ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var bandeja = await _bandejaService.GetByIdAsync(id);
                return bandeja == null
                    ? NotFound(new { message = $"Bandeja {id} no encontrada" })
                    : Ok(bandeja);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bandeja {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Bandejas disponibles (dropdown de asignación).</summary>
        [HttpGet("disponibles")]
        [Authorize(Roles = "Ambulancia, Administrador")]
        [ProducesResponseType(typeof(List<BandejaDisponibleDTO>), 200)]
        public async Task<IActionResult> GetDisponibles()
        {
            try { return Ok(await _bandejaService.GetDisponiblesAsync()); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bandejas disponibles");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Estadísticas de ocupación del mortuorio.</summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor, " +
                           "EnfermeriaTecnica, EnfermeriaLicenciada")]
        [ProducesResponseType(typeof(EstadisticasBandejaDTO), 200)]
        public async Task<IActionResult> GetEstadisticas()
        {
            try { return Ok(await _bandejaService.GetEstadisticasAsync()); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Asigna un expediente a una bandeja disponible.</summary>
        [HttpPost("asignar")]
        [Authorize(Roles = "Ambulancia, Administrador")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AsignarBandeja([FromBody] AsignarBandejaDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var resultado = await _bandejaService.AsignarBandejaAsync(dto, UsuarioActualId);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar bandeja");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MANTENIMIENTO — ACTUALIZADO v2
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Pone una bandeja en Mantenimiento con datos completos del modal.
        /// CAMBIOS v2: acepta IniciarMantenimientoDTO en lugar de string.
        /// </summary>
        [HttpPut("{bandejaId}/mantenimiento/iniciar")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IniciarMantenimiento(
            int bandejaId,
            [FromBody] IniciarMantenimientoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Motivo))
                return BadRequest(new { message = "El motivo del mantenimiento es obligatorio." });

            if (!MotivoMantenimiento.EsValido(dto.Motivo))
                return BadRequest(new
                {
                    message = $"Motivo inválido. Valores permitidos: " +
                              string.Join(", ", MotivoMantenimiento.Valores)
                });

            try
            {
                var resultado = await _bandejaService.IniciarMantenimientoAsync(
                    bandejaId, dto, UsuarioActualId);

                _logger.LogInformation(
                    "Usuario {UID} → Bandeja {BID} en Mantenimiento. Motivo: {Motivo}",
                    UsuarioActualId, bandejaId, dto.Motivo);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar mantenimiento de Bandeja {BID}", bandejaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Finaliza el mantenimiento y pone la bandeja Disponible.</summary>
        [HttpPut("{bandejaId}/mantenimiento/finalizar")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> FinalizarMantenimiento(int bandejaId)
        {
            try
            {
                var resultado = await _bandejaService.FinalizarMantenimientoAsync(
                    bandejaId, UsuarioActualId);

                _logger.LogInformation(
                    "Usuario {UID} finalizó mantenimiento de Bandeja {BID}",
                    UsuarioActualId, bandejaId);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al finalizar mantenimiento de Bandeja {BID}", bandejaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Libera manualmente una bandeja ocupada (emergencia/corrección admin).
        /// Requiere motivo obligatorio para auditoría.
        /// </summary>
        [HttpPut("{bandejaId}/liberar-manualmente")]
        [Authorize(Roles = "Administrador, JefeGuardia, VigilanteSupervisor")]
        [ProducesResponseType(typeof(BandejaDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LiberarManualmente(
            int bandejaId,
            [FromBody] LiberarBandejaManualDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.BandejaID != bandejaId)
                return BadRequest(new { message = "El ID de la bandeja no coincide." });

            try
            {
                dto.UsuarioLiberaID = UsuarioActualId;
                var resultado = await _bandejaService.LiberarManualmenteAsync(dto);

                _logger.LogWarning(
                    "LIBERACIÓN MANUAL: Usuario {UID} liberó Bandeja {BID}. Motivo: {Motivo}",
                    UsuarioActualId, bandejaId, dto.MotivoLiberacion);

                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al liberar manualmente Bandeja {BID}", bandejaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}