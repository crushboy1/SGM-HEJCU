using SisMortuorio.Business.DTOs;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestionar deudas económicas de expedientes.
    /// Permite registrar, liquidar, exonerar y consultar el estado de deudas hospitalarias.
    /// </summary>
    public interface IDeudaEconomicaService
    {
        /// <summary>
        /// Registra una nueva deuda económica para un expediente.
        /// Usado por Sup. Vigilancia cuando Cuentas Pacientes informa verbalmente.
        /// Solo puede haber una deuda por expediente.
        /// </summary>
        /// <param name="dto">Datos de la deuda a registrar</param>
        /// <returns>DeudaEconomicaDTO creada con su ID</returns>
        Task<DeudaEconomicaDTO> RegistrarDeudaAsync(CreateDeudaEconomicaDTO dto);

        /// <summary>
        /// Obtiene la deuda económica de un expediente específico.
        /// Devuelve null si no existe deuda.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>DeudaEconomicaDTO o null</returns>
        Task<DeudaEconomicaDTO?> ObtenerPorExpedienteAsync(int expedienteId);

        /// <summary>
        /// Obtiene el semáforo simplificado para Sup. Vigilancia.
        /// Solo muestra DEBE/NO DEBE sin montos.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>DeudaEconomicaSemaforoDTO</returns>
        Task<DeudaEconomicaSemaforoDTO> ObtenerSemaforoAsync(int expedienteId);

        /// <summary>
        /// Marca una deuda como sin deuda.
        /// Usado cuando paciente es SIS o no tiene consumos.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="usuarioId">ID del usuario que marca</param>
        /// <returns>DeudaEconomicaDTO actualizada</returns>
        Task<DeudaEconomicaDTO> MarcarSinDeudaAsync(int expedienteId, int usuarioId);

        /// <summary>
        /// Marca una deuda como liquidada con pago en Caja.
        /// Usado por Sup. Vigilancia cuando familiar presenta boleta física.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <param name="dto">Datos del pago con número de boleta</param>
        /// <returns>DeudaEconomicaDTO actualizada</returns>
        Task<DeudaEconomicaDTO> MarcarLiquidadoAsync(int expedienteId, LiquidarDeudaEconomicaDTO dto);

        /// <summary>
        /// Aplica exoneración parcial o total por Servicio Social.
        /// Requiere adjuntar PDF de Ficha Socioeconómica como sustento.
        /// </summary>
        /// <param name="dto">Datos de exoneración con PDF sustento</param>
        /// <returns>DeudaEconomicaDTO actualizada</returns>
        Task<DeudaEconomicaDTO> AplicarExoneracionAsync(AplicarExoneracionDTO dto);

        /// <summary>
        /// Verifica si una deuda bloquea el retiro del cuerpo.
        /// Bloquea si: Estado == Pendiente.
        /// No bloquea si: SinDeuda, Liquidado, Exonerado.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>true si bloquea el retiro, false si no</returns>
        Task<bool> BloqueaRetiroAsync(int expedienteId);

        /// <summary>
        /// Obtiene todas las deudas pendientes.
        /// Usado por Cuentas Pacientes y Admin para reportes.
        /// </summary>
        /// <returns>Lista de DeudaEconomicaDTO pendientes</returns>
        Task<List<DeudaEconomicaDTO>> ObtenerDeudasPendientesAsync();

        /// <summary>
        /// Obtiene todas las deudas exoneradas.
        /// Usado por Servicio Social y Admin para reportes.
        /// </summary>
        /// <returns>Lista de DeudaEconomicaDTO exoneradas</returns>
        Task<List<DeudaEconomicaDTO>> ObtenerDeudasExoneradasAsync();

        /// <summary>
        /// Obtiene el historial completo de una deuda.
        /// Incluye: registro inicial, pagos, exoneraciones.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>Lista de eventos históricos</returns>
        Task<List<HistorialDeudaEconomicaDTO>> ObtenerHistorialAsync(int expedienteId);

        /// <summary>
        /// Calcula estadísticas de deudas económicas.
        /// Usado para dashboards administrativos.
        /// </summary>
        /// <returns>DTO con totales, promedios y porcentajes</returns>
        Task<EstadisticasDeudaEconomicaDTO> ObtenerEstadisticasAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // DTO ADICIONAL PARA ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO con estadísticas generales de deudas económicas.
    /// </summary>
    public class EstadisticasDeudaEconomicaDTO
    {
        public int TotalDeudas { get; set; }
        public int DeudasPendientes { get; set; }
        public int DeudasLiquidadas { get; set; }
        public int DeudasExoneradas { get; set; }
        public decimal MontoTotalDeudas { get; set; }
        public decimal MontoTotalExonerado { get; set; }
        public decimal MontoTotalPagado { get; set; }
        public decimal MontoTotalPendiente { get; set; }
        public decimal PromedioExoneracion { get; set; }
    }
}