using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_Ingreso_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_AutoridadesExternas_Expedientes_ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosLegales_Expedientes_ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_Expedientes_ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_Expedientes_NumeroCertificadoSINADEF",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "NumeroCertificadoSINADEF",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "TipoSeguro",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.RenameColumn(
                name: "RequiereIntervencionLegal",
                table: "Expedientes",
                newName: "EsNN");

            migrationBuilder.AddColumn<bool>(
                name: "CausaViolentaODudosa",
                table: "Expedientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FuenteFinanciamiento",
                table: "Expedientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Expedientes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "NumeroOficioLegal",
                table: "ActasRetiro",
                newName: "NumeroOficioPolicial");
            */
        }
                

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CausaViolentaODudosa",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "FuenteFinanciamiento",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Expedientes");

            migrationBuilder.RenameColumn(
                name: "NumeroOficioPolicial",
                table: "ActasRetiro",
                newName: "NumeroOficioLegal");

            migrationBuilder.RenameColumn(
                name: "EsNN",
                table: "Expedientes",
                newName: "RequiereIntervencionLegal");

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "ExpedientesLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCertificadoSINADEF",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSeguro",
                table: "Expedientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "DocumentosLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "AutoridadesExternas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_ExpedienteID1",
                table: "ExpedientesLegales",
                column: "ExpedienteID1",
                unique: true,
                filter: "[ExpedienteID1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_NumeroCertificadoSINADEF",
                table: "Expedientes",
                column: "NumeroCertificadoSINADEF",
                unique: true,
                filter: "[NumeroCertificadoSINADEF] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_ExpedienteID1",
                table: "DocumentosLegales",
                column: "ExpedienteID1");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_ExpedienteID1",
                table: "AutoridadesExternas",
                column: "ExpedienteID1");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoridadesExternas_Expedientes_ExpedienteID1",
                table: "AutoridadesExternas",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosLegales_Expedientes_ExpedienteID1",
                table: "DocumentosLegales",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_Expedientes_ExpedienteID1",
                table: "ExpedientesLegales",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");
        }
    }
}
