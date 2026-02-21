using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Repositorio para gestión de Actas de Retiro
/// </summary>
public interface IActaRetiroRepository
{
    /// <summary>
    /// Obtiene un acta por su ID con todas sus navegaciones
    /// </summary>
    Task<ActaRetiro?> GetByIdAsync(int actaRetiroId);

    /// <summary>
    /// Obtiene un acta por ID de expediente
    /// </summary>
    Task<ActaRetiro?> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene actas filtradas por fecha de registro
    /// </summary>
    Task<List<ActaRetiro>> GetByFechaRegistroAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Obtiene actas pendientes de firmar (sin PDF firmado)
    /// </summary>
    Task<List<ActaRetiro>> GetPendientesFirmaAsync();

    /// <summary>
    /// Obtiene actas por número de documento del responsable
    /// Busca tanto en Familiar como en Autoridad Legal
    /// </summary>
    Task<List<ActaRetiro>> GetByResponsableDocumentoAsync(string numeroDocumento);

    /// <summary>
    /// Obtiene actas por tipo de autoridad legal
    /// Útil para reportes de casos PNP, Fiscalía, etc.
    /// </summary>
    Task<List<ActaRetiro>> GetByTipoAutoridadAsync(TipoAutoridadExterna tipoAutoridad);

    /// <summary>
    /// Obtiene actas por número de documento del familiar
    /// DEPRECADO: Usar GetByResponsableDocumentoAsync() para búsqueda unificada
    /// Mantenido por compatibilidad con código existente
    /// </summary>
    Task<List<ActaRetiro>> GetByFamiliarDocumentoAsync(string numeroDocumento);
    /// <summary>
    /// Verifica si existe un acta con el certificado SINADEF especificado
    /// </summary>
    Task<bool> ExisteByCertificadoSINADEFAsync(string numeroCertificado);

    /// <summary>
    /// Verifica si existe un acta con el número de oficio legal especificado
    /// </summary>
    Task<bool> ExistsByOficioLegalAsync(string numeroOficio);
    /// <summary>
    /// Crea una nueva acta de retiro
    /// </summary>
    Task<ActaRetiro> CreateAsync(ActaRetiro actaRetiro);

    /// <summary>
    /// Actualiza un acta existente
    /// </summary>
    Task<ActaRetiro> UpdateAsync(ActaRetiro actaRetiro);

    /// <summary>
    /// Verifica si existe un acta para un expediente
    /// </summary>
    Task<bool> ExistsByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene actas por usuario de admisión (quien las creó)
    /// </summary>
    Task<List<ActaRetiro>> GetByUsuarioAdmisionAsync(int usuarioAdmisionId);
}