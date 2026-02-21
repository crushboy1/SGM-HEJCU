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
            // Configuración de licencia Community
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
                    // Configuración de página
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // 1. HEADER (Logos)
                    page.Header().Row(row =>
                    {
                        // Intenta cargar logo desde wwwroot/img
                        var rutaLogo = Path.Combine(_env.WebRootPath, "img", "header_minsa_hejcu.png");

                        if (File.Exists(rutaLogo))
                        {
                            // Ajustar altura del logo
                            row.RelativeItem().Height(2.5f, Unit.Centimetre).Image(rutaLogo).FitArea();
                        }
                        else
                        {
                            // Fallback texto si no hay logo
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("MINISTERIO DE SALUD").Bold().FontSize(14);
                                c.Item().Text("HOSPITAL DE EMERGENCIAS JOSÉ CASIMIRO ULLOA").FontSize(10);
                            });
                        }
                    });

                    // 2. CONTENIDO
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Fecha alineada a la derecha
                        col.Item().AlignRight().Text($"Miraflores, {DateTime.Now:dd 'de' MMMM 'del' yyyy}");

                        col.Item().Height(1.5f, Unit.Centimetre);

                        // Título
                        col.Item().AlignCenter().Text("COMPROMISO DE REPOSICIÓN")
                           .FontSize(16).Bold().Underline();
                        col.Item().AlignCenter().Text("DE UNIDADES DE SANGRE")
                           .FontSize(16).Bold().Underline();

                        col.Item().Height(2, Unit.Centimetre);

                        // Cuerpo del texto legal
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

                        // 3. FIRMAS (Grid)
                        col.Item().Row(row =>
                        {
                            // Columna Izquierda: Firma Familiar
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Height(2, Unit.Centimetre);
                                c.Item().AlignCenter().Text("_________________________");
                                c.Item().AlignCenter().Text("Familiar Responsable").FontSize(9).Bold();
                                c.Item().AlignCenter().Text(datos.NombreFamiliar).FontSize(8);
                                c.Item().AlignCenter().Text($"DNI: {datos.DNIFamiliar}").FontSize(8);
                            });

                            row.ConstantItem(1, Unit.Centimetre);

                            // Columna Derecha: Firma Banco Sangre
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Height(2, Unit.Centimetre);
                                c.Item().AlignCenter().Text("_________________________");
                                c.Item().AlignCenter().Text("SERVICIO DE HEMOTERAPIA").FontSize(9).Bold();
                                c.Item().AlignCenter().Text("Y BANCO DE SANGRE").FontSize(9).Bold();
                            });
                        });
                    });

                    // 4. FOOTER
                    page.Footer().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        c.Item().PaddingTop(5).AlignCenter().Text("Av. Roosevelt N° 6355 - Miraflores / Teléfono: 446-2321")
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
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configuración de página
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // ═════════════════════════════════════════════════════════
                    // HEADER
                    // ═════════════════════════════════════════════════════════
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
                                column.Item().Text("ACTA DE RETIRO DE CADÁVER")
                                    .FontSize(12).Bold();
                                column.Item().Text($"N° {acta.Expediente?.CodigoExpediente ?? "S/N"}")
                                    .FontSize(10);
                            });
                        }

                        row.ConstantItem(100).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Fecha: {acta.FechaRegistro:dd/MM/yyyy}");
                            col.Item().AlignRight().Text($"Tipo: {ObtenerDescripcionTipoSalida(acta.TipoSalida)}")
                                .FontSize(9).Italic();
                        });
                    });

                    // ═════════════════════════════════════════════════════════
                    // CONTENT
                    // ═════════════════════════════════════════════════════════
                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // ─────────────────────────────────────────────────────
                        // DATOS DEL FALLECIDO
                        // ─────────────────────────────────────────────────────
                        column.Item().Text("I. DATOS DEL FALLECIDO")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Nombre: {acta.NombreCompletoFallecido ?? "No especificado"}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"HC: {acta.HistoriaClinica}");
                            row.RelativeItem().Text($"{acta.TipoDocumentoFallecido}: {acta.NumeroDocumentoFallecido}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Servicio: {acta.ServicioFallecimiento}");
                            row.RelativeItem().Text($"Fecha: {acta.FechaHoraFallecimiento:dd/MM/yyyy HH:mm}");
                        });

                        column.Item().PaddingTop(10);

                        // ─────────────────────────────────────────────────────
                        // DOCUMENTO LEGAL (CONDICIONAL)
                        // ─────────────────────────────────────────────────────
                        column.Item().Text("II. DOCUMENTO LEGAL")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5);

                        if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar)
                        {
                            column.Item().Text($"Certificado de Defunción SINADEF: {acta.NumeroCertificadoDefuncion ?? "No registrado"}");
                        }
                        else if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.AutoridadLegal)
                        {
                            column.Item().Text($"Número de Oficio Legal: {acta.NumeroOficioLegal ?? "No registrado"}");
                        }

                        column.Item().PaddingTop(10);

                        // ─────────────────────────────────────────────────────
                        // MÉDICO CERTIFICANTE
                        // ─────────────────────────────────────────────────────
                        column.Item().Text("III. MÉDICO CERTIFICANTE")
                            .FontSize(11).Bold().Underline();

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Nombre: {acta.MedicoCertificaNombre}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"CMP: {acta.MedicoCMP}");
                            if (!string.IsNullOrWhiteSpace(acta.MedicoRNE))
                            {
                                row.RelativeItem().Text($"RNE: {acta.MedicoRNE}");
                            }
                        });

                        column.Item().PaddingTop(10);

                        // ─────────────────────────────────────────────────────
                        // JEFE DE GUARDIA
                        // ─────────────────────────────────────────────────────
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

                        column.Item().PaddingTop(10);

                        // ─────────────────────────────────────────────────────
                        // RESPONSABLE DEL RETIRO (CONDICIONAL)
                        // ─────────────────────────────────────────────────────
                        if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar)
                        {
                            column.Item().Text("V. FAMILIAR RESPONSABLE DEL RETIRO")
                                .FontSize(11).Bold().Underline();

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Nombre: {acta.FamiliarNombreCompleto ?? "No registrado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"{acta.FamiliarTipoDocumento}: {acta.FamiliarNumeroDocumento ?? "N/A"}");
                                row.RelativeItem().Text($"Parentesco: {acta.FamiliarParentesco ?? "No especificado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Teléfono: {acta.FamiliarTelefono ?? "No registrado"}");
                            });
                        }
                        else if (acta.TipoSalida == Data.Entities.Enums.TipoSalida.AutoridadLegal)
                        {
                            column.Item().Text("V. AUTORIDAD LEGAL RESPONSABLE")
                                .FontSize(11).Bold().Underline();

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Tipo: {ObtenerDescripcionAutoridad(acta.TipoAutoridad)}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Nombre: {acta.AutoridadNombreCompleto ?? "No registrado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"{acta.AutoridadTipoDocumento}: {acta.AutoridadNumeroDocumento ?? "N/A"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Cargo: {acta.AutoridadCargo ?? "No especificado"}");
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Institución: {acta.AutoridadInstitucion ?? "No registrada"}");
                            });

                            if (!string.IsNullOrWhiteSpace(acta.AutoridadPlacaVehiculo))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Placa Vehículo: {acta.AutoridadPlacaVehiculo}");
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(acta.AutoridadTelefono))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Teléfono: {acta.AutoridadTelefono}");
                                });
                            }
                        }

                        // ─────────────────────────────────────────────────────
                        // DESTINO Y OBSERVACIONES
                        // ─────────────────────────────────────────────────────
                        if (!string.IsNullOrWhiteSpace(acta.Destino))
                        {
                            column.Item().PaddingTop(10).Text($"Destino: {acta.Destino}").Bold();
                        }

                        if (!string.IsNullOrWhiteSpace(acta.Observaciones))
                        {
                            column.Item().PaddingTop(5).Text($"Observaciones: {acta.Observaciones}");
                        }

                        column.Item().PaddingTop(20);

                        // ─────────────────────────────────────────────────────
                        // FIRMAS
                        // ─────────────────────────────────────────────────────
                        column.Item().Text("VI. FIRMAS")
                            .FontSize(11).Bold().Underline().AlignCenter();

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            // Firma Responsable (Familiar O Autoridad)
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Border(1).Height(50);
                                col.Item().PaddingTop(5).Text(
                                    acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar
                                        ? "Firma del Familiar"
                                        : "Firma de la Autoridad"
                                ).FontSize(8).AlignCenter();

                                var nombreResponsable = acta.TipoSalida == Data.Entities.Enums.TipoSalida.Familiar
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
                                col.Item().Text("(Mortuorio)")
                                    .FontSize(7).AlignCenter();
                            });
                        });
                    });

                    // ═════════════════════════════════════════════════════════
                    // FOOTER
                    // ═════════════════════════════════════════════════════════
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

        // Helper simple para convertir números a letras (1-10)
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

        /// <summary>
        /// Helpers para Acta de Retiro
        /// Obtiene descripción legible del tipo de salida
        /// </summary>
        private string ObtenerDescripcionTipoSalida(Data.Entities.Enums.TipoSalida tipo)
        {
            return tipo switch
            {
                Data.Entities.Enums.TipoSalida.Familiar => "RETIRO POR FAMILIAR",
                Data.Entities.Enums.TipoSalida.AutoridadLegal => "RETIRO POR AUTORIDAD LEGAL",
                _ => "OTRO"
            };
        }

        /// <summary>
        /// Obtiene descripción legible del tipo de autoridad
        /// </summary>
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
