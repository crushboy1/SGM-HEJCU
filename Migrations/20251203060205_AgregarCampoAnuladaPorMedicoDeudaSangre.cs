using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCampoAnuladaPorMedicoDeudaSangre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnidadesAdeudadas",
                table: "DeudasSangre",
                newName: "CantidadUnidades");

            migrationBuilder.AddColumn<bool>(
                name: "AnuladaPorMedico",
                table: "DeudasSangre",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnuladaPorMedico",
                table: "DeudasSangre");

            migrationBuilder.RenameColumn(
                name: "CantidadUnidades",
                table: "DeudasSangre",
                newName: "UnidadesAdeudadas");
        }
    }
}
