using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarCausaMuerteADiagnosticoFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CausaMuerte",
                table: "Expedientes",
                newName: "DiagnosticoFinal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiagnosticoFinal",
                table: "Expedientes",
                newName: "CausaMuerte");
        }
    }
}
