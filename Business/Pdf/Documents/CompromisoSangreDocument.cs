using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Compromiso de Reposición de Unidades de Sangre.
    /// </summary>
    public class CompromisoSangreDocument(
        GenerarCompromisoDTO datos,
        byte[]? logoBytes) : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Row(row =>
                {
                    if (logoBytes != null)
                        row.RelativeItem().Height(2.5f, Unit.Centimetre)
                           .Image(logoBytes).FitArea();
                    else
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("MINISTERIO DE SALUD").Bold().FontSize(14);
                            c.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA")
                                .FontSize(10);
                        });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().AlignRight()
                        .Text($"Miraflores, {DateTime.Now:dd 'de' MMMM 'del' yyyy}");
                    col.Item().Height(1.5f, Unit.Centimetre);
                    col.Item().AlignCenter().Text("COMPROMISO DE REPOSICIÓN")
                        .FontSize(16).Bold().Underline();
                    col.Item().AlignCenter().Text("DE UNIDADES DE SANGRE")
                        .FontSize(16).Bold().Underline();
                    col.Item().Height(2, Unit.Centimetre);

                    col.Item().Text(text =>
                    {
                        text.Span("Por la presente yo, ");
                        text.Span(datos.NombreFamiliar.ToUpper()).Bold();
                        text.Span(", identificado(a) con D.N.I. N° ");
                        text.Span(datos.DNIFamiliar).Bold();
                        text.Span(", me comprometo formalmente (en un plazo máximo de 07 días calendarios), a traer ");
                        text.Span($"{datos.CantidadUnidades} ({NumeroALetras(datos.CantidadUnidades)})").Bold();
                        text.Span(" donantes voluntarios para reponer las unidades de sangre utilizadas por el paciente fallecido ");
                        text.Span(datos.NombrePaciente.ToUpper()).Bold();
                        text.Span(", durante su atención en esta institución hospitalaria.");
                    });

                    col.Item().PaddingTop(10)
                        .Text("Asimismo, declaro conocer que este compromiso es indispensable para regularizar los trámites administrativos del paciente.");

                    col.Item().Height(4, Unit.Centimetre);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(2, Unit.Centimetre);
                            c.Item().AlignCenter().Text("_________________________");
                            c.Item().AlignCenter().Text("Familiar Responsable").FontSize(9).Bold();
                            c.Item().AlignCenter().Text(datos.NombreFamiliar).FontSize(8);
                            c.Item().AlignCenter().Text($"DNI: {datos.DNIFamiliar}").FontSize(8);
                        });
                        row.ConstantItem(1, Unit.Centimetre);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(2, Unit.Centimetre);
                            c.Item().AlignCenter().Text("_________________________");
                            c.Item().AlignCenter().Text("SERVICIO DE HEMOTERAPIA").FontSize(9).Bold();
                            c.Item().AlignCenter().Text("Y BANCO DE SANGRE").FontSize(9).Bold();
                        });
                    });
                });

                page.Footer().Column(c =>
                {
                    c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    c.Item().PaddingTop(5).AlignCenter()
                        .Text("Av. Roosevelt N° 6355 - Miraflores / Teléfono: 446-2321")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private static string NumeroALetras(int n) => n switch
        {
            1 => "UN",
            2 => "DOS",
            3 => "TRES",
            4 => "CUATRO",
            5 => "CINCO",
            6 => "SEIS",
            7 => "SIETE",
            8 => "OCHO",
            9 => "NUEVE",
            10 => "DIEZ",
            _ => n.ToString()
        };
    }
}