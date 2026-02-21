using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class ActaRetiroUnificadaFamiliarYAutoridadLegal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirmadoFamiliar",
                table: "ActasRetiro",
                newName: "FirmadoResponsable");

            migrationBuilder.RenameColumn(
                name: "FechaFirmaFamiliar",
                table: "ActasRetiro",
                newName: "FechaFirmaResponsable");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCertificadoDefuncion",
                table: "ActasRetiro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompletoFallecido",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<int>(
                name: "FamiliarTipoDocumento",
                table: "ActasRetiro",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarParentesco",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarNumeroDocumento",
                table: "ActasRetiro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarNombreCompleto",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadApellidoMaterno",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadApellidoPaterno",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadCargo",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadInstitucion",
                table: "ActasRetiro",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadNombreCompleto",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadNombres",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadNumeroDocumento",
                table: "ActasRetiro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadPlacaVehiculo",
                table: "ActasRetiro",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoridadTelefono",
                table: "ActasRetiro",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutoridadTipoDocumento",
                table: "ActasRetiro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamiliarApellidoMaterno",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamiliarApellidoPaterno",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamiliarNombres",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOficioLegal",
                table: "ActasRetiro",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoAutoridad",
                table: "ActasRetiro",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_AutoridadNumeroDocumento",
                table: "ActasRetiro",
                column: "AutoridadNumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_NumeroOficioLegal",
                table: "ActasRetiro",
                column: "NumeroOficioLegal");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_TipoAutoridad",
                table: "ActasRetiro",
                column: "TipoAutoridad");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_TipoSalida_FechaRegistro",
                table: "ActasRetiro",
                columns: new[] { "TipoSalida", "FechaRegistro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_AutoridadNumeroDocumento",
                table: "ActasRetiro");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_NumeroOficioLegal",
                table: "ActasRetiro");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_TipoAutoridad",
                table: "ActasRetiro");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_TipoSalida_FechaRegistro",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadApellidoMaterno",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadApellidoPaterno",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadCargo",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadInstitucion",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadNombreCompleto",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadNombres",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadNumeroDocumento",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadPlacaVehiculo",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadTelefono",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "AutoridadTipoDocumento",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "FamiliarApellidoMaterno",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "FamiliarApellidoPaterno",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "FamiliarNombres",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "NumeroOficioLegal",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "TipoAutoridad",
                table: "ActasRetiro");

            migrationBuilder.RenameColumn(
                name: "FirmadoResponsable",
                table: "ActasRetiro",
                newName: "FirmadoFamiliar");

            migrationBuilder.RenameColumn(
                name: "FechaFirmaResponsable",
                table: "ActasRetiro",
                newName: "FechaFirmaFamiliar");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCertificadoDefuncion",
                table: "ActasRetiro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompletoFallecido",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FamiliarTipoDocumento",
                table: "ActasRetiro",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarParentesco",
                table: "ActasRetiro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarNumeroDocumento",
                table: "ActasRetiro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FamiliarNombreCompleto",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
