using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposFunerariaSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunerariaRUC",
                table: "SalidasMortuorio",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunerariaTelefono",
                table: "SalidasMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "ActasRetiro",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID1",
                table: "SalidasMortuorio",
                column: "ExpedienteID1",
                unique: true,
                filter: "[ExpedienteID1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_ExpedienteID1",
                table: "ActasRetiro",
                column: "ExpedienteID1",
                unique: true,
                filter: "[ExpedienteID1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ActasRetiro_Expedientes_ExpedienteID1",
                table: "ActasRetiro",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID1",
                table: "SalidasMortuorio",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActasRetiro_Expedientes_ExpedienteID1",
                table: "ActasRetiro");

            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_Expedientes_ExpedienteID1",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteID1",
                table: "SalidasMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_ExpedienteID1",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "FunerariaRUC",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "FunerariaTelefono",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "ActasRetiro");
        }
    }
}
