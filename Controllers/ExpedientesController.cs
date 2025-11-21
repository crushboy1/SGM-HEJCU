using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities.Enums;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpedientesController : ControllerBase
    {
        private readonly IExpedienteService _expedienteService;
        private readonly ILogger<ExpedientesController> _logger;

        public ExpedientesController(
            IExpedienteService expedienteService,
            ILogger<ExpedientesController> logger)
        {
            _expedienteService = expedienteService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los expedientes
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ExpedienteDTO>), 200)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var expedientes = await _expedienteService.GetAllAsync();
                return Ok(expedientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener expedientes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener expediente por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExpedienteDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var expediente = await _expedienteService.GetByIdAsync(id);

                if (expediente == null)
                    return NotFound(new { message = $"Expediente con ID {id} no encontrado" });

                return Ok(expediente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener expediente {ExpedienteId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Buscar expedientes con filtros
        /// </summary>
        [HttpGet("buscar")]
        [ProducesResponseType(typeof(List<ExpedienteDTO>), 200)]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? hc,
            [FromQuery] string? numeroDocumento,
            [FromQuery] string? servicio,
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta,
            [FromQuery] EstadoExpediente? estado)
        {
            try
            {
                var expedientes = await _expedienteService.GetByFiltrosAsync(
                    hc,
                    numeroDocumento,
                    servicio,
                    fechaDesde,
                    fechaHasta,
                    estado);

                return Ok(expedientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar expedientes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crear nuevo expediente (solo Enfermería)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(ExpedienteDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateExpedienteDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Obtener ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Usuario no autenticado" });

                var userId = int.Parse(userIdClaim);

                var expediente = await _expedienteService.CreateAsync(dto, userId);

                _logger.LogInformation(
                    "Expediente {CodigoExpediente} creado por usuario {UserId}",
                    expediente.CodigoExpediente,
                    userId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = expediente.ExpedienteID },
                    expediente);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación fallida al crear expediente");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear expediente");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualizar expediente existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EnfermeriaTecnica,EnfermeriaLicenciada,SupervisoraEnfermeria,Administrador")]
        [ProducesResponseType(typeof(ExpedienteDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExpedienteDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var expediente = await _expedienteService.UpdateAsync(id, dto);

                if (expediente == null)
                    return NotFound(new { message = $"Expediente con ID {id} no encontrado" });

                _logger.LogInformation("Expediente {ExpedienteId} actualizado", id);

                return Ok(expediente);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación fallida al actualizar expediente {ExpedienteId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar expediente {ExpedienteId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Validar si HC es único
        /// </summary>
        [HttpGet("validar-hc/{hc}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> ValidarHC(string hc)
        {
            try
            {
                var esUnico = await _expedienteService.ValidarHCUnicoAsync(hc);
                return Ok(new { hc, esUnico });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar HC {HC}", hc);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Validar si certificado SINADEF es único
        /// </summary>
        [HttpGet("validar-sinadef/{certificado}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> ValidarSINADEF(string certificado)
        {
            try
            {
                var esUnico = await _expedienteService.ValidarCertificadoSINADEFUnicoAsync(certificado);
                return Ok(new { certificado, esUnico });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar certificado SINADEF {Certificado}", certificado);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}