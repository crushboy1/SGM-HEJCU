using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.Pdf.Helpers;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Reporte de Actas de Retiro — módulo Admisión.
    /// Incluye KPIs de bypass, médico externo y estado de firma.
    /// </summary>
    public class ReporteActasDocument(
        List<ActaReportesItemDTO> datos,
        ActaEstadisticasDTO estadisticas,
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
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                PdfStyles.BuildReporteHeader(page,
                    "REPORTE DE ACTAS DE RETIRO — ADMISIÓN",
                    fechaInicio, fechaFin, generadoPor, logoBytes);

                page.Content().PaddingTop(10).Column(col =>
                {
                    BuildKPIs(col);
                    col.Item().PaddingTop(10);
                    BuildTabla(col);
                });

                PdfStyles.BuildFooter(page);
            });
        }

        private void BuildKPIs(ColumnDescriptor col)
        {
            col.Item().Row(row =>
            {
                void K(string v, string l, string c)
                {
                    row.RelativeItem().Element(PdfStyles.KpiBox).Column(col =>
                    {
                        col.Item().Text(v).FontSize(18).Bold().FontColor(c).AlignCenter();
                        col.Item().Text(l).FontSize(7).AlignCenter();
                    });
                    row.ConstantItem(6);
                }
                K($"{estadisticas.Total}", "Total", Colors.Cyan.Darken2);
                K($"{estadisticas.TipoFamiliar}", "Familiar", Colors.Green.Darken2);
                K($"{estadisticas.TipoAutoridadLegal}", "Autoridad", Colors.Blue.Darken2);
                K($"{estadisticas.Firmadas}", "Firmadas", Colors.Purple.Darken2);
                K($"{estadisticas.ConBypass}", "Con bypass",
                    estadisticas.ConBypass > 0 ? Colors.Orange.Darken2 : Colors.Grey.Medium);
                K($"{estadisticas.ConMedicoExterno}", "Médico externo", Colors.Grey.Darken2);
                K($"{estadisticas.SinPDFFirmado}", "Sin PDF firmado",
                    estadisticas.SinPDFFirmado > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
            });
        }

        private void BuildTabla(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(4);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(4);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    foreach (var t in new[]
                        { "#", "Código", "Paciente", "Fecha", "Tipo Salida",
                          "Responsable", "Estado Acta", "Bypass", "Med.Ext", "PDF Firmado" })
                        h.Cell().Element(PdfStyles.HeaderCell)
                            .Text(t).FontSize(8).Bold().FontColor(Colors.White);
                });

                for (int i = 0; i < datos.Count; i++)
                {
                    var a = datos[i];
                    var bg = PdfStyles.RowColor(i, esBypass: a.TieneBypass);

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text($"{i + 1}").FontSize(8).AlignCenter();
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.CodigoExpediente).FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.NombreCompleto).FontSize(8);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.FechaRegistro.ToString("dd/MM/yy")).FontSize(7).AlignCenter();
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.TipoSalida == "Familiar" ? "Familiar" : "Autoridad")
                        .FontSize(7).AlignCenter();
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.ResponsableNombre ?? "—").FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.EstadoActa).FontSize(7).AlignCenter()
                        .FontColor(a.EstadoActa == "Firmada"
                            ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.TieneBypass ? "SÍ" : "—").FontSize(8).AlignCenter()
                        .FontColor(a.TieneBypass ? Colors.Orange.Darken2 : Colors.Grey.Medium);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.TieneMedicoExterno ? "SÍ" : "—").FontSize(8).AlignCenter()
                        .FontColor(a.TieneMedicoExterno ? Colors.Blue.Darken2 : Colors.Grey.Medium);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(a.TienePDFFirmado ? "✓" : "Pendiente").FontSize(8).AlignCenter()
                        .FontColor(a.TienePDFFirmado ? Colors.Green.Darken2 : Colors.Red.Darken2);
                }
            });
        }
    }
}