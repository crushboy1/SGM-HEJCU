using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Repositorio para gestión de documentos digitalizados del expediente.
/// Reemplaza el proceso manual de "juegos de copias físicas".
/// </summary>
public interface IDocumentoExpedienteRepository
{
    /// <summary>
    /// Obtiene todos los documentos de un expediente
    /// </summary>
    Task<List<DocumentoExpediente>> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene un documento por su ID
    /// </summary>
    Task<DocumentoExpediente?> GetByIdAsync(int documentoId);

    /// <summary>
    /// Obtiene un documento específico por expediente y tipo
    /// Retorna el más reciente si hay varios del mismo tipo
    /// </summary>
    Task<DocumentoExpediente?> GetByExpedienteIdYTipoAsync(int expedienteId, TipoDocumentoExpediente tipo);

    /// <summary>
    /// Obtiene solo los documentos verificados de un expediente
    /// </summary>
    Task<List<DocumentoExpediente>> GetVerificadosByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Verifica si existe al menos un documento del tipo especificado para el expediente
    /// </summary>
    Task<bool> ExisteDocumentoTipoAsync(int expedienteId, TipoDocumentoExpediente tipo);

    /// <summary>
    /// Verifica si existe al menos un documento verificado del tipo especificado
    /// </summary>
    Task<bool> ExisteDocumentoVerificadoTipoAsync(int expedienteId, TipoDocumentoExpediente tipo);

    /// <summary>
    /// Crea un nuevo documento digitalizado
    /// </summary>
    Task<DocumentoExpediente> CreateAsync(DocumentoExpediente documento);

    /// <summary>
    /// Actualiza un documento existente (para verificación/rechazo)
    /// </summary>
    Task<DocumentoExpediente> UpdateAsync(DocumentoExpediente documento);

    /// <summary>
    /// Elimina un documento por su ID
    /// Solo permitido cuando Estado == PendienteVerificacion o Rechazado
    /// </summary>
    Task DeleteAsync(int documentoId);

    /// <summary>
    /// Obtiene el conteo de documentos verificados de un expediente
    /// </summary>
    Task<int> GetCountVerificadosAsync(int expedienteId);
}