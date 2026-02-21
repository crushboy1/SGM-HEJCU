using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Repositorio para gestión de Expedientes Legales (casos externos)
/// </summary>
public interface IExpedienteLegalRepository
{
    // ===================================================================
    // CONSULTAS BÁSICAS
    // ===================================================================

    /// <summary>
    /// Obtiene un expediente legal por su ID (sin includes - ligero)
    /// </summary>
    Task<ExpedienteLegal?> GetByIdAsync(int expedienteLegalId);

    /// <summary>
    /// Obtiene un expediente legal completo por su ID
    /// Incluye: Expediente + Documentos + Autoridades + Usuarios
    /// </summary>
    Task<ExpedienteLegal?> GetCompletoByIdAsync(int expedienteLegalId);

    /// <summary>
    /// Busca un expediente legal por el ID del expediente base
    /// Útil para verificar si un expediente ya tiene expediente legal
    /// </summary>
    Task<ExpedienteLegal?> GetByExpedienteIdAsync(int expedienteId);

    // ===================================================================
    // CONSULTAS POR ESTADO
    // ===================================================================

    /// <summary>
    /// Obtiene expedientes legales filtrados por estado
    /// </summary>
    Task<List<ExpedienteLegal>> GetByEstadoAsync(EstadoExpedienteLegal estado);

    /// <summary>
    /// Obtiene todos los expedientes legales en estado "EnRegistro"
    /// </summary>
    Task<List<ExpedienteLegal>> GetEnRegistroAsync();

    /// <summary>
    /// Obtiene expedientes pendientes de validación por Admisión
    /// </summary>
    Task<List<ExpedienteLegal>> GetPendientesValidacionAdmisionAsync();

    /// <summary>
    /// Obtiene expedientes rechazados por Admisión (requieren corrección)
    /// </summary>
    Task<List<ExpedienteLegal>> GetRechazadosAdmisionAsync();

    /// <summary>
    /// Obtiene expedientes validados por Admisión (pendientes de Jefe Guardia)
    /// </summary>
    Task<List<ExpedienteLegal>> GetPendientesAutorizacionJefeGuardiaAsync();

    /// <summary>
    /// Obtiene expedientes autorizados por Jefe de Guardia (listos para levantamiento)
    /// </summary>
    Task<List<ExpedienteLegal>> GetAutorizadosAsync();

    // ===================================================================
    // CONSULTAS ESPECIALES Y ALERTAS
    // ===================================================================

    /// <summary>
    /// Obtiene todos los expedientes legales ordenados por fecha de creación
    /// </summary>
    Task<List<ExpedienteLegal>> GetAllAsync();

    /// <summary>
    /// Obtiene expedientes con documentación incompleta
    /// Útil para alertas y seguimiento
    /// </summary>
    Task<List<ExpedienteLegal>> GetConDocumentosIncompletosAsync();

    /// <summary>
    /// Obtiene expedientes con alerta de tiempo (>48h sin completar documentos)
    /// </summary>
    Task<List<ExpedienteLegal>> GetConAlertaTiempoAsync(DateTime fechaLimite);

    /// <summary>
    /// Obtiene expedientes creados por un usuario específico (Vigilante)
    /// </summary>
    Task<List<ExpedienteLegal>> GetByUsuarioRegistroAsync(int usuarioId);

    // ===================================================================
    // OPERACIONES DE ESCRITURA
    // ===================================================================

    /// <summary>
    /// Crea un nuevo expediente legal
    /// </summary>
    Task<ExpedienteLegal> CreateAsync(ExpedienteLegal expedienteLegal);

    /// <summary>
    /// Actualiza un expediente legal existente
    /// </summary>
    Task UpdateAsync(ExpedienteLegal expedienteLegal);

    // ===================================================================
    // MANEJO DE SUB-ENTIDADES
    // ===================================================================

    /// <summary>
    /// Agrega un documento legal al expediente
    /// </summary>
    Task AddDocumentoAsync(DocumentoLegal documento);

    /// <summary>
    /// Agrega una autoridad externa al expediente
    /// </summary>
    Task AddAutoridadAsync(AutoridadExterna autoridad);

    // ===================================================================
    // VALIDACIONES Y VERIFICACIONES
    // ===================================================================

    /// <summary>
    /// Verifica si existe un expediente legal para un expediente base
    /// </summary>
    Task<bool> ExistsByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Cuenta expedientes legales por estado (para dashboards)
    /// </summary>
    Task<int> CountByEstadoAsync(EstadoExpedienteLegal estado);
}