using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Implementación del repositorio para SalidaMortuorio.
/// Todos los filtros por TipoSalida se aplican sobre ActaRetiro.TipoSalida.
/// Todas las consultas incluyen ActaRetiro para que MapToSalidaDTO
/// pueda leer TipoSalida y datos del responsable sin consultas adicionales.
/// </summary>
public class SalidaMortuorioRepository(ApplicationDbContext context) : ISalidaMortuorioRepository
{
    // ═══════════════════════════════════════════════════════════
    // ESCRITURA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crea el registro único de salida y recarga con todas las relaciones
    /// necesarias para MapToSalidaDTO (Expediente, Vigilante, ActaRetiro).
    /// </summary>
    public async Task<SalidaMortuorio> CreateAsync(SalidaMortuorio salida)
    {
        context.SalidasMortuorio.Add(salida);
        await context.SaveChangesAsync();

        // Recargar con relaciones completas para DTOs y logs
        var salidaCreada = await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
                .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.SalidaID == salida.SalidaID);

        return salidaCreada ?? salida;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS INDIVIDUALES
    // ═══════════════════════════════════════════════════════════

    public async Task<SalidaMortuorio?> GetByIdAsync(int salidaId)
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
                .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.SalidaID == salidaId);
    }

    public async Task<SalidaMortuorio?> GetByExpedienteIdAsync(int expedienteId)
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
                .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.ExpedienteID == expedienteId);
    }

    public async Task<bool> ExistsByExpedienteIdAsync(int expedienteId)
    {
        return await context.SalidasMortuorio
            .AnyAsync(s => s.ExpedienteID == expedienteId);
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS POR FILTRO
    // ═══════════════════════════════════════════════════════════

    public async Task<List<SalidaMortuorio>> GetByRegistradoPorIdAsync(int registradoPorId)
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.RegistradoPorID == registradoPorId)
            .OrderByDescending(s => s.FechaHoraSalida)
            .ToListAsync();
    }

    public async Task<List<SalidaMortuorio>> GetSalidasPorRangoFechasAsync(
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.FechaHoraSalida >= fechaInicio &&
                        s.FechaHoraSalida <= fechaFin)
            .OrderByDescending(s => s.FechaHoraSalida)
            .ToListAsync();
    }

    public async Task<List<SalidaMortuorio>> GetSalidasConIncidentesAsync()
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.IncidenteRegistrado)
            .OrderByDescending(s => s.FechaHoraSalida)
            .ToListAsync();
    }

    public async Task<List<SalidaMortuorio>> GetByFunerariaAsync(string nombreFuneraria)
    {
        return await context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.NombreFuneraria != null &&
                        s.NombreFuneraria.Contains(nombreFuneraria))
            .OrderByDescending(s => s.FechaHoraSalida)
            .ToListAsync();
    }

    public async Task<List<SalidaMortuorio>> GetSalidasExcedieronLimiteAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        var query = context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.TiempoPermanenciaMinutos != null &&
                   s.TiempoPermanenciaMinutos.Value > 48 * 60);

        if (fechaInicio.HasValue)
            query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

        return await query
            .OrderByDescending(s => s.TiempoPermanenciaMinutos)
            .ToListAsync();
    }

    /// <summary>
    /// Filtra por ActaRetiro.TipoSalida — no por campo propio de SalidaMortuorio.
    /// </summary>
    public async Task<List<SalidaMortuorio>> GetSalidasPorTipoAsync(
        TipoSalida tipo,
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        var query = context.SalidasMortuorio
            .Include(s => s.Expediente)
            .Include(s => s.ActaRetiro)
            .Include(s => s.RegistradoPor)
            .Where(s => s.ActaRetiro.TipoSalida == tipo);

        if (fechaInicio.HasValue)
            query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

        return await query
            .OrderByDescending(s => s.FechaHoraSalida)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde ActaRetiro para el formulario del Vigilante.
    /// Valida: expediente existe, estado PendienteRetiro, acta con PDF firmado.
    /// </summary>
    public async Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId)
    {
        var expediente = await context.Expedientes
            .Include(e => e.ActaRetiro)
            .Include(e => e.DeudaSangre)
            .Include(e => e.DeudaEconomica)
            .Include(e => e.BandejaActual)
            .FirstOrDefaultAsync(e => e.ExpedienteID == expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        var acta = expediente.ActaRetiro
            ?? throw new InvalidOperationException(
                $"El expediente {expedienteId} no tiene Acta de Retiro registrada"
            );

        // Validar estado del expediente
        if (expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
            throw new InvalidOperationException(
                $"El expediente debe estar en PendienteRetiro. " +
                $"Estado actual: {expediente.EstadoActual}"
            );

        // Validar que el acta tiene PDF firmado cargado
        if (!acta.TienePDFFirmado())
            throw new InvalidOperationException(
                "El Acta de Retiro debe tener el PDF firmado cargado antes de registrar la salida"
            );

        // Evaluar deudas
        bool tieneDeudaSangre = expediente.DeudaSangre?.BloqueaRetiro() ?? false;
        bool tieneDeudaEconomica = expediente.DeudaEconomica?.BloqueaRetiro() ?? false;
        bool pagosOK = !tieneDeudaSangre && !tieneDeudaEconomica;

        return new DatosPreLlenadoSalidaDTO
        {
            // ── Expediente ──────────────────────────────────────
            ExpedienteID = expediente.ExpedienteID,
            CodigoExpediente = expediente.CodigoExpediente,
            NombrePaciente = expediente.NombreCompleto,
            HC = expediente.HC,
            Servicio = expediente.ServicioFallecimiento,
            BandejaAsignada = expediente.BandejaActual?.Codigo,

            // ── Acta ────────────────────────────────────────────
            ActaRetiroID = acta.ActaRetiroID,
            TieneActaFirmada = acta.TienePDFFirmado(),

            // ── Tipo de salida ───────────────────────────────────
            TipoSalida = acta.TipoSalida,

            // ── Responsable (Familiar O Autoridad según TipoSalida) ──
            ResponsableApellidoPaterno = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarApellidoPaterno
                : acta.AutoridadApellidoPaterno,

            ResponsableApellidoMaterno = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarApellidoMaterno
                : acta.AutoridadApellidoMaterno,

            ResponsableNombres = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarNombres
                : acta.AutoridadNombres,

            ResponsableTipoDocumento = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarTipoDocumento?.ToString()
                : acta.AutoridadTipoDocumento?.ToString(),

            ResponsableNumeroDocumento = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarNumeroDocumento
                : acta.AutoridadNumeroDocumento,

            // Parentesco solo aplica si es Familiar
            ResponsableParentesco = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarParentesco
                : null,

            ResponsableTelefono = acta.TipoSalida == TipoSalida.Familiar
                ? acta.FamiliarTelefono
                : acta.AutoridadTelefono,

            // ── Autoridad Legal (solo si AutoridadLegal) ─────────
            TipoAutoridad = acta.TipoAutoridad?.ToString(),
            AutoridadInstitucion = acta.AutoridadInstitucion,
            AutoridadCargo = acta.AutoridadCargo,
            NumeroOficioLegal = acta.NumeroOficioLegal,

            // ── Destino sugerido ─────────────────────────────────
            Destino = acta.TipoSalida == TipoSalida.AutoridadLegal
                ? "Morgue Central de Lima"
                : null,

            // ── Deudas ───────────────────────────────────────────
            DeudaSangreOK = !tieneDeudaSangre,
            DeudaSangreMensaje = tieneDeudaSangre
                ? "Pendiente firma de compromiso de sangre"
                : null,

            DeudaEconomicaOK = !tieneDeudaEconomica,
            DeudaEconomicaMensaje = tieneDeudaEconomica
                ? "Pendiente liquidación económica"
                : null,

            PuedeRegistrarSalida = pagosOK,
            MensajeBloqueo = !pagosOK
                ? "No se puede registrar la salida: existen deudas pendientes"
                : null
        };
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estadísticas consolidadas de salidas.
    /// Los conteos por tipo se calculan desde ActaRetiro.TipoSalida.
    /// </summary>
    public async Task<SalidaEstadisticas> GetEstadisticasAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null)
    {
        var query = context.SalidasMortuorio
            .Include(s => s.ActaRetiro)
            .AsQueryable();

        if (fechaInicio.HasValue)
            query = query.Where(s => s.FechaHoraSalida >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(s => s.FechaHoraSalida <= fechaFin.Value);

        var total = await query.CountAsync();
        var familiar = await query.CountAsync(s => s.ActaRetiro.TipoSalida == TipoSalida.Familiar);
        var autoridadLegal = await query.CountAsync(s => s.ActaRetiro.TipoSalida == TipoSalida.AutoridadLegal);
        var conIncidentes = await query.CountAsync(s => s.IncidenteRegistrado);
        var conFuneraria = await query.CountAsync(s => !string.IsNullOrEmpty(s.NombreFuneraria));

        return new SalidaEstadisticas
        {
            TotalSalidas = total,
            SalidasFamiliar = familiar,
            SalidasAutoridadLegal = autoridadLegal,
            ConIncidentes = conIncidentes,
            ConFuneraria = conFuneraria,
            PorcentajeIncidentes = total > 0 ? (conIncidentes * 100.0 / total) : 0
        };
    }
}