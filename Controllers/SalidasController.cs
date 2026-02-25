using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities.Enums;
using System.Security.Claims;

namespace SisMortuorio.Controllers;

/// <summary>
/// Controlador para gestión de salidas del mortuorio.
/// Gestiona registro de salidas, consultas y estadísticas.
///
/// ROLES:
/// - VigilanteSupervisor / VigilanciaMortuorio: registrar salida y pre-llenado
/// - JefeGuardia / VigilanteSupervisor / Administrador: historial, estadísticas y reportes
/// - Cualquier rol autenticado: consulta individual por expediente
/// </summary>
[ApiController]
[Route("api/salidas")]
[Authorize]
public class SalidasController(
    ISalidaMortuorioService salidaService,
    ILogger<SalidasController> logger) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE SALIDA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la salida física de un cuerpo del mortuorio.
    /// El Vigilante confirma el retiro y registra datos de funeraria/vehículo.
    /// Cambia estado a Retirado y libera la bandeja automáticamente (RN-34).
    ///
    /// PRECONDICIONES:
    /// - Expediente en estado PendienteRetiro
    /// - ActaRetiro con PDF firmado cargado
    /// - Familiar: NombreFuneraria, ConductorFuneraria, DNIConductor y PlacaVehiculo obligatorios
    /// - AutoridadLegal: solo PlacaVehiculo obligatorio
    /// </summary>
    /// <param name="dto">Datos capturados por el Vigilante</param>
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
            var registradoPorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var resultado = await salidaService.RegistrarSalidaAsync(dto, registradoPorId);

            logger.LogInformation(
                "Salida registrada — ExpedienteID: {ExpedienteID}, RegistradoPorID: {RegistradoPorID}",
                dto.ExpedienteID, registradoPorId
            );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex,
                "Expediente no encontrado al registrar salida. ExpedienteID: {ExpedienteID}",
                dto.ExpedienteID);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex,
                "Error de negocio al registrar salida. ExpedienteID: {ExpedienteID}",
                dto.ExpedienteID);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error interno al registrar salida. ExpedienteID: {ExpedienteID}",
                dto.ExpedienteID);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde ActaRetiro para el formulario de salida.
    /// El Vigilante recibe datos del responsable en modo readonly y completa los de funeraria.
    /// </summary>
    /// <param name="expedienteId">ID del expediente en estado PendienteRetiro</param>
    /// <returns>DatosPreLlenadoSalidaDTO o 404 si no cumple requisitos</returns>
    [HttpGet("prellenar/{expedienteId:int}")]
    [Authorize(Roles = "VigilanteSupervisor,VigilanciaMortuorio,Administrador")]
    [ProducesResponseType(typeof(DatosPreLlenadoSalidaDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDatosParaPrellenar(int expedienteId)
    {
        try
        {
            var datos = await salidaService.GetDatosParaPrellenarAsync(expedienteId);

            if (datos == null)
            {
                logger.LogWarning(
                    "No se encontraron datos de pre-llenado para ExpedienteID: {ExpedienteID}",
                    expedienteId
                );
                return NotFound(new
                {
                    message = "No se pueden obtener datos de pre-llenado para este expediente",
                    posiblesCausas = new[]
                    {
                        "El expediente no existe",
                        "El expediente no está en estado Pendiente Retiro",
                        "El Acta de Retiro no tiene PDF firmado cargado"
                    }
                });
            }

            logger.LogInformation(
                "Datos de pre-llenado obtenidos — Expediente: {CodigoExpediente}, " +
                "TipoSalida: {TipoSalida}, PuedeRegistrarSalida: {PuedeRegistrarSalida}",
                datos.CodigoExpediente, datos.TipoSalida, datos.PuedeRegistrarSalida
            );

            return Ok(datos);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex,
                "Expediente no encontrado en pre-llenado. ExpedienteID: {ExpedienteID}",
                expedienteId
            );
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex,
                "Error de negocio en pre-llenado. ExpedienteID: {ExpedienteID}",
                expedienteId
            );
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error interno en pre-llenado. ExpedienteID: {ExpedienteID}",
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
    /// Accesible por cualquier rol autenticado para consulta de detalle.
    /// </summary>
    /// <param name="expedienteId">ID del expediente</param>
    /// <returns>SalidaDTO si existe, 404 si no hay registro de salida</returns>
    [HttpGet("expediente/{expedienteId:int}")]
    [ProducesResponseType(typeof(SalidaDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByExpedienteId(int expedienteId)
    {
        try
        {
            var salida = await salidaService.GetByExpedienteIdAsync(expedienteId);

            if (salida == null)
                return NotFound(new
                {
                    message = $"No se encontró registro de salida para el expediente ID {expedienteId}"
                });

            return Ok(salida);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error al obtener salida del expediente {ExpedienteID}",
                expedienteId
            );
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // HISTORIAL POR RANGO DE FECHAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene historial de salidas por rango de fechas.
    /// Útil para reportes diarios, semanales o mensuales.
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
            var historial = await salidaService.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);

            logger.LogInformation(
                "Historial de salidas consultado: {Cantidad} registros entre {FechaInicio} y {FechaFin}",
                historial.Count, fechaInicio, fechaFin
            );

            return Ok(historial);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener historial de salidas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS ESPECIALIZADAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene salidas que excedieron el límite de permanencia de 48 horas.
    /// Útil para reportes DIRESA y auditoría.
    /// </summary>
    /// <param name="fechaInicio">Fecha inicio del rango (opcional)</param>
    /// <param name="fechaFin">Fecha fin del rango (opcional)</param>
    /// <returns>Lista de salidas con TiempoPermanencia mayor a 48 horas</returns>
    [HttpGet("excedieron-limite")]
    [Authorize(Roles = "Administrador,JefeGuardia,VigilanteSupervisor")]
    [ProducesResponseType(typeof(List<SalidaDTO>), 200)]
    public async Task<IActionResult> GetSalidasExcedieronLimite(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin)
    {
        try
        {
            var salidas = await salidaService.GetSalidasExcedieronLimiteAsync(fechaInicio, fechaFin);

            logger.LogInformation(
                "Consulta salidas que excedieron límite: {Cantidad} registros",
                salidas.Count
            );

            return Ok(salidas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener salidas que excedieron límite");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene salidas filtradas por tipo.
    /// El filtro se aplica sobre ActaRetiro.TipoSalida en el repositorio.
    /// </summary>
    /// <param name="tipo">Familiar o AutoridadLegal</param>
    /// <param name="fechaInicio">Fecha inicio del rango (opcional)</param>
    /// <param name="fechaFin">Fecha fin del rango (opcional)</param>
    /// <returns>Lista de SalidaDTO del tipo especificado</returns>
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
            var salidas = await salidaService.GetSalidasPorTipoAsync(tipoSalida, fechaInicio, fechaFin);

            logger.LogInformation(
                "Consulta salidas por tipo '{Tipo}': {Cantidad} registros",
                tipo, salidas.Count
            );

            return Ok(salidas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener salidas por tipo {Tipo}", tipo);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene estadísticas consolidadas de salidas.
    /// Incluye totales por tipo, incidentes y porcentaje de incidentes.
    /// </summary>
    /// <param name="fechaInicio">Fecha inicio del rango (opcional)</param>
    /// <param name="fechaFin">Fecha fin del rango (opcional)</param>
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
            var stats = await salidaService.GetEstadisticasAsync(fechaInicio, fechaFin);

            logger.LogInformation(
                "Estadísticas de salidas consultadas: {TotalSalidas} salidas totales",
                stats.TotalSalidas
            );

            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener estadísticas de salidas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}