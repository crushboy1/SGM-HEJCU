using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Rename_VigilanteID_To_RegistradoPorID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_AspNetUsers_VigilanteID",
                table: "SalidasMortuorio");

            migrationBuilder.RenameColumn(
                name: "VigilanteID",
                table: "SalidasMortuorio",
                newName: "RegistradoPorID");

            migrationBuilder.RenameIndex(
                name: "IX_SalidasMortuorio_VigilanteID",
                table: "SalidasMortuorio",
                newName: "IX_SalidasMortuorio_RegistradoPorID");

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_AspNetUsers_RegistradoPorID",
                table: "SalidasMortuorio",
                column: "RegistradoPorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalidasMortuorio_AspNetUsers_RegistradoPorID",
                table: "SalidasMortuorio");

            migrationBuilder.RenameColumn(
                name: "RegistradoPorID",
                table: "SalidasMortuorio",
                newName: "VigilanteID");

            migrationBuilder.RenameIndex(
                name: "IX_SalidasMortuorio_RegistradoPorID",
                table: "SalidasMortuorio",
                newName: "IX_SalidasMortuorio_VigilanteID");

            migrationBuilder.AddForeignKey(
                name: "FK_SalidasMortuorio_AspNetUsers_VigilanteID",
                table: "SalidasMortuorio",
                column: "VigilanteID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
