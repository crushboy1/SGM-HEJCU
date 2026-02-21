using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.Services;
using System.Security.Claims;

namespace SisMortuorio.Controllers
{
    /// <summary>
    /// Controlador unificado para gestión de Deudas (Sangre y Económica).
    /// Centraliza todos los bloqueos administrativos que impiden la salida del cuerpo.
    /// Facilita integración con Frontend mediante una sola URL base.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeudasController : ControllerBase
    {
        private readonly IDeudaSangreService _deudaSangreService;
        private readonly IDeudaEconomicaService _deudaEconomicaService;
        private readonly ILogger<DeudasController> _logger;
        private readonly IPdfGeneratorService _pdfService;
        private readonly ILocalFileStorageService _fileStorage;
        public DeudasController(
            IDeudaSangreService deudaSangreService,
            IDeudaEconomicaService deudaEconomicaService,
            IPdfGeneratorService pdfGeneratorService,
            ILocalFileStorageService fileStorage,
            ILogger<DeudasController> logger)
        {
            _deudaSangreService = deudaSangreService;
            _deudaEconomicaService = deudaEconomicaService;
            _pdfService = pdfGeneratorService;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════
        //  MÓDULO DEUDA DE SANGRE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra nueva deuda de sangre para un expediente.
        /// ROL: Banco Sangre, Administrador
        /// </summary>
        [HttpPost("sangre")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        [ProducesResponseType(typeof(DeudaSangreDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DeudaSangreDTO>> RegistrarDeudaSangre([FromBody] CreateDeudaSangreDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioRegistroID = ObtenerUsuarioIdDelToken();
                var resultado = await _deudaSangreService.RegistrarDeudaAsync(dto);

                _logger.LogInformation(
                    "Deuda sangre registrada - ExpedienteID: {ExpedienteID}, Unidades: {Unidades}",
                    dto.ExpedienteID, dto.CantidadUnidades
                );

                return CreatedAtAction(
                    nameof(ObtenerDeudaSangrePorExpediente),
                    new { expedienteId = dto.ExpedienteID },
                    resultado
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar deuda sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene deuda de sangre de un expediente específico.
        /// ROL: Todos
        /// </summary>
        [HttpGet("sangre/expediente/{expedienteId}")]
        [ProducesResponseType(typeof(DeudaSangreDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaSangreDTO>> ObtenerDeudaSangrePorExpediente(int expedienteId)
        {
            try
            {
                var resultado = await _deudaSangreService.ObtenerPorExpedienteAsync(expedienteId);
                return resultado != null ? Ok(resultado) : NotFound(new { message = "No existe deuda de sangre para este expediente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener deuda sangre - ExpedienteID: {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Marca deuda de sangre como "Sin Deuda".
        /// ROL: Banco Sangre, Administrador
        /// </summary>
        [HttpPut("sangre/expediente/{expedienteId}/sin-deuda")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        [ProducesResponseType(typeof(DeudaSangreDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaSangreDTO>> MarcarSinDeudaSangre(int expedienteId)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdDelToken();
                var resultado = await _deudaSangreService.MarcarSinDeudaAsync(expedienteId, usuarioId);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar sin deuda sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Marca deuda de sangre como liquidada con firma del familiar.
        /// ROL: Banco Sangre, Vigilante Supervisor, Administrador
        /// </summary>
        [HttpPut("sangre/expediente/{expedienteId}/liquidar")]
        [Authorize(Roles = "BancoSangre,VigilanteSupervisor,Administrador")]
        [ProducesResponseType(typeof(DeudaSangreDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaSangreDTO>> LiquidarDeudaSangre(
            int expedienteId,
            [FromBody] LiquidarDeudaSangreDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _deudaSangreService.MarcarLiquidadaAsync(expedienteId, dto);

                _logger.LogInformation(
                    "Deuda sangre liquidada - ExpedienteID: {ExpedienteID}, Familiar: {Familiar}",
                    expedienteId, dto.NombreFamiliarCompromiso
                );

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
                _logger.LogError(ex, "Error al liquidar deuda sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Anula deuda de sangre por decisión médica (excepción).
        /// ROL: Banco Sangre (Médico), Administrador
        /// </summary>
        [HttpPut("sangre/expediente/{expedienteId}/anular")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        [ProducesResponseType(typeof(DeudaSangreDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaSangreDTO>> AnularDeudaSangre(
            int expedienteId,
            [FromBody] AnularDeudaSangreDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _deudaSangreService.AnularDeudaAsync(expedienteId, dto);

                _logger.LogWarning(
                    "Deuda sangre anulada - ExpedienteID: {ExpedienteID}, MédicoID: {MedicoID}, Justificación: {Justificacion}",
                    expedienteId, dto.MedicoAnulaID, dto.JustificacionAnulacion
                );

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
                _logger.LogError(ex, "Error al anular deuda sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Verifica si deuda de sangre bloquea el retiro.
        /// ROL: Todos
        /// </summary>
        [HttpGet("sangre/expediente/{expedienteId}/bloquea-retiro")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> VerificarBloqueoSangre(int expedienteId)
        {
            try
            {
                var resultado = await _deudaSangreService.BloqueaRetiroAsync(expedienteId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar bloqueo sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene semáforo visual para Banco de Sangre.
        /// ROL: Todos
        /// </summary>
        [HttpGet("sangre/expediente/{expedienteId}/semaforo")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> ObtenerSemaforoSangre(int expedienteId)
        {
            try
            {
                var resultado = await _deudaSangreService.ObtenerSemaforoAsync(expedienteId);
                return Ok(new { semaforo = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener semáforo sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene todas las deudas de sangre pendientes.
        /// ROL: Banco Sangre, Administrador
        /// </summary>
        [HttpGet("sangre/pendientes")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        [ProducesResponseType(typeof(List<DeudaSangreDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DeudaSangreDTO>>> ObtenerDeudasSangrePendientes()
        {
            try
            {
                var resultado = await _deudaSangreService.ObtenerDeudasPendientesAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener deudas sangre pendientes");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene historial completo de una deuda de sangre.
        /// ROL: Banco Sangre, Administrador
        /// </summary>
        [HttpGet("sangre/expediente/{expedienteId}/historial")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        [ProducesResponseType(typeof(List<HistorialDeudaSangreDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<HistorialDeudaSangreDTO>>> ObtenerHistorialSangre(int expedienteId)
        {
            try
            {
                var resultado = await _deudaSangreService.ObtenerHistorialAsync(expedienteId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial sangre");
                return StatusCode(500, new { message = "Error interno" });
            }
        }
        [HttpPost("sangre/generar-compromiso")]
        [Authorize(Roles = "BancoSangre,Administrador")]
        public IActionResult GenerarCompromisoPDF([FromBody] GenerarCompromisoDTO dto)
        {
            try
            {
                // 1. Validar lógica (opcional: verificar que exista la deuda)
                // var deuda = await _deudaSangreService.ObtenerPorExpedienteAsync(dto.ExpedienteID);

                // 2. Generar bytes del PDF
                var pdfBytes = _pdfService.GenerarCompromisoSangre(dto);

                // 3. Retornar archivo stream (no se guarda en disco)
                return File(pdfBytes, "application/pdf", $"Compromiso_{dto.DNIFamiliar}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF compromiso");
                return StatusCode(500, new { message = "Error al generar el documento" });
            }
        }
        /// <summary>
        /// Sube el PDF escaneado del compromiso firmado (sangre).
        /// Retorna la ruta relativa para adjuntarla al DTO de liquidación.
        /// ROL: Banco Sangre, Vigilante Supervisor, Administrador
        /// </summary>
        [HttpPost("sangre/upload-compromiso")]
        [Authorize(Roles = "BancoSangre,VigilanteSupervisor,Administrador")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB Máx
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> UploadCompromisoSangre(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No se ha enviado ningún archivo." });

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".pdf")
                    return BadRequest(new { message = "Solo se permiten archivos PDF." });

                // Carpeta organizada por año/mes
                var carpeta = Path.Combine("compromisos-sangre", DateTime.Now.ToString("yyyy-MM"));

                // Nombre único para evitar colisiones
                var nombreArchivo = $"compromiso_{Guid.NewGuid():N}.pdf";

                // Guardar archivo usando servicio existente
                var ruta = await _fileStorage.GuardarArchivoAsync(file, carpeta, nombreArchivo);

                _logger.LogInformation(
                    "Compromiso sangre subido - Usuario: {Usuario}, Ruta: {Ruta}",
                    ObtenerUsuarioIdDelToken(), ruta
                );

                return Ok(new { rutaArchivo = ruta });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo compromiso sangre");
                return StatusCode(500, new { message = "Error al guardar el archivo en el servidor." });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MÓDULO DEUDA ECONÓMICA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra nueva deuda económica (caso especial: particular→SIS).
        /// ROL: Cuentas Pacientes, Administrador
        /// </summary>
        [HttpPost("economica")]
        [Authorize(Roles = "CuentasPacientes,Administrador")]
        [ProducesResponseType(typeof(DeudaEconomicaDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DeudaEconomicaDTO>> RegistrarDeudaEconomica([FromBody] CreateDeudaEconomicaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioRegistroID = ObtenerUsuarioIdDelToken();
                var resultado = await _deudaEconomicaService.RegistrarDeudaAsync(dto);

                _logger.LogInformation(
                    "Deuda económica registrada - ExpedienteID: {ExpedienteID}, Monto: {Monto}",
                    dto.ExpedienteID, dto.MontoDeuda
                );

                return CreatedAtAction(
                    nameof(ObtenerDeudaEconomicaPorExpediente),
                    new { expedienteId = dto.ExpedienteID },
                    resultado
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar deuda económica");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene deuda económica de un expediente específico.
        /// ROL: Todos
        /// </summary>
        [HttpGet("economica/expediente/{expedienteId}")]
        [ProducesResponseType(typeof(DeudaEconomicaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaEconomicaDTO>> ObtenerDeudaEconomicaPorExpediente(int expedienteId)
        {
            try
            {
                var resultado = await _deudaEconomicaService.ObtenerPorExpedienteAsync(expedienteId);
                return resultado != null ? Ok(resultado) : NotFound(new { message = "No existe deuda económica para este expediente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener deuda económica - ExpedienteID: {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno" });
            }
        }


        /// <summary>
        /// Marca deuda económica como "Sin Deuda" (paciente SIS sin consumos).
        /// ROL: Cuentas Pacientes, Administrador
        /// </summary>
        [HttpPut("economica/expediente/{expedienteId}/sin-deuda")]
        [Authorize(Roles = "CuentasPacientes,Administrador")]
        [ProducesResponseType(typeof(DeudaEconomicaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaEconomicaDTO>> MarcarSinDeudaEconomica(int expedienteId)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdDelToken();
                var resultado = await _deudaEconomicaService.MarcarSinDeudaAsync(expedienteId, usuarioId);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar sin deuda económica");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Marca deuda económica como liquidada con pago en Caja.
        /// ROL: Vigilante Supervisor, Cuentas Pacientes, Administrador
        /// </summary>
        [HttpPut("economica/expediente/{expedienteId}/liquidar")]
        [Authorize(Roles = "VigilanteSupervisor,CuentasPacientes,Administrador")]
        [ProducesResponseType(typeof(DeudaEconomicaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeudaEconomicaDTO>> LiquidarDeudaEconomica(
            int expedienteId,
            [FromBody] LiquidarDeudaEconomicaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _deudaEconomicaService.MarcarLiquidadoAsync(expedienteId, dto);

                _logger.LogInformation(
                    "Deuda económica liquidada - ExpedienteID: {ExpedienteID}, Boleta: {Boleta}, Monto: {Monto}",
                    expedienteId, dto.NumeroBoleta, dto.MontoPagado
                );

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
                _logger.LogError(ex, "Error al liquidar deuda económica");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Aplica exoneración parcial/total por Servicio Social.
        /// ROL: Servicio Social, Administrador
        /// </summary>
        [HttpPut("economica/expediente/{expedienteId}/exonerar")]
        [Authorize(Roles = "ServicioSocial,Administrador")]
        public async Task<ActionResult<DeudaEconomicaDTO>> AplicarExoneracion(
    int expedienteId,
    [FromBody] AplicarExoneracionDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validar que el ID de la URL coincida con el del DTO
                if (dto.ExpedienteID != expedienteId)
                    return BadRequest(new { message = "El ID del expediente no coincide" });

                var resultado = await _deudaEconomicaService.AplicarExoneracionAsync(dto);

                _logger.LogInformation(
                    "Exoneración aplicada - ExpedienteID: {ExpedienteID}, Tipo: {Tipo}, Monto: {Monto}",
                    expedienteId, dto.TipoExoneracion, dto.MontoExonerado
                );

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
                _logger.LogError(ex, "Error al aplicar exoneración");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Verifica si deuda económica bloquea el retiro.
        /// ROL: Todos
        /// </summary>
        [HttpGet("economica/expediente/{expedienteId}/bloquea-retiro")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> VerificarBloqueoEconomica(int expedienteId)
        {
            try
            {
                var resultado = await _deudaEconomicaService.BloqueaRetiroAsync(expedienteId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar bloqueo económica");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene todas las deudas económicas pendientes.
        /// ROL: Cuentas Pacientes, Administrador
        /// </summary>
        [HttpGet("economica/pendientes")]
        [Authorize(Roles = "CuentasPacientes,Administrador")]
        [ProducesResponseType(typeof(List<DeudaEconomicaDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DeudaEconomicaDTO>>> ObtenerDeudasEconomicasPendientes()
        {
            try
            {
                var resultado = await _deudaEconomicaService.ObtenerDeudasPendientesAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener deudas económicas pendientes");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene todas las deudas económicas exoneradas.
        /// ROL: Servicio Social, Administrador
        /// </summary>
        [HttpGet("economica/exoneradas")]
        [Authorize(Roles = "ServicioSocial,Administrador")]
        [ProducesResponseType(typeof(List<DeudaEconomicaDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DeudaEconomicaDTO>>> ObtenerDeudasEconomicasExoneradas()
        {
            try
            {
                var resultado = await _deudaEconomicaService.ObtenerDeudasExoneradasAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener deudas económicas exoneradas");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene el semáforo simplificado (DEBE / NO DEBE).
        /// ROL: Vigilancia (No ve montos, solo estado).
        /// </summary>
        /// <summary>
        /// Obtiene el semáforo completo de deuda económica para Vigilancia.
        /// Retorna: { tieneDeuda, mensaje, detalles (opcional según rol) }
        /// ROL: Todos (roles ven diferentes niveles de detalle)
        /// </summary>
        [HttpGet("economica/expediente/{expedienteId}/semaforo")]
        [ProducesResponseType(typeof(DeudaEconomicaSemaforoDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<DeudaEconomicaSemaforoDTO>> ObtenerSemaforoEconomica(int expedienteId)
        {
            try
            {
                var resultado = await _deudaEconomicaService.ObtenerSemaforoAsync(expedienteId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener semáforo económica - ExpedienteID: {ExpedienteID}", expedienteId);
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        /// <summary>
        /// Obtiene historial completo de una deuda económica.
        /// ROL: Cuentas Pacientes, Servicio Social, Administrador
        /// </summary>
        [HttpGet("economica/expediente/{expedienteId}/historial")]
        [Authorize(Roles = "CuentasPacientes,ServicioSocial,Administrador")]
        [ProducesResponseType(typeof(List<HistorialDeudaEconomicaDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<HistorialDeudaEconomicaDTO>>> ObtenerHistorialEconomica(int expedienteId)
        {
            try
            {
                var resultado = await _deudaEconomicaService.ObtenerHistorialAsync(expedienteId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial económica");
                return StatusCode(500, new { message = "Error interno" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ESTADÍSTICAS GENERALES (DASHBOARD)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene estadísticas combinadas de ambos tipos de deuda para el Dashboard.
        /// ROL: Roles con visibilidad de deudas
        /// </summary>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "BancoSangre,ServicioSocial,CuentasPacientes,VigilanteSupervisor,Administrador")]
        [ProducesResponseType(typeof(DeudaStatsDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<DeudaStatsDTO>> GetEstadisticasGenerales()
        {
            try
            {
                // Obtener stats de sangre
                var deudasSangre = await _deudaSangreService.ObtenerDeudasPendientesAsync();
                var deudasSangreAnuladas = await _deudaSangreService.ObtenerHistorialAsync(0); // TODO: Método específico

                // Obtener stats de económica
                var statsEconomica = await _deudaEconomicaService.ObtenerEstadisticasAsync();
                var deudasEconPendientes = await _deudaEconomicaService.ObtenerDeudasPendientesAsync();
                var deudasEconExoneradas = await _deudaEconomicaService.ObtenerDeudasExoneradasAsync();

                var resultado = new DeudaStatsDTO
                {
                    // Sangre
                    SangrePendientes = deudasSangre.Count(d => d.Estado == "Pendiente"),
                    SangreAnuladas = deudasSangre.Count(d => d.Estado == "Anulada"),

                    // Económica
                    EconomicasPendientes = deudasEconPendientes.Count,
                    EconomicasExoneradas = deudasEconExoneradas.Count,
                    MontoTotalPendiente = statsEconomica?.MontoTotalPendiente ?? 0,
                    MontoTotalExonerado = statsEconomica?.MontoTotalExonerado ?? 0
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de deudas");
                return StatusCode(500, new { message = "Error al cargar estadísticas" });
            }
        }
        // ═══════════════════════════════════════════════════════════
        // GESTIÓN DE ARCHIVOS (DEUDA ECONÓMICA)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Sube el PDF de sustento (Informe Social) temporalmente.
        /// Retorna la ruta relativa para adjuntarla al DTO de exoneración.
        /// ROL: Servicio Social, Administrador
        /// </summary>
        [HttpPost("economica/upload-pdf")]
        [Authorize(Roles = "ServicioSocial,Administrador")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB Máx
        public async Task<ActionResult<object>> UploadSustentoPdf(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No se ha enviado ningún archivo." });

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".pdf")
                    return BadRequest(new { message = "Solo se permiten archivos PDF." });

                // Carpeta organizada por año/mes para no saturar una sola carpeta
                var carpeta = Path.Combine("deudas-economicas", DateTime.Now.ToString("yyyy-MM"));

                // Nombre único para evitar colisiones
                var nombreArchivo = $"sustento_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";

                // Usamos tu servicio existente
                var ruta = await _fileStorage.GuardarArchivoAsync(file, carpeta, nombreArchivo);

                return Ok(new { rutaArchivo = ruta });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo sustento económico");
                return StatusCode(500, new { message = "Error al guardar el archivo en el servidor." });
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
}