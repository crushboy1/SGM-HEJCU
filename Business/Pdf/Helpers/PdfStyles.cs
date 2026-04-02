using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SisMortuorio.Business.Pdf.Helpers
{
    /// <summary>
    /// Helpers visuales compartidos por todos los documentos PDF del Sistema.
    /// Centraliza estilos para mantener consistencia y facilitar cambios globales.
    /// </summary>
    public static class PdfStyles
    {
        // ── Colores corporativos ─────────────────────────────────────────
        public static readonly string HeaderBg = Colors.Cyan.Darken2;
        public static readonly string HeaderText = Colors.White;
        public static readonly string RowAlt = Colors.Grey.Lighten5;
        public static readonly string RowAlerta = Colors.Red.Lighten5;
        public static readonly string RowBypass = Colors.Orange.Lighten5;
        public static readonly string KpiBg = Colors.Grey.Lighten4;
        public static readonly string KpiBorder = Colors.Grey.Lighten2;
        public static readonly string FooterText = Colors.Grey.Medium;
        public static readonly string TitleColor = Colors.Cyan.Darken3;

        // ── Celdas de encabezado de tabla ────────────────────────────────

        /// <summary>Celda de encabezado con fondo cyan corporativo.</summary>
        public static IContainer HeaderCell(IContainer c) =>
            c.Background(HeaderBg).Padding(5);

        /// <summary>Celda de cuerpo con fondo variable según fila.</summary>
        public static IContainer BodyCell(IContainer c, string bgColor) =>
            c.Background(bgColor).Padding(4)
             .BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

        /// <summary>Color de fila según índice y flags de alerta.</summary>
        public static string RowColor(int index, bool esAlerta = false, bool esBypass = false)
        {
            if (esAlerta) return RowAlerta;
            if (esBypass) return RowBypass;
            return index % 2 == 0 ? Colors.White : RowAlt;
        }

        // ── KPI cards ────────────────────────────────────────────────────

        /// <summary>Contenedor de KPI con borde y fondo gris claro.</summary>
        public static IContainer KpiBox(IContainer c) =>
            c.Background(KpiBg)
             .Border(1).BorderColor(KpiBorder)
             .Padding(8).AlignCenter();

        // ── Header de reporte ────────────────────────────────────────────

        /// <summary>
        /// Header estándar para todos los reportes.
        /// Muestra logo HEJCU (o texto fallback), título y período.
        /// </summary>
        public static void BuildReporteHeader(
            PageDescriptor page,
            string titulo,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor,
            byte[]? logoBytes)
        {
            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    if (logoBytes != null)
                        row.RelativeItem().Height(2.2f, Unit.Centimetre)
                           .Image(logoBytes).FitArea();
                    else
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA")
                                .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                        });

                    row.ConstantItem(130).Column(c =>
                    {
                        c.Item().AlignRight()
                            .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(FooterText);
                        c.Item().AlignRight()
                            .Text($"Por: {generadoPor}")
                            .FontSize(8).FontColor(FooterText);
                    });
                });

                col.Item().PaddingTop(4)
                    .Background(Colors.Cyan.Lighten4)
                    .Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(titulo.ToUpper())
                                .FontSize(13).Bold().FontColor(TitleColor);
                            c.Item().Text(
                                $"Período: {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}")
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                col.Item().LineHorizontal(2).LineColor(Colors.Cyan.Medium);
            });
        }

        // ── Footer estándar ──────────────────────────────────────────────

        public static void BuildFooter(PageDescriptor page)
        {
            page.Footer().Column(c =>
            {
                c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                c.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text("Av. Roosevelt N° 6355 - Miraflores | Tel: 446-2321")
                        .FontSize(7).FontColor(FooterText);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(7).FontColor(FooterText);
                        text.CurrentPageNumber().FontSize(7).FontColor(FooterText);
                        text.Span(" de ").FontSize(7).FontColor(FooterText);
                        text.TotalPages().FontSize(7).FontColor(FooterText);
                    });
                });
            });
        }

        // ── Helpers de texto ─────────────────────────────────────────────

        /// <summary>
        /// Formatea minutos en texto legible. Consistente con frontend y backend.
        /// Acualmente se usa en todo el sistema 90 → "1h 30m", 1500 → "1d 1h"
        /// </summary>
        public static string FormatearMinutos(int minutos)
        {
            var dias = minutos / (60 * 24);
            var horas = (minutos % (60 * 24)) / 60;
            var mins = minutos % 60;
            if (dias > 0) return mins > 0 ? $"{dias}d {horas}h {mins}m" : $"{dias}d {horas}h";
            if (horas > 0) return $"{horas}h {mins}m";
            return $"{mins}m";
        }
    }
}