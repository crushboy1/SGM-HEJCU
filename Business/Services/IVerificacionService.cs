using SisMortuorio.Business.DTOs.Verificacion;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de negocio para gestionar el flujo de Verificación
    /// de ingreso al mortuorio.
    /// </summary>
    public interface IVerificacionService
    {
        /// <summary>
        /// Procesa un intento de verificación de ingreso al mortuorio.
        /// Es una transacción atómica:
        /// 1. Compara datos del brazalete (DTO) vs. Expediente (BD).
        /// 2. Si es OK (Happy Path): Cambia estado a 'PendienteAsignacionBandeja'.
        /// 3. Si es NO OK (Sad Path): Cambia estado a 'VerificacionRechazada' y crea una Solicitud de Corrección.
        /// </summary>
        /// <param name="dto">Datos leídos del brazalete</param>
        /// <param name="vigilanteId">ID del vigilante (del Token)</param>
        /// <returns>Un DTO con el resultado de la verificación (Aprobada/Rechazada)</returns>
        Task<VerificacionResultadoDTO> VerificarIngresoAsync(VerificacionRequestDTO dto, int vigilanteId);

        /// <summary>
        /// Obtiene el historial de intentos de verificación de un expediente.
        /// </summary>
        Task<List<VerificacionHistorialDTO>> GetHistorialByExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Obtiene las estadísticas de verificaciones (aprobadas vs. rechazadas).
        /// </summary>
        Task<EstadisticasVerificacionDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin);
    }
}