using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.Pdf.Helpers;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.Pdf.Documents
{
    /// <summary>
    /// Documento PDF: Acta de Retiro de Cadáver.
    /// Requiere firmas de: Responsable (familiar/autoridad), Admisionista y Supervisor Vigilancia.
    /// </summary>
    public class ActaRetiroDocument(
        ActaRetiro acta,
        byte[]? logoBytes) : IDocument
    {
        // KPIs calculados una sola vez en construcción
        private readonly int? _edad = CalcularEdad(acta.Expediente?.FechaNacimiento);
        private readonly string? _diagnostico = acta.Expediente?.DiagnosticoFinal;
        private readonly bool _tieneMedExt = !string.IsNullOrWhiteSpace(acta.MedicoExternoNombre);
        private readonly bool _tieneBypass = acta.BypassDeudaAutorizado;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                BuildHeader(page);
                BuildContent(page);
                PdfStyles.BuildFooter(page);
            });
        }

        private void BuildHeader(PageDescriptor page)
        {
            page.Header().Row(row =>
            {
                if (logoBytes != null)
                    row.RelativeItem().Height(2.5f, Unit.Centimetre)
                       .Image(logoBytes).FitArea();
                else
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        c.Item().Text("ACTA DE RETIRO DE CADÁVER").FontSize(12).Bold();
                    });

                row.ConstantItem(100).Column(c =>
                {
                    c.Item().AlignRight().Text($"Fecha: {acta.FechaRegistro:dd/MM/yyyy}");
                    c.Item().AlignRight()
                        .Text($"Tipo: {DescripcionTipoSalida(acta.TipoSalida)}")
                        .FontSize(9).Italic();
                });
            });
        }

        private void BuildContent(PageDescriptor page)
        {
            page.Content().PaddingVertical(10).Column(col =>
            {
                BuildSeccionFallecido(col);
                BuildSeccionDocumentoLegal(col);
                BuildSeccionMedico(col);
                BuildSeccionJefeGuardia(col);
                BuildSeccionResponsable(col);
                BuildDestino(col);
                BuildFirmas(col);
            });
        }

        private void BuildSeccionFallecido(ColumnDescriptor col)
        {
            col.Item().Text("I. DATOS DEL FALLECIDO").FontSize(11).Bold().Underline();
            col.Item().PaddingTop(5).Row(r =>
                r.RelativeItem().Text($"Nombre: {acta.NombreCompletoFallecido ?? "No especificado"}"));
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"HC: {acta.HistoriaClinica}");
                r.RelativeItem().Text($"{acta.TipoDocumentoFallecido}: {acta.NumeroDocumentoFallecido}");
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"Servicio: {acta.ServicioFallecimiento}");
                r.RelativeItem().Text($"Fecha fallecimiento: {acta.FechaHoraFallecimiento:dd/MM/yyyy HH:mm}");
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(
                    $"Edad: {(_edad.HasValue && _edad > 0 ? $"{_edad} años" : "No registrada")}");
                r.RelativeItem().Text($"Diagnóstico: {_diagnostico ?? "No registrado"}");
            });
            col.Item().PaddingTop(10);
        }

        private void BuildSeccionDocumentoLegal(ColumnDescriptor col)
        {
            col.Item().Text("II. DOCUMENTO LEGAL").FontSize(11).Bold().Underline();
            col.Item().PaddingTop(5);
            if (acta.TipoSalida == TipoSalida.Familiar)
                col.Item().Text($"Certificado SINADEF: {acta.NumeroCertificadoDefuncion ?? "No registrado"}");
            else
                col.Item().Text($"Número de Oficio Legal: {acta.NumeroOficioPolicial ?? "No registrado"}");
            col.Item().PaddingTop(10);
        }

        private void BuildSeccionMedico(ColumnDescriptor col)
        {
            col.Item().Text("III. MÉDICO CERTIFICANTE").FontSize(11).Bold().Underline();
            if (_tieneMedExt)
                col.Item().PaddingTop(4)
                    .Text("Médico Interno (constata el fallecimiento en SIGEM/HOSP):")
                    .FontSize(9).Bold().FontColor(Colors.Grey.Darken1);

            col.Item().PaddingTop(5).Row(r =>
                r.RelativeItem().Text($"Nombre: {acta.MedicoCertificaNombre}"));
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"CMP: {acta.MedicoCMP}");
                if (!string.IsNullOrWhiteSpace(acta.MedicoRNE))
                    r.RelativeItem().Text($"RNE: {acta.MedicoRNE}");
            });

            if (_tieneMedExt)
            {
                col.Item().PaddingTop(8)
                    .Text("Médico Externo (genera certificado SINADEF):")
                    .FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(3).Row(r =>
                    r.RelativeItem().Text($"Nombre: {acta.MedicoExternoNombre}").FontSize(10));
                col.Item().Row(r =>
                    r.RelativeItem()
                        .Text($"CMP: {acta.MedicoExternoCMP ?? "No registrado"}").FontSize(10));
            }
            col.Item().PaddingTop(10);
        }

        private void BuildSeccionJefeGuardia(ColumnDescriptor col)
        {
            col.Item().Text("IV. JEFE DE GUARDIA AUTORIZANTE").FontSize(11).Bold().Underline();
            col.Item().PaddingTop(5).Row(r =>
                r.RelativeItem().Text($"Nombre: {acta.JefeGuardiaNombre}"));
            col.Item().Row(r =>
                r.RelativeItem().Text($"CMP: {acta.JefeGuardiaCMP}"));

            if (_tieneBypass)
                col.Item().PaddingTop(4).Text(text =>
                {
                    text.Span("EXCEPCIÓN AUTORIZADA: ").Bold().FontSize(9);
                    text.Span("Retiro autorizado con deudas pendientes. ").FontSize(9);
                    text.Span($"Justificación: {acta.BypassDeudaJustificacion ?? "Sin detalle"}")
                        .FontSize(9).Italic();
                });
            col.Item().PaddingTop(10);
        }

        private void BuildSeccionResponsable(ColumnDescriptor col)
        {
            if (acta.TipoSalida == TipoSalida.Familiar)
            {
                col.Item().Text("V. FAMILIAR RESPONSABLE DEL RETIRO")
                    .FontSize(11).Bold().Underline();
                col.Item().PaddingTop(5).Row(r =>
                    r.RelativeItem().Text($"Nombre: {acta.FamiliarNombreCompleto ?? "No registrado"}"));
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(
                        $"{acta.FamiliarTipoDocumento}: {acta.FamiliarNumeroDocumento ?? "N/A"}");
                    r.RelativeItem().Text($"Parentesco: {acta.FamiliarParentesco ?? "No especificado"}");
                });
                col.Item().Row(r =>
                    r.RelativeItem().Text($"Teléfono: {acta.FamiliarTelefono ?? "No registrado"}"));
            }
            else
            {
                col.Item().Text("V. AUTORIDAD LEGAL RESPONSABLE")
                    .FontSize(11).Bold().Underline();
                col.Item().PaddingTop(5).Row(r =>
                    r.RelativeItem().Text($"Tipo: {DescripcionAutoridad(acta.TipoAutoridad)}"));
                col.Item().Row(r =>
                    r.RelativeItem().Text($"Nombre: {acta.AutoridadNombreCompleto ?? "No registrado"}"));
                col.Item().Row(r =>
                    r.RelativeItem().Text(
                        $"{acta.AutoridadTipoDocumento}: {acta.AutoridadNumeroDocumento ?? "N/A"}"));
                col.Item().Row(r =>
                    r.RelativeItem().Text($"Cargo: {acta.AutoridadCargo ?? "No especificado"}"));
                col.Item().Row(r =>
                    r.RelativeItem().Text($"Institución: {acta.AutoridadInstitucion ?? "No registrada"}"));
            }
        }

        private void BuildDestino(ColumnDescriptor col)
        {
            if (!string.IsNullOrWhiteSpace(acta.Destino))
                col.Item().PaddingTop(10).Text($"Destino: {acta.Destino}").Bold();
            if (!string.IsNullOrWhiteSpace(acta.Observaciones))
                col.Item().PaddingTop(5).Text($"Observaciones: {acta.Observaciones}");
        }

        private void BuildFirmas(ColumnDescriptor col)
        {
            col.Item().PaddingTop(20);
            col.Item().Text("VI. FIRMAS").FontSize(11).Bold().Underline().AlignCenter();
            col.Item().PaddingTop(15).Row(row =>
            {
                FirmaBox(row, acta.TipoSalida == TipoSalida.Familiar
                    ? "Firma del Familiar" : "Firma de la Autoridad",
                    acta.TipoSalida == TipoSalida.Familiar
                        ? acta.FamiliarNombreCompleto : acta.AutoridadNombreCompleto);
                row.ConstantItem(15);
                FirmaBox(row, "Firma del Admisionista", acta.UsuarioAdmision?.NombreCompleto);
                row.ConstantItem(15);
                FirmaBox(row, "Firma Supervisor Vigilancia", "(Mortuorio)");
            });
        }

        private static void FirmaBox(RowDescriptor row, string titulo, string? nombre)
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Border(1).Height(50);
                c.Item().PaddingTop(5).Text(titulo).FontSize(8).AlignCenter();
                c.Item().Text(nombre ?? "").FontSize(7).AlignCenter();
            });
        }

        // ── Helpers estáticos ────────────────────────────────────────────

        private static int? CalcularEdad(DateTime? fechaNac)
        {
            if (!fechaNac.HasValue || fechaNac == default) return null;
            var hoy = DateTime.Today;
            var edad = hoy.Year - fechaNac.Value.Year;
            if (fechaNac.Value.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        private static string DescripcionTipoSalida(TipoSalida tipo) => tipo switch
        {
            TipoSalida.Familiar => "RETIRO POR FAMILIAR",
            TipoSalida.AutoridadLegal => "RETIRO POR AUTORIDAD LEGAL",
            _ => "OTRO"
        };

        private static string DescripcionAutoridad(TipoAutoridadExterna? tipo) => tipo switch
        {
            TipoAutoridadExterna.Policia => "POLICÍA NACIONAL DEL PERÚ (PNP)",
            TipoAutoridadExterna.Fiscal => "MINISTERIO PÚBLICO - FISCALÍA",
            TipoAutoridadExterna.MedicoLegista => "MÉDICO LEGISTA",
            _ => "AUTORIDAD"
        };
    }
}