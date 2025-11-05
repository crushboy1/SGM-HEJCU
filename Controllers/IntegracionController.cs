using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.Services;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IntegracionController : ControllerBase
    {
        private readonly IIntegracionService _integracionService;
        private readonly ILogger<IntegracionController> _logger;

        public IntegracionController(
            IIntegracionService integracionService,
            ILogger<IntegracionController> logger)
        {
            _integracionService = integracionService;
            _logger = logger;
        }

        /// <summary>
        /// Consulta datos combinados de un paciente desde Galenhos y SIGEM
        /// </summary>
        /// <param name="hc">Historia Clínica del paciente</param>
        /// <returns>Datos demográficos y médicos combinados</returns>
        /// <response code="200">Datos encontrados exitosamente</response>
        /// <response code="404">Paciente no encontrado en Galenhos</response>
        /// <response code="401">No autorizado</response>
        [HttpGet("consultar-paciente/{hc}")]
        [ProducesResponseType(typeof(Business.DTOs.ConsultarPacienteDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ConsultarPaciente(string hc)
        {
            try
            {
                _logger.LogInformation(
                    "Solicitud de consulta integrada. HC: {HC}, Usuario: {Usuario}",
                    hc, User.Identity?.Name);

                var resultado = await _integracionService.ConsultarPacienteByHCAsync(hc);

                if (resultado == null || !resultado.ExisteEnGalenhos)
                {
                    _logger.LogWarning("Paciente no encontrado. HC: {HC}", hc);
                    return NotFound(new
                    {
                        mensaje = "Paciente no encontrado en Galenhos",
                        hc = hc,
                        sugerencia = "Verifique que el número de HC sea correcto"
                    });
                }

                _logger.LogInformation(
                    "Consulta integrada exitosa. HC: {HC}, ExisteEnSigem: {ExisteSigem}, Advertencias: {NumAdvertencias}",
                    hc, resultado.ExisteEnSigem, resultado.Advertencias.Count);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en consulta integrada. HC: {HC}", hc);
                return StatusCode(500, new
                {
                    mensaje = "Error al consultar datos del paciente",
                    detalle = ex.Message
                });
            }
        }
    }
}