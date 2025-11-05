using Microsoft.AspNetCore.Identity;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Seeders.Core
{
    /// <summary>
    /// Seeder de roles del sistema
    /// Se ejecuta siempre (Development y Production)
    /// </summary>
    public static class RolSeeder
    {
        public static async Task SeedAsync(RoleManager<Rol> roleManager)
        {
            var roles = GetRoles();

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

        private static List<(string Name, string Descripcion)> GetRoles()
        {
            return new()
            {
                ("Administrador", "Acceso total al sistema, gestión de usuarios y configuración"),
                ("JefeGuardia", "Jefe de Guardia - Autoriza excepciones y supervisa operaciones (rol rotativo)"),
                ("EnfermeriaTecnica", "Técnica de Enfermería - Crea expedientes, genera QR, registra pertenencias"),
                ("EnfermeriaLicenciada", "Enfermera Licenciada - Mismas funciones que Técnica + supervisión del servicio"),
                ("SupervisoraEnfermeria", "Supervisora de Enfermería - Ve todos los servicios, coordina operaciones"),
                ("Ambulancia", "Técnico de Ambulancia - Recibe custodia, traslada y asigna bandejas en mortuorio"),
                ("VigilanteSupervisor", "Vigilante Supervisor - Gestiona verificaciones, registra retiros y casos externos"),
                ("VigilanciaMortuorio", "Vigilante de Mortuorio - Registra salidas físicas y escanea QR"),
                ("Admision", "Admisionista - Genera actas de autorización de retiro"),
                ("CuentasPacientes", "Personal de Cuentas Pacientes - Registra y actualiza deudas económicas"),
                ("BancoSangre", "Personal de Banco de Sangre - Registra deudas de sangre y compromisos"),
                ("ServicioSocial", "Trabajador Social - Gestiona exoneraciones y casos especiales")
            };
        }
    }
}