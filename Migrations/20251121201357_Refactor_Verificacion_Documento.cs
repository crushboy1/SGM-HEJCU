using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_Verificacion_Documento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DNICoincide",
                table: "VerificacionesMortuorio",
                newName: "DocumentoCoincide");

            migrationBuilder.RenameColumn(
                name: "DNIBrazalete",
                table: "VerificacionesMortuorio",
                newName: "TipoDocumentoBrazalete");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoBrazalete",
                table: "VerificacionesMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroDocumentoBrazalete",
                table: "VerificacionesMortuorio");

            migrationBuilder.RenameColumn(
                name: "TipoDocumentoBrazalete",
                table: "VerificacionesMortuorio",
                newName: "DNIBrazalete");

            migrationBuilder.RenameColumn(
                name: "DocumentoCoincide",
                table: "VerificacionesMortuorio",
                newName: "DNICoincide");
        }
    }
}
