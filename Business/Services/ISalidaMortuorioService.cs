using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Servicio de negocio para gestionar el flujo de Salida física del cuerpo del mortuorio.
/// Soporta casos internos (Familiar) y externos (AutoridadLegal).
/// TipoSalida y datos del responsable se leen siempre desde ActaRetiro.
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
    /// 1. Valida expediente existe y está en estado PendienteRetiro
    /// 2. Carga ActaRetiro — fuente de verdad para TipoSalida y datos del responsable
    /// 3. Valida que el acta tiene PDF firmado cargado
    /// 4. Valida transición en State Machine (PendienteRetiro → Retirado)
    /// 5. Crea registro en SalidaMortuorio con datos capturados por el usuario
    /// 6. Resuelve PlacaVehiculo desde ActaRetiro si TipoSalida = AutoridadLegal
    /// 7. Calcula TiempoPermanenciaMinutos (FechaHoraSalida - FechaHoraIngresoMortuorio)
    /// 8. Dispara StateMachine: PendienteRetiro → Retirado
    /// 9. Libera bandeja automáticamente (RN-34)
    /// 10. Notifica cambio de estado vía SignalR a Admision, JefeGuardia y VigilanteSupervisor
    ///
    /// VALIDACIONES:
    /// - Estado actual debe ser PendienteRetiro
    /// - ActaRetiro debe existir y tener PDF firmado
    /// - Familiar: NombreFuneraria, ConductorFuneraria, DNIConductor y PlacaVehiculo obligatorios
    /// - AutoridadLegal: PlacaVehiculo resuelto desde ActaRetiro si no viene del frontend
    /// </summary>
    /// <param name="dto">Datos capturados por el usuario (funeraria, placa, destino, observaciones)</param>
    /// <param name="registradoPorId">ID del usuario obtenido desde el token JWT</param>
    /// <returns>SalidaDTO con datos completos incluyendo tiempo de permanencia</returns>
    /// <exception cref="KeyNotFoundException">Expediente no encontrado</exception>
    /// <exception cref="InvalidOperationException">
    /// Estado inválido, acta sin PDF firmado o documentación incompleta
    /// </exception>
    Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int registradoPorId);

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde ActaRetiro para el formulario de salida.
    ///
    /// DATOS PRE-LLENADOS (readonly en UI — desde ActaRetiro firmado):
    /// - TipoSalida (Familiar / AutoridadLegal)
    /// - Responsable (nombre, documento, parentesco, teléfono)
    /// - Autoridad (tipo, institución, cargo, número de oficio) — si AutoridadLegal
    /// - Validaciones de deudas (DeudaSangre y DeudaEconomica)
    ///
    /// DATOS A CAPTURAR (editables por usuario):
    /// - NombreFuneraria, RUC, Teléfono — solo Familiar
    /// - Conductor + DNI — solo Familiar
    /// - Ayudante + DNI (opcional)
    /// - PlacaVehiculo — solo Familiar (AutoridadLegal la resuelve el backend)
    /// - Observaciones (opcional)
    /// </summary>
    /// <param name="expedienteId">ID del expediente en estado PendienteRetiro</param>
    /// <returns>DTO con datos pre-llenados o null si no cumple requisitos</returns>
    /// <exception cref="KeyNotFoundException">Expediente no encontrado</exception>
    /// <exception cref="InvalidOperationException">
    /// Expediente no está en PendienteRetiro o no tiene acta con PDF firmado
    /// </exception>
    Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Obtiene el registro de salida de un expediente específico.</summary>
    Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>
    /// Obtiene estadísticas consolidadas de salidas.
    /// Incluye totales por tipo, salidas con funeraria, incidentes y porcentaje de incidentes.
    /// </summary>
    Task<EstadisticasSalidaDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene historial de salidas por rango de fechas ordenado descendente.
    /// </summary>
    Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Obtiene salidas que excedieron 48 horas de permanencia.
    /// Filtra por TiempoPermanenciaMinutos > 2880 en BD.
    /// </summary>
    Task<List<SalidaDTO>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene salidas filtradas por tipo.
    /// El filtro se aplica sobre ActaRetiro.TipoSalida en el repositorio.
    /// </summary>
    Task<List<SalidaDTO>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin);
}