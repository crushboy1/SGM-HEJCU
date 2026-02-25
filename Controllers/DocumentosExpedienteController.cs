using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.Services;
using SisMortuorio.Data.Entities.Enums;
using System.Security.Claims;

namespace SisMortuorio.Controllers;

/// <summary>
/// Controlador para gestión de documentos digitalizados del expediente.
/// Reemplaza los "juegos de copias físicas" del proceso manual.
/// Solo accesible por Admisión y Administrador.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentosExpedienteController(
    IDocumentoExpedienteService documentoService,
    ILogger<DocumentosExpedienteController> logger) : ControllerBase
{
    // ===================================================================
    // CONSULTAS
    // ===================================================================

    /// <summary>
    /// Obtiene todos los documentos de un expediente
    /// </summary>
    [HttpGet("expediente/{expedienteId:int}")]
    [Authorize(Roles = "Admision,Administrador,VigilanciaMortuorio,VigilanteSupervisor")]
    [ProducesResponseType(typeof(List<DocumentoExpedienteDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DocumentoExpedienteDTO>>> ObtenerPorExpediente(int expedienteId)
    {
        try
        {
            var documentos = await documentoService.GetByExpedienteIdAsync(expedienteId);
            return Ok(documentos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener documentos del expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el resumen de documentación de un expediente.
    /// Indica qué documentos están presentes/verificados/faltantes según TipoSalida.
    /// Usado por Admisión para habilitar el botón "Crear Acta".
    /// </summary>
    [HttpGet("expediente/{expedienteId:int}/resumen")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(ResumenDocumentosDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumenDocumentosDTO>> ObtenerResumen(int expedienteId)
    {
        try
        {
            var resumen = await documentoService.GetResumenAsync(expedienteId);
            return Ok(resumen);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Expediente {ExpedienteID} no encontrado", expedienteId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener resumen de documentos para expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene un documento por su ID
    /// </summary>
    [HttpGet("{documentoId:int}")]
    [Authorize(Roles = "Admision,Administrador,VigilanciaMortuorio,VigilanteSupervisor")]
    [ProducesResponseType(typeof(DocumentoExpedienteDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoExpedienteDTO>> ObtenerPorId(int documentoId)
    {
        try
        {
            var documento = await documentoService.GetByIdAsync(documentoId);

            if (documento is null)
                return NotFound(new { mensaje = $"Documento {documentoId} no encontrado" });

            return Ok(documento);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener documento {DocumentoID}", documentoId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // SUBIDA DE DOCUMENTOS
    // ===================================================================

    /// <summary>
    /// Sube un nuevo documento digitalizado al expediente.
    /// Formatos permitidos: .pdf, .jpg, .jpeg, .png — Máximo 5MB.
    /// </summary>
    [HttpPost("subir")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(DocumentoExpedienteDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoExpedienteDTO>> SubirDocumento(
        [FromForm] int expedienteId,
        [FromForm] TipoDocumentoExpediente tipoDocumento,
        [FromForm] string? observaciones,
        IFormFile archivo)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var usuarioId = int.Parse(userIdClaim);

            var dto = new SubirDocumentoDTO
            {
                ExpedienteID = expedienteId,
                TipoDocumento = tipoDocumento,
                Observaciones = observaciones,
                UsuarioSubioID = usuarioId
            };

            var documento = await documentoService.SubirDocumentoAsync(dto, archivo);

            logger.LogInformation(
                "Documento {Tipo} subido para expediente {ExpedienteID} por usuario {UsuarioID}",
                tipoDocumento, expedienteId, usuarioId);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { documentoId = documento.DocumentoExpedienteID },
                documento);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Expediente no encontrado al subir documento");
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Archivo inválido al subir documento para expediente {ExpedienteID}", expedienteId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al subir documento para expediente {ExpedienteID}", expedienteId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // VERIFICACIÓN / RECHAZO
    // ===================================================================

    /// <summary>
    /// Marca un documento como verificado contra el original físico presentado.
    /// Actualiza DocumentacionCompleta del expediente automáticamente.
    /// </summary>
    [HttpPost("{documentoId:int}/verificar")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(DocumentoExpedienteDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoExpedienteDTO>> VerificarDocumento(
        int documentoId,
        [FromBody] string? observaciones = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var usuarioId = int.Parse(userIdClaim);

            var dto = new VerificarDocumentoDTO
            {
                DocumentoExpedienteID = documentoId,
                Observaciones = observaciones,
                UsuarioVerificoID = usuarioId
            };

            var documento = await documentoService.VerificarDocumentoAsync(dto);

            logger.LogInformation(
                "Documento {DocumentoID} verificado por usuario {UsuarioID}",
                documentoId, usuarioId);

            return Ok(documento);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Documento {DocumentoID} no encontrado", documentoId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error al verificar documento {DocumentoID}", documentoId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar documento {DocumentoID}", documentoId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Rechaza un documento indicando el motivo.
    /// El familiar deberá presentar nuevamente el documento.
    /// </summary>
    [HttpPost("{documentoId:int}/rechazar")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(typeof(DocumentoExpedienteDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoExpedienteDTO>> RechazarDocumento(
        int documentoId,
        [FromBody] RechazarDocumentoDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            dto.DocumentoExpedienteID = documentoId;
            dto.UsuarioVerificoID = int.Parse(userIdClaim);

            var documento = await documentoService.RechazarDocumentoAsync(dto);

            logger.LogInformation(
                "Documento {DocumentoID} rechazado. Motivo: {Motivo}",
                documentoId, dto.Motivo);

            return Ok(documento);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Documento {DocumentoID} no encontrado", documentoId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error al rechazar documento {DocumentoID}", documentoId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al rechazar documento {DocumentoID}", documentoId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // DESCARGA
    // ===================================================================

    /// <summary>
    /// Descarga el archivo de un documento digitalizado.
    /// </summary>
    [HttpGet("{documentoId:int}/descargar")]
    [Authorize(Roles = "Admision,Administrador,VigilanciaMortuorio,VigilanteSupervisor")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarDocumento(int documentoId)
    {
        try
        {
            var (fileStream, contentType, fileName) =
                await documentoService.DescargarDocumentoAsync(documentoId);

            logger.LogInformation(
                "Descarga de documento {DocumentoID}: {FileName}",
                documentoId, fileName);

            return File(fileStream, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Documento {DocumentoID} no encontrado para descarga", documentoId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Archivo físico no encontrado para documento {DocumentoID}", documentoId);
            return NotFound(new { mensaje = "El archivo no se encuentra en el servidor." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al descargar documento {DocumentoID}", documentoId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // ===================================================================
    // ELIMINACIÓN
    // ===================================================================

    /// <summary>
    /// Elimina un documento.
    /// Solo permitido si Estado == PendienteVerificacion o Rechazado.
    /// </summary>
    [HttpDelete("{documentoId:int}")]
    [Authorize(Roles = "Admision,Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarDocumento(int documentoId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var usuarioId = int.Parse(userIdClaim);

            await documentoService.EliminarDocumentoAsync(documentoId, usuarioId);

            logger.LogInformation(
                "Documento {DocumentoID} eliminado por usuario {UsuarioID}",
                documentoId, usuarioId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Documento {DocumentoID} no encontrado para eliminar", documentoId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "No se puede eliminar documento {DocumentoID}", documentoId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al eliminar documento {DocumentoID}", documentoId);
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }
}