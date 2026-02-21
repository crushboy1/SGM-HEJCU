using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Fase5_ModulosAdministrativos_RefactorizacionCompleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OcupacionesBandejas");

            migrationBuilder.DropIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID",
                table: "VerificacionesMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_DeudasSangre_ExpedienteID",
                table: "DeudasSangre");

            migrationBuilder.DropIndex(
                name: "IX_DeudasEconomicas_ExpedienteID",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "CompromisoFirmado",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "TieneDeuda",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "PorcentajeExoneracion",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "AutoridadesExternas");

            migrationBuilder.RenameColumn(
                name: "EstaCompleto",
                table: "DocumentosLegales",
                newName: "Validado");

            migrationBuilder.AddColumn<int>(
                name: "BandejaAsignadaID",
                table: "VerificacionesMortuorio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BandejaLiberadaID",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TiempoPermanencia",
                table: "SalidasMortuorio",
                type: "time",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoSeguro",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "Expedientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "MedicoRNE",
                table: "Expedientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoQR",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoExpediente",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "BandejaActualID",
                table: "Expedientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoExternoCMP",
                table: "Expedientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoExternoNombre",
                table: "Expedientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereIntervencionLegal",
                table: "Expedientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "DocumentosLegales",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RutaArchivo",
                table: "DocumentosLegales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Adjuntado",
                table: "DocumentosLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "DocumentosLegales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteLegalID",
                table: "DocumentosLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extension",
                table: "DocumentosLegales",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacion",
                table: "DocumentosLegales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivo",
                table: "DocumentosLegales",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesValidacion",
                table: "DocumentosLegales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TamañoArchivo",
                table: "DocumentosLegales",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioValidadorID",
                table: "DocumentosLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DNIFamiliarCompromiso",
                table: "DeudasSangre",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "DeudasSangre",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "DeudasSangre",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCompromisoFirmado",
                table: "DeudasSangre",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificacionAnulacion",
                table: "DeudasSangre",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MedicoAnulaID",
                table: "DeudasSangre",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreFamiliarCompromiso",
                table: "DeudasSangre",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaPDFCompromiso",
                table: "DeudasSangre",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSangre",
                table: "DeudasSangre",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadesAdeudadas",
                table: "DeudasSangre",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoDeuda",
                table: "DeudasEconomicas",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "DeudasEconomicas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AsistentaSocialID",
                table: "DeudasEconomicas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExoneracion",
                table: "DeudasEconomicas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "DeudasEconomicas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoExonerado",
                table: "DeudasEconomicas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagado",
                table: "DeudasEconomicas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoSustento",
                table: "DeudasEconomicas",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesPago",
                table: "DeudasEconomicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaPDFSustento",
                table: "DeudasEconomicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TamañoArchivoSustento",
                table: "DeudasEconomicas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoExoneracion",
                table: "DeudasEconomicas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "AutoridadesExternas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TipoAutoridad",
                table: "AutoridadesExternas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "AutoridadesExternas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMaterno",
                table: "AutoridadesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoPaterno",
                table: "AutoridadesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "AutoridadesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentoEntregado",
                table: "AutoridadesExternas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDocumentoOficial",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraLlegada",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraSalida",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "AutoridadesExternas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nombres",
                table: "AutoridadesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoOficial",
                table: "AutoridadesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "AutoridadesExternas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "AutoridadesExternas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Modulo",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "BandejaHistoriales",
                columns: table => new
                {
                    OcupacionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BandejaID = table.Column<int>(type: "int", nullable: false),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioAsignadorID = table.Column<int>(type: "int", nullable: false),
                    UsuarioLiberaID = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BandejaHistoriales", x => x.OcupacionID);
                    table.ForeignKey(
                        name: "FK_BandejaHistoriales_AspNetUsers_UsuarioAsignadorID",
                        column: x => x.UsuarioAsignadorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandejaHistoriales_AspNetUsers_UsuarioLiberaID",
                        column: x => x.UsuarioLiberaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandejaHistoriales_Bandejas_BandejaID",
                        column: x => x.BandejaID,
                        principalTable: "Bandejas",
                        principalColumn: "BandejaID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandejaHistoriales_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpedientesLegales",
                columns: table => new
                {
                    ExpedienteLegalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    DocumentosCompletos = table.Column<bool>(type: "bit", nullable: false),
                    DocumentosPendientes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidadoJefeGuardia = table.Column<bool>(type: "bit", nullable: false),
                    JefeGuardiaValidadorID = table.Column<int>(type: "int", nullable: true),
                    FechaValidacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservacionesValidacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TienePendientes = table.Column<bool>(type: "bit", nullable: false),
                    FechaLimitePendientes = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NombrePoliciaRegistrado = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ComisariaOrigen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreFiscalRegistrado = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FiscaliaOrigen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreMedicoLegista = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreadorID = table.Column<int>(type: "int", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesLegales", x => x.ExpedienteLegalID);
                    table.ForeignKey(
                        name: "FK_ExpedientesLegales_AspNetUsers_JefeGuardiaValidadorID",
                        column: x => x.JefeGuardiaValidadorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpedientesLegales_AspNetUsers_UsuarioCreadorID",
                        column: x => x.UsuarioCreadorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpedientesLegales_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_BandejaAsignadaID",
                table: "VerificacionesMortuorio",
                column: "BandejaAsignadaID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID",
                table: "VerificacionesMortuorio",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_BandejaLiberadaID",
                table: "SalidasMortuorio",
                column: "BandejaLiberadaID");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_BandejaActualID",
                table: "Expedientes",
                column: "BandejaActualID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_ExpedienteLegalID",
                table: "DocumentosLegales",
                column: "ExpedienteLegalID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_UsuarioValidadorID",
                table: "DocumentosLegales",
                column: "UsuarioValidadorID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_ExpedienteID",
                table: "DeudasSangre",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_MedicoAnulaID",
                table: "DeudasSangre",
                column: "MedicoAnulaID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_AsistentaSocialID",
                table: "DeudasEconomicas",
                column: "AsistentaSocialID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_ExpedienteID",
                table: "DeudasEconomicas",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BandejaHistoriales_BandejaID_FechaHoraSalida",
                table: "BandejaHistoriales",
                columns: new[] { "BandejaID", "FechaHoraSalida" });

            migrationBuilder.CreateIndex(
                name: "IX_BandejaHistoriales_ExpedienteID",
                table: "BandejaHistoriales",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_BandejaHistoriales_UsuarioAsignadorID",
                table: "BandejaHistoriales",
                column: "UsuarioAsignadorID");

            migrationBuilder.CreateIndex(
                name: "IX_BandejaHistoriales_UsuarioLiberaID",
                table: "BandejaHistoriales",
                column: "UsuarioLiberaID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_ExpedienteID",
                table: "ExpedientesLegales",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_FechaLimitePendientes",
                table: "ExpedientesLegales",
                column: "FechaLimitePendientes");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_JefeGuardiaValidadorID",
                table: "ExpedientesLegales",
                column: "JefeGuardiaValidadorID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_TienePendientes",
                table: "ExpedientesLegales",
                column: "TienePendientes");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_UsuarioCreadorID",
                table: "ExpedientesLegales",
                column: "UsuarioCreadorID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeudasEconomicas_AspNetUsers_AsistentaSocialID",
                table: "DeudasEconomicas",
                column: "AsistentaSocialID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeudasSangre_AspNetUsers_MedicoAnulaID",
                table: "DeudasSangre",
                column: "MedicoAnulaID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosLegales_AspNetUsers_UsuarioValidadorID",
                table: "DocumentosLegales",
                column: "UsuarioValidadorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosLegales_ExpedientesLegales_ExpedienteLegalID",
                table: "DocumentosLegales",
                column: "ExpedienteLegalID",
                principalTable: "ExpedientesLegales",
                principalColumn: "ExpedienteLegalID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_Bandejas_BandejaActualID",
                table: "Expedientes",
                column: "BandejaActualID",
                principalTable: "Bandejas",
                principalColumn: "BandejaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_Bandejas_BandejaLiberadaID",
                table: "SalidasMortuorio",
                column: "BandejaLiberadaID",
                principalTable: "Bandejas",
                principalColumn: "BandejaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificacionesMortuorio_Bandejas_BandejaAsignadaID",
                table: "VerificacionesMortuorio",
                column: "BandejaAsignadaID",
                principalTable: "Bandejas",
                principalColumn: "BandejaID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeudasEconomicas_AspNetUsers_AsistentaSocialID",
                table: "DeudasEconomicas");

            migrationBuilder.DropForeignKey(
                name: "FK_DeudasSangre_AspNetUsers_MedicoAnulaID",
                table: "DeudasSangre");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosLegales_AspNetUsers_UsuarioValidadorID",
                table: "DocumentosLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosLegales_ExpedientesLegales_ExpedienteLegalID",
                table: "DocumentosLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_Bandejas_BandejaActualID",
                table: "Expedientes");

            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_Bandejas_BandejaLiberadaID",
                table: "SalidasMortuorio");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificacionesMortuorio_Bandejas_BandejaAsignadaID",
                table: "VerificacionesMortuorio");

            migrationBuilder.DropTable(
                name: "BandejaHistoriales");

            migrationBuilder.DropTable(
                name: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_VerificacionesMortuorio_BandejaAsignadaID",
                table: "VerificacionesMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID",
                table: "VerificacionesMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_BandejaLiberadaID",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_Expedientes_BandejaActualID",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_ExpedienteLegalID",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_UsuarioValidadorID",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_DeudasSangre_ExpedienteID",
                table: "DeudasSangre");

            migrationBuilder.DropIndex(
                name: "IX_DeudasSangre_MedicoAnulaID",
                table: "DeudasSangre");

            migrationBuilder.DropIndex(
                name: "IX_DeudasEconomicas_AsistentaSocialID",
                table: "DeudasEconomicas");

            migrationBuilder.DropIndex(
                name: "IX_DeudasEconomicas_ExpedienteID",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "BandejaAsignadaID",
                table: "VerificacionesMortuorio");

            migrationBuilder.DropColumn(
                name: "BandejaLiberadaID",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "TiempoPermanencia",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "BandejaActualID",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "MedicoExternoCMP",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "MedicoExternoNombre",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "RequiereIntervencionLegal",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "Adjuntado",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "ExpedienteLegalID",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "Extension",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "FechaValidacion",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "NombreArchivo",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "ObservacionesValidacion",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "TamañoArchivo",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "UsuarioValidadorID",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "DNIFamiliarCompromiso",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "FechaCompromisoFirmado",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "JustificacionAnulacion",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "MedicoAnulaID",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "NombreFamiliarCompromiso",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "RutaPDFCompromiso",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "TipoSangre",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "UnidadesAdeudadas",
                table: "DeudasSangre");

            migrationBuilder.DropColumn(
                name: "AsistentaSocialID",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "FechaExoneracion",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "MontoExonerado",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "MontoPagado",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "NombreArchivoSustento",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "ObservacionesPago",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "RutaPDFSustento",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "TamañoArchivoSustento",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "TipoExoneracion",
                table: "DeudasEconomicas");

            migrationBuilder.DropColumn(
                name: "ApellidoMaterno",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "ApellidoPaterno",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "DocumentoEntregado",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "FechaDocumentoOficial",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "FechaHoraLlegada",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "FechaHoraSalida",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "Nombres",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "NumeroDocumentoOficial",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "AutoridadesExternas");

            migrationBuilder.RenameColumn(
                name: "Validado",
                table: "DocumentosLegales",
                newName: "EstaCompleto");

            migrationBuilder.AlterColumn<string>(
                name: "TipoSeguro",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumento",
                table: "Expedientes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "MedicoRNE",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoQR",
                table: "Expedientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoExpediente",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumento",
                table: "DocumentosLegales",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "RutaArchivo",
                table: "DocumentosLegales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "CompromisoFirmado",
                table: "DeudasSangre",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneDeuda",
                table: "DeudasSangre",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoDeuda",
                table: "DeudasEconomicas",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Estado",
                table: "DeudasEconomicas",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeExoneracion",
                table: "DeudasEconomicas",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumento",
                table: "AutoridadesExternas",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "TipoAutoridad",
                table: "AutoridadesExternas",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "AutoridadesExternas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "AutoridadesExternas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Modulo",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "OcupacionesBandejas",
                columns: table => new
                {
                    OcupacionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BandejaID = table.Column<int>(type: "int", nullable: false),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    UsuarioAsignadorID = table.Column<int>(type: "int", nullable: false),
                    UsuarioLiberaID = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaHoraIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
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
                        name: "FK_OcupacionesBandejas_AspNetUsers_UsuarioLiberaID",
                        column: x => x.UsuarioLiberaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacionesBandejas_Bandejas_BandejaID",
                        column: x => x.BandejaID,
                        principalTable: "Bandejas",
                        principalColumn: "BandejaID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacionesBandejas_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID",
                table: "VerificacionesMortuorio",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasSangre_ExpedienteID",
                table: "DeudasSangre",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DeudasEconomicas_ExpedienteID",
                table: "DeudasEconomicas",
                column: "ExpedienteID");

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

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionesBandejas_UsuarioLiberaID",
                table: "OcupacionesBandejas",
                column: "UsuarioLiberaID");
        }
    }
}
