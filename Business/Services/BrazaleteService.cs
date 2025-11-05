using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    public class BrazaleteService : IBrazaleteService
    {
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly IQRService _qrService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BrazaleteService> _logger;

        public BrazaleteService(
            IExpedienteRepository expedienteRepository,
            IQRService qrService,
            IWebHostEnvironment environment,
            ILogger<BrazaleteService> logger)
        {
            _expedienteRepository = expedienteRepository;
            _qrService = qrService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<BrazaleteDTO> GenerarBrazaleteAsync(int expedienteId, bool esReimpresion = false)
        {
            // 1. Obtener expediente
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId);

            if (expediente == null)
                throw new InvalidOperationException($"Expediente con ID {expedienteId} no encontrado");

            // 2. Validar que tenga QR generado
            if (string.IsNullOrEmpty(expediente.CodigoQR))
                throw new InvalidOperationException(
                    $"El expediente {expediente.CodigoExpediente} no tiene QR generado. " +
                    "Debe generar el QR primero antes de imprimir el brazalete.");

            // 3. Obtener información del QR
            var qrInfo = await _qrService.ObtenerQRExistenteAsync(expedienteId);

            // 4. Preparar datos para el brazalete
            var brazaleteDTO = new BrazaleteDTO
            {
                CodigoExpediente = expediente.CodigoExpediente,
                CodigoQR = expediente.CodigoQR,
                HC = expediente.HC,
                NombreCompleto = expediente.NombreCompleto,
                FechaHoraFallecimiento = expediente.FechaHoraFallecimiento,
                ServicioFallecimiento = expediente.ServicioFallecimiento,
                NumeroCama = expediente.NumeroCama,
                RutaImagenQR = qrInfo.RutaImagenQR
            };

            // 5. Obtener ruta física de la imagen QR
            var rutaFisicaQR = Path.Combine(
                _environment.WebRootPath,
                qrInfo.RutaImagenQR.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (!File.Exists(rutaFisicaQR))
                throw new InvalidOperationException($"No se encontró la imagen QR en: {rutaFisicaQR}");

            // 6. Generar PDF del brazalete
            var pdfBytes = GenerarPDF(brazaleteDTO, rutaFisicaQR);
            brazaleteDTO.PDFBytes = pdfBytes;

            // 7. Registrar en logs
            var accion = esReimpresion ? "Reimpresión" : "Impresión";
            _logger.LogInformation(
                "{Accion} de brazalete para expediente {CodigoExpediente}",
                accion,
                expediente.CodigoExpediente);

            return brazaleteDTO;
        }

        private byte[] GenerarPDF(BrazaleteDTO datos, string rutaImagenQR)
        {
            // Configurar licencia de QuestPDF (Community License)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configurar página - tamaño de brazalete (10cm x 15cm aprox)
                    page.Size(new PageSize(283.46f, 425.20f)); // 10x15 cm en puntos
                    page.Margin(10);

                    page.Content().Column(column =>
                    {
                        column.Spacing(5);

                        // TÍTULO
                        column.Item().AlignCenter().Text("SISTEMA GESTIÓN DE MORTUORIO")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Black);

                        column.Item().AlignCenter().Text("HOSPITAL DE EMERGENCIA JOSE CASIMIRO ULLOA")
                            .FontSize(10)
                            .SemiBold()
                            .FontColor(Colors.Black);

                        // LÍNEA SEPARADORA
                        column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // CÓDIGO QR (CENTRADO Y GRANDE)
                        column.Item()
                            .AlignCenter()
                            .PaddingVertical(5)
                            .Width(120)
                            .Height(120)
                            .Image(rutaImagenQR);

                        // CÓDIGO DE EXPEDIENTE (GRANDE Y DESTACADO)
                        column.Item().AlignCenter().Text(datos.CodigoExpediente)
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Black);

                        // LÍNEA SEPARADORA
                        column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // DATOS DEL FALLECIDO
                        column.Item().Text(text =>
                        {
                            text.Span("HC: ").FontSize(9).SemiBold();
                            text.Span(datos.HC).FontSize(9);
                        });

                        column.Item().Text(datos.NombreCompleto)
                            .FontSize(10)
                            .Bold();

                        column.Item().Text(text =>
                        {
                            text.Span("Servicio: ").FontSize(8).SemiBold();
                            text.Span(datos.ServicioFallecimiento).FontSize(8);
                        });

                        if (!string.IsNullOrEmpty(datos.NumeroCama))
                        {
                            column.Item().Text(text =>
                            {
                                text.Span("Cama: ").FontSize(8).SemiBold();
                                text.Span(datos.NumeroCama).FontSize(8);
                            });
                        }

                        column.Item().Text(text =>
                        {
                            text.Span("Fallecimiento: ").FontSize(8).SemiBold();
                            text.Span(datos.FechaHoraFallecimiento.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                        });

                        // LÍNEA SEPARADORA
                        column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // INSTRUCCIONES
                        column.Item()
                            .AlignCenter()
                            .PaddingTop(5)
                            .Text("Escanee el código QR para ver detalles")
                            .FontSize(7)
                            .Italic()
                            .FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            // Generar PDF en memoria
            return document.GeneratePdf();
        }
    }
}