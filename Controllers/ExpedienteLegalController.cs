using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs.ExpedienteLegal;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    /// <summary>
    /// Controller para gestión de expedientes legales (casos externos).
    /// Modelo Híbrido: Vigilancia → Admisión → Jefe Guardia
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpedienteLegalController(
    IExpedienteLegalService expedienteLegalService,
    ILocalFileStorageService fileStorageService,
    ILogger<ExpedienteLegalController> logger) : ControllerBase
    {

        // ═══════════════════════════════════════════════════════════
        // CRUD BÁSICO
        // ═══════════════════════════════════════════════════════════

        [HttpPost]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Authorize(Roles = "Admision,Administrador")]
        public async Task<ActionResult<ExpedienteLegalDTO>> CrearExpedienteLegal([FromBody] CreateExpedienteLegalDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await expedienteLegalService.CrearExpedienteLegalAsync(dto);

                logger.LogInformation(
                    "ExpedienteLegal {ExpedienteLegalID} creado para Expediente {ExpedienteID}",
                    resultado.ExpedienteLegalID, dto.ExpedienteID
                );

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = resultado.ExpedienteLegalID },
                    resultado
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear expediente legal");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> ObtenerPorId(int id)
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerPorIdAsync(id);
                return resultado != null ? Ok(resultado) : NotFound(new { message = $"Expediente legal {id} no encontrado" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expediente legal {ID}", id);
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("por-expediente/{expedienteId}")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> ObtenerPorExpedienteId(int expedienteId)
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerPorExpedienteIdAsync(expedienteId);
                return resultado != null ? Ok(resultado) : NotFound(new { message = $"No existe expediente legal para expediente {expedienteId}" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expediente legal por ExpedienteID {ID}", expedienteId);
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ListarExpedientesLegales()
        {
            // Este método debe llamar a tu servicio expedienteLegalService.ListarAsync()
            // Asegúrate de que el servicio haga el Select mapeando ApellidoPaterno, Materno, HC, etc.
            var resultado = await expedienteLegalService.ListarTodosAsync();
            return Ok(resultado);
        }

        [HttpPut("{id}/observaciones")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> ActualizarObservaciones(
            int id,
            [FromBody] ActualizarObservacionesDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuarioId = ObtenerUsuarioIdDelToken();
                var resultado = await expedienteLegalService.ActualizarObservacionesAsync(id, dto.Observaciones, usuarioId);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al actualizar observaciones");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // FLUJO HÍBRIDO - TRANSICIONES DE ESTADO
        // ═══════════════════════════════════════════════════════════

        [HttpPost("{id}/marcar-listo-admision")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> MarcarListoParaAdmision(
    int id,
    [FromBody] MarcarListoRequestDTO? request = null)
        {
            try
            {
                var dto = new MarcarListoAdmisionDTO
                {
                    ExpedienteLegalID = id,
                    Observaciones = request?.Observaciones
                };

                var resultado = await expedienteLegalService.MarcarListoAdmisionAsync(dto);
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
                logger.LogError(ex, "Error al marcar listo para admisión");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Admisión aprueba la documentación.
        /// Usa ValidarPorAdmisionAsync con Aprobado = true
        /// </summary>
        [HttpPost("{id}/validar-admision")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> ValidarAdmision(int id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdDelToken();

                var dto = new ValidarDocumentacionAdmisionDTO
                {
                    ExpedienteLegalID = id,
                    UsuarioAdmisionID = usuarioId,
                    Aprobado = true,
                    Observaciones = "Documentación validada correctamente"
                };

                var resultado = await expedienteLegalService.ValidarPorAdmisionAsync(dto);
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
                logger.LogError(ex, "Error al validar admisión");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Admisión rechaza la documentación.
        /// Usa ValidarPorAdmisionAsync con Aprobado = false
        /// </summary>
        [HttpPost("{id}/rechazar-admision")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> RechazarAdmision(
            int id,
            [FromBody] RechazarDTO rechazoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuarioId = ObtenerUsuarioIdDelToken();

                var dto = new ValidarDocumentacionAdmisionDTO
                {
                    ExpedienteLegalID = id,
                    UsuarioAdmisionID = usuarioId,
                    Aprobado = false,
                    Observaciones = rechazoDto.MotivoRechazo
                };

                var resultado = await expedienteLegalService.ValidarPorAdmisionAsync(dto);
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
                logger.LogError(ex, "Error al rechazar admisión");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // AUTORIDADES - JEFE DE GUARDIA
        // ═══════════════════════════════════════════════════════════

        [HttpPost("{id}/autoridades")]
        [ProducesResponseType(typeof(AutoridadExternaDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AutoridadExternaDTO>> AgregarAutoridad(
    int id,
    [FromBody] CreateAutoridadExternaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Asegurar que el DTO tenga el ID correcto
                dto.ExpedienteLegalID = id;
                dto.UsuarioRegistroID = ObtenerUsuarioIdDelToken();

                var resultado = await expedienteLegalService.RegistrarAutoridadAsync(dto);

                logger.LogInformation(
                    "Autoridad {TipoAutoridad} agregada al ExpedienteLegal {ExpedienteLegalID}",
                    dto.TipoAutoridad, id
                );

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id },
                    resultado
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al agregar autoridad");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("{id}/autoridades")]
        [ProducesResponseType(typeof(List<AutoridadExternaDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AutoridadExternaDTO>>> ObtenerAutoridades(int id)
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerAutoridadesAsync(id);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener autoridades");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpDelete("autoridades/{autoridadId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarAutoridad(int autoridadId)
        {
            try
            {
                await expedienteLegalService.EliminarAutoridadAsync(autoridadId);

                logger.LogInformation(
                    "Autoridad {AutoridadID} eliminada por Usuario {UsuarioID}",
                    autoridadId, ObtenerUsuarioIdDelToken()
                );

                return NoContent();
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
                logger.LogError(ex, "Error al eliminar autoridad");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Jefe de Guardia autoriza el levantamiento.
        /// Usa AutorizarPorJefeGuardiaAsync con Validado = true
        /// </summary>
        [HttpPost("{id}/autorizar-jefe-guardia")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> AutorizarJefeGuardia(int id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdDelToken();

                var dto = new ValidarExpedienteLegalDTO
                {
                    ExpedienteLegalID = id,
                    JefeGuardiaID = usuarioId,
                    Validado = true,
                    ObservacionesValidacion = "Autorizado para levantamiento"
                };

                var resultado = await expedienteLegalService.AutorizarPorJefeGuardiaAsync(dto);
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
                logger.LogError(ex, "Error al autorizar jefe guardia");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Jefe de Guardia rechaza y devuelve a Admisión.
        /// Usa AutorizarPorJefeGuardiaAsync con Validado = false
        /// </summary>
        [HttpPost("{id}/rechazar-jefe-guardia")]
        [ProducesResponseType(typeof(ExpedienteLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpedienteLegalDTO>> RechazarJefeGuardia(
            int id,
            [FromBody] RechazarDTO rechazoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuarioId = ObtenerUsuarioIdDelToken();

                var dto = new ValidarExpedienteLegalDTO
                {
                    ExpedienteLegalID = id,
                    JefeGuardiaID = usuarioId,
                    Validado = false,
                    ObservacionesValidacion = rechazoDto.MotivoRechazo
                };

                var resultado = await expedienteLegalService.AutorizarPorJefeGuardiaAsync(dto);
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
                logger.LogError(ex, "Error al rechazar jefe guardia");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DOCUMENTOS LEGALES
        // ═══════════════════════════════════════════════════════════

        [HttpPost("{id}/documentos")]
        [ProducesResponseType(typeof(DocumentoLegalDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentoLegalDTO>> RegistrarDocumento(
            int id,
            [FromBody] RegistrarDocumentoRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuarioId = ObtenerUsuarioIdDelToken();

                var dto = new CreateDocumentoLegalDTO
                {
                    ExpedienteLegalID = id,
                    TipoDocumento = request.TipoDocumento,
                    UsuarioSubeID = usuarioId
                };

                var resultado = await expedienteLegalService.RegistrarDocumentoAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerDocumento),
                    new { id, documentoId = resultado.DocumentoLegalID },
                    resultado
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al registrar documento");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("{id}/documentos/{documentoId}")]
        [ProducesResponseType(typeof(DocumentoLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentoLegalDTO>> ObtenerDocumento(int id, int documentoId)
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerDocumentoAsync(id, documentoId);
                return resultado != null ? Ok(resultado) : NotFound(new { message = $"Documento {documentoId} no encontrado" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener documento {DocumentoID}", documentoId);
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // GESTIÓN DE ARCHIVOS
        // ═══════════════════════════════════════════════════════════

        [HttpPost("{id}/documentos/{documentoId}/upload")]
        [ProducesResponseType(typeof(DocumentoLegalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<DocumentoLegalDTO>> UploadArchivo(
            int id,
            int documentoId,
            IFormFile archivo)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return BadRequest(new { message = "No se proporcionó ningún archivo" });

                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                    return BadRequest(new { message = "Solo se permiten archivos PDF" });

                if (archivo.Length > 10 * 1024 * 1024)
                    return BadRequest(new { message = "El archivo no puede superar los 10MB" });

                var documento = await expedienteLegalService.ObtenerDocumentoAsync(id, documentoId);
                if (documento == null)
                    return NotFound(new { message = $"Documento {documentoId} no encontrado" });

                var nombreArchivo = $"{documento.TipoDocumento}-{documentoId}{extension}";
                var carpeta = id.ToString();

                var rutaRelativa = await fileStorageService.GuardarArchivoAsync(archivo, carpeta, nombreArchivo);

                var usuarioId = ObtenerUsuarioIdDelToken();
                var resultado = await expedienteLegalService.ActualizarRutaArchivoAsync(
                    id,
                    documentoId,
                    rutaRelativa,
                    archivo.FileName,
                    archivo.Length,
                    usuarioId
                );

                logger.LogInformation(
                    "Archivo subido para Documento {DocumentoID} - Ruta: {Ruta}",
                    documentoId, rutaRelativa
                );

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al subir archivo");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("{id}/documentos/{documentoId}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadArchivo(int id, int documentoId)
        {
            try
            {
                var documento = await expedienteLegalService.ObtenerDocumentoAsync(id, documentoId);
                if (documento == null)
                    return NotFound(new { message = $"Documento {documentoId} no encontrado" });

                if (string.IsNullOrEmpty(documento.RutaArchivo))
                    return NotFound(new { message = "El documento no tiene archivo asociado" });

                (Stream stream, string contentType) = await fileStorageService.ObtenerArchivoAsync(documento.RutaArchivo);

                return File(stream, contentType, documento.NombreArchivo ?? "documento.pdf");
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Archivo no encontrado en el servidor" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al descargar archivo");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("{id}/documentos/{documentoId}/view")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ViewArchivo(int id, int documentoId)
        {
            try
            {
                var documento = await expedienteLegalService.ObtenerDocumentoAsync(id, documentoId);
                if (documento == null)
                    return NotFound(new { message = $"Documento {documentoId} no encontrado" });

                if (string.IsNullOrEmpty(documento.RutaArchivo))
                    return NotFound(new { message = "El documento no tiene archivo asociado" });

                (Stream stream, string contentType) = await fileStorageService.ObtenerArchivoAsync(documento.RutaArchivo);

                Response.Headers.Append("Content-Disposition", "inline");
                return File(stream, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Archivo no encontrado en el servidor" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al visualizar archivo");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpDelete("{id}/documentos/{documentoId}/archivo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarArchivo(int id, int documentoId)
        {
            try
            {
                var documento = await expedienteLegalService.ObtenerDocumentoAsync(id, documentoId);
                if (documento == null)
                    return NotFound(new { message = $"Documento {documentoId} no encontrado" });

                if (string.IsNullOrEmpty(documento.RutaArchivo))
                    return NotFound(new { message = "El documento no tiene archivo asociado" });

                await fileStorageService.EliminarArchivoAsync(documento.RutaArchivo);

                var usuarioId = ObtenerUsuarioIdDelToken();
                await expedienteLegalService.ActualizarRutaArchivoAsync(
                    id,
                    documentoId,
                    null,
                    null,
                    null,
                    usuarioId
                );

                logger.LogWarning(
                    "Archivo eliminado para Documento {DocumentoID} por Usuario {UsuarioID}",
                    documentoId, usuarioId
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al eliminar archivo");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARDS Y REPORTES
        // ═══════════════════════════════════════════════════════════

        [HttpGet("en-registro")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerEnRegistro()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerEnRegistroAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes en registro");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("pendientes-admision")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerPendientesAdmision()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerPendientesAdmisionAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes pendientes de Admisión");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("rechazados-admision")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerRechazadosAdmision()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerRechazadosAdmisionAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes rechazados por Admisión");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("pendientes-jefe-guardia")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerPendientesJefeGuardia()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerPendientesJefeGuardiaAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes pendientes de Jefe Guardia");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("autorizados")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerAutorizados()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerAutorizadosAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes autorizados");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("documentos-incompletos")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerConDocumentosIncompletos()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerConDocumentosIncompletosAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes con documentos incompletos");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        [HttpGet("alertas-tiempo")]
        [ProducesResponseType(typeof(List<ExpedienteLegalDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpedienteLegalDTO>>> ObtenerConAlertaTiempo()
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerConAlertaTiempoAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener expedientes con alerta de tiempo");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // HISTORIAL Y AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        [HttpGet("{id}/historial")]
        [ProducesResponseType(typeof(List<HistorialExpedienteLegalDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<HistorialExpedienteLegalDTO>>> ObtenerHistorial(int id)
        {
            try
            {
                var resultado = await expedienteLegalService.ObtenerHistorialAsync(id);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener historial");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS AUXILIARES
        // ═══════════════════════════════════════════════════════════

        private int ObtenerUsuarioIdDelToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Token inválido o usuario no autenticado");
            }

            return userId;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DTOs AUXILIARES PARA REQUESTS
    // ═══════════════════════════════════════════════════════════

    public class RechazarDTO
    {
        public string MotivoRechazo { get; set; } = string.Empty;
    }

    public class RegistrarDocumentoRequestDTO
    {
        public string TipoDocumento { get; set; } = string.Empty;
    }
    public class MarcarListoRequestDTO
    {
        public string? Observaciones { get; set; }
    }
}