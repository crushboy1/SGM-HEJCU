using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Repositorio para gestión de Documentos Legales
/// </summary>
public interface IDocumentoLegalRepository
{
    // ===================================================================
    // CONSULTAS BÁSICAS
    // ===================================================================

    /// <summary>
    /// Obtiene un documento por su ID
    /// </summary>
    Task<DocumentoLegal?> GetByIdAsync(int documentoId);

    /// <summary>
    /// Obtiene un documento específico por expediente legal y tipo
    /// Útil para verificar si ya existe un documento de cierto tipo
    /// </summary>
    Task<DocumentoLegal?> GetByExpedienteYTipoAsync(int expedienteLegalId, TipoDocumentoLegal tipo);

    /// <summary>
    /// Obtiene todos los documentos de un expediente legal
    /// </summary>
    Task<List<DocumentoLegal>> GetByExpedienteLegalIdAsync(int expedienteLegalId);

    // ===================================================================
    // CONSULTAS ESPECIALES
    // ===================================================================

    /// <summary>
    /// Obtiene documentos pendientes de adjuntar (sin archivo)
    /// </summary>
    Task<List<DocumentoLegal>> GetPendientesAdjuntarAsync(int expedienteLegalId);

    /// <summary>
    /// Obtiene documentos adjuntados pero no validados
    /// </summary>
    Task<List<DocumentoLegal>> GetPendientesValidacionAsync(int expedienteLegalId);

    /// <summary>
    /// Obtiene documentos validados
    /// </summary>
    Task<List<DocumentoLegal>> GetValidadosAsync(int expedienteLegalId);

    // ===================================================================
    // OPERACIONES DE ESCRITURA
    // ===================================================================

    /// <summary>
    /// Crea un nuevo documento legal
    /// </summary>
    Task<DocumentoLegal> CreateAsync(DocumentoLegal documento);

    /// <summary>
    /// Actualiza un documento legal existente
    /// </summary>
    Task<DocumentoLegal> UpdateAsync(DocumentoLegal documento);

    /// <summary>
    /// Elimina un documento legal
    /// </summary>
    Task DeleteAsync(int documentoId);

    // ===================================================================
    // VALIDACIONES Y VERIFICACIONES
    // ===================================================================

    /// <summary>
    /// Verifica si existe un documento de un tipo específico para un expediente
    /// </summary>
    Task<bool> ExisteDocumentoTipoAsync(int expedienteLegalId, TipoDocumentoLegal tipo);

    /// <summary>
    /// Cuenta documentos adjuntados por expediente legal
    /// </summary>
    Task<int> CountAdjuntadosAsync(int expedienteLegalId);

    /// <summary>
    /// Cuenta documentos validados por expediente legal
    /// </summary>
    Task<int> CountValidadosAsync(int expedienteLegalId);
}