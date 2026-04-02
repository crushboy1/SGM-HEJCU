using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Pdf.Helpers;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Reporte de Salidas del Mortuorio.
    /// Incluye KPIs y tabla detallada con funeraria y tiempo de permanencia.
    /// </summary>
    public class ReporteSalidasDocument(
        List<SalidaDTO> datos,
        EstadisticasSalidaDTO estadisticas,
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
                    "REPORTE DE SALIDAS DEL MORTUORIO",
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
                KpiItem(row, $"{estadisticas.TotalSalidas}", "Total salidas", Colors.Cyan.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{estadisticas.SalidasFamiliar}", "Por familiar", Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{estadisticas.SalidasAutoridadLegal}", "Autoridad legal", Colors.Blue.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{estadisticas.ConIncidentes}", "Con incidentes",
                    estadisticas.ConIncidentes > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{estadisticas.PorcentajeIncidentes:N1}%", "% incidentes",
                    Colors.Grey.Darken2);
            });
        }

        private static void KpiItem(RowDescriptor row, string valor, string label, string color)
        {
            row.RelativeItem().Element(PdfStyles.KpiBox).Column(c =>
            {
                c.Item().Text(valor).FontSize(22).Bold().FontColor(color).AlignCenter();
                c.Item().Text(label).FontSize(8).AlignCenter();
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
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    foreach (var t in new[]
                        { "#", "Código", "Paciente", "Fecha Salida", "Tipo",
                          "Responsable", "Funeraria", "Permanencia", "Incidente" })
                        h.Cell().Element(PdfStyles.HeaderCell)
                            .Text(t).FontSize(8).Bold().FontColor(Colors.White);
                });

                for (int i = 0; i < datos.Count; i++)
                {
                    var s = datos[i];
                    var bg = PdfStyles.RowColor(i, s.IncidenteRegistrado);

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text($"{i + 1}").FontSize(8).AlignCenter();
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.CodigoExpediente).FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.NombrePaciente).FontSize(8);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.FechaHoraSalida.ToString("dd/MM/yy HH:mm")).FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.TipoSalida == "Familiar" ? "Familiar" : "Autoridad")
                        .FontSize(7).AlignCenter();
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.ResponsableNombre ?? "-").FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.NombreFuneraria ?? "—").FontSize(7);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.TiempoPermanenciaLegible ?? "—").FontSize(8).AlignCenter()
                        .FontColor(s.ExcedioLimite ? Colors.Red.Darken2 : Colors.Black);
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(s.IncidenteRegistrado ? "SÍ" : "No").FontSize(8).AlignCenter()
                        .FontColor(s.IncidenteRegistrado ? Colors.Red.Darken2 : Colors.Green.Darken2);
                }
            });
        }
    }
}