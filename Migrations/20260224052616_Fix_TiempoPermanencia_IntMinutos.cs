using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Fix_TiempoPermanencia_IntMinutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiempoPermanencia",
                table: "SalidasMortuorio");

            migrationBuilder.AddColumn<int>(
                name: "TiempoPermanenciaMinutos",
                table: "SalidasMortuorio",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiempoPermanenciaMinutos",
                table: "SalidasMortuorio");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TiempoPermanencia",
                table: "SalidasMortuorio",
                type: "time",
                nullable: true);
        }
    }
}
