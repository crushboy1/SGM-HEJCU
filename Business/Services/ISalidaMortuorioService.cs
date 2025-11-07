using SisMortuorio.Business.DTOs.Salida;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de negocio para gestionar el flujo de Salida
    /// física del cuerpo del mortuorio.
    /// </summary>
    public interface ISalidaMortuorioService
    {
        /// <summary>
        /// Registra la salida física de un cuerpo del mortuorio.
        /// 1. Valida que el estado sea 'PendienteRetiro'.
        /// 2. Crea el registro en la tabla SalidaMortuorio.
        /// 3. Dispara el trigger 'RegistrarSalida' (estado -> 'Retirado').
        /// 4. Llama a IBandejaService.LiberarBandejaAsync.
        /// </summary>
        /// <param name="dto">Datos de la salida (responsable, funeraria, etc.)</param>
        /// <param name="vigilanteId">ID del Vigilante (del Token)</param>
        /// <returns>Un DTO con el resumen de la salida registrada</returns>
        Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int vigilanteId);

        /// <summary>
        /// Obtiene el registro de salida de un expediente.
        /// </summary>
        Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene estadísticas de todas las salidas.
        /// </summary>
        Task<EstadisticasSalidaDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin);

        /// <summary>
        /// Obtiene los registros de salida en un rango de fechas.
        /// </summary>
        Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
    }
}