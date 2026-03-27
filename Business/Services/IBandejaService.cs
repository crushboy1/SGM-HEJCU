using SisMortuorio.Business.DTOs.Bandeja;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de negocio para gestionar la infraestructura de Bandejas del mortuorio.
    /// CAMBIOS v2: IniciarMantenimientoAsync acepta IniciarMantenimientoDTO completo.
    /// </summary>
    public interface IBandejaService
    {
        /// <summary>Obtiene todas las bandejas con su estado actual (mapa visual).</summary>
        Task<List<BandejaDTO>> GetOcupacionDashboardAsync();

        /// <summary>Obtiene una bandeja por ID.</summary>
        Task<BandejaDTO?> GetByIdAsync(int id);

        /// <summary>Obtiene bandejas disponibles (dropdown de asignación).</summary>
        Task<List<BandejaDisponibleDTO>> GetDisponiblesAsync();

        /// <summary>
        /// Asigna un expediente a una bandeja disponible.
        /// Transición: PendienteAsignacionBandeja → EnBandeja.
        /// </summary>
        Task<BandejaDTO> AsignarBandejaAsync(AsignarBandejaDTO dto, int usuarioAsignaId);

        /// <summary>Libera una bandeja (llamado por ISalidaMortuorioService).</summary>
        Task LiberarBandejaAsync(int expedienteId, int usuarioLiberaId);

        /// <summary>Estadísticas de ocupación del mortuorio.</summary>
        Task<EstadisticasBandejaDTO> GetEstadisticasAsync();

        /// <summary>
        /// Pone una bandeja en Mantenimiento con datos completos.
        /// CAMBIOS v2: recibe IniciarMantenimientoDTO en lugar de string.
        /// Roles: Administrador, JefeGuardia, VigilanteSupervisor.
        /// </summary>
        Task<BandejaDTO> IniciarMantenimientoAsync(
            int bandejaId,
            IniciarMantenimientoDTO dto,
            int usuarioId);

        /// <summary>Finaliza el mantenimiento y pone la bandeja Disponible.</summary>
        Task<BandejaDTO> FinalizarMantenimientoAsync(int bandejaId, int usuarioId);

        /// <summary>
        /// Libera manualmente una bandeja ocupada (emergencia/corrección admin).
        /// Roles: Administrador, JefeGuardia, VigilanteSupervisor.
        /// </summary>
        Task<BandejaDTO> LiberarManualmenteAsync(LiberarBandejaManualDTO dto);
    }
}