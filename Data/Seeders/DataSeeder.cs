using Microsoft.AspNetCore.Identity;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager)
        {
            // 1. Crear Roles
            await SeedRolesAsync(roleManager);

            // 2. Crear Usuarios
            await SeedUsuariosAsync(userManager, roleManager);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<Rol> roleManager)
        {
            var roles = new[]
            {
                new { Name = "Administrador", Descripcion = "Acceso total al sistema, gestión de usuarios y configuración" },
                new { Name = "JefeGuardia", Descripcion = "Jefe de Guardia - Autoriza excepciones y supervisa operaciones" },
                new { Name = "EnfermeriaTecnica", Descripcion = "Técnica de Enfermería - Crea expedientes, genera QR, registra pertenencias" },
                new { Name = "EnfermeriaLicenciada", Descripcion = "Enfermera Licenciada - Mismas funciones que Técnica + supervisión del servicio" },
                new { Name = "SupervisoraEnfermeria", Descripcion = "Supervisora de Enfermería - Ve todos los servicios, coordina operaciones" },
                new { Name = "Ambulancia", Descripcion = "Técnico de Ambulancia - Recibe custodia, traslada y asigna bandejas en mortuorio" },
                new { Name = "VigilanteSupervisor", Descripcion = "Vigilante Supervisor - Gestiona verificaciones, registra retiros y casos externos" },
                new { Name = "VigilanciaMortuorio", Descripcion = "Vigilante de Mortuorio - Registra salidas físicas y escanea QR" },
                new { Name = "Admision", Descripcion = "Admisionista - Genera actas de autorización de retiro" },
                new { Name = "CuentasPacientes", Descripcion = "Personal de Cuentas Pacientes - Registra y actualiza deudas económicas" },
                new { Name = "BancoSangre", Descripcion = "Personal de Banco de Sangre - Registra deudas de sangre y compromisos" },
                new { Name = "ServicioSocial", Descripcion = "Trabajador Social - Gestiona exoneraciones y casos especiales" }
            };

            foreach (var rolData in roles)
            {
                if (!await roleManager.RoleExistsAsync(rolData.Name))
                {
                    var rol = new Rol
                    {
                        Name = rolData.Name,
                        NormalizedName = rolData.Name.ToUpper(),
                        Descripcion = rolData.Descripcion,
                        Activo = true
                    };
                    await roleManager.CreateAsync(rol);
                    Console.WriteLine($"✅ Rol creado: {rolData.Name}");
                }
            }
        }

        private static async Task SeedUsuariosAsync(
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager)
        {
            var usuarios = new[]
{
    new { Username = "admin", Password = "admin123", NumeroDocumento = "12345678", NombreCompleto = "Administrador Sistema", Rol = "Administrador" },
    new { Username = "jguardia", Password = "jefe123", NumeroDocumento = "23456789", NombreCompleto = "Dr. Juan Pérez Rojas", Rol = "JefeGuardia" },
    new { Username = "tec.cirugia", Password = "enf123", NumeroDocumento = "34567890", NombreCompleto = "María González López", Rol = "EnfermeriaTecnica" },
    new { Username = "tec.medicina", Password = "enf123", NumeroDocumento = "34567891", NombreCompleto = "Rosa Martínez Silva", Rol = "EnfermeriaTecnica" },
    new { Username = "tec.uci", Password = "enf123", NumeroDocumento = "34567892", NombreCompleto = "Carmen Rodríguez Torres", Rol = "EnfermeriaTecnica" },
    new { Username = "lic.cirugia", Password = "lic123", NumeroDocumento = "45678900", NombreCompleto = "Lic. Patricia Vega Ramos", Rol = "EnfermeriaLicenciada" },
    new { Username = "lic.medicina", Password = "lic123", NumeroDocumento = "45678901", NombreCompleto = "Lic. Laura Sánchez Cruz", Rol = "EnfermeriaLicenciada" },
    new { Username = "sup.enfermeria", Password = "sup123", NumeroDocumento = "56789000", NombreCompleto = "Lic. Ana Torres Mendoza", Rol = "SupervisoraEnfermeria" },
    new { Username = "ambulancia1", Password = "amb123", NumeroDocumento = "56789012", NombreCompleto = "Carlos Rodríguez Díaz", Rol = "Ambulancia" },
    new { Username = "ambulancia2", Password = "amb123", NumeroDocumento = "56789013", NombreCompleto = "José Fernández Quispe", Rol = "Ambulancia" },
    new { Username = "vig.supervisor", Password = "vig123", NumeroDocumento = "67890123", NombreCompleto = "Luis Martínez Huamán", Rol = "VigilanteSupervisor" },
    new { Username = "vig.mortuorio", Password = "vig123", NumeroDocumento = "67890124", NombreCompleto = "Pedro Castillo Vargas", Rol = "VigilanciaMortuorio" },
    new { Username = "admision1", Password = "adm123", NumeroDocumento = "78901234", NombreCompleto = "Sofía Ramírez Flores", Rol = "Admision" },
    new { Username = "admision2", Password = "adm123", NumeroDocumento = "78901235", NombreCompleto = "Gabriela Herrera Ponce", Rol = "Admision" },
    new { Username = "cuentas1", Password = "cue123", NumeroDocumento = "89012345", NombreCompleto = "Roberto Sánchez León", Rol = "CuentasPacientes" },
    new { Username = "bsangre1", Password = "san123", NumeroDocumento = "90123456", NombreCompleto = "Dra. Elena Flores Morales", Rol = "BancoSangre" },
    new { Username = "ssocial1", Password = "soc123", NumeroDocumento = "01234567", NombreCompleto = "Lic. Miguel Ángel Ruiz", Rol = "ServicioSocial" }
};

            foreach (var userData in usuarios)
            {
                var existingUser = await userManager.FindByNameAsync(userData.Username);
                if (existingUser == null)
                {
                    // Obtener el rol
                    var rol = await roleManager.FindByNameAsync(userData.Rol);
                    if (rol == null)
                    {
                        Console.WriteLine($"❌ Rol no encontrado: {userData.Rol} para usuario {userData.Username}");
                        continue;
                    }

                    var usuario = new Usuario
                    {
                        UserName = userData.Username,
                        NormalizedUserName = userData.Username.ToUpper(),
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
                        // Agregar a rol de Identity también
                        await userManager.AddToRoleAsync(usuario, userData.Rol);
                        Console.WriteLine($"✅ Usuario creado: {userData.Username} - {userData.NombreCompleto}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Error creando usuario {userData.Username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}