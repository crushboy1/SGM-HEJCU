using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.ActaRetiro;
using SisMortuorio.Business.Services;

namespace SisMortuorio.Controllers;

/// <summary>
/// Controlador para gestión de Actas de Retiro
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActaRetiroController(
    IActaRetiroService actaRetiroService,
    ILogger<ActaRetiroController> logger) : ControllerBase
{
    // ===================================================================
    // CONSULTAS
    // ===================================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ActaRetiroDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActaRetiroDTO>> ObtenerPorId(int id)
    {
        try
        {
            var acta = await actaRetiroService.GetByIdAsync(id);

            if (acta is null)
            {
                logger.LogWarning("Acta de retiro {ActaRetiroID} no encontrada", id);
                return NotFound(new { mensaje = $"Acta de retiro {id} no encontrada" });
            }

            return Ok(acta);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener acta de retiro {ActaRetiroID}", id);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("expediente/{expedienteId:int}")]
    [ProducesResponseType(typeof(ActaRetiroDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActaRetiroDTO>> ObtenerPorExpediente(int expedienteId)
    {
        try
        {
            var acta = await actaRetiroService.GetByExpedienteIdAsync(expedienteId);

            if (acta is null)
            {
                logger.LogWarning("No se encontró acta de retiro para expediente {ExpedienteID}", expedienteId);
                return NotFound(new { mensaje = $"No existe acta de retiro para el expediente {expedienteId}" });
            }

            return Ok(acta);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener acta de retiro por expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("pendientes-firma")]
    [ProducesResponseType(typeof(List<ActaRetiroDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ActaRetiroDTO>>> ObtenerPendientesFirma()
    {
        try
        {
            var actas = await actaRetiroService.GetPendientesFirmaAsync();
            return Ok(actas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener actas pendientes de firma");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("por-fecha")]
    [ProducesResponseType(typeof(List<ActaRetiroDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ActaRetiroDTO>>> ObtenerPorFechaRango(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            if (fechaInicio > fechaFin)
                return BadRequest(new { mensaje = "La fecha de inicio no puede ser mayor a la fecha de fin" });

            var actas = await actaRetiroService.GetByFechaRangoAsync(fechaInicio, fechaFin);
            return Ok(actas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener actas por rango de fechas");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("existe/{expedienteId:int}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> ExisteActaParaExpediente(int expedienteId)
    {
        try
        {
            var existe = await actaRetiroService.ExisteActaParaExpedienteAsync(expedienteId);
            return Ok(existe);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar existencia de acta para expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("existe-certificado/{numeroCertificado}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> ExisteCertificadoSINADEF(string numeroCertificado)
    {
        try
        {
            var existe = await actaRetiroService.ExisteByCertificadoSINADEFAsync(numeroCertificado);
            return Ok(existe);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar certificado SINADEF {Certificado}", numeroCertificado);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    [HttpGet("existe-oficio/{numeroOficio}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> ExisteOficioLegal(string numeroOficio)
    {
        try
        {
            var existe = await actaRetiroService.ExistsByOficioLegalAsync(numeroOficio);
            return Ok(existe);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar oficio legal {Oficio}", numeroOficio);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // CREAR ACTA DE RETIRO
    // ===================================================================

    [HttpPost]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(ActaRetiroDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActaRetiroDTO>> Crear([FromBody] CreateActaRetiroDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var actaCreada = await actaRetiroService.CreateAsync(dto);

            logger.LogInformation(
                "Acta de retiro {ActaRetiroID} creada para expediente {ExpedienteID} por usuario {UsuarioID}",
                actaCreada.ActaRetiroID, dto.ExpedienteID, dto.UsuarioAdmisionID);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = actaCreada.ActaRetiroID },
                actaCreada);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error de validación al crear acta de retiro");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Recurso no encontrado al crear acta de retiro");
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear acta de retiro para expediente {ExpedienteID}", dto.ExpedienteID);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // BYPASS DE DEUDA
    // Solo JefeGuardia y Administrador pueden autorizar.
    // El UsuarioAutorizaID se extrae del JWT — nunca viene del body.
    // ===================================================================

    /// <summary>
    /// Autoriza excepcionalmente el retiro con deudas pendientes (económica y/o sangre).
    /// Solo aplica para expedientes con TipoSalidaPreliminar = AutoridadLegal.
    /// Cubre AMBAS deudas — el hospital no puede recuperarlas sin familiar presente.
    /// </summary>
    [HttpPost("autorizar-bypass-deuda")]
    [Authorize(Roles = "JefeGuardia,Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AutorizarBypassDeuda([FromBody] AutorizarBypassDeudaDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Extraer ID y rol del JWT — nunca del body
            var usuarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var rolClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioAutorizaID))
                return Unauthorized(new { mensaje = "Token inválido o usuario no identificado" });

            if (string.IsNullOrEmpty(rolClaim))
                return Forbid();

            await actaRetiroService.AutorizarBypassDeudaAsync(dto, usuarioAutorizaID, rolClaim);

            logger.LogWarning(
                "Bypass de deuda autorizado para expediente {ExpedienteID} por usuario {UsuarioID} rol {Rol}. Justificación: {Justificacion}",
                dto.ExpedienteID, usuarioAutorizaID, rolClaim, dto.Justificacion);

            return Ok(new
            {
                mensaje = "Bypass de deuda autorizado correctamente. El admisionista puede proceder a crear el acta.",
                expedienteID = dto.ExpedienteID,
                autorizadoPor = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                fecha = DateTime.Now
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Intento de bypass no autorizado para expediente {ExpedienteID}", dto.ExpedienteID);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error de validación en bypass para expediente {ExpedienteID}", dto.ExpedienteID);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al autorizar bypass para expediente {ExpedienteID}", dto.ExpedienteID);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // GENERAR PDF SIN FIRMAR
    // ===================================================================

    [HttpPost("{id:int}/generar-pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerarPDF(int id)
    {
        try
        {
            var (pdfBytes, fileName) = await actaRetiroService.GenerarPDFSinFirmarAsync(id);

            logger.LogInformation("PDF generado para acta de retiro {ActaRetiroID}: {FileName}", id, fileName);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Acta de retiro {ActaRetiroID} no encontrada", id);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error de validación al generar PDF para acta {ActaRetiroID}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al generar PDF para acta de retiro {ActaRetiroID}", id);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // REIMPRIMIR PDF SIN FIRMAR
    // ===================================================================

    [HttpGet("expediente/{expedienteId:int}/reimprimir-pdf")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReimprimirPDFPorExpediente(int expedienteId)
    {
        try
        {
            var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usuarioNombre = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            logger.LogInformation(
                "Usuario {UsuarioNombre} (ID: {UsuarioId}) solicita REIMPRIMIR PDF para expediente {ExpedienteID}",
                usuarioNombre, usuarioId, expedienteId);

            var acta = await actaRetiroService.GetByExpedienteIdAsync(expedienteId);

            if (acta is null)
            {
                logger.LogWarning("No se encontró acta para expediente {ExpedienteID}", expedienteId);
                return NotFound(new { mensaje = $"No existe acta de retiro para el expediente {expedienteId}" });
            }

            var resultado = await actaRetiroService.GenerarPDFSinFirmarAsync(acta.ActaRetiroID);

            logger.LogInformation(
                "PDF REIMPRESO para expediente {ExpedienteID}, acta {ActaRetiroID} por {UsuarioNombre}",
                expedienteId, acta.ActaRetiroID, usuarioNombre);

            var fileNameReimpresion = resultado.FileName.Replace(".pdf", "_reimpresion.pdf");
            return File(resultado.PdfBytes, "application/pdf", fileNameReimpresion);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Acta no encontrada para expediente {ExpedienteID}", expedienteId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error al reimprimir PDF para expediente {ExpedienteID}", expedienteId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al reimprimir PDF para expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // SUBIR PDF FIRMADO
    // ===================================================================

    [HttpPost("subir-pdf-firmado")]
    [ProducesResponseType(typeof(ActaRetiroDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActaRetiroDTO>> SubirPDFFirmado([FromBody] UpdateActaRetiroPDFDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var actaActualizada = await actaRetiroService.SubirPDFFirmadoAsync(dto);

            logger.LogInformation(
                "PDF firmado subido para acta de retiro {ActaRetiroID} por usuario {UsuarioID}",
                dto.ActaRetiroID, dto.UsuarioSubidaPDFID);

            return Ok(actaActualizada);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Acta de retiro no encontrada al subir PDF firmado");
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error de validación al subir PDF firmado");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al subir PDF firmado para acta {ActaRetiroID}", dto.ActaRetiroID);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }
}