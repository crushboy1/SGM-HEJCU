using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class QuitarAutoridadPlacaVehiculoDeActaRetiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoridadPlacaVehiculo",
                table: "ActasRetiro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoridadPlacaVehiculo",
                table: "ActasRetiro",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
