using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private readonly IWebHostEnvironment _env;

        public PdfGeneratorService(IWebHostEnvironment env)
        {
            _env = env;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ===================================================================
        // MÉTODO 1: COMPROMISO DE SANGRE
        // ===================================================================
        public byte[] GenerarCompromisoSangre(GenerarCompromisoDTO datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        var rutaLogo = Path.Combine(_env.WebRootPath, "img", "header_minsa_hejcu.png");
                        if (File.Exists(rutaLogo))
                        {
                            row.RelativeItem().Height(2.5f, Unit.Centimetre).Image(rutaLogo).FitArea();
                        }
                        else
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("MINISTERIO DE SALUD").Bold().FontSize(14);
                                c.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA").FontSize(10);
                            });
                        }
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Miraflores, {DateTime.Now:dd 'de' MMMM 'del' yyyy}");
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

                        col.Item().PaddingTop(10).Text("Asimismo, declaro conocer que este compromiso es indispensable para regularizar los trámites administrativos del paciente.");
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
            });

            return documento.GeneratePdf();
        }

        // ===================================================================
        // MÉTODO 2: ACTA DE RETIRO
        // ===================================================================
        public byte[] GenerarActaRetiro(ActaRetiro acta)
        {
            // Calcular edad desde FechaNacimiento del Expediente relacionado
            int? edad = null;
            if (acta.Expediente?.FechaNacimiento is DateTime fn && fn != default)
            {
                var hoy = DateTime.Today;
                edad = hoy.Year - fn.Year;
                if (fn.Date > hoy.AddYears(-edad.Value)) edad--;
            }
            var diagnostico = acta.Expediente?.DiagnosticoFinal;
            var tieneMedExt = !string.IsNullOrWhiteSpace(acta.MedicoExternoNombre);
            var tieneBypass = acta.BypassDeudaAutorizado;

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // ─────────────────────────────────────────────────────────
                    // HEADER
                    // ─────────────────────────────────────────────────────────
                    page.Header().Row(row =>
                    {
                        var rutaLogo = Path.Combine(_env.WebRootPath, "img", "header_minsa_hejcu.png");
                        if (File.Exists(rutaLogo))
                        {
                            row.RelativeItem().Height(2.5f, Unit.Centimetre).Image(rutaLogo).FitArea();
                        }
                        else
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA")
                                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text("ACTA DE RETIRO DE CADÁVER").FontSize(12).Bold();
                                column.Item().Text($"N° {acta.Expediente?.CodigoExpediente ?? "S/N"}").FontSize(10);
                            });
                        }

                        row.ConstantItem(100).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Fecha: {acta.FechaRegistro:dd/MM/yyyy}");
                            col.Item().AlignRight()
                                .Text($"Tipo: {ObtenerDescripcionTipoSalida(acta.TipoSalida)}")
                                .FontSize(9).Italic();
                        });
                    });

                    // ─────────────────────────────────────────────────────────
                    // CONTENT
                    // ─────────────────────────────────────────────────────────
                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // ═══════════════════════════════════════════════════
                        // I. DATOS DEL FALLECIDO
                        // Incluye edad y diagnóstico (digitaliza cuaderno VigSup)
                        // ═══════════════════════════════════════════════════
                        column.Item().Text("I. DATOS DEL FALLECIDO")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text(
                                $"Nombre: {acta.NombreCompletoFallecido ?? "No especificado"}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"HC: {acta.HistoriaClinica}");
                            row.RelativeItem().Text(
                                $"{acta.TipoDocumentoFallecido}: {acta.NumeroDocumentoFallecido}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Servicio: {acta.ServicioFallecimiento}");
                            row.RelativeItem().Text(
                                $"Fecha fallecimiento: {acta.FechaHoraFallecimiento:dd/MM/yyyy HH:mm}");
                        });

                        // Edad y Diagnóstico — digitalizan cuaderno de control de permanencia
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(
                                $"Edad: {(edad.HasValue && edad > 0 ? $"{edad} años" : "No registrada")}");
                            row.RelativeItem().Text(
                                $"Diagnóstico: {diagnostico ?? "No registrado"}");
                        });

                        column.Item().PaddingTop(10);

                        // ═══════════════════════════════════════════════════
                        // II. DOCUMENTO LEGAL
                        // ═══════════════════════════════════════════════════
                        column.Item().Text("II. DOCUMENTO LEGAL")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5);

                        if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar)
                        {
                            column.Item().Text(
                                $"Certificado de Defunción SINADEF: {acta.NumeroCertificadoDefuncion ?? "No registrado"}");
                        }
                        else if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.AutoridadLegal)
                        {
                            column.Item().Text(
                                $"Número de Oficio Legal: {acta.NumeroOficioPolicial ?? "No registrado"}");
                        }

                        column.Item().PaddingTop(10);

                        // ═══════════════════════════════════════════════════
                        // III. MÉDICO CERTIFICANTE
                        // Si hay médico externo: el médico interno se rotula como
                        // "Médico Interno (constata fallecimiento)" para distinguirlo
                        // del médico externo que genera el SINADEF.
                        // ═══════════════════════════════════════════════════

                        // Título de sección varía según si hay médico externo
                        column.Item().Text("III. MÉDICO CERTIFICANTE")
                            .FontSize(11).Bold().Underline();

                        // Sub-etiqueta contextual solo cuando hay médico externo
                        if (tieneMedExt)
                        {
                            column.Item().PaddingTop(4)
                                .Text("Médico Interno (constata el fallecimiento en SIGEM/HOSP):")
                                .FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                        }

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Nombre: {acta.MedicoCertificaNombre}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"CMP: {acta.MedicoCMP}");
                            if (!string.IsNullOrWhiteSpace(acta.MedicoRNE))
                                row.RelativeItem().Text($"RNE: {acta.MedicoRNE}");
                        });

                        // Médico externo — solo cuando familia trae médico de cabecera
                        if (tieneMedExt)
                        {
                            column.Item().PaddingTop(8)
                                .Text("Médico Externo (genera certificado SINADEF):")
                                .FontSize(9).Bold().FontColor(Colors.Grey.Darken1);

                            column.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Nombre: {acta.MedicoExternoNombre}").FontSize(10);
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"CMP: {acta.MedicoExternoCMP ?? "No registrado"}").FontSize(10);
                            });
                        }

                        column.Item().PaddingTop(10);

                        // ═══════════════════════════════════════════════════
                        // IV. JEFE DE GUARDIA
                        // ═══════════════════════════════════════════════════
                        column.Item().Text("IV. JEFE DE GUARDIA AUTORIZANTE")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Nombre: {acta.JefeGuardiaNombre}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"CMP: {acta.JefeGuardiaCMP}");
                        });

                        // Nota de excepción de deuda — solo si JG/Admin autorizó bypass
                        if (tieneBypass)
                        {
                            column.Item().PaddingTop(4)
                                
                                .Text(text =>
                                {
                                    text.Span("EXCEPCIÓN AUTORIZADA: ").Bold().FontSize(9);
                                    text.Span("Retiro autorizado con deudas pendientes. ")
                                        .FontSize(9);
                                    text.Span(
                                        $"Justificación: {acta.BypassDeudaJustificacion ?? "Sin detalle"}")
                                        .FontSize(9).Italic();
                                });
                        }

                        column.Item().PaddingTop(10);

                        // ═══════════════════════════════════════════════════
                        // V. RESPONSABLE DEL RETIRO (CONDICIONAL)
                        // ═══════════════════════════════════════════════════
                        if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar)
                        {
                            column.Item().Text("V. FAMILIAR RESPONSABLE DEL RETIRO")
                                .FontSize(11).Bold().Underline();

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Nombre: {acta.FamiliarNombreCompleto ?? "No registrado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"{acta.FamiliarTipoDocumento}: {acta.FamiliarNumeroDocumento ?? "N/A"}");
                                row.RelativeItem().Text(
                                    $"Parentesco: {acta.FamiliarParentesco ?? "No especificado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Teléfono: {acta.FamiliarTelefono ?? "No registrado"}");
                            });
                        }
                        else if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.AutoridadLegal)
                        {
                            column.Item().Text("V. AUTORIDAD LEGAL RESPONSABLE")
                                .FontSize(11).Bold().Underline();

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Tipo: {ObtenerDescripcionAutoridad(acta.TipoAutoridad)}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Nombre: {acta.AutoridadNombreCompleto ?? "No registrado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"{acta.AutoridadTipoDocumento}: {acta.AutoridadNumeroDocumento ?? "N/A"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Grado/Cargo: {acta.AutoridadCargo ?? "No especificado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Institución: {acta.AutoridadInstitucion ?? "No registrada"}");
                            });

                            if (!string.IsNullOrWhiteSpace(acta.AutoridadTelefono))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Teléfono: {acta.AutoridadTelefono}");
                                });
                            }
                        }

                        // ═══════════════════════════════════════════════════
                        // DESTINO Y OBSERVACIONES
                        // ═══════════════════════════════════════════════════
                        if (!string.IsNullOrWhiteSpace(acta.Destino))
                        {
                            column.Item().PaddingTop(10)
                                .Text($"Destino: {acta.Destino}").Bold();
                        }

                        if (!string.IsNullOrWhiteSpace(acta.Observaciones))
                        {
                            column.Item().PaddingTop(5)
                                .Text($"Observaciones: {acta.Observaciones}");
                        }

                        column.Item().PaddingTop(20);

                        // ═══════════════════════════════════════════════════
                        // VI. FIRMAS
                        // ═══════════════════════════════════════════════════
                        column.Item().Text("VI. FIRMAS")
                            .FontSize(11).Bold().Underline().AlignCenter();

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            // Firma Responsable
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Border(1).Height(50);
                                col.Item().PaddingTop(5).Text(
                                    acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar
                                        ? "Firma del Familiar"
                                        : "Firma de la Autoridad"
                                ).FontSize(8).AlignCenter();

                                var nombreResponsable =
                                    acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar
                                        ? acta.FamiliarNombreCompleto
                                        : acta.AutoridadNombreCompleto;

                                col.Item().Text(nombreResponsable ?? "").FontSize(7).AlignCenter();
                            });

                            row.ConstantItem(15);

                            // Firma Admisionista
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Border(1).Height(50);
                                col.Item().PaddingTop(5).Text("Firma del Admisionista")
                                    .FontSize(8).AlignCenter();
                                col.Item().Text(acta.UsuarioAdmision?.NombreCompleto ?? "")
                                    .FontSize(7).AlignCenter();
                            });

                            row.ConstantItem(15);

                            // Firma Supervisor Vigilancia
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Border(1).Height(50);
                                col.Item().PaddingTop(5).Text("Firma Supervisor Vigilancia")
                                    .FontSize(8).AlignCenter();
                                col.Item().Text("(Mortuorio)").FontSize(7).AlignCenter();
                            });
                        });
                    });

                    // ─────────────────────────────────────────────────────────
                    // FOOTER
                    // ─────────────────────────────────────────────────────────
                    page.Footer().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        c.Item().PaddingTop(5).AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                            text.Span("Av. Roosevelt N° 6355 - Miraflores / Teléfono: 446-2321 | ");
                            text.Span("Documento generado el ").FontSize(7);
                            text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).Bold();
                        });
                    });
                });
            });

            return documento.GeneratePdf();
        }

        // ===================================================================
        // HELPERS
        // ===================================================================

        private string NumeroALetras(int numero)
        {
            return numero switch
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
                _ => numero.ToString()
            };
        }

        private string ObtenerDescripcionTipoSalida(Data.Entities.Enums.TipoSalida tipo)
        {
            return tipo switch
            {
                Data.Entities.Enums.TipoSalida.Familiar => "RETIRO POR FAMILIAR",
                Data.Entities.Enums.TipoSalida.AutoridadLegal => "RETIRO POR AUTORIDAD LEGAL",
                _ => "OTRO"
            };
        }

        private string ObtenerDescripcionAutoridad(Data.Entities.Enums.TipoAutoridadExterna? tipo)
        {
            if (tipo == null) return "No especificado";

            return tipo switch
            {
                Data.Entities.Enums.TipoAutoridadExterna.Policia => "POLICÍA NACIONAL DEL PERÚ (PNP)",
                Data.Entities.Enums.TipoAutoridadExterna.Fiscal => "MINISTERIO PÚBLICO - FISCALÍA",
                Data.Entities.Enums.TipoAutoridadExterna.MedicoLegista => "MÉDICO LEGISTA",
                _ => "AUTORIDAD"
            };
        }
    }
}