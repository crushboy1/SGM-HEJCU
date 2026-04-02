using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.DTOs.Verificacion;
using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.Services;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities.Enums;
using System.Security.Claims;

namespace SisMortuorio.Controllers;

/// <summary>
/// Endpoints de Reportes y Estadísticas del SGM.
/// Consolida services existentes — sin service dedicado.
///
/// ROLES POR ENDPOINT:
///   /dashboard    → VigSup, JefeGuardia, Admin
///   /permanencia  → VigSup, JefeGuardia, Admin
///   /salidas      → VigSup, JefeGuardia, Admin, Admision
///   /actas        → Admision, JefeGuardia, Admin
///   /deudas       → JefeGuardia, Admin (con montos)
///   /expedientes-servicio → SupervisoraEnfermeria, EnfermeriaLicenciada, Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController(
    IBandejaService bandejaService,
    ISalidaMortuorioService salidaService,
    IVerificacionService verificacionService,
    IPdfGeneratorService pdfService,
    ApplicationDbContext context,
    ILogger<ReportesController> logger) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    // DASHBOARD — KPIs CONSOLIDADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// KPIs consolidados para la pantalla principal de Reportes.
    /// Una sola llamada que combina bandeja, salidas, verificaciones y deudas.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "VigilanteSupervisor,JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(DashboardReportesDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardReportesDTO>> GetDashboard(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin)
    {
        try
        {
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);
            var bandeja = await bandejaService.GetEstadisticasAsync();

            EstadisticasSalidaDTO salidas;
            try { salidas = await salidaService.GetEstadisticasAsync(fi, ff); }
            catch { salidas = new EstadisticasSalidaDTO(); }

            EstadisticasVerificacionDTO verificaciones;
            try { verificaciones = await verificacionService.GetEstadisticasAsync(fi, ff); }
            catch { verificaciones = new EstadisticasVerificacionDTO(); }

            var deudaStats = await ObtenerDeudaStatsAsync();

            return Ok(new DashboardReportesDTO
            {
                FechaInicio = fi,
                FechaFin = ff,
                GeneradoEn = DateTime.Now,
                Bandeja = bandeja,
                Salidas = salidas,
                Verificaciones = verificaciones,
                Deudas = deudaStats
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener dashboard");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CUADERNO DE PERMANENCIA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cuaderno digital de permanencia — digitaliza el registro físico del VigSup.
    /// Fuente: BandejaHistorial (Asignacion + Liberacion).
    /// soloActivos=true retorna solo los cuerpos actualmente en mortuorio.
    /// </summary>
    [HttpGet("permanencia")]
    [Authorize(Roles = "VigilanteSupervisor,JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(List<PermanenciaItemDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermanenciaItemDTO>>> GetPermanencia(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] bool soloActivos = false)
    {
        try
        {
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);
            var resultado = await ObtenerDatosPermanencia(fi, ff, soloActivos);

            logger.LogInformation(
                "[Reportes] Permanencia — {Fi:dd/MM} al {Ff:dd/MM}, registros: {Total}",
                fi, ff, resultado.Count);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener cuaderno de permanencia");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // SALIDAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Historial de salidas con filtros.
    /// Reutiliza GetSalidasPorRangoFechasAsync() existente.
    /// </summary>
    [HttpGet("salidas")]
    [Authorize(Roles = "VigilanteSupervisor,JefeGuardia,Administrador,Admision")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalidas(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? tipoSalida = null,
        [FromQuery] bool soloIncidentes = false,
        [FromQuery] bool soloExcedieronLimite = false)
    {
        try
        {
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);

            var salidas = await salidaService.GetSalidasPorRangoFechasAsync(fi, ff);

            // Filtros adicionales en memoria (la lista ya viene filtrada por fecha)
            if (!string.IsNullOrWhiteSpace(tipoSalida))
                salidas = salidas.Where(s =>
                    s.TipoSalida.Equals(tipoSalida, StringComparison.OrdinalIgnoreCase)).ToList();

            if (soloIncidentes)
                salidas = salidas.Where(s => s.IncidenteRegistrado).ToList();

            if (soloExcedieronLimite)
                salidas = salidas.Where(s => s.ExcedioLimite).ToList();

            var estadisticas = await salidaService.GetEstadisticasAsync(fi, ff);

            return Ok(new
            {
                estadisticas,
                total = salidas.Count,
                salidas
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener salidas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ACTAS DE RETIRO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estadísticas y lista de actas de retiro.
    /// KPIs: total, tipo salida, bypass, médico externo, estado acta.
    /// </summary>
    [HttpGet("actas")]
    [Authorize(Roles = "Admision,JefeGuardia,Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActas(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? tipoSalida = null,
        [FromQuery] bool conBypass = false)
    {
        try
        {
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);

            var query = context.ActasRetiro
                .Include(a => a.Expediente)
                .Where(a => a.FechaRegistro >= fi && a.FechaRegistro <= ff)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipoSalida) &&
                Enum.TryParse<TipoSalida>(tipoSalida, out var tipoEnum))
                query = query.Where(a => a.TipoSalida == tipoEnum);

            if (conBypass)
                query = query.Where(a => a.BypassDeudaAutorizado);

            var actas = await query
                .OrderByDescending(a => a.FechaRegistro)
                .ToListAsync();

            // KPIs
            var estadisticas = new ActaEstadisticasDTO
            {
                Total = actas.Count,
                TipoFamiliar = actas.Count(a => a.TipoSalida == TipoSalida.Familiar),
                TipoAutoridadLegal = actas.Count(a => a.TipoSalida == TipoSalida.AutoridadLegal),
                ConBypass = actas.Count(a => a.BypassDeudaAutorizado),
                ConMedicoExterno = actas.Count(a => !string.IsNullOrWhiteSpace(a.MedicoExternoNombre)),
                Firmadas = actas.Count(a => a.EstadoActa == EstadoActaRetiro.Firmada),
                Borrador = actas.Count(a => a.EstadoActa == EstadoActaRetiro.Borrador),
                SinPDFFirmado = actas.Count(a => string.IsNullOrWhiteSpace(a.RutaPDFFirmado)),
            };

            // Lista
            var items = actas.Select(a => new ActaReportesItemDTO
            {
                ActaRetiroID = a.ActaRetiroID,
                ExpedienteID = a.ExpedienteID,
                CodigoExpediente = a.Expediente?.CodigoExpediente ?? a.HistoriaClinica,
                NombreCompleto = a.NombreCompletoFallecido ?? a.Expediente?.NombreCompleto ?? "—",
                HC = a.HistoriaClinica,
                Servicio = a.ServicioFallecimiento,
                FechaRegistro = a.FechaRegistro,
                TipoSalida = a.TipoSalida.ToString(),
                EstadoActa = a.EstadoActa.ToString(),
                TieneBypass = a.BypassDeudaAutorizado,
                TieneMedicoExterno = !string.IsNullOrWhiteSpace(a.MedicoExternoNombre),
                TienePDFFirmado = a.TienePDFFirmado(),
                ResponsableNombre = a.ObtenerNombreResponsableFirma(),
                ResponsableDoc = a.TipoSalida == TipoSalida.Familiar
                    ? $"{a.FamiliarTipoDocumento} {a.FamiliarNumeroDocumento}".Trim()
                    : $"{a.AutoridadTipoDocumento} {a.AutoridadNumeroDocumento}".Trim(),
                JefeGuardiaNombre = a.JefeGuardiaNombre
            }).ToList();

            return Ok(new { estadisticas, total = items.Count, actas = items });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener actas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DEUDAS CONSOLIDADAS (con montos — Admin y JG)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estadísticas consolidadas de deudas económicas y de sangre con montos.
    /// Solo Admin y JefeGuardia acceden a este endpoint.
    /// VigSup accede a conteos sin montos vía /dashboard.
    /// </summary>
    [HttpGet("deudas")]
    [Authorize(Roles = "JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(DeudaConsolidadaDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeudaConsolidadaDTO>> GetDeudas(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin)
    {
        try
        {
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);

            // Deuda económica con filtro de fechas
            var economicas = await context.DeudasEconomicas
                .Where(d => d.FechaRegistro >= fi && d.FechaRegistro <= ff)
                .AsNoTracking()
                .ToListAsync();

            // Deuda sangre con filtro de fechas
            var sangre = await context.DeudasSangre
                .Where(d => d.FechaRegistro >= fi && d.FechaRegistro <= ff)
                .AsNoTracking()
                .ToListAsync();

            var resultado = new DeudaConsolidadaDTO
            {
                FechaInicio = fi,
                FechaFin = ff,

                // Sangre
                SangrePendientes = sangre.Count(d => d.Estado == EstadoDeudaSangre.Pendiente),
                SangreLiquidadas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Liquidado),
                SangreAnuladas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Anulado),
                SangreSinDeuda = sangre.Count(d => d.Estado == EstadoDeudaSangre.SinDeuda),

                // Económica
                EconomicasPendientes = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Pendiente),
                EconomicasLiquidadas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Liquidado),
                EconomicasExoneradas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Exonerado),
                EconomicasSinDeuda = economicas.Count(d => d.Estado == EstadoDeudaEconomica.SinDeuda),
                MontoTotalDeudas = economicas.Sum(d => d.MontoDeuda),
                MontoTotalPendiente = economicas.Sum(d => d.MontoPendiente),
                MontoTotalPagado = economicas.Sum(d => d.MontoPagado),
                MontoTotalExonerado = economicas.Sum(d => d.MontoExonerado),
                PromedioExoneracion = economicas.Where(d => d.MontoDeuda > 0 && d.MontoExonerado > 0)
                    .Select(d => (double)(d.MontoExonerado / d.MontoDeuda * 100))
                    .DefaultIfEmpty(0)
                    .Average()
            };

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener deudas consolidadas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXPEDIENTES POR SERVICIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Expedientes filtrados por servicio.
    /// SupervisoraEnfermeria → puede ver todos los servicios.
    /// EnfermeriaLicenciada  → solo ve su propio servicio (claim JWT).
    /// </summary>
    [HttpGet("expedientes-servicio")]
    [Authorize(Roles = "SupervisoraEnfermeria,EnfermeriaLicenciada,Administrador")]
    [ProducesResponseType(typeof(List<ExpedienteServicioItemDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExpedienteServicioItemDTO>>> GetExpedientesPorServicio(
        [FromQuery] string? servicio = null,
        [FromQuery] string? estado = null,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var rol = User.FindFirst("sgm_role")?.Value ?? string.Empty;
            var fi = fechaInicio ?? DateTime.Today.AddDays(-30);
            var ff = fechaFin ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);

            var query = context.Expedientes
                .Include(e => e.BandejaActual)
                .Include(e => e.ActaRetiro)
                .Include(e => e.UsuarioCreador)
                .Where(e => !e.Eliminado &&
                            e.FechaCreacion >= fi &&
                            e.FechaCreacion <= ff)
                .AsNoTracking()
                .AsQueryable();

            // EnfermeriaLicenciada: forzar filtro por su servicio
            if (rol == "EnfermeriaLicenciada")
            {
                var servicioUsuario = User.FindFirst("servicio")?.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(servicioUsuario))
                    query = query.Where(e => e.ServicioFallecimiento == servicioUsuario);
            }
            else if (!string.IsNullOrWhiteSpace(servicio))
            {
                // SupervisoraEnfermeria y Admin pueden filtrar por servicio opcional
                query = query.Where(e => e.ServicioFallecimiento.Contains(servicio));
            }

            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoExpediente>(estado, out var estadoEnum))
                query = query.Where(e => e.EstadoActual == estadoEnum);

            var expedientes = await query
                .OrderByDescending(e => e.FechaCreacion)
                .ToListAsync();

            var resultado = expedientes.Select(e =>
            {
                string? tiempoMort = null;
                if (e.BandejaActual?.FechaHoraAsignacion != null)
                {
                    var min = (int)(DateTime.Now - e.BandejaActual.FechaHoraAsignacion.Value).TotalMinutes;
                    tiempoMort = FormatearTiempo(min);
                }

                return new ExpedienteServicioItemDTO
                {
                    ExpedienteID = e.ExpedienteID,
                    CodigoExpediente = e.CodigoExpediente,
                    NombreCompleto = e.NombreCompleto,
                    HC = e.HC,
                    Servicio = e.ServicioFallecimiento,
                    EstadoActual = e.EstadoActual.ToString(),
                    FechaHoraFallecimiento = e.FechaHoraFallecimiento,
                    FechaCreacion = e.FechaCreacion,
                    CodigoBandeja = e.BandejaActual?.Codigo,
                    TiempoEnMortuorio = tiempoMort,
                    TieneActa = e.ActaRetiro != null,
                    DocumentacionCompleta = e.DocumentacionCompleta,
                    UsuarioCreadorNombre = e.UsuarioCreador?.NombreCompleto ?? "—"
                };
            }).ToList();

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al obtener expedientes por servicio");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Conteos de deudas sin montos para el dashboard de VigSup.
    /// Query directa — sin filtro de fecha (estado actual).
    /// </summary>
    private async Task<DeudaStatsReportesDTO> ObtenerDeudaStatsAsync()
    {
        var sangre = await context.DeudasSangre.AsNoTracking().ToListAsync();
        var economicas = await context.DeudasEconomicas.AsNoTracking().ToListAsync();

        return new DeudaStatsReportesDTO
        {
            SangrePendientes = sangre.Count(d => d.Estado == EstadoDeudaSangre.Pendiente),
            SangreLiquidadas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Liquidado),
            SangreAnuladas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Anulado),
            EconomicasPendientes = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Pendiente),
            EconomicasLiquidadas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Liquidado),
            EconomicasExoneradas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Exonerado),
        };
    }

    /// <summary>
    /// Formatea minutos en formato legible. Consistente con BandejaService y frontend.
    /// </summary>
    private static string FormatearTiempo(int minutos)
    {
        var dias = minutos / (60 * 24);
        var horas = (minutos % (60 * 24)) / 60;
        var mins = minutos % 60;

        if (dias > 0) return mins > 0 ? $"{dias}d {horas}h {mins}m" : $"{dias}d {horas}h";
        if (horas > 0) return $"{horas}h {mins}m";
        return $"{mins}m";
    }


    // ═══════════════════════════════════════════════════════════
    // EXPORTACIÓN PDF
    // ═══════════════════════════════════════════════════════════
    // Nota: inyectar IPdfGeneratorService en el constructor del controller
    // Agregar: IPdfGeneratorService pdfService al primary constructor

    /// <summary>
    /// Exporta el cuaderno de permanencia como PDF oficial con membrete HEJCU.
    /// </summary>
    [HttpPost("exportar/permanencia")]
    [Authorize(Roles = "VigilanteSupervisor,JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarPermanenciaPdf(
        [FromBody] ExportarReporteDTO dto)
    {
        try
        {
            var fi = dto.FechaInicio;
            var ff = dto.FechaFin;
            var generadoPor = User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";

            var datos = await ObtenerDatosPermanencia(fi, ff, dto.SoloActivos);
            var pdfBytes = pdfService.GenerarReportePermanencia(datos, fi, ff, generadoPor);

            return File(pdfBytes, "application/pdf",
                $"Permanencia_SGM_{fi:yyyyMMdd}_{ff:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al exportar permanencia PDF");
            return StatusCode(500, new { message = "Error al generar el PDF" });
        }
    }

    /// <summary>
    /// Exporta el reporte de salidas como PDF oficial.
    /// </summary>
    [HttpPost("exportar/salidas")]
    [Authorize(Roles = "VigilanteSupervisor,JefeGuardia,Administrador,Admision")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarSalidasPdf(
        [FromBody] ExportarReporteDTO dto)
    {
        try
        {
            var fi = dto.FechaInicio;
            var ff = dto.FechaFin;
            var generadoPor = User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";

            var datos = await salidaService.GetSalidasPorRangoFechasAsync(fi, ff);
            var estadisticas = await salidaService.GetEstadisticasAsync(fi, ff);
            var pdfBytes = pdfService.GenerarReporteSalidas(datos, estadisticas, fi, ff, generadoPor);

            return File(pdfBytes, "application/pdf",
                $"Salidas_SGM_{fi:yyyyMMdd}_{ff:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al exportar salidas PDF");
            return StatusCode(500, new { message = "Error al generar el PDF" });
        }
    }

    /// <summary>
    /// Exporta el reporte de actas como PDF oficial (Admisión).
    /// </summary>
    [HttpPost("exportar/actas")]
    [Authorize(Roles = "Admision,JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarActasPdf(
        [FromBody] ExportarReporteDTO dto)
    {
        try
        {
            var fi = dto.FechaInicio;
            var ff = dto.FechaFin;
            var generadoPor = User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";

            var actas = await context.ActasRetiro
                .Include(a => a.Expediente)
                .Where(a => a.FechaRegistro >= fi && a.FechaRegistro <= ff)
                .AsNoTracking()
                .OrderByDescending(a => a.FechaRegistro)
                .ToListAsync();

            var items = actas.Select(a => new ActaReportesItemDTO
            {
                ActaRetiroID = a.ActaRetiroID,
                ExpedienteID = a.ExpedienteID,
                CodigoExpediente = a.Expediente?.CodigoExpediente ?? a.HistoriaClinica,
                NombreCompleto = a.NombreCompletoFallecido ?? a.Expediente?.NombreCompleto ?? "—",
                HC = a.HistoriaClinica,
                Servicio = a.ServicioFallecimiento,
                FechaRegistro = a.FechaRegistro,
                TipoSalida = a.TipoSalida.ToString(),
                EstadoActa = a.EstadoActa.ToString(),
                TieneBypass = a.BypassDeudaAutorizado,
                TieneMedicoExterno = !string.IsNullOrWhiteSpace(a.MedicoExternoNombre),
                TienePDFFirmado = a.TienePDFFirmado(),
                ResponsableNombre = a.ObtenerNombreResponsableFirma(),
                JefeGuardiaNombre = a.JefeGuardiaNombre
            }).ToList();

            var estadisticas = new ActaEstadisticasDTO
            {
                Total = items.Count,
                TipoFamiliar = items.Count(a => a.TipoSalida == "Familiar"),
                TipoAutoridadLegal = items.Count(a => a.TipoSalida == "AutoridadLegal"),
                ConBypass = items.Count(a => a.TieneBypass),
                ConMedicoExterno = items.Count(a => a.TieneMedicoExterno),
                Firmadas = actas.Count(a => a.EstadoActa == EstadoActaRetiro.Firmada),
                Borrador = actas.Count(a => a.EstadoActa == EstadoActaRetiro.Borrador),
                SinPDFFirmado = items.Count(a => !a.TienePDFFirmado),
            };

            var pdfBytes = pdfService.GenerarReporteActas(items, estadisticas, fi, ff, generadoPor);

            return File(pdfBytes, "application/pdf",
                $"Actas_SGM_{fi:yyyyMMdd}_{ff:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al exportar actas PDF");
            return StatusCode(500, new { message = "Error al generar el PDF" });
        }
    }

    /// <summary>
    /// Exporta el reporte de deudas como PDF confidencial (solo JG/Admin).
    /// </summary>
    [HttpPost("exportar/deudas")]
    [Authorize(Roles = "JefeGuardia,Administrador")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarDeudasPdf(
        [FromBody] ExportarReporteDTO dto)
    {
        try
        {
            var fi = dto.FechaInicio;
            var ff = dto.FechaFin;
            var generadoPor = User.FindFirst(ClaimTypes.Name)?.Value ?? "Sistema";

            var economicas = await context.DeudasEconomicas
                .Where(d => d.FechaRegistro >= fi && d.FechaRegistro <= ff)
                .AsNoTracking().ToListAsync();
            var sangre = await context.DeudasSangre
                .Where(d => d.FechaRegistro >= fi && d.FechaRegistro <= ff)
                .AsNoTracking().ToListAsync();

            var datos = new DeudaConsolidadaDTO
            {
                FechaInicio = fi,
                FechaFin = ff,
                SangrePendientes = sangre.Count(d => d.Estado == EstadoDeudaSangre.Pendiente),
                SangreLiquidadas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Liquidado),
                SangreAnuladas = sangre.Count(d => d.Estado == EstadoDeudaSangre.Anulado),
                SangreSinDeuda = sangre.Count(d => d.Estado == EstadoDeudaSangre.SinDeuda),
                EconomicasPendientes = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Pendiente),
                EconomicasLiquidadas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Liquidado),
                EconomicasExoneradas = economicas.Count(d => d.Estado == EstadoDeudaEconomica.Exonerado),
                EconomicasSinDeuda = economicas.Count(d => d.Estado == EstadoDeudaEconomica.SinDeuda),
                MontoTotalDeudas = economicas.Sum(d => d.MontoDeuda),
                MontoTotalPendiente = economicas.Sum(d => d.MontoPendiente),
                MontoTotalPagado = economicas.Sum(d => d.MontoPagado),
                MontoTotalExonerado = economicas.Sum(d => d.MontoExonerado),
            };

            var pdfBytes = pdfService.GenerarReporteDeudas(datos, fi, ff, generadoPor);

            return File(pdfBytes, "application/pdf",
                $"Deudas_CONFIDENCIAL_SGM_{fi:yyyyMMdd}_{ff:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Reportes] Error al exportar deudas PDF");
            return StatusCode(500, new { message = "Error al generar el PDF" });
        }
    }

    // ─── Helper compartido entre GET y POST exportar ──────────────────
    private async Task<List<PermanenciaItemDTO>> ObtenerDatosPermanencia(
    DateTime fi, DateTime ff, bool soloActivos)
    {
        var query = context.BandejaHistoriales
            .Include(bh => bh.Bandeja)
            .Include(bh => bh.Expediente)
                .ThenInclude(e => e!.ActaRetiro)
            .Include(bh => bh.UsuarioAsignador)
            .Where(bh =>
                bh.Accion == AccionBandeja.Asignacion &&
                bh.ExpedienteID != null &&
                bh.FechaHoraIngreso >= fi &&
                bh.FechaHoraIngreso <= ff)
            .AsNoTracking()
            .AsQueryable();

        // Filtro directo — FechaHoraSalida ya está en la fila de Asignacion
        // (antes requería cruzar con fila separada de Liberacion)
        if (soloActivos)
            query = query.Where(bh => bh.FechaHoraSalida == null);

        var historial = await query
            .OrderByDescending(bh => bh.FechaHoraIngreso)
            .ToListAsync();

        if (!historial.Any())
            return new List<PermanenciaItemDTO>();

        return historial.Select(bh =>
        {
            var estaActivo = bh.FechaHoraSalida == null;
            var finTiempo = bh.FechaHoraSalida ?? DateTime.Now;
            var durMin = (int)(finTiempo - bh.FechaHoraIngreso).TotalMinutes;
            var acta = bh.Expediente?.ActaRetiro;

            var responsableRetiro = acta?.TipoSalida switch
            {
                TipoSalida.Familiar => acta.FamiliarParentesco,
                TipoSalida.AutoridadLegal => acta.AutoridadCargo,
                _ => null
            };

            var observacionesMedico = !string.IsNullOrWhiteSpace(acta?.JefeGuardiaNombre)
                ? $"JG: {acta.JefeGuardiaNombre}"
                : !string.IsNullOrWhiteSpace(acta?.MedicoCertificaNombre)
                    ? acta.MedicoCertificaNombre
                    : bh.Expediente?.MedicoCertificaNombre;

            return new PermanenciaItemDTO
            {
                HistorialID = bh.OcupacionID,
                CodigoBandeja = bh.Bandeja?.Codigo ?? "—",
                ExpedienteID = bh.ExpedienteID ?? 0,
                CodigoExpediente = bh.Expediente?.CodigoExpediente ?? "—",
                NombreCompleto = bh.Expediente?.NombreCompleto ?? "—",
                HC = bh.Expediente?.HC ?? "—",
                Servicio = bh.Expediente?.ServicioFallecimiento ?? "—",
                TipoExpediente = bh.Expediente?.TipoExpediente.ToString() ?? "—",
                DiagnosticoFinal = bh.Expediente?.DiagnosticoFinal ?? "—",
                ResponsableRetiro = responsableRetiro ?? "—",
                Destino = acta?.Destino ?? "—",
                ObservacionesMedico = observacionesMedico ?? "—",
                FechaHoraIngreso = bh.FechaHoraIngreso,
                FechaHoraSalida = bh.FechaHoraSalida,  // directo, sin cruce
                TiempoMinutos = durMin,
                TiempoLegible = FormatearTiempo(durMin),
                EstaActivo = estaActivo,
                ExcedioLimite = durMin > 48 * 60,
                UsuarioAsignadorNombre = bh.UsuarioAsignador?.NombreCompleto,
                Observaciones = bh.Observaciones
            };
        }).ToList();
    }
}