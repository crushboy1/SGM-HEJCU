using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

public class ActaRetiroRepository(ApplicationDbContext context) : IActaRetiroRepository
{
    public async Task<ActaRetiro?> GetByIdAsync(int actaRetiroId)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Include(a => a.UsuarioSubidaPDF)
            .Include(a => a.SalidaMortuorio)
            .FirstOrDefaultAsync(a => a.ActaRetiroID == actaRetiroId);
    }

    public async Task<ActaRetiro?> GetByExpedienteIdAsync(int expedienteId)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Include(a => a.UsuarioSubidaPDF)
            .Include(a => a.SalidaMortuorio)
            .FirstOrDefaultAsync(a => a.ExpedienteID == expedienteId);
    }

    public async Task<List<ActaRetiro>> GetByFechaRegistroAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Where(a => a.FechaRegistro >= fechaInicio && a.FechaRegistro <= fechaFin)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }

    public async Task<List<ActaRetiro>> GetPendientesFirmaAsync()
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Where(a => string.IsNullOrEmpty(a.RutaPDFFirmado))
            .OrderBy(a => a.FechaRegistro)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene actas por número de documento del responsable (Familiar O Autoridad)
    /// </summary>
    public async Task<List<ActaRetiro>> GetByResponsableDocumentoAsync(string numeroDocumento)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Where(a => a.FamiliarNumeroDocumento == numeroDocumento ||
                        a.AutoridadNumeroDocumento == numeroDocumento)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene actas de casos con autoridades legales de un tipo específico
    /// </summary>
    public async Task<List<ActaRetiro>> GetByTipoAutoridadAsync(TipoAutoridadExterna tipoAutoridad)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Include(a => a.UsuarioAdmision)
            .Where(a => a.TipoAutoridad == tipoAutoridad)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }
    public async Task<bool> ExisteByCertificadoSINADEFAsync(string numeroCertificado)
    {
        return await context.ActasRetiro
            .AnyAsync(a => a.NumeroCertificadoDefuncion == numeroCertificado);
    }

    public async Task<bool> ExistsByOficioLegalAsync(string numeroOficio)
    {
        return await context.ActasRetiro
            .AnyAsync(a => a.NumeroOficioLegal == numeroOficio);
    }
    /// <summary>
    /// Obtiene actas por número de documento del familiar
    /// DEPRECADO: Redirige a GetByResponsableDocumentoAsync()
    /// Mantenido por compatibilidad con código existente
    /// </summary>
    [Obsolete("Usar GetByResponsableDocumentoAsync() en su lugar")]
    public async Task<List<ActaRetiro>> GetByFamiliarDocumentoAsync(string numeroDocumento)
    {
        // Wrapper para compatibilidad
        return await GetByResponsableDocumentoAsync(numeroDocumento);
    }
    public async Task<ActaRetiro> CreateAsync(ActaRetiro actaRetiro)
    {
        context.ActasRetiro.Add(actaRetiro);
        await context.SaveChangesAsync();
        return actaRetiro;
    }

    public async Task<ActaRetiro> UpdateAsync(ActaRetiro actaRetiro)
    {
        context.ActasRetiro.Update(actaRetiro);
        await context.SaveChangesAsync();
        return actaRetiro;
    }

    public async Task<bool> ExistsByExpedienteIdAsync(int expedienteId)
    {
        return await context.ActasRetiro
            .AnyAsync(a => a.ExpedienteID == expedienteId);
    }

    public async Task<List<ActaRetiro>> GetByUsuarioAdmisionAsync(int usuarioAdmisionId)
    {
        return await context.ActasRetiro
            .Include(a => a.Expediente)
            .Where(a => a.UsuarioAdmisionID == usuarioAdmisionId)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }
}