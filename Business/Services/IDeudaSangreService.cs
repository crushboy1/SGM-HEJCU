using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestionar deudas de sangre de expedientes.
    /// Permite registrar, liquidar y anular deudas de sangre del Banco de Sangre.
    /// </summary>
    public interface IDeudaSangreService
    {
        /// <summary>
        /// Registra una nueva deuda de sangre para un expediente.
        /// Solo puede haber una deuda por expediente.
        /// </summary>
        /// <param name="dto">Datos de la deuda a registrar</param>
        /// <returns>DeudaSangreDTO creada con su ID</returns>
        Task<DeudaSangreDTO> RegistrarDeudaAsync(CreateDeudaSangreDTO dto);

        /// <summary>
        /// Obtiene la deuda de sangre de un expediente específico.
        /// Devuelve null si no existe deuda.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>DeudaSangreDTO o null</returns>
        Task<DeudaSangreDTO?> ObtenerPorExpedienteAsync(int expedienteId);

        /// <summary>
        /// Marca una deuda como "Sin Deuda" (no tiene paquetes pendientes).
        /// Usado cuando Banco de Sangre confirma que no hay deuda.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="usuarioId">ID del usuario que marca (BancoSangre)</param>
        /// <returns>DeudaSangreDTO actualizada</returns>
        Task<DeudaSangreDTO> MarcarSinDeudaAsync(int expedienteId, int usuarioId);

        /// <summary>
        /// Marca una deuda como "Liquidada" con firma del familiar.
        /// Requiere adjuntar PDF del compromiso firmado.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="dto">Datos de liquidación con info del familiar</param>
        /// <returns>DeudaSangreDTO actualizada</returns>
        Task<DeudaSangreDTO> MarcarLiquidadaAsync(int expedienteId, LiquidarDeudaSangreDTO dto);

        /// <summary>
        /// Anula una deuda por decisión médica (excepción).
        /// Solo puede ser ejecutado por rol BancoSangre (médico).
        /// Genera registro auditable de la anulación.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="dto">Datos de anulación con justificación médica</param>
        /// <returns>DeudaSangreDTO actualizada</returns>
        Task<DeudaSangreDTO> AnularDeudaAsync(int expedienteId, AnularDeudaSangreDTO dto);

        /// <summary>
        /// Verifica si una deuda bloquea el retiro del cuerpo.
        /// Bloquea si: Estado == Pendiente
        /// No bloquea si: SinDeuda, Liquidado, Anulado
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>true si bloquea el retiro, false si no</returns>
        Task<bool> BloqueaRetiroAsync(int expedienteId);

        /// <summary>
        /// Obtiene el semáforo visual para Banco de Sangre.
        /// Formato: " PENDIENTE (5 unidades)" o " SIN DEUDA"
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>String con icono y descripción</returns>
        Task<string> ObtenerSemaforoAsync(int expedienteId);

        /// <summary>
        /// Obtiene todas las deudas pendientes (para reportes).
        /// Usado por Banco de Sangre para ver panorama general.
        /// </summary>
        /// <returns>Lista de DeudaSangreDTO pendientes</returns>
        Task<List<DeudaSangreDTO>> ObtenerDeudasPendientesAsync();

        /// <summary>
        /// Obtiene el historial completo de una deuda (auditoría).
        /// Incluye: registro inicial, liquidaciones, anulaciones.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>Lista de eventos históricos</returns>
        Task<List<HistorialDeudaSangreDTO>> ObtenerHistorialAsync(int expedienteId);
    }
}