using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActaRetiroYActualizarSalidaMortuorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<int>(
                name: "TipoSalida",
                table: "SalidasMortuorio",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<bool>(
                name: "PagoRealizado",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IncidenteRegistrado",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHoraSalida",
                table: "SalidasMortuorio",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "DocumentacionVerificada",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "ActaRetiroID",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteLegalID",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActasRetiro",
                columns: table => new
                {
                    ActaRetiroID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    NumeroCertificadoDefuncion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreCompletoFallecido = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    HistoriaClinica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoDocumentoFallecido = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumentoFallecido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServicioFallecimiento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaHoraFallecimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MedicoCertificaNombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MedicoCMP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MedicoRNE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JefeGuardiaNombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    JefeGuardiaCMP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoSalida = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FamiliarNombreCompleto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FamiliarTipoDocumento = table.Column<int>(type: "int", nullable: false),
                    FamiliarNumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FamiliarParentesco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FamiliarTelefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmadoFamiliar = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaFirmaFamiliar = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirmadoAdmisionista = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaFirmaAdmisionista = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirmadoSupervisorVigilancia = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaSupervisorVigilancia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RutaPDFSinFirmar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NombreArchivoPDFSinFirmar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TamañoPDFSinFirmar = table.Column<long>(type: "bigint", nullable: true),
                    RutaPDFFirmado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NombreArchivoPDFFirmado = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TamañoPDFFirmado = table.Column<long>(type: "bigint", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsuarioAdmisionID = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UsuarioSubidaPDFID = table.Column<int>(type: "int", nullable: true),
                    FechaSubidaPDF = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasRetiro", x => x.ActaRetiroID);
                    table.ForeignKey(
                        name: "FK_ActasRetiro_AspNetUsers_UsuarioAdmisionID",
                        column: x => x.UsuarioAdmisionID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActasRetiro_AspNetUsers_UsuarioSubidaPDFID",
                        column: x => x.UsuarioSubidaPDFID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActasRetiro_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio",
                column: "ActaRetiroID",
                unique: true,
                filter: "[ActaRetiroID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteLegalID",
                table: "SalidasMortuorio",
                column: "ExpedienteLegalID");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_ExpedienteID",
                table: "ActasRetiro",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_FamiliarNumeroDocumento",
                table: "ActasRetiro",
                column: "FamiliarNumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_FechaRegistro",
                table: "ActasRetiro",
                column: "FechaRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_NumeroCertificadoDefuncion",
                table: "ActasRetiro",
                column: "NumeroCertificadoDefuncion");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_TipoSalida",
                table: "ActasRetiro",
                column: "TipoSalida");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_UsuarioAdmisionID",
                table: "ActasRetiro",
                column: "UsuarioAdmisionID");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_UsuarioSubidaPDFID",
                table: "ActasRetiro",
                column: "UsuarioSubidaPDFID");

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_ActasRetiro_ActaRetiroID",
                table: "SalidasMortuorio",
                column: "ActaRetiroID",
                principalTable: "ActasRetiro",
                principalColumn: "ActaRetiroID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_ExpedientesLegales_ExpedienteLegalID",
                table: "SalidasMortuorio",
                column: "ExpedienteLegalID",
                principalTable: "ExpedientesLegales",
                principalColumn: "ExpedienteLegalID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_ActasRetiro_ActaRetiroID",
                table: "SalidasMortuorio");

            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_ExpedientesLegales_ExpedienteLegalID",
                table: "SalidasMortuorio");

            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.DropTable(
                name: "ActasRetiro");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteLegalID",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ActaRetiroID",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ExpedienteLegalID",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<string>(
                name: "TipoSalida",
                table: "SalidasMortuorio",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "PagoRealizado",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IncidenteRegistrado",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHoraSalida",
                table: "SalidasMortuorio",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "DocumentacionVerificada",
                table: "SalidasMortuorio",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
