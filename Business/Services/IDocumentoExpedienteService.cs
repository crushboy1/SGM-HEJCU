using Microsoft.AspNetCore.Http;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Servicio para gestión de documentos digitalizados del expediente.
/// Reemplaza el proceso manual de "juegos de copias físicas".
/// </summary>
public interface IDocumentoExpedienteService
{
    /// <summary>
    /// Obtiene todos los documentos de un expediente
    /// </summary>
    Task<List<DocumentoExpedienteDTO>> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene un documento por su ID
    /// </summary>
    Task<DocumentoExpedienteDTO?> GetByIdAsync(int documentoId);

    /// <summary>
    /// Obtiene el resumen de documentos de un expediente.
    /// Indica qué documentos están presentes, pendientes o faltantes
    /// según el TipoSalida del ActaRetiro (si existe).
    /// </summary>
    Task<ResumenDocumentosDTO> GetResumenAsync(int expedienteId);

    /// <summary>
    /// Sube y registra un nuevo documento digitalizado.
    /// Valida formato (.pdf, .jpg, .jpeg, .png) y tamaño (máx 5MB).
    /// Estado inicial: PendienteVerificacion.
    /// </summary>
    Task<DocumentoExpedienteDTO> SubirDocumentoAsync(SubirDocumentoDTO dto, IFormFile archivo);

    /// <summary>
    /// Marca un documento como verificado contra el original físico.
    /// Actualiza Expediente.DocumentacionCompleta si corresponde.
    /// </summary>
    Task<DocumentoExpedienteDTO> VerificarDocumentoAsync(VerificarDocumentoDTO dto);

    /// <summary>
    /// Marca un documento como rechazado indicando el motivo.
    /// El familiar deberá presentar nuevamente el documento.
    /// </summary>
    Task<DocumentoExpedienteDTO> RechazarDocumentoAsync(RechazarDocumentoDTO dto);

    /// <summary>
    /// Elimina un documento.
    /// Solo permitido cuando Estado == PendienteVerificacion o Rechazado.
    /// No se puede eliminar un documento ya Verificado.
    /// </summary>
    Task EliminarDocumentoAsync(int documentoId, int usuarioId);

    /// <summary>
    /// Verifica si el expediente tiene todos los documentos requeridos verificados
    /// según el TipoSalida:
    /// - Familiar       → DNI_Familiar + DNI_Fallecido + CertificadoDefuncion
    /// - AutoridadLegal → OficioLegal
    /// Actualiza Expediente.DocumentacionCompleta automáticamente.
    /// </summary>
    Task<bool> VerificarDocumentacionCompletaAsync(int expedienteId);

    /// <summary>
    /// Retorna el stream del archivo para descarga.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)> DescargarDocumentoAsync(int documentoId);
}