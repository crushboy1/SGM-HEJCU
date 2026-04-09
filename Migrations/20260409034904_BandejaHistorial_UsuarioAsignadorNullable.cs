using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class BandejaHistorial_UsuarioAsignadorNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioAsignadorID",
                table: "BandejaHistoriales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioAsignadorID",
                table: "BandejaHistoriales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID");
        }
    }
}
