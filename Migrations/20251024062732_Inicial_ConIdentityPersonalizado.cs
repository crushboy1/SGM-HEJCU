using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SisMortuorio.Migrations
{
    /// <inheritdoc />
    public partial class Inicial_ConIdentityPersonalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DNI = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RolID = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetRoles_RolID",
                        column: x => x.RolID,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expedientes",
                columns: table => new
                {
                    ExpedienteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoExpediente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoExpediente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DNI = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sexo = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    TipoSeguro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServicioFallecimiento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroCama = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaHoraFallecimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MedicoCertificaNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MedicoCMP = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MedicoRNE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumeroCertificadoSINADEF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CausaMuerte = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstadoActual = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoQR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaGeneracionQR = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCreadorID = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoEliminacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expedientes", x => x.ExpedienteID);
                    table.ForeignKey(
                        name: "FK_Expedientes_AspNetUsers_UsuarioCreadorID",
                        column: x => x.UsuarioCreadorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodiaTransferencias",
                columns: table => new
                {
                    TransferenciaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    UsuarioOrigenID = table.Column<int>(type: "int", nullable: false),
                    UsuarioDestinoID = table.Column<int>(type: "int", nullable: false),
                    FechaHoraTransferencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UbicacionOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UbicacionDestino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodiaTransferencias", x => x.TransferenciaID);
                    table.ForeignKey(
                        name: "FK_CustodiaTransferencias_AspNetUsers_UsuarioDestinoID",
                        column: x => x.UsuarioDestinoID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodiaTransferencias_AspNetUsers_UsuarioOrigenID",
                        column: x => x.UsuarioOrigenID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodiaTransferencias_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pertenencias",
                columns: table => new
                {
                    PertenenciaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteID = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pertenencias", x => x.PertenenciaID);
                    table.ForeignKey(
                        name: "FK_Pertenencias_Expedientes_ExpedienteID",
                        column: x => x.ExpedienteID,
                        principalTable: "Expedientes",
                        principalColumn: "ExpedienteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DNI",
                table: "AspNetUsers",
                column: "DNI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RolID",
                table: "AspNetUsers",
                column: "RolID");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustodiaTransferencias_ExpedienteID",
                table: "CustodiaTransferencias",
                column: "ExpedienteID");

            migrationBuilder.CreateIndex(
                name: "IX_CustodiaTransferencias_UsuarioDestinoID",
                table: "CustodiaTransferencias",
                column: "UsuarioDestinoID");

            migrationBuilder.CreateIndex(
                name: "IX_CustodiaTransferencias_UsuarioOrigenID",
                table: "CustodiaTransferencias",
                column: "UsuarioOrigenID");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_CodigoExpediente",
                table: "Expedientes",
                column: "CodigoExpediente",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_CodigoQR",
                table: "Expedientes",
                column: "CodigoQR",
                unique: true,
                filter: "[CodigoQR] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_DNI",
                table: "Expedientes",
                column: "DNI");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_HC",
                table: "Expedientes",
                column: "HC");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_NumeroCertificadoSINADEF",
                table: "Expedientes",
                column: "NumeroCertificadoSINADEF",
                unique: true,
                filter: "[NumeroCertificadoSINADEF] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_UsuarioCreadorID",
                table: "Expedientes",
                column: "UsuarioCreadorID");

            migrationBuilder.CreateIndex(
                name: "IX_Pertenencias_ExpedienteID",
                table: "Pertenencias",
                column: "ExpedienteID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CustodiaTransferencias");

            migrationBuilder.DropTable(
                name: "Pertenencias");

            migrationBuilder.DropTable(
                name: "Expedientes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AspNetRoles");
        }
    }
}
