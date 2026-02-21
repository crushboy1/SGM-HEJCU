using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

public class ExpedienteLegalRepository(ApplicationDbContext context) : IExpedienteLegalRepository
{
    // ===================================================================
    // CONSULTAS BÁSICAS
    // ===================================================================

    public async Task<ExpedienteLegal?> GetByIdAsync(int expedienteLegalId)
    {
        return await context.ExpedientesLegales
            .FirstOrDefaultAsync(el => el.ExpedienteLegalID == expedienteLegalId);
    }

    public async Task<ExpedienteLegal?> GetCompletoByIdAsync(int expedienteLegalId)
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.UsuarioRegistro)
            .Include(el => el.UsuarioActualizacion)
            .Include(el => el.UsuarioAdmision)
            .Include(el => el.JefeGuardia)
            .Include(el => el.Autoridades)
            .Include(el => el.Documentos)
                .ThenInclude(d => d.UsuarioAdjunto)
            .FirstOrDefaultAsync(el => el.ExpedienteLegalID == expedienteLegalId);
    }

    public async Task<ExpedienteLegal?> GetByExpedienteIdAsync(int expedienteId)
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.Documentos)
            .Include(el => el.Autoridades)
            .FirstOrDefaultAsync(el => el.ExpedienteID == expedienteId);
    }

    // ===================================================================
    // CONSULTAS POR ESTADO
    // ===================================================================

    public async Task<List<ExpedienteLegal>> GetByEstadoAsync(EstadoExpedienteLegal estado)
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.UsuarioRegistro)
            .Include(el => el.UsuarioAdmision)
            .Include(el => el.JefeGuardia)
            .Include(el => el.Autoridades)
            .Include(el => el.Documentos)
            .Where(el => el.Estado == estado)
            .OrderBy(el => el.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<ExpedienteLegal>> GetEnRegistroAsync()
        => await GetByEstadoAsync(EstadoExpedienteLegal.EnRegistro);

    public async Task<List<ExpedienteLegal>> GetPendientesValidacionAdmisionAsync()
        => await GetByEstadoAsync(EstadoExpedienteLegal.PendienteValidacionAdmision);

    public async Task<List<ExpedienteLegal>> GetRechazadosAdmisionAsync()
        => await GetByEstadoAsync(EstadoExpedienteLegal.RechazadoAdmision);

    public async Task<List<ExpedienteLegal>> GetPendientesAutorizacionJefeGuardiaAsync()
        => await GetByEstadoAsync(EstadoExpedienteLegal.ValidadoAdmision);

    public async Task<List<ExpedienteLegal>> GetAutorizadosAsync()
        => await GetByEstadoAsync(EstadoExpedienteLegal.AutorizadoJefeGuardia);

    // ===================================================================
    // CONSULTAS ESPECIALES Y ALERTAS
    // ===================================================================

    public async Task<List<ExpedienteLegal>> GetAllAsync()
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.Documentos)
            .Include(el => el.Autoridades)
            .Include(el => el.UsuarioRegistro)
            .OrderByDescending(el => el.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<ExpedienteLegal>> GetConDocumentosIncompletosAsync()
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.UsuarioRegistro)
            .Include(el => el.Autoridades)
            .Include(el => el.Documentos)
            .Where(el => !el.DocumentosCompletos)
            .OrderBy(el => el.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<ExpedienteLegal>> GetConAlertaTiempoAsync(DateTime fechaLimite)
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.UsuarioRegistro)
            .Include(el => el.Autoridades)
            .Include(el => el.Documentos)
            .Where(el => !el.DocumentosCompletos && el.FechaCreacion <= fechaLimite)
            .OrderBy(el => el.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<ExpedienteLegal>> GetByUsuarioRegistroAsync(int usuarioId)
    {
        return await context.ExpedientesLegales
            .Include(el => el.Expediente)
            .Include(el => el.Documentos)
            .Include(el => el.Autoridades)
            .Where(el => el.UsuarioRegistroID == usuarioId)
            .OrderByDescending(el => el.FechaCreacion)
            .ToListAsync();
    }

    // ===================================================================
    // OPERACIONES DE ESCRITURA
    // ===================================================================

    public async Task<ExpedienteLegal> CreateAsync(ExpedienteLegal expedienteLegal)
    {
        context.ExpedientesLegales.Add(expedienteLegal);
        await context.SaveChangesAsync();
        return expedienteLegal;
    }

    public async Task UpdateAsync(ExpedienteLegal expedienteLegal)
    {
        context.ExpedientesLegales.Update(expedienteLegal);
        await context.SaveChangesAsync();
    }

    // ===================================================================
    // MANEJO DE SUB-ENTIDADES
    // ===================================================================

    public async Task AddDocumentoAsync(DocumentoLegal documento)
    {
        context.DocumentosLegales.Add(documento);
        await context.SaveChangesAsync();
    }

    public async Task AddAutoridadAsync(AutoridadExterna autoridad)
    {
        context.AutoridadesExternas.Add(autoridad);
        await context.SaveChangesAsync();
    }

    // ===================================================================
    // VALIDACIONES Y VERIFICACIONES
    // ===================================================================

    public async Task<bool> ExistsByExpedienteIdAsync(int expedienteId)
    {
        return await context.ExpedientesLegales
            .AnyAsync(el => el.ExpedienteID == expedienteId);
    }

    public async Task<int> CountByEstadoAsync(EstadoExpedienteLegal estado)
    {
        return await context.ExpedientesLegales
            .CountAsync(el => el.Estado == estado);
    }
}