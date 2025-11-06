using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Fase4_2_Entidades_Mortuorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BandejaID",
                table: "OcupacionesBandejas",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "Accion",
                table: "OcupacionesBandejas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "OcupacionesBandejas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioLiberaID",
                table: "OcupacionesBandejas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bandejas",
                columns: table => new
                {
                    BandejaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpedienteID = table.Column<int>(type: "int", nullable: true),
                    UsuarioAsignaID = table.Column<int>(type: "int", nullable: true),
                    FechaHoraAsignacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioLiberaID = table.Column<int>(type: "int", nullable: true),
                    FechaHoraLiberacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    MotivoEliminacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bandejas", x => x.BandejaID);
                    table.ForeignKey(
                        name: "FK_Bandejas_AspNetUsers_UsuarioAsignaID",
                        column: x => x.UsuarioAsignaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bandejas_AspNetUsers_UsuarioLiberaID",
                        column: x => x.UsuarioLiberaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bandejas_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalidasMortuorio",
                columns: table => new
                {
                    SalidaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    VigilanteID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraSalida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoSalida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResponsableNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResponsableTipoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResponsableNumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResponsableParentesco = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponsableTelefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntidadAutorizante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentacionVerificada = table.Column<bool>(type: "bit", nullable: false),
                    PagoRealizado = table.Column<bool>(type: "bit", nullable: false),
                    NumeroRecibo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NombreFuneraria = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConductorFuneraria = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DNIConductor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IncidenteRegistrado = table.Column<bool>(type: "bit", nullable: false),
                    DetalleIncidente = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalidasMortuorio", x => x.SalidaID);
                    table.ForeignKey(
                        name: "FK_SalidasMortuorio_AspNetUsers_VigilanteID",
                        column: x => x.VigilanteID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalidasMortuorio_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCorreccion",
                columns: table => new
                {
                    SolicitudID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioSolicitaID = table.Column<int>(type: "int", nullable: false),
                    UsuarioResponsableID = table.Column<int>(type: "int", nullable: false),
                    DatosIncorrectos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescripcionProblema = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ObservacionesSolicitud = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resuelta = table.Column<bool>(type: "bit", nullable: false),
                    FechaHoraResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DescripcionResolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ObservacionesResolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BrazaleteReimpreso = table.Column<bool>(type: "bit", nullable: false),
                    FechaHoraReimpresion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificadoSupervisora = table.Column<bool>(type: "bit", nullable: false),
                    FechaHoraNotificacionSupervisora = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificadoJefeGuardia = table.Column<bool>(type: "bit", nullable: false),
                    FechaHoraNotificacionJefeGuardia = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCorreccion", x => x.SolicitudID);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_AspNetUsers_UsuarioResponsableID",
                        column: x => x.UsuarioResponsableID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_AspNetUsers_UsuarioSolicitaID",
                        column: x => x.UsuarioSolicitaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccion_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificacionesMortuorio",
                columns: table => new
                {
                    VerificacionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    VigilanteID = table.Column<int>(type: "int", nullable: false),
                    TecnicoAmbulanciaID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraVerificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aprobada = table.Column<bool>(type: "bit", nullable: false),
                    HCBrazalete = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DNIBrazalete = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCompletoBrazalete = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ServicioBrazalete = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoExpedienteBrazalete = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HCCoincide = table.Column<bool>(type: "bit", nullable: false),
                    DNICoincide = table.Column<bool>(type: "bit", nullable: false),
                    NombreCoincide = table.Column<bool>(type: "bit", nullable: false),
                    ServicioCoincide = table.Column<bool>(type: "bit", nullable: false),
                    CodigoExpedienteCoincide = table.Column<bool>(type: "bit", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacionesMortuorio", x => x.VerificacionID);
                    table.ForeignKey(
                        name: "FK_VerificacionesMortuorio_AspNetUsers_TecnicoAmbulanciaID",
                        column: x => x.TecnicoAmbulanciaID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificacionesMortuorio_AspNetUsers_VigilanteID",
                        column: x => x.VigilanteID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificacionesMortuorio_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacionesBandejas_UsuarioLiberaID",
                table: "OcupacionesBandejas",
                column: "UsuarioLiberaID");

            migrationBuilder.CreateIndex(
                name: "IX_Bandejas_Codigo",
                table: "Bandejas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bandejas_ExpedienteID",
                table: "Bandejas",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Bandejas_UsuarioAsignaID",
                table: "Bandejas",
                column: "UsuarioAsignaID");

            migrationBuilder.CreateIndex(
                name: "IX_Bandejas_UsuarioLiberaID",
                table: "Bandejas",
                column: "UsuarioLiberaID");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ExpedienteID",
                table: "SalidasMortuorio",
                column: "ExpedienteID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_FechaHoraSalida",
                table: "SalidasMortuorio",
                column: "FechaHoraSalida");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_ResponsableNumeroDocumento",
                table: "SalidasMortuorio",
                column: "ResponsableNumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_TipoSalida",
                table: "SalidasMortuorio",
                column: "TipoSalida");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasMortuorio_VigilanteID",
                table: "SalidasMortuorio",
                column: "VigilanteID");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_ExpedienteID",
                table: "SolicitudesCorreccion",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_FechaHoraSolicitud",
                table: "SolicitudesCorreccion",
                column: "FechaHoraSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_Resuelta",
                table: "SolicitudesCorreccion",
                column: "Resuelta");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_Resuelta_FechaHoraSolicitud",
                table: "SolicitudesCorreccion",
                columns: new[] { "Resuelta", "FechaHoraSolicitud" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_UsuarioResponsableID",
                table: "SolicitudesCorreccion",
                column: "UsuarioResponsableID");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccion_UsuarioSolicitaID",
                table: "SolicitudesCorreccion",
                column: "UsuarioSolicitaID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID",
                table: "VerificacionesMortuorio",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_ExpedienteID_Aprobada",
                table: "VerificacionesMortuorio",
                columns: new[] { "ExpedienteID", "Aprobada" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_FechaHoraVerificacion",
                table: "VerificacionesMortuorio",
                column: "FechaHoraVerificacion");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_TecnicoAmbulanciaID",
                table: "VerificacionesMortuorio",
                column: "TecnicoAmbulanciaID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMortuorio_VigilanteID",
                table: "VerificacionesMortuorio",
                column: "VigilanteID");

            migrationBuilder.AddForeignKey(
                name: "FK_OcupacionesBandejas_AspNetUsers_UsuarioLiberaID",
                table: "OcupacionesBandejas",
                column: "UsuarioLiberaID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OcupacionesBandejas_Bandejas_BandejaID",
                table: "OcupacionesBandejas",
                column: "BandejaID",
                principalTable: "Bandejas",
                principalColumn: "BandejaID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OcupacionesBandejas_AspNetUsers_UsuarioLiberaID",
                table: "OcupacionesBandejas");

            migrationBuilder.DropForeignKey(
                name: "FK_OcupacionesBandejas_Bandejas_BandejaID",
                table: "OcupacionesBandejas");

            migrationBuilder.DropTable(
                name: "Bandejas");

            migrationBuilder.DropTable(
                name: "SalidasMortuorio");

            migrationBuilder.DropTable(
                name: "SolicitudesCorreccion");

            migrationBuilder.DropTable(
                name: "VerificacionesMortuorio");

            migrationBuilder.DropIndex(
                name: "IX_OcupacionesBandejas_UsuarioLiberaID",
                table: "OcupacionesBandejas");

            migrationBuilder.DropColumn(
                name: "Accion",
                table: "OcupacionesBandejas");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "OcupacionesBandejas");

            migrationBuilder.DropColumn(
                name: "UsuarioLiberaID",
                table: "OcupacionesBandejas");

            migrationBuilder.AlterColumn<string>(
                name: "BandejaID",
                table: "OcupacionesBandejas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
