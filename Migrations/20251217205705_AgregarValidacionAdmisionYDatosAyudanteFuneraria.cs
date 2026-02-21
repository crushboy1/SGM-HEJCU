using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarValidacionAdmisionYDatosAyudanteFuneraria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AyudanteFuneraria",
                table: "SalidasMortuorio",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DNIAyudante",
                table: "SalidasMortuorio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentacionCompleta",
                table: "Expedientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacionAdmision",
                table: "Expedientes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioAdmisionID",
                table: "Expedientes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_UsuarioAdmisionID",
                table: "Expedientes",
                column: "UsuarioAdmisionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_AspNetUsers_UsuarioAdmisionID",
                table: "Expedientes",
                column: "UsuarioAdmisionID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_AspNetUsers_UsuarioAdmisionID",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_Expedientes_UsuarioAdmisionID",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "AyudanteFuneraria",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "DNIAyudante",
                table: "SalidasMortuorio");

            migrationBuilder.DropColumn(
                name: "DocumentacionCompleta",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "FechaValidacionAdmision",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "UsuarioAdmisionID",
                table: "Expedientes");
        }
    }
}
