using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Repositorio para gestionar el registro de salida física del mortuorio.
/// Este es el registro final de auditoría del expediente.
/// Todos los filtros por TipoSalida se aplican sobre ActaRetiro.TipoSalida.
/// </summary>
public interface ISalidaMortuorioRepository
{
    // ═══════════════════════════════════════════════════════════
    // ESCRITURA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea el registro único de salida para un expediente.
    /// Recarga la entidad con relaciones (Expediente, RegistradoPor, ActaRetiro) tras guardar.
    /// </summary>
    Task<SalidaMortuorio> CreateAsync(SalidaMortuorio salida);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS INDIVIDUALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>Obtiene una salida por su ID.</summary>
    Task<SalidaMortuorio?> GetByIdAsync(int salidaId);

    /// <summary>Obtiene el registro de salida asociado a un expediente.</summary>
    Task<SalidaMortuorio?> GetByExpedienteIdAsync(int expedienteId);

    /// <summary>Verifica si ya existe un registro de salida para un expediente.</summary>
    Task<bool> ExistsByExpedienteIdAsync(int expedienteId);

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS POR FILTRO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las salidas registradas por un usuario específico.
    /// Aplica para Vigilante, Admin u otro rol autorizado.
    /// </summary>
    Task<List<SalidaMortuorio>> GetByRegistradoPorIdAsync(int registradoPorId);

    /// <summary>Obtiene salidas en un rango de fechas ordenadas por fecha descendente.</summary>
    Task<List<SalidaMortuorio>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>Obtiene salidas con incidentes registrados.</summary>
    Task<List<SalidaMortuorio>> GetSalidasConIncidentesAsync();

    /// <summary>Obtiene salidas realizadas por una funeraria específica.</summary>
    Task<List<SalidaMortuorio>> GetByFunerariaAsync(string nombreFuneraria);

    /// <summary>
    /// Obtiene salidas que excedieron 48 horas de permanencia.
    /// Filtra por TiempoPermanenciaMinutos > 2880 (columna real en BD).
    /// </summary>
    Task<List<SalidaMortuorio>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin);

    /// <summary>
    /// Obtiene salidas filtradas por tipo.
    /// El filtro se aplica sobre ActaRetiro.TipoSalida — no sobre campo propio de SalidaMortuorio.
    /// </summary>
    Task<List<SalidaMortuorio>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin);

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO Y ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde ActaRetiro para el formulario de salida.
    /// Valida estado PendienteRetiro y que el acta tenga PDF firmado.
    /// </summary>
    Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId);

    /// <summary>
    /// Obtiene estadísticas consolidadas de salidas.
    /// Los conteos por tipo se calculan desde ActaRetiro.TipoSalida.
    /// </summary>
    Task<SalidaEstadisticas> GetEstadisticasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
}

/// <summary>
/// Estadísticas agregadas de salidas. Mapeada a EstadisticasSalidaDTO en el service.
/// </summary>
public class SalidaEstadisticas
{
    public int TotalSalidas          { get; set; }
    public int SalidasFamiliar       { get; set; }
    public int SalidasAutoridadLegal { get; set; }
    public int ConIncidentes         { get; set; }
    public int ConFuneraria          { get; set; }
    public double PorcentajeIncidentes { get; set; }
}