using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Fase5_MantenimientoBandeja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetalleMantenimiento",
                table: "Bandejas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEstimadaFinMantenimiento",
                table: "Bandejas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioMantenimiento",
                table: "Bandejas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoMantenimiento",
                table: "Bandejas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableMantenimiento",
                table: "Bandejas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioRegistraMantenimientoID",
                table: "Bandejas",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExpedienteID",
                table: "BandejaHistoriales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Bandejas_UsuarioRegistraMantenimientoID",
                table: "Bandejas",
                column: "UsuarioRegistraMantenimientoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Bandejas_AspNetUsers_UsuarioRegistraMantenimientoID",
                table: "Bandejas",
                column: "UsuarioRegistraMantenimientoID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bandejas_AspNetUsers_UsuarioRegistraMantenimientoID",
                table: "Bandejas");

            migrationBuilder.DropIndex(
                name: "IX_Bandejas_UsuarioRegistraMantenimientoID",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "DetalleMantenimiento",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "FechaEstimadaFinMantenimiento",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "FechaInicioMantenimiento",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "MotivoMantenimiento",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "ResponsableMantenimiento",
                table: "Bandejas");

            migrationBuilder.DropColumn(
                name: "UsuarioRegistraMantenimientoID",
                table: "Bandejas");

            migrationBuilder.AlterColumn<int>(
                name: "ExpedienteID",
                table: "BandejaHistoriales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
