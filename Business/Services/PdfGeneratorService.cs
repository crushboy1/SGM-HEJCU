using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Pdf.Documents;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Fachada del generador de PDFs del Sistema.
    /// Resolver el logo y delegar a cada Document.
    /// Cada tipo de PDF vive en su propia clase en Business/Pdf/Documents/.
    /// </summary>
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private readonly string _rutaLogo;
        private byte[]? _logoCache;

        public PdfGeneratorService(IWebHostEnvironment env)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _rutaLogo = Path.Combine(env.WebRootPath, "img", "header_minsa_hejcu.png");
        }

        /// <summary>
        /// Logo cacheado — se lee del disco una sola vez por instancia del service.
        /// </summary>
        private byte[]? GetLogo()
        {
            if (_logoCache != null) return _logoCache;
            if (File.Exists(_rutaLogo))
                _logoCache = File.ReadAllBytes(_rutaLogo);
            return _logoCache;
        }

        // ── Documentos legales ───────────────────────────────────────────

        public byte[] GenerarCompromisoSangre(GenerarCompromisoDTO datos) =>
            new CompromisoSangreDocument(datos, GetLogo()).GeneratePdf();

        public byte[] GenerarActaRetiro(ActaRetiro acta) =>
            new ActaRetiroDocument(acta, GetLogo()).GeneratePdf();

        // ── Reportes ─────────────────────────────────────────────────────

        public byte[] GenerarReportePermanencia(
            List<PermanenciaItemDTO> datos,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor) =>
            new ReportePermanenciaDocument(
                datos, fechaInicio, fechaFin, generadoPor, GetLogo()).GeneratePdf();

        public byte[] GenerarReporteSalidas(
            List<SalidaDTO> datos,
            EstadisticasSalidaDTO estadisticas,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor) =>
            new ReporteSalidasDocument(
                datos, estadisticas, fechaInicio, fechaFin, generadoPor, GetLogo()).GeneratePdf();

        public byte[] GenerarReporteActas(
            List<ActaReportesItemDTO> datos,
            ActaEstadisticasDTO estadisticas,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor) =>
            new ReporteActasDocument(
                datos, estadisticas, fechaInicio, fechaFin, generadoPor, GetLogo()).GeneratePdf();

        public byte[] GenerarReporteDeudas(
            DeudaConsolidadaDTO datos,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor) =>
            new ReporteDeudasDocument(
                datos, fechaInicio, fechaFin, generadoPor, GetLogo()).GeneratePdf();
    }
}