using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoDocumentoYSistemasExternos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expedientes_DNI",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DNI",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DNI",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "DNI",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "DNI",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumento",
                table: "Expedientes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "AutoridadesExternas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumento",
                table: "AutoridadesExternas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumento",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_NumeroDocumento",
                table: "Expedientes",
                column: "NumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NumeroDocumento",
                table: "AspNetUsers",
                column: "NumeroDocumento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expedientes_NumeroDocumento",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NumeroDocumento",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "Expedientes",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "AutoridadesExternas",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "AspNetUsers",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_DNI",
                table: "Expedientes",
                column: "DNI");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DNI",
                table: "AspNetUsers",
                column: "DNI",
                unique: true);
        }
    }
}
