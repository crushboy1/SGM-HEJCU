using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Data.Seeders.Development
{
    /// <summary>
    /// Seeder de expedientes de prueba
    /// Solo se ejecuta en Development
    /// </summary>
    public static class ExpedienteTestSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Verificar que no existan expedientes
            if (await context.Expedientes.AnyAsync())
            {
                Console.WriteLine("⏭️  Expedientes ya existen, saltando seeding de prueba");
                return;
            }

            Console.WriteLine("🧪 Creando expedientes de prueba...");

            // TODO: Implementar expedientes de prueba cuando sea necesario
            // Por ahora dejamos vacío para no interferir con las pruebas manuales

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Expedientes de prueba listos");
        }
    }
}