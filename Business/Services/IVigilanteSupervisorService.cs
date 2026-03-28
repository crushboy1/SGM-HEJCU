using SisMortuorio.Business.DTOs.Vigilancia;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de consulta para el módulo Supervisor de Vigilancia.
    /// Solo lectura — VigSup consulta pero no crea ni modifica expedientes.
    ///
    /// RESPONSABILIDADES:
    /// 1. Ver todos los expedientes con semáforo de deudas precalculado.
    /// 2. Ver detalle completo de un expediente (modal Ver).
    /// </summary>
    public interface IVigilanteSupervisorService
    {
        /// <summary>
        /// Obtiene todos los expedientes activos con semáforo de deudas precalculado.
        /// Permite búsqueda por texto libre sobre HC, NumeroDocumento o NombreCompleto.
        ///
        /// SEMÁFORO:
        ///   bool? null  = sin registro (amarillo — expediente externo o sin evaluar)
        ///   bool? true  = bloquea retiro (rojo)
        ///   bool? false = no bloquea (verde)
        ///
        /// BYPASS:
        ///   Si BypassDeudaAutorizado = true en el expediente, la UI muestra
        ///   ambos semáforos en verde con indicador de bypass.
        /// </summary>
        /// <param name="busqueda">
        ///   Texto libre — busca en HC, NumeroDocumento y NombreCompleto.
        ///   null o vacío devuelve todos los expedientes.
        /// </param>
        /// <returns>Lista de expedientes con semáforo precalculado</returns>
        Task<List<ExpedienteVigilanciaDTO>> ObtenerExpedientesAsync(string? busqueda);

        /// <summary>
        /// Obtiene el detalle completo de un expediente para el modal Ver.
        /// Incluye semáforo expandido, datos del responsable de retiro y JG.
        /// No incluye montos económicos.
        /// </summary>
        /// <param name="expedienteId">ID del expediente</param>
        /// <returns>DetalleVigilanciaDTO o null si no existe</returns>
        Task<DetalleVigilanciaDTO?> ObtenerDetalleAsync(int expedienteId);
    }
}