using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDocumentosExpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoActa",
                table: "ActasRetiro",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "DocumentosExpediente",
                columns: table => new
                {
                    DocumentoExpedienteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExtensionArchivo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TamañoBytes = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioSubioID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraSubida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UsuarioVerificoID = table.Column<int>(type: "int", nullable: true),
                    FechaHoraVerificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosExpediente", x => x.DocumentoExpedienteID);
                    table.ForeignKey(
                        name: "FK_DocumentosExpediente_AspNetUsers_UsuarioSubioID",
                        column: x => x.UsuarioSubioID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosExpediente_AspNetUsers_UsuarioVerificoID",
                        column: x => x.UsuarioVerificoID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosExpediente_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_Estado",
                table: "DocumentosExpediente",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_ExpedienteID",
                table: "DocumentosExpediente",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_ExpedienteID_TipoDocumento",
                table: "DocumentosExpediente",
                columns: new[] { "ExpedienteID", "TipoDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_FechaHoraSubida",
                table: "DocumentosExpediente",
                column: "FechaHoraSubida");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_TipoDocumento",
                table: "DocumentosExpediente",
                column: "TipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_UsuarioSubioID",
                table: "DocumentosExpediente",
                column: "UsuarioSubioID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExpediente_UsuarioVerificoID",
                table: "DocumentosExpediente",
                column: "UsuarioVerificoID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosExpediente");

            migrationBuilder.DropColumn(
                name: "EstadoActa",
                table: "ActasRetiro");
        }
    }
}
