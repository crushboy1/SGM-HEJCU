using Microsoft.AspNetCore.Identity;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Seeders.Core;
using SisMortuorio.Data.Seeders.Development;

namespace SisMortuorio.Data.Seeders
{
    /// <summary>
    /// Orquestador maestro de seeders
    /// Ejecuta seeders según el ambiente (Development/Production)
    /// </summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager,
            IWebHostEnvironment environment)
        {
            Console.WriteLine("════════════════════════════════════════════════════");
            Console.WriteLine("🌱 INICIANDO SEEDING DE BASE DE DATOS");
            Console.WriteLine($"   Ambiente: {environment.EnvironmentName}");
            Console.WriteLine("════════════════════════════════════════════════════");

            // ═══════════════════════════════════════════════════════════
            // CORE: Siempre se ejecuta (Development y Production)
            // ═══════════════════════════════════════════════════════════
            await SeedCoreDataAsync(context, userManager, roleManager);

            // ═══════════════════════════════════════════════════════════
            // DEVELOPMENT: Solo en desarrollo (datos de prueba)
            // ═══════════════════════════════════════════════════════════
            if (environment.IsDevelopment())
            {
                await SeedDevelopmentDataAsync(context);
            }

            // ═══════════════════════════════════════════════════════════
            // PRODUCTION: Solo en producción (configuración real)
            // ═══════════════════════════════════════════════════════════
            if (environment.IsProduction())
            {
                await SeedProductionDataAsync(context);  // Ya no es async, pero sigue siendo Task
            }

            Console.WriteLine("════════════════════════════════════════════════════");
            Console.WriteLine("✅ SEEDING COMPLETADO EXITOSAMENTE");
            Console.WriteLine("════════════════════════════════════════════════════");
        }

        private static async Task SeedCoreDataAsync(
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            RoleManager<Rol> roleManager)
        {
            Console.WriteLine();
            Console.WriteLine("📋 SEEDING: Roles , Usuarios y Bandejas (Core)...");
            Console.WriteLine("────────────────────────────────────────────────────");

            await RolSeeder.SeedAsync(roleManager);
            await UsuarioSeeder.SeedAsync(userManager, roleManager);
            await BandejaSeeder.SeedAsync(context);

            Console.WriteLine("────────────────────────────────────────────────────");
            Console.WriteLine("✅ Core data completado");
        }

        private static async Task SeedDevelopmentDataAsync(ApplicationDbContext context)
        {
            Console.WriteLine();
            Console.WriteLine("🧪 SEEDING: Datos de prueba (Development)...");
            Console.WriteLine("────────────────────────────────────────────────────");

            await ExpedienteTestSeeder.SeedAsync(context);

            Console.WriteLine("────────────────────────────────────────────────────");
            Console.WriteLine("✅ Development data completado");
        }

        private static Task SeedProductionDataAsync(ApplicationDbContext context)  //  Sin "async"
        {
            Console.WriteLine();
            Console.WriteLine("🏭 SEEDING: Configuración de producción...");
            Console.WriteLine("────────────────────────────────────────────────────");

            // TODO: Aquí irían seeders de producción
            // Ejemplo: Parámetros del sistema

            Console.WriteLine("⏭️  No hay seeders de producción por ahora");
            Console.WriteLine("────────────────────────────────────────────────────");

            return Task.CompletedTask;
        }
    }
}