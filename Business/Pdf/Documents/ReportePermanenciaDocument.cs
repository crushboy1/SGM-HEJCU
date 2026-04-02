using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.Pdf.Helpers;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Cuaderno Digital de Permanencia del Mortuorio.
    /// Digitaliza el registro físico del Supervisor de Vigilancia.
    /// Orientación horizontal (A4 landscape) para mostrar todas las columnas.
    /// Filas en rojo para casos que excedieron 48h.
    /// </summary>
    public class ReportePermanenciaDocument(
        List<PermanenciaItemDTO> datos,
        DateTime fechaInicio,
        DateTime fechaFin,
        string generadoPor,
        byte[]? logoBytes) : IDocument
    {
        private readonly int _activos = datos.Count(d => d.EstaActivo);
        private readonly int _excedidos = datos.Count(d => d.ExcedioLimite);
        private readonly int _promMinutos = datos.Any()
            ? (int)datos.Average(d => d.TiempoMinutos) : 0;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                PdfStyles.BuildReporteHeader(page,
                    "CUADERNO DIGITAL DE PERMANENCIA — MORTUORIO",
                    fechaInicio, fechaFin, generadoPor, logoBytes);

                page.Content().PaddingTop(10).Column(col =>
                {
                    BuildKPIs(col);
                    col.Item().PaddingTop(10);
                    BuildTabla(col);
                    BuildLeyenda(col);
                });

                PdfStyles.BuildFooter(page);
            });
        }

        private void BuildKPIs(ColumnDescriptor col)
        {
            col.Item().Row(row =>
            {
                KpiItem(row, $"{datos.Count}", "Total registros", Colors.Cyan.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{_activos}", "En mortuorio", Colors.Blue.Darken2);
                row.ConstantItem(8);
                KpiItem(row, $"{_excedidos}", "Excedieron 48h",
                    _excedidos > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                row.ConstantItem(8);
                KpiItem(row, PdfStyles.FormatearMinutos(_promMinutos),
                    "Permanencia promedio", Colors.Grey.Darken2);
            });
        }

        private static void KpiItem(RowDescriptor row, string valor, string label, string color)
        {
            row.RelativeItem().Element(PdfStyles.KpiBox).Column(c =>
            {
                c.Item().Text(valor).FontSize(22).Bold()
                    .FontColor(color).AlignCenter();
                c.Item().Text(label).FontSize(8).AlignCenter();
            });
        }

        private void BuildTabla(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                // 14 columnas — landscape A4 (297mm - 30mm márgenes = 267mm útiles)
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(20);   // #
                    cols.RelativeColumn(1.5f); // Bandeja
                    cols.RelativeColumn(2.2f); // Código
                    cols.RelativeColumn(3.5f); // Paciente
                    cols.RelativeColumn(1.8f); // HC
                    cols.RelativeColumn(2.2f); // Servicio
                    cols.RelativeColumn(2.8f); // Diagnóstico
                    cols.RelativeColumn(2.5f); // Ingreso
                    cols.RelativeColumn(2.5f); // Salida
                    cols.RelativeColumn(1.8f); // Tiempo
                    cols.RelativeColumn(2f);   // Responsable
                    cols.RelativeColumn(2.2f); // Destino
                    cols.RelativeColumn(2.5f); // Obs/Médico
                    cols.RelativeColumn(1.5f); // Estado
                });

                // Header
                table.Header(h =>
                {
                    foreach (var titulo in new[]
                    {
                        "#", "Bandeja", "Código", "Paciente", "HC",
                        "Servicio", "Diagnóstico",
                        "Ingreso Mortuorio", "Retiro Mortuorio", "Tiempo",
                        "Responsable", "Destino", "Obs/Médico",
                        "Estado"
                    })
                        h.Cell().Element(PdfStyles.HeaderCell)
                            .Text(titulo).FontSize(7).Bold()
                            .FontColor(Colors.White);
                });

                // Filas
                for (int i = 0; i < datos.Count; i++)
                {
                    var item = datos[i];
                    var bg = PdfStyles.RowColor(i, item.ExcedioLimite);

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text($"{i + 1}").FontSize(7).AlignCenter();

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.CodigoBandeja).FontSize(7).Bold().AlignCenter();

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.CodigoExpediente).FontSize(6.5f);

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.NombreCompleto).FontSize(7);

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.HC).FontSize(7).AlignCenter();

                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.Servicio).FontSize(6.5f);

                    // Diagnóstico — puede ser largo
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.DiagnosticoFinal).FontSize(6.5f);

                    // Ingreso: fecha y hora
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.FechaHoraIngreso.ToString("dd/MM/yy HH:mm"))
                        .FontSize(7).AlignCenter();

                    // Retiro: fecha y hora, o "En mortuorio" en azul
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.FechaHoraSalida.HasValue
                            ? item.FechaHoraSalida.Value.ToString("dd/MM/yy HH:mm")
                            : "En mortuorio")
                        .FontSize(7).AlignCenter()
                        .FontColor(item.EstaActivo ? Colors.Blue.Darken2 : Colors.Black);

                    // Tiempo — rojo si excedió
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.TiempoLegible).FontSize(7).Bold().AlignCenter()
                        .FontColor(item.ExcedioLimite
                            ? Colors.Red.Darken2
                            : item.TiempoMinutos > 1440
                                ? Colors.Orange.Darken2
                                : Colors.Black);

                    // Responsable retiro (parentesco / cargo)
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.ResponsableRetiro).FontSize(6.5f);

                    // Destino
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.Destino).FontSize(6.5f);

                    // Observaciones / médico JG
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.ObservacionesMedico).FontSize(6.5f);

                    // Estado
                    table.Cell().Element(c => PdfStyles.BodyCell(c, bg))
                        .Text(item.EstaActivo ? "ACTIVO" : "RETIRADO")
                        .FontSize(7).Bold().AlignCenter()
                        .FontColor(item.EstaActivo
                            ? Colors.Blue.Darken2
                            : Colors.Green.Darken2);
                }
            });
        }

        private void BuildLeyenda(ColumnDescriptor col)
        {
            col.Item().PaddingTop(8).Row(row =>
            {
                // Activos
                row.AutoItem().Width(10).Height(10)
                    .Background(Colors.Blue.Lighten5)
                    .Border(1).BorderColor(Colors.Blue.Lighten2);
                row.ConstantItem(5);
                row.AutoItem()
                    .Text("Azul: cuerpo aún en mortuorio")
                    .FontSize(7).FontColor(Colors.Blue.Darken2);
                row.ConstantItem(16);

                if (_excedidos > 0)
                {
                    // Excedidos
                    row.AutoItem().Width(10).Height(10)
                        .Background(Colors.Red.Lighten5)
                        .Border(1).BorderColor(Colors.Red.Lighten2);
                    row.ConstantItem(5);
                    row.RelativeItem()
                        .Text($"Fila en rojo: {_excedidos} caso(s) que excedieron las 48h.")
                        .FontSize(7).FontColor(Colors.Red.Darken2).Italic();
                }
            });
        }
    }
}