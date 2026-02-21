using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities.Enums;
namespace SisMortuorio.Business.Services;

/// <summary>
/// Servicio de negocio para gestionar el flujo de Salida física del cuerpo del mortuorio.
/// Soporta tanto casos internos (Familiar con ActaRetiro) como externos (AutoridadLegal con ExpedienteLegal).
/// </summary>
public interface ISalidaMortuorioService
{
    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE SALIDA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la salida física de un cuerpo del mortuorio.
    /// 
    /// FLUJO:
    /// 1. Valida expediente existe y está en estado 'PendienteRetiro'
    /// 2. Valida referencias polimórficas (ActaRetiroID o ExpedienteLegalID según TipoSalida)
    /// 3. Valida que Admisión haya completado documentación (DocumentacionCompleta = true)
    /// 4. Valida campos según tipo de salida:
    ///    - Familiar: ActaRetiroID, Parentesco, NombreFuneraria, PlacaVehiculo
    ///    - AutoridadLegal: ExpedienteLegalID, NumeroOficio, PlacaVehiculo
    /// 5. Crea registro en tabla SalidaMortuorio
    /// 6. Calcula TiempoPermanencia (FechaHoraSalida - FechaIngresoMortuorio)
    /// 7. Dispara StateMachine: PendienteRetiro → Retirado
    /// 8. Libera bandeja automáticamente (RN-34)
    /// 9. Notifica cambio de estado vía SignalR
    /// 
    /// VALIDACIONES:
    /// - Estado actual debe ser PendienteRetiro
    /// - Documentación debe estar completa (validada por Admisión)
    /// - Referencias polimórficas deben ser consistentes con TipoSalida
    /// - Campos obligatorios según tipo (método ValidarReferencias() de entidad)
    /// </summary>
    /// <param name="dto">Datos de la salida (responsable, funeraria, etc.)</param>
    /// <param name="vigilanteId">ID del Vigilante que confirma el retiro físico</param>
    /// <returns>DTO con el resumen de la salida registrada</returns>
    /// <exception cref="KeyNotFoundException">Expediente no encontrado</exception>
    /// <exception cref="InvalidOperationException">Estado inválido, documentación incompleta o referencias incorrectas</exception>
    Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int vigilanteId);

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde el Acta de Retiro para facilitar el registro de salida.
    /// 
    /// FLUJO:
    /// 1. Valida que el expediente esté en estado PendienteRetiro
    /// 2. Valida que exista Acta de Retiro con PDF firmado
    /// 3. Verifica estado de deudas (DeudaSangre y DeudaEconomica)
    /// 4. Pre-llena datos del familiar (READONLY en UI)
    /// 5. Deja campos de funeraria en null (Vigilante los captura)
    /// 
    /// DATOS PRE-LLENADOS (desde Acta):
    /// - Responsable (nombre, documento, parentesco, teléfono)
    /// - Tipo de Salida (Familiar/AutoridadLegal)
    /// - Destino (cementerio o Morgue Central)
    /// - Validaciones automáticas (DocumentosOK, PagosOK)
    /// 
    /// DATOS A CAPTURAR (por Vigilante):
    /// - Nombre Funeraria
    /// - RUC Funeraria (opcional)
    /// - Teléfono Funeraria (opcional)
    /// - Conductor + DNI
    /// - Ayudante + DNI (opcional)
    /// - Placa Vehículo
    /// - Observaciones (opcional)
    /// </summary>
    /// <param name="expedienteId">ID del expediente en estado PendienteRetiro</param>
    /// <returns>DTO con datos pre-llenados o null si no cumple requisitos</returns>
    /// <exception cref="KeyNotFoundException">Expediente no encontrado</exception>
    /// <exception cref="InvalidOperationException">Expediente no está en PendienteRetiro o no tiene acta firmada</exception>
    Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId);
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el registro de salida de un expediente específico.
    /// Incluye información completa del responsable, vehículo y destino.
    /// </summary>
    /// <param name="expedienteId">ID del expediente</param>
    /// <returns>DTO de salida o null si no se ha registrado salida</returns>
    Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene estadísticas de salidas del mortuorio.
    /// Incluye:
    /// - Total de salidas por tipo (Familiar, AutoridadLegal)
    /// - Cantidad de salidas con funeraria
    /// - Cantidad de salidas con incidentes
    /// - Porcentaje de incidentes
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del rango (opcional)</param>
    /// <param name="fechaFin">Fecha de fin del rango (opcional)</param>
    /// <returns>DTO con estadísticas agregadas</returns>
    Task<EstadisticasSalidaDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene lista de salidas en un rango de fechas.
    /// Útil para reportes diarios, semanales o mensuales.
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del rango</param>
    /// <param name="fechaFin">Fecha de fin del rango</param>
    /// <returns>Lista de DTOs de salida ordenados por fecha descendente</returns>
    Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS ESPECÍFICAS (OPCIONAL - PUEDEN AGREGARSE DESPUÉS)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene salidas con incidentes registrados.
    /// Útil para auditorías y revisión de irregularidades.
    /// </summary>
    // Task<List<SalidaDTO>> GetSalidasConIncidentesAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene salidas que excedieron el límite de permanencia (48 horas).
    /// </summary>
     Task<List<SalidaDTO>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene salidas por tipo específico.
    /// </summary>
    Task<List<SalidaDTO>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin);
}