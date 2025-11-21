using SisMortuorio.Business.DTOs.Bandeja;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de negocio para gestionar la infraestructura de Bandejas del mortuorio.
    /// Maneja el estado, asignación, liberación y consulta del mapa visual.
    /// </summary>
    public interface IBandejaService
    {
        /// <summary>
        /// Obtiene la lista completa de bandejas con su estado actual de ocupación.
        /// (Usado para el mapa visual del mortuorio).
        /// </summary>
        Task<List<BandejaDTO>> GetOcupacionDashboardAsync();

        /// <summary>
        /// Obtiene una bandeja específica por su ID.
        /// (Necesario para la pantalla de asignación).
        /// </summary>
        Task<BandejaDTO?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene una lista simplificada de bandejas disponibles.
        /// (Usado para el dropdown de asignación).
        /// </summary>
        Task<List<BandejaDisponibleDTO>> GetDisponiblesAsync();

        /// <summary>
        /// Asigna un expediente a una bandeja disponible.
        /// Transición: PendienteAsignacionBandeja -> EnBandeja
        /// </summary>
        Task<BandejaDTO> AsignarBandejaAsync(AsignarBandejaDTO dto, int usuarioAsignaId);

        /// <summary>
        /// Libera una bandeja que estaba ocupada por un expediente.
        /// (Llamado por ISalidaMortuorioService).
        /// </summary>
        Task LiberarBandejaAsync(int expedienteId, int usuarioLiberaId);

        /// <summary>
        /// Obtiene las estadísticas de ocupación del mortuorio.
        /// </summary>
        Task<EstadisticasBandejaDTO> GetEstadisticasAsync();

        /// <summary>
        /// Pone una bandeja en estado de Mantenimiento.
        /// </summary>
        Task<BandejaDTO> IniciarMantenimientoAsync(int bandejaId, string observaciones, int usuarioId);

        /// <summary>
        /// Saca una bandeja de Mantenimiento y la pone Disponible.
        /// </summary>
        Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId);
    }
}