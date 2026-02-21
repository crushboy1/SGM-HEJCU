using SisMortuorio.Business.DTOs.ActaRetiro;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Servicio para gestión de Actas de Retiro
/// </summary>
public interface IActaRetiroService
{
    /// <summary>
    /// Obtiene un acta por su ID
    /// </summary>
    Task<ActaRetiroDTO?> GetByIdAsync(int actaRetiroId);

    /// <summary>
    /// Obtiene un acta por ID de expediente
    /// </summary>
    Task<ActaRetiroDTO?> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Crea una nueva acta de retiro
    /// </summary>
    Task<ActaRetiroDTO> CreateAsync(CreateActaRetiroDTO dto);

    /// <summary>
    /// Genera el PDF sin firmar del acta
    /// </summary>
    Task<(byte[] PdfBytes, string FileName)> GenerarPDFSinFirmarAsync(int actaRetiroId);

    /// <summary>
    /// Sube el PDF firmado escaneado
    /// </summary>
    Task<ActaRetiroDTO> SubirPDFFirmadoAsync(UpdateActaRetiroPDFDTO dto);

    /// <summary>
    /// Obtiene actas pendientes de firma
    /// </summary>
    Task<List<ActaRetiroDTO>> GetPendientesFirmaAsync();

    /// <summary>
    /// Obtiene actas por rango de fechas
    /// </summary>
    Task<List<ActaRetiroDTO>> GetByFechaRangoAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Verifica si existe un acta para un expediente
    /// </summary>
    Task<bool> ExisteActaParaExpedienteAsync(int expedienteId);
    /// <summary>
    /// Verifica si existe un acta con el certificado SINADEF especificado
    /// </summary>
    Task<bool> ExisteByCertificadoSINADEFAsync(string numeroCertificado);

    /// <summary>
    /// Verifica si existe un acta con el número de oficio legal especificado
    /// </summary>
    Task<bool> ExistsByOficioLegalAsync(string numeroOficio);
}