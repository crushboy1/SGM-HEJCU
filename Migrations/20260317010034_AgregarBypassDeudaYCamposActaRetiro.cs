using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBypassDeudaYCamposActaRetiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BypassDeudaAutorizado",
                table: "Expedientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BypassDeudaFecha",
                table: "Expedientes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BypassDeudaJustificacion",
                table: "Expedientes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BypassDeudaUsuarioID",
                table: "Expedientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BypassDeudaAutorizado",
                table: "ActasRetiro",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BypassDeudaFecha",
                table: "ActasRetiro",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BypassDeudaJustificacion",
                table: "ActasRetiro",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BypassDeudaUsuarioID",
                table: "ActasRetiro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoExternoCMP",
                table: "ActasRetiro",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoExternoNombre",
                table: "ActasRetiro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_BypassDeudaUsuarioID",
                table: "Expedientes",
                column: "BypassDeudaUsuarioID");

            migrationBuilder.CreateIndex(
                name: "IX_ActasRetiro_BypassDeudaUsuarioID",
                table: "ActasRetiro",
                column: "BypassDeudaUsuarioID");

            migrationBuilder.AddForeignKey(
                name: "FK_ActasRetiro_AspNetUsers_BypassDeudaUsuarioID",
                table: "ActasRetiro",
                column: "BypassDeudaUsuarioID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expedientes_AspNetUsers_BypassDeudaUsuarioID",
                table: "Expedientes",
                column: "BypassDeudaUsuarioID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActasRetiro_AspNetUsers_BypassDeudaUsuarioID",
                table: "ActasRetiro");

            migrationBuilder.DropForeignKey(
                name: "FK_Expedientes_AspNetUsers_BypassDeudaUsuarioID",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_Expedientes_BypassDeudaUsuarioID",
                table: "Expedientes");

            migrationBuilder.DropIndex(
                name: "IX_ActasRetiro_BypassDeudaUsuarioID",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "BypassDeudaAutorizado",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "BypassDeudaFecha",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "BypassDeudaJustificacion",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "BypassDeudaUsuarioID",
                table: "Expedientes");

            migrationBuilder.DropColumn(
                name: "BypassDeudaAutorizado",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "BypassDeudaFecha",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "BypassDeudaJustificacion",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "BypassDeudaUsuarioID",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "MedicoExternoCMP",
                table: "ActasRetiro");

            migrationBuilder.DropColumn(
                name: "MedicoExternoNombre",
                table: "ActasRetiro");
        }
    }
}
