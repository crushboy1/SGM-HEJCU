using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Fase6C_ModeloHibridoExpedienteLegal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_JefeGuardiaValidadorID",
                table: "ExpedientesLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioCreadorID",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_FechaLimitePendientes",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_TienePendientes",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "DocumentosCompletos",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "DocumentosPendientes",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "NombreFiscalRegistrado",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "NombreMedicoLegista",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "NombrePoliciaRegistrado",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "TienePendientes",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "ValidadoJefeGuardia",
                table: "ExpedientesLegales");

            migrationBuilder.RenameColumn(
                name: "UsuarioCreadorID",
                table: "ExpedientesLegales",
                newName: "UsuarioRegistroID");

            migrationBuilder.RenameColumn(
                name: "ObservacionesValidacion",
                table: "ExpedientesLegales",
                newName: "ObservacionesJefeGuardia");

            migrationBuilder.RenameColumn(
                name: "JefeGuardiaValidadorID",
                table: "ExpedientesLegales",
                newName: "UsuarioAdmisionID");

            migrationBuilder.RenameColumn(
                name: "FiscaliaOrigen",
                table: "ExpedientesLegales",
                newName: "Fiscalia");

            migrationBuilder.RenameColumn(
                name: "FechaValidacion",
                table: "ExpedientesLegales",
                newName: "FechaValidacionAdmision");

            migrationBuilder.RenameColumn(
                name: "FechaLimitePendientes",
                table: "ExpedientesLegales",
                newName: "FechaAutorizacion");

            migrationBuilder.RenameColumn(
                name: "ComisariaOrigen",
                table: "ExpedientesLegales",
                newName: "Destino");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedientesLegales_UsuarioCreadorID",
                table: "ExpedientesLegales",
                newName: "IX_ExpedientesLegales_UsuarioRegistroID");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedientesLegales_JefeGuardiaValidadorID",
                table: "ExpedientesLegales",
                newName: "IX_ExpedientesLegales_UsuarioAdmisionID");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "ExpedientesLegales",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "AutorizadoJefeGuardia",
                table: "ExpedientesLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Comisaria",
                table: "ExpedientesLegales",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "ExpedientesLegales",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "ExpedientesLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JefeGuardiaID",
                table: "ExpedientesLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOficioPNP",
                table: "ExpedientesLegales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "ExpedientesLegales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesAdmision",
                table: "ExpedientesLegales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioActualizacionID",
                table: "ExpedientesLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ValidadoAdmision",
                table: "ExpedientesLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "Validado",
                table: "DocumentosLegales",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumento",
                table: "DocumentosLegales",
                type: "int",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<bool>(
                name: "Adjuntado",
                table: "DocumentosLegales",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "DocumentosLegales",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumento",
                table: "AutoridadesExternas",
                type: "int",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "TipoAutoridad",
                table: "AutoridadesExternas",
                type: "int",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHoraLlegada",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteID1",
                table: "AutoridadesExternas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpedienteLegalID",
                table: "AutoridadesExternas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_Estado",
                table: "ExpedientesLegales",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_ExpedienteID1",
                table: "ExpedientesLegales",
                column: "ExpedienteID1",
                unique: true,
                filter: "[ExpedienteID1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_FechaCreacion",
                table: "ExpedientesLegales",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_JefeGuardiaID",
                table: "ExpedientesLegales",
                column: "JefeGuardiaID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_UsuarioActualizacionID",
                table: "ExpedientesLegales",
                column: "UsuarioActualizacionID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_ExpedienteID1",
                table: "DocumentosLegales",
                column: "ExpedienteID1");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_FechaAdjunto",
                table: "DocumentosLegales",
                column: "FechaAdjunto");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosLegales_TipoDocumento",
                table: "DocumentosLegales",
                column: "TipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_ExpedienteID1",
                table: "AutoridadesExternas",
                column: "ExpedienteID1");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_ExpedienteLegalID",
                table: "AutoridadesExternas",
                column: "ExpedienteLegalID");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_FechaHoraLlegada",
                table: "AutoridadesExternas",
                column: "FechaHoraLlegada");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_NumeroDocumento",
                table: "AutoridadesExternas",
                column: "NumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_AutoridadesExternas_TipoAutoridad",
                table: "AutoridadesExternas",
                column: "TipoAutoridad");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoridadesExternas_ExpedientesLegales_ExpedienteLegalID",
                table: "AutoridadesExternas",
                column: "ExpedienteLegalID",
                principalTable: "ExpedientesLegales",
                principalColumn: "ExpedienteLegalID",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_ExpedientesLegales_AspNetUsers_JefeGuardiaID",
                table: "ExpedientesLegales",
                column: "JefeGuardiaID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioActualizacionID",
                table: "ExpedientesLegales",
                column: "UsuarioActualizacionID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioAdmisionID",
                table: "ExpedientesLegales",
                column: "UsuarioAdmisionID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioRegistroID",
                table: "ExpedientesLegales",
                column: "UsuarioRegistroID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_Expedientes_ExpedienteID1",
                table: "ExpedientesLegales",
                column: "ExpedienteID1",
                principalTable: "Expedientes",
                principalColumn: "ExpedienteID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoridadesExternas_ExpedientesLegales_ExpedienteLegalID",
                table: "AutoridadesExternas");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoridadesExternas_Expedientes_ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosLegales_Expedientes_ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_JefeGuardiaID",
                table: "ExpedientesLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioActualizacionID",
                table: "ExpedientesLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioAdmisionID",
                table: "ExpedientesLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioRegistroID",
                table: "ExpedientesLegales");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesLegales_Expedientes_ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_Estado",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_FechaCreacion",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_JefeGuardiaID",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesLegales_UsuarioActualizacionID",
                table: "ExpedientesLegales");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_FechaAdjunto",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosLegales_TipoDocumento",
                table: "DocumentosLegales");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_ExpedienteLegalID",
                table: "AutoridadesExternas");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_FechaHoraLlegada",
                table: "AutoridadesExternas");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_NumeroDocumento",
                table: "AutoridadesExternas");

            migrationBuilder.DropIndex(
                name: "IX_AutoridadesExternas_TipoAutoridad",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "AutorizadoJefeGuardia",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "Comisaria",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "JefeGuardiaID",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "NumeroOficioPNP",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "ObservacionesAdmision",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "UsuarioActualizacionID",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "ValidadoAdmision",
                table: "ExpedientesLegales");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "DocumentosLegales");

            migrationBuilder.DropColumn(
                name: "ExpedienteID1",
                table: "AutoridadesExternas");

            migrationBuilder.DropColumn(
                name: "ExpedienteLegalID",
                table: "AutoridadesExternas");

            migrationBuilder.RenameColumn(
                name: "UsuarioRegistroID",
                table: "ExpedientesLegales",
                newName: "UsuarioCreadorID");

            migrationBuilder.RenameColumn(
                name: "UsuarioAdmisionID",
                table: "ExpedientesLegales",
                newName: "JefeGuardiaValidadorID");

            migrationBuilder.RenameColumn(
                name: "ObservacionesJefeGuardia",
                table: "ExpedientesLegales",
                newName: "ObservacionesValidacion");

            migrationBuilder.RenameColumn(
                name: "Fiscalia",
                table: "ExpedientesLegales",
                newName: "FiscaliaOrigen");

            migrationBuilder.RenameColumn(
                name: "FechaValidacionAdmision",
                table: "ExpedientesLegales",
                newName: "FechaValidacion");

            migrationBuilder.RenameColumn(
                name: "FechaAutorizacion",
                table: "ExpedientesLegales",
                newName: "FechaLimitePendientes");

            migrationBuilder.RenameColumn(
                name: "Destino",
                table: "ExpedientesLegales",
                newName: "ComisariaOrigen");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedientesLegales_UsuarioRegistroID",
                table: "ExpedientesLegales",
                newName: "IX_ExpedientesLegales_UsuarioCreadorID");

            migrationBuilder.RenameIndex(
                name: "IX_ExpedientesLegales_UsuarioAdmisionID",
                table: "ExpedientesLegales",
                newName: "IX_ExpedientesLegales_JefeGuardiaValidadorID");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "ExpedientesLegales",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "DocumentosCompletos",
                table: "ExpedientesLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DocumentosPendientes",
                table: "ExpedientesLegales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreFiscalRegistrado",
                table: "ExpedientesLegales",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreMedicoLegista",
                table: "ExpedientesLegales",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombrePoliciaRegistrado",
                table: "ExpedientesLegales",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TienePendientes",
                table: "ExpedientesLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ValidadoJefeGuardia",
                table: "ExpedientesLegales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "Validado",
                table: "DocumentosLegales",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "DocumentosLegales",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<bool>(
                name: "Adjuntado",
                table: "DocumentosLegales",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "AutoridadesExternas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "TipoAutoridad",
                table: "AutoridadesExternas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHoraLlegada",
                table: "AutoridadesExternas",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_FechaLimitePendientes",
                table: "ExpedientesLegales",
                column: "FechaLimitePendientes");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesLegales_TienePendientes",
                table: "ExpedientesLegales",
                column: "TienePendientes");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_JefeGuardiaValidadorID",
                table: "ExpedientesLegales",
                column: "JefeGuardiaValidadorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesLegales_AspNetUsers_UsuarioCreadorID",
                table: "ExpedientesLegales",
                column: "UsuarioCreadorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
