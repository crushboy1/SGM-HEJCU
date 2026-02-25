using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCamposDuplicadosSalidaMortuorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ResponsableNumeroDocumento",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_TipoSalida",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "NumeroOficio",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ResponsableNombre",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ResponsableNumeroDocumento",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ResponsableParentesco",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ResponsableTelefono",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ResponsableTipoDocumento",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "TipoSalida",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<int>(
                name: "ActaRetiroID",
                table: "SalidasMortuorio",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio",
                column: "ActaRetiroID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<int>(
                name: "ActaRetiroID",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "NumeroOficio",
                table: "SalidasMortuorio",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableNombre",
                table: "SalidasMortuorio",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableNumeroDocumento",
                table: "SalidasMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableParentesco",
                table: "SalidasMortuorio",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableTelefono",
                table: "SalidasMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableTipoDocumento",
                table: "SalidasMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoSalida",
                table: "SalidasMortuorio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ActaRetiroID",
                table: "SalidasMortuorio",
                column: "ActaRetiroID",
                unique: true,
                filter: "[ActaRetiroID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ResponsableNumeroDocumento",
                table: "SalidasMortuorio",
                column: "ResponsableNumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_TipoSalida",
                table: "SalidasMortuorio",
                column: "TipoSalida");
        }
    }
}
