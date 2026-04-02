using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePermanenciaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActasRetiro_Expedientes_ExpedienteID1",
                table: "ActasRetiro");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_ExpedienteID1",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "ActasRetiro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "ActasRetiro",
                type: "int",
                nullable: true);

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
        }
    }
}
