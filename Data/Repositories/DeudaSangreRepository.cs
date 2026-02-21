using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories
{
    /// <summary>
    /// Repositorio para gestión de deudas de sangre.
    /// Maneja persistencia y consultas relacionadas con banco de sangre.
    /// </summary>
    public class DeudaSangreRepository : IDeudaSangreRepository
    {
        private readonly ApplicationDbContext _context;

        public DeudaSangreRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la deuda de sangre de un expediente específico.
        /// Incluye relaciones: Expediente, Usuario que registró, Médico que anuló.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>Deuda de sangre o null si no existe</returns>
        public async Task<DeudaSangre?> GetByExpedienteIdAsync(int expedienteId)
        {
            return await _context.DeudasSangre
                .Include(d => d.Expediente)
                .Include(d => d.UsuarioRegistro)
                .Include(d => d.MedicoAnula)
                .Include(d => d.UsuarioActualizacion)
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);
        }

        /// <summary>
        /// Obtiene todas las deudas de sangre pendientes.
        /// Para pantalla "Input" de Banco de Sangre.
        /// </summary>
        /// <returns>Lista de deudas con estado Pendiente</returns>
        public async Task<List<DeudaSangre>> GetPendientesAsync()
        {
            return await _context.DeudasSangre
                .Include(d => d.Expediente)
                .Include(d => d.UsuarioRegistro)
                .Where(d => d.Estado == EstadoDeudaSangre.Pendiente)
                .OrderByDescending(d => d.FechaRegistro)
                .ToListAsync();
        }

        /// <summary>
        /// Crea un nuevo registro de deuda de sangre.
        /// </summary>
        /// <param name="deuda">Entidad a crear</param>
        /// <returns>Entidad creada con ID asignado</returns>
        public async Task<DeudaSangre> CreateAsync(DeudaSangre deuda)
        {
            if (deuda == null)
                throw new ArgumentNullException(nameof(deuda));

            // Validar que no exista ya una deuda para este expediente
            var existe = await _context.DeudasSangre
                .AnyAsync(d => d.ExpedienteID == deuda.ExpedienteID);

            if (existe)
                throw new InvalidOperationException($"Ya existe una deuda de sangre para el expediente {deuda.ExpedienteID}");

            _context.DeudasSangre.Add(deuda);
            await _context.SaveChangesAsync();

            return deuda;
        }

        /// <summary>
        /// Actualiza una deuda de sangre existente.
        /// </summary>
        /// <param name="deuda">Entidad con cambios</param>
        public async Task UpdateAsync(DeudaSangre deuda)
        {
            if (deuda == null)
                throw new ArgumentNullException(nameof(deuda));

            _context.DeudasSangre.Update(deuda);
            await _context.SaveChangesAsync();
        }
    }
}