using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestión de deudas económicas.
    /// Maneja persistencia y consultas relacionadas con Caja y Servicio Social.
    /// </summary>
    public class DeudaEconomicaRepository : IDeudaEconomicaRepository
    {
        private readonly ApplicationDbContext _context;

        public DeudaEconomicaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la deuda económica de un expediente específico.
        /// Incluye relaciones: Expediente, Usuarios de auditoría.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>Deuda económica o null si no existe</returns>
        public async Task<DeudaEconomica?> GetByExpedienteIdAsync(int expedienteId)
        {
            return await _context.DeudasEconomicas
                .Include(d => d.Expediente)
                .Include(d => d.UsuarioRegistro)
                .Include(d => d.UsuarioActualizacion)
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);
        }

        /// <summary>
        /// Obtiene todas las deudas económicas pendientes.
        /// Para pantalla "Input" de Cuentas Pacientes / Servicio Social.
        /// </summary>
        /// <returns>Lista de deudas con estado Pendiente</returns>
        public async Task<List<DeudaEconomica>> GetPendientesAsync()
        {
            return await _context.DeudasEconomicas
                .Include(d => d.Expediente)
                .Include(d => d.UsuarioRegistro)
                .Where(d => d.Estado == EstadoDeudaEconomica.Pendiente)
                .OrderByDescending(d => d.FechaRegistro)
                .ToListAsync();
        }

        /// <summary>
        /// Crea un nuevo registro de deuda económica.
        /// </summary>
        /// <param name="deuda">Entidad a crear</param>
        /// <returns>Entidad creada con ID asignado</returns>
        public async Task<DeudaEconomica> CreateAsync(DeudaEconomica deuda)
        {
            if (deuda == null)
                throw new ArgumentNullException(nameof(deuda));

            // Validar que no exista ya una deuda para este expediente
            var existe = await _context.DeudasEconomicas
                .AnyAsync(d => d.ExpedienteID == deuda.ExpedienteID);

            if (existe)
                throw new InvalidOperationException($"Ya existe una deuda económica para el expediente {deuda.ExpedienteID}");

            _context.DeudasEconomicas.Add(deuda);
            await _context.SaveChangesAsync();

            return deuda;
        }

        /// <summary>
        /// Actualiza una deuda económica existente.
        /// </summary>
        /// <param name="deuda">Entidad con cambios</param>
        public async Task UpdateAsync(DeudaEconomica deuda)
        {
            if (deuda == null)
                throw new ArgumentNullException(nameof(deuda));

            _context.DeudasEconomicas.Update(deuda);
            await _context.SaveChangesAsync();
        }
    }
}