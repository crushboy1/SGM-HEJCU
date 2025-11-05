using Microsoft.AspNetCore.Identity;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Seeders.Core
{
    /// <summary>
    /// Seeder de usuarios del sistema
    /// Se ejecuta siempre (Development y Production)
    /// </summary>
    public static class UsuarioSeeder
    {
        public static async Task SeedAsync(
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager)
        {
            var usuarios = GetUsuarios();

            foreach (var userData in usuarios)
            {
                await CreateUsuarioIfNotExistsAsync(userManager, roleManager, userData);
            }
        }

        private static async Task CreateUsuarioIfNotExistsAsync(
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager,
            UsuarioSeedData userData)
        {
            var existingUser = await userManager.FindByNameAsync(userData.Username);
            if (existingUser != null) return;

            var rol = await roleManager.FindByNameAsync(userData.Rol);
            if (rol == null)
            {
                Console.WriteLine($"❌ Rol no encontrado: {userData.Rol} para usuario {userData.Username}");
                return;
            }

            var usuario = new Usuario
            {
                UserName = userData.Username,
                NormalizedUserName = userData.Username.ToUpper(),
                Email = userData.Email,
                NormalizedEmail = userData.Email.ToUpper(),
                EmailConfirmed = true,
                TipoDocumento = TipoDocumentoIdentidad.DNI,
                NumeroDocumento = userData.NumeroDocumento,
                NombreCompleto = userData.NombreCompleto,
                Activo = true,
                FechaCreacion = DateTime.Now,
                RolID = rol.Id,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(usuario, userData.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(usuario, userData.Rol);
                Console.WriteLine($"✅ Usuario creado: {userData.Username} - {userData.NombreCompleto} ({userData.Email})");
            }
            else
            {
                Console.WriteLine($"❌ Error creando usuario {userData.Username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private static List<UsuarioSeedData> GetUsuarios()
        {
            return new()
            {
                // ═══════════════════════════════════════════════════════════
                // ADMINISTRACIÓN
                // ═══════════════════════════════════════════════════════════
                new("admin", "admin123", "12345678",
                    "Administrador Sistema",
                    "admin@hejcu.gob.pe",
                    "Administrador"),

                // ═══════════════════════════════════════════════════════════
                // JEFE DE GUARDIA (ROL ROTATIVO)
                // ═══════════════════════════════════════════════════════════
                // TODO IMPORTANTE: Este usuario representa un ROL ROTATIVO
                // En el futuro (Fase 6-7) implementar:
                // 1. LoginJefeGuardiaDTO con campos: NombreMedicoTurno, CMPMedicoTurno
                // 2. Claims adicionales en JWT: "NombreMedicoTurno", "CMPMedicoTurno"
                // 3. Validación en AuthService que requiera estos datos
                // 4. Logs de auditoría que incluyan el médico real
                // Alternativa futura: Tabla MedicoGuardia + TurnoGuardia
                // ═══════════════════════════════════════════════════════════
                new("jguardia", "jefe123", "99999999",
                    "Jefe de Guardia",
                    "jefe.guardia@hejcu.gob.pe",
                    "JefeGuardia"),

                // ═══════════════════════════════════════════════════════════
                // ENFERMERÍA TÉCNICA
                // ═══════════════════════════════════════════════════════════
                new("tec.cirugia", "enf123", "34567890",
                    "María González López",
                    "tecnica.cirugia@hejcu.gob.pe",
                    "EnfermeriaTecnica"),

                new("tec.medicina", "enf123", "34567891",
                    "Rosa Martínez Silva",
                    "tecnica.medicina@hejcu.gob.pe",
                    "EnfermeriaTecnica"),

                new("tec.uci", "enf123", "34567892",
                    "Carmen Rodríguez Torres",
                    "tecnica.uci@hejcu.gob.pe",
                    "EnfermeriaTecnica"),

                // ═══════════════════════════════════════════════════════════
                // ENFERMERÍA LICENCIADA
                // ═══════════════════════════════════════════════════════════
                new("lic.cirugia", "lic123", "45678900",
                    "Lic. Patricia Vega Ramos",
                    "licenciada.cirugia@hejcu.gob.pe",
                    "EnfermeriaLicenciada"),

                new("lic.medicina", "lic123", "45678901",
                    "Lic. Laura Sánchez Cruz",
                    "licenciada.medicina@hejcu.gob.pe",
                    "EnfermeriaLicenciada"),

                // ═══════════════════════════════════════════════════════════
                // SUPERVISIÓN DE ENFERMERÍA
                // ═══════════════════════════════════════════════════════════
                new("sup.enfermeria", "sup123", "56789000",
                    "Lic. Ana Torres Mendoza",
                    "supervisora.enfermeria@hejcu.gob.pe",
                    "SupervisoraEnfermeria"),

                // ═══════════════════════════════════════════════════════════
                // AMBULANCIA
                // ═══════════════════════════════════════════════════════════
                new("ambulancia1", "amb123", "56789012",
                    "Carlos Rodríguez Díaz",
                    "ambulancia1@hejcu.gob.pe",
                    "Ambulancia"),

                new("ambulancia2", "amb123", "56789013",
                    "José Fernández Quispe",
                    "ambulancia2@hejcu.gob.pe",
                    "Ambulancia"),

                // ═══════════════════════════════════════════════════════════
                // VIGILANCIA
                // ═══════════════════════════════════════════════════════════
                new("vig.supervisor", "vig123", "67890123",
                    "Luis Martínez Huamán",
                    "vigilante.supervisor@hejcu.gob.pe",
                    "VigilanteSupervisor"),

                new("vig.mortuorio", "vig123", "67890124",
                    "Pedro Castillo Vargas",
                    "vigilante.mortuorio@hejcu.gob.pe",
                    "VigilanciaMortuorio"),

                // ═══════════════════════════════════════════════════════════
                // ADMISIÓN
                // ═══════════════════════════════════════════════════════════
                new("admision1", "adm123", "78901234",
                    "Sofía Ramírez Flores",
                    "admision1@hejcu.gob.pe",
                    "Admision"),

                new("admision2", "adm123", "78901235",
                    "Gabriela Herrera Ponce",
                    "admision2@hejcu.gob.pe",
                    "Admision"),

                // ═══════════════════════════════════════════════════════════
                // OTROS SERVICIOS
                // ═══════════════════════════════════════════════════════════
                new("cuentas1", "cue123", "89012345",
                    "Roberto Sánchez León",
                    "cuentas.pacientes@hejcu.gob.pe",
                    "CuentasPacientes"),

                new("bsangre1", "san123", "90123456",
                    "Dra. Elena Flores Morales",
                    "banco.sangre@hejcu.gob.pe",
                    "BancoSangre"),

                new("ssocial1", "soc123", "01234567",
                    "Lic. Miguel Ángel Ruiz",
                    "servicio.social@hejcu.gob.pe",
                    "ServicioSocial")
            };
        }

        /// <summary>
        /// Record para encapsular datos de usuario del seeder
        /// </summary>
        private record UsuarioSeedData(
            string Username,
            string Password,
            string NumeroDocumento,
            string NombreCompleto,
            string Email,
            string Rol);
    }
}