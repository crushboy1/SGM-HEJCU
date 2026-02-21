using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities.Enums;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    /// <summary>
    /// Controlador para gestión de salidas del mortuorio.
    /// Gestiona registro de salidas (casos internos y externos), consultas y estadísticas.
    /// </summary>
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

        // ═══════════════════════════════════════════════════════════
        // REGISTRO DE SALIDA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra la salida física de un cuerpo del mortuorio.
        /// Vigilante registra datos de funeraria/familiar o autoridades.
        /// Cambia estado a 'Retirado' y libera la bandeja automáticamente.
        /// 
        /// Validaciones:
        /// - Expediente en estado PendienteRetiro
        /// - DocumentacionCompleta validada por Admisión
        /// - Referencias polimórficas según tipo (ActaRetiro o ExpedienteLegal)
        /// - Documentación específica según TipoSalida
        /// </summary>
        /// <param name="dto">Datos de la salida (incluye referencias polimórficas)</param>
        /// <returns>SalidaDTO con tiempo de permanencia calculado</returns>
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

                _logger.LogInformation(
                    "Salida registrada: Expediente {ExpedienteID}, Tipo: {TipoSalida}, Vigilante: {VigilanteID}",
                    dto.ExpedienteID, dto.TipoSalida, vigilanteId
                );

                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Expediente no encontrado: {ExpedienteID}", dto.ExpedienteID);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al registrar salida. Expediente: {ExpedienteID}", dto.ExpedienteID);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno al registrar salida. Expediente: {ExpedienteID}", dto.ExpedienteID);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
        // ═══════════════════════════════════════════════════════════
        // PRE-LLENADO DE FORMULARIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene datos pre-llenados desde el Acta de Retiro para facilitar el registro de salida.
        /// </summary>
        /// <param name="expedienteId">ID del expediente en estado PendienteRetiro</param>
        /// <returns>DatosPreLlenadoSalidaDTO o 404 si no cumple requisitos</returns>
        [HttpGet("prellenar/{expedienteId}")]
        [Authorize(Roles = "VigilanteSupervisor,VigilanciaMortuorio,Administrador")]
        [ProducesResponseType(typeof(DatosPreLlenadoSalidaDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetDatosParaPrellenar(int expedienteId)
        {
            try
            {
                var datos = await _salidaService.GetDatosParaPrellenarAsync(expedienteId);

                if (datos == null)
                {
                    _logger.LogWarning(
                        "No se encontraron datos de pre-llenado para expediente {ExpedienteID}. " +
                        "Causas posibles: no existe, no está en PendienteRetiro, o no tiene acta firmada.",
                        expedienteId
                    );

                    return NotFound(new
                    {
                        message = "No se pueden obtener datos de pre-llenado para este expediente",
                        posiblesCausas = new[]
                        {
                        "El expediente no existe",
                        "El expediente no está en estado 'Pendiente Retiro'",
                        "El expediente no tiene Acta de Retiro con PDF firmado"
                    }
                    });
                }

                _logger.LogInformation(
                    "Datos de pre-llenado obtenidos para expediente {CodigoExpediente}. " +
                    "TipoSalida: {TipoSalida}, PagosOK: {PagosOK}",
                    datos.CodigoExpediente, datos.TipoSalida, datos.PagosOK
                );

                return Ok(datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error interno al obtener datos de pre-llenado. Expediente: {ExpedienteID}",
                    expedienteId
                );
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
        // ═══════════════════════════════════════════════════════════
        // CONSULTAS INDIVIDUALES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el registro de salida de un expediente específico.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>SalidaDTO si existe, 404 si no hay registro</returns>
        [HttpGet("expediente/{expedienteId}")]
        [ProducesResponseType(typeof(SalidaDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByExpedienteId(int expedienteId)
        {
            try
            {
                var salida = await _salidaService.GetByExpedienteIdAsync(expedienteId);

                if (salida == null)
                    return NotFound(new { message = $"No se encontró registro de salida para el expediente ID {expedienteId}" });

                return Ok(salida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener salida del expediente {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CONSULTAS POR RANGO DE FECHAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene historial de salidas por rango de fechas.
        /// Útil para reportes mensuales/semanales.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <returns>Lista de SalidaDTO ordenadas por fecha descendente</returns>
        [HttpGet("historial")]
        [Authorize(Roles = "Administrador,JefeGuardia,VigilanteSupervisor")]
        [ProducesResponseType(typeof(List<SalidaDTO>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            if (fechaInicio > fechaFin)
                return BadRequest(new { message = "La fecha de inicio no puede ser mayor a la fecha fin" });

            try
            {
                var historial = await _salidaService.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);

                _logger.LogInformation(
                    "Historial de salidas consultado: {Cantidad} registros entre {FechaInicio} y {FechaFin}",
                    historial.Count, fechaInicio, fechaFin
                );

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de salidas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CONSULTAS ESPECIALIZADAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene salidas que excedieron el límite de permanencia (48 horas).
        /// Útil para reportes DIRESA/auditoría.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicio del rango (opcional)</param>
        /// <param name="fechaFin">Fecha fin del rango (opcional)</param>
        /// <returns>Lista de salidas con TiempoPermanencia > 48 horas</returns>
        [HttpGet("excedieron-limite")]
        [Authorize(Roles = "Administrador,JefeGuardia,VigilanteSupervisor")]
        [ProducesResponseType(typeof(List<SalidaDTO>), 200)]
        public async Task<IActionResult> GetSalidasExcedieronLimite(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var salidas = await _salidaService.GetSalidasExcedieronLimiteAsync(fechaInicio, fechaFin);

                _logger.LogInformation(
                    "Consulta de salidas que excedieron límite: {Cantidad} registros",
                    salidas.Count
                );

                return Ok(salidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener salidas que excedieron límite");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene salidas filtradas por tipo específico.
        /// Útil para estadísticas diferenciadas (casos internos vs externos).
        /// </summary>
        /// <param name="tipo">Tipo de salida: Familiar | AutoridadLegal | TrasladoHospital | Otro</param>
        /// <param name="fechaInicio">Fecha inicio del rango (opcional)</param>
        /// <param name="fechaFin">Fecha fin del rango (opcional)</param>
        /// <returns>Lista de salidas del tipo especificado</returns>
        [HttpGet("por-tipo/{tipo}")]
        [Authorize(Roles = "Administrador,JefeGuardia,VigilanteSupervisor")]
        [ProducesResponseType(typeof(List<SalidaDTO>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSalidasPorTipo(
            string tipo,
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            if (!Enum.TryParse<TipoSalida>(tipo, true, out var tipoSalida))
            {
                return BadRequest(new
                {
                    message = "Tipo de salida inválido",
                    valoresPermitidos = Enum.GetNames(typeof(TipoSalida))
                });
            }

            try
            {
                var salidas = await _salidaService.GetSalidasPorTipoAsync(tipoSalida, fechaInicio, fechaFin);

                _logger.LogInformation(
                    "Consulta de salidas por tipo '{Tipo}': {Cantidad} registros",
                    tipo, salidas.Count
                );

                return Ok(salidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener salidas por tipo {Tipo}", tipo);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ESTADÍSTICAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene estadísticas consolidadas de salidas.
        /// Incluye totales por tipo, porcentaje de incidentes, etc.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicio del rango (opcional - por defecto todo)</param>
        /// <param name="fechaFin">Fecha fin del rango (opcional - por defecto todo)</param>
        /// <returns>EstadisticasSalidaDTO con datos agregados</returns>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Administrador,JefeGuardia,VigilanteSupervisor")]
        [ProducesResponseType(typeof(EstadisticasSalidaDTO), 200)]
        public async Task<IActionResult> GetEstadisticas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var stats = await _salidaService.GetEstadisticasAsync(fechaInicio, fechaFin);

                _logger.LogInformation(
                    "Estadísticas de salidas consultadas: {TotalSalidas} salidas totales",
                    stats.TotalSalidas
                );

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