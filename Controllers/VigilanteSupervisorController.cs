using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Vigilancia;
using SisMortuorio.Business.Services;

namespace SisMortuorio.Controllers
{
    /// <summary>
    /// Endpoints exclusivos para el módulo Supervisor de Vigilancia.
    /// Solo lectura — VigSup consulta expedientes y semáforos pero no modifica datos.
    ///
    /// ACCESO: VigilanteSupervisor, Administrador, JefeGuardia
    ///
    /// ENDPOINTS:
    ///   GET /api/VigilanteSupervisor/expedientes?busqueda=xxx
    ///   GET /api/VigilanteSupervisor/expedientes/{id}/detalle
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "VigilanteSupervisor,Administrador,JefeGuardia")]
    public class VigilanteSupervisorController(
        IVigilanteSupervisorService vigilanciaService,
        ILogger<VigilanteSupervisorController> logger) : ControllerBase
    {
        private readonly IVigilanteSupervisorService _vigilanciaService = vigilanciaService;
        private readonly ILogger<VigilanteSupervisorController> _logger = logger;

        /// <summary>
        /// Obtiene todos los expedientes con semáforo de deudas precalculado.
        ///
        /// SEMÁFORO (bool?):
        ///   null  = sin registro de deuda (amarillo)
        ///   true  = bloquea retiro (rojo)
        ///   false = no bloquea (verde)
        ///
        /// Si BypassDeudaAutorizado = true: ambos semáforos vienen como false
        /// con descripción "Bypass autorizado" — la UI puede mostrar indicador especial.
        /// </summary>
        /// <param name="busqueda">
        /// Texto libre. Busca en HC, NumeroDocumento y NombreCompleto.
        /// Omitir o vacío para traer todos.
        /// </param>
        [HttpGet("expedientes")]
        [ProducesResponseType(typeof(List<ExpedienteVigilanciaDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteVigilanciaDTO>>> ObtenerExpedientes(
            [FromQuery] string? busqueda = null)
        {
            try
            {
                var resultado = await _vigilanciaService.ObtenerExpedientesAsync(busqueda);

                _logger.LogInformation(
                    "[VigSup] Consulta de expedientes. Búsqueda: '{Busqueda}'. Total: {Total}",
                    busqueda ?? "(todos)", resultado.Count);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VigSup] Error al obtener expedientes. Búsqueda: {Busqueda}", busqueda);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de un expediente para el modal Ver.
        /// Incluye semáforo expandido, responsable de retiro y Jefe de Guardia.
        /// No incluye montos económicos.
        /// </summary>
        /// <param name="id">ID del expediente</param>
        [HttpGet("expedientes/{id:int}/detalle")]
        [ProducesResponseType(typeof(DetalleVigilanciaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DetalleVigilanciaDTO>> ObtenerDetalle(int id)
        {
            try
            {
                var resultado = await _vigilanciaService.ObtenerDetalleAsync(id);

                if (resultado == null)
                    return NotFound(new { message = $"Expediente {id} no encontrado" });

                _logger.LogInformation(
                    "[VigSup] Detalle de expediente consultado. ID: {ID}, Código: {Codigo}",
                    id, resultado.CodigoExpediente);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VigSup] Error al obtener detalle del expediente {ID}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}