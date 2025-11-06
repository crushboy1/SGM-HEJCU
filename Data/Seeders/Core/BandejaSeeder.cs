using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SisMortuorio.Data.Seeders.Core
{
    /// <summary>
    /// Seeder para las 8 bandejas del mortuorio.
    /// Se ejecuta en TODOS los entornos (Development y Production).
    /// Las bandejas son infraestructura básica del sistema.
    /// </summary>
    public static class BandejaSeeder
    {
        /// <summary>
        /// Crea las 8 bandejas del mortuorio si no existen.
        /// Códigos: B-01, B-02, B-03, B-04, B-05, B-06, B-07, B-08
        /// Estado inicial: Disponible
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Verificar si ya existen bandejas
            if (await context.Bandejas.AnyAsync())
            {
                Console.WriteLine("✓ Bandejas ya existen en la base de datos. Omitiendo seeding.");
                return;
            }

            Console.WriteLine("→ Creando 8 bandejas del mortuorio...");

            var bandejas = new List<Bandeja>();

            // Crear las 8 bandejas
            for (int i = 1; i <= 8; i++)
            {
                var codigo = $"B-{i:D2}"; // B-01, B-02, ..., B-08

                bandejas.Add(new Bandeja
                {
                    Codigo = codigo,
                    Estado = EstadoBandeja.Disponible,
                    ExpedienteID = null,
                    UsuarioAsignaID = null,
                    FechaHoraAsignacion = null,
                    UsuarioLiberaID = null,
                    FechaHoraLiberacion = null,
                    Observaciones = $"Bandeja {codigo} del mortuorio - Creada en seeding inicial",
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = null,
                    Eliminado = false,
                    MotivoEliminacion = null
                });
            }

            // Agregar al contexto
            await context.Bandejas.AddRangeAsync(bandejas);
            await context.SaveChangesAsync();

            Console.WriteLine($"✓ {bandejas.Count} bandejas creadas exitosamente:");
            foreach (var bandeja in bandejas)
            {
                Console.WriteLine($"  - {bandeja.Codigo}: {bandeja.Estado}");
            }
        }

        /// <summary>
        /// Método para reinicializar todas las bandejas a estado Disponible.
        /// Útil para entornos de desarrollo cuando se necesita resetear el mortuorio.
        /// </summary>
        public static async Task ResetearBandejasAsync(ApplicationDbContext context)
        {
            Console.WriteLine("→ Reseteando estado de bandejas...");

            var bandejas = await context.Bandejas.ToListAsync();

            foreach (var bandeja in bandejas)
            {
                bandeja.Estado = EstadoBandeja.Disponible;
                bandeja.ExpedienteID = null;
                bandeja.UsuarioAsignaID = null;
                bandeja.FechaHoraAsignacion = null;
                bandeja.UsuarioLiberaID = null;
                bandeja.FechaHoraLiberacion = null;
                bandeja.Observaciones = $"Bandeja {bandeja.Codigo} - Reseteada";
                bandeja.FechaModificacion = DateTime.Now;
            }

            await context.SaveChangesAsync();

            Console.WriteLine($"✓ {bandejas.Count} bandejas reseteadas a estado Disponible");
        }
    }
}