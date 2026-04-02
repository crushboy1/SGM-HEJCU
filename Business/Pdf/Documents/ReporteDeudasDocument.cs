using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.Pdf.Helpers;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Reporte Consolidado de Deudas — CONFIDENCIAL.
    /// Solo para JefeGuardia y Administrador. Incluye montos económicos.
    /// </summary>
    public class ReporteDeudasDocument(
        DeudaConsolidadaDTO datos,
        DateTime fechaInicio,
        DateTime fechaFin,
        string generadoPor,
        byte[]? logoBytes) : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                PdfStyles.BuildReporteHeader(page,
                    "REPORTE CONSOLIDADO DE DEUDAS — CONFIDENCIAL",
                    fechaInicio, fechaFin, generadoPor, logoBytes);

                page.Content().PaddingTop(12).Column(col =>
                {
                    BuildSeccionSangre(col);
                    col.Item().PaddingTop(16);
                    BuildSeccionEconomica(col);
                    col.Item().PaddingTop(16);
                    BuildResumenFinanciero(col);
                    col.Item().PaddingTop(16);
                    BuildNotaConfidencial(col);
                });

                PdfStyles.BuildFooter(page);
            });
        }

        private void BuildSeccionSangre(ColumnDescriptor col)
        {
            col.Item().Text("DEUDAS DE SANGRE")
                .FontSize(11).Bold().FontColor(Colors.Red.Darken2).Underline();
            col.Item().PaddingTop(6).Row(row =>
            {
                KpiSangre(row, $"{datos.SangrePendientes}", "Pendientes",
                    datos.SangrePendientes > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.SangreLiquidadas}", "Liquidadas", Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.SangreAnuladas}", "Anuladas", Colors.Orange.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.SangreSinDeuda}", "Sin deuda", Colors.Grey.Darken2);
            });
        }

        private static void KpiSangre(RowDescriptor row, string valor, string label, string color)
        {
            row.RelativeItem().Element(PdfStyles.KpiBox).Column(c =>
            {
                c.Item().Text(valor).FontSize(20).Bold().FontColor(color).AlignCenter();
                c.Item().Text(label).FontSize(8).AlignCenter();
            });
        }

        private void BuildSeccionEconomica(ColumnDescriptor col)
        {
            col.Item().Text("DEUDAS ECONÓMICAS")
                .FontSize(11).Bold().FontColor(Colors.Orange.Darken2).Underline();
            col.Item().PaddingTop(6).Row(row =>
            {
                KpiSangre(row, $"{datos.EconomicasPendientes}", "Pendientes",
                    datos.EconomicasPendientes > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.EconomicasLiquidadas}", "Liquidadas", Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.EconomicasExoneradas}", "Exoneradas", Colors.Purple.Darken2);
                row.ConstantItem(8);
                KpiSangre(row, $"{datos.EconomicasSinDeuda}", "Sin deuda", Colors.Grey.Darken2);
            });
        }

        private void BuildResumenFinanciero(ColumnDescriptor col)
        {
            col.Item().Background(Colors.Grey.Lighten4)
                .Border(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(12).Column(c =>
                {
                    c.Item().Text("RESUMEN FINANCIERO").FontSize(10).Bold()
                        .FontColor(Colors.Cyan.Darken3);

                    FilaMonto(c, "Monto total deudas:",
                        datos.MontoTotalDeudas, Colors.Black, bold: true);
                    FilaMonto(c, "Monto pendiente de cobro:",
                        datos.MontoTotalPendiente, Colors.Red.Darken2, bold: true);
                    FilaMonto(c, "Monto cobrado:",
                        datos.MontoTotalPagado, Colors.Green.Darken2);
                    FilaMonto(c, "Monto exonerado:",
                        datos.MontoTotalExonerado, Colors.Purple.Darken2);

                    c.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    c.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("% promedio de exoneración:").Italic();
                        row.ConstantItem(20);
                        row.RelativeItem().AlignRight()
                            .Text($"{datos.PromedioExoneracion:N1}%").Italic();
                    });
                });
        }

        private static void FilaMonto(
            ColumnDescriptor col, string label, decimal monto, string color, bool bold = false)
        {
            col.Item().PaddingTop(4).Row(row =>
            {
                var labelText = row.RelativeItem().Text(label);
                if (bold) labelText.Bold().FontColor(color);

                row.ConstantItem(20);
                var montoText = row.RelativeItem().AlignRight()
                    .Text($"S/ {monto:N2}");
                if (bold) montoText.Bold().FontColor(color);
                else montoText.FontColor(color);
            });
        }

        private static void BuildNotaConfidencial(ColumnDescriptor col)
        {
            col.Item().Background(Colors.Red.Lighten5)
                .Border(1).BorderColor(Colors.Red.Lighten2)
                .Padding(8)
                .Text("DOCUMENTO CONFIDENCIAL — Uso exclusivo para Jefe de Guardia y Administración. " +
                      "No distribuir sin autorización.")
                .FontSize(8).Italic().FontColor(Colors.Red.Darken2).AlignCenter();
        }
    }
}