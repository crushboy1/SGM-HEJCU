using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEntidadesExpedientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: true),
                    UsuarioID = table.Column<int>(type: "int", nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatosAntes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosDespues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPOrigen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UsuarioID",
                        column: x => x.UsuarioID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoridadesExternas",
                columns: table => new
                {
                    AutoridadID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    TipoAutoridad = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DNI = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CodigoEspecial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Institucion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioRegistroID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoridadesExternas", x => x.AutoridadID);
                    table.ForeignKey(
                        name: "FK_AutoridadesExternas_AspNetUsers_UsuarioRegistroID",
                        column: x => x.UsuarioRegistroID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoridadesExternas_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeudasEconomicas",
                columns: table => new
                {
                    DeudaEconomicaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    MontoDeuda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    NumeroBoleta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PorcentajeExoneracion = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ObservacionesExoneracion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsuarioRegistroID = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioActualizacionID = table.Column<int>(type: "int", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeudasEconomicas", x => x.DeudaEconomicaID);
                    table.ForeignKey(
                        name: "FK_DeudasEconomicas_AspNetUsers_UsuarioActualizacionID",
                        column: x => x.UsuarioActualizacionID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeudasEconomicas_AspNetUsers_UsuarioRegistroID",
                        column: x => x.UsuarioRegistroID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeudasEconomicas_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeudasSangre",
                columns: table => new
                {
                    DeudaSangreID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    TieneDeuda = table.Column<bool>(type: "bit", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompromisoFirmado = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioRegistroID = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioActualizacionID = table.Column<int>(type: "int", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeudasSangre", x => x.DeudaSangreID);
                    table.ForeignKey(
                        name: "FK_DeudasSangre_AspNetUsers_UsuarioActualizacionID",
                        column: x => x.UsuarioActualizacionID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeudasSangre_AspNetUsers_UsuarioRegistroID",
                        column: x => x.UsuarioRegistroID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeudasSangre_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosLegales",
                columns: table => new
                {
                    DocumentoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstaCompleto = table.Column<bool>(type: "bit", nullable: false),
                    FechaAdjunto = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioAdjuntoID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosLegales", x => x.DocumentoID);
                    table.ForeignKey(
                        name: "FK_DocumentosLegales_AspNetUsers_UsuarioAdjuntoID",
                        column: x => x.UsuarioAdjuntoID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosLegales_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcupacionesBandejas",
                columns: table => new
                {
                    OcupacionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BandejaID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioAsignadorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcupacionesBandejas", x => x.OcupacionID);
                    table.ForeignKey(
                        name: "FK_OcupacionesBandejas_AspNetUsers_UsuarioAsignadorID",
                        column: x => x.UsuarioAsignadorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacionesBandejas_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ExpedienteID",
                table: "AuditLogs",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FechaHora",
                table: "AuditLogs",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Modulo_Accion",
                table: "AuditLogs",
                columns: new[] { "Modulo", "Accion" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UsuarioID",
                table: "AuditLogs",
                column: "UsuarioID");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_ExpedienteID",
                table: "AutoridadesExternas",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_UsuarioRegistroID",
                table: "AutoridadesExternas",
                column: "UsuarioRegistroID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_ExpedienteID",
                table: "DeudasEconomicas",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_UsuarioActualizacionID",
                table: "DeudasEconomicas",
                column: "UsuarioActualizacionID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_UsuarioRegistroID",
                table: "DeudasEconomicas",
                column: "UsuarioRegistroID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_ExpedienteID",
                table: "DeudasSangre",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_UsuarioActualizacionID",
                table: "DeudasSangre",
                column: "UsuarioActualizacionID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_UsuarioRegistroID",
                table: "DeudasSangre",
                column: "UsuarioRegistroID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_ExpedienteID",
                table: "DocumentosLegales",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_UsuarioAdjuntoID",
                table: "DocumentosLegales",
                column: "UsuarioAdjuntoID");

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionesBandejas_BandejaID_FechaHoraSalida",
                table: "OcupacionesBandejas",
                columns: new[] { "BandejaID", "FechaHoraSalida" });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionesBandejas_ExpedienteID",
                table: "OcupacionesBandejas",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionesBandejas_UsuarioAsignadorID",
                table: "OcupacionesBandejas",
                column: "UsuarioAsignadorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AutoridadesExternas");

            migrationBuilder.DropTable(
                name: "DeudasEconomicas");

            migrationBuilder.DropTable(
                name: "DeudasSangre");

            migrationBuilder.DropTable(
                name: "DocumentosLegales");

            migrationBuilder.DropTable(
                name: "OcupacionesBandejas");
        }
    }
}
