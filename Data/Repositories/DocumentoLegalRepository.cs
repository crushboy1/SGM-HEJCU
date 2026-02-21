using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

public class DocumentoLegalRepository(ApplicationDbContext context) : IDocumentoLegalRepository
{
    // ===================================================================
    // CONSULTAS BÁSICAS
    // ===================================================================

    public async Task<DocumentoLegal?> GetByIdAsync(int documentoId)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Include(d => d.UsuarioValidador)
            .Include(d => d.ExpedienteLegal!)
                .ThenInclude(el => el.Expediente)
            .FirstOrDefaultAsync(d => d.DocumentoID == documentoId);
    }

    public async Task<DocumentoLegal?> GetByExpedienteYTipoAsync(int expedienteLegalId, TipoDocumentoLegal tipo)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Include(d => d.UsuarioValidador)
            .FirstOrDefaultAsync(d => d.ExpedienteLegalID == expedienteLegalId && d.TipoDocumento == tipo);
    }

    public async Task<List<DocumentoLegal>> GetByExpedienteLegalIdAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Include(d => d.UsuarioValidador)
            .Where(d => d.ExpedienteLegalID == expedienteLegalId)
            .OrderBy(d => d.TipoDocumento)
            .ToListAsync();
    }

    // ===================================================================
    // CONSULTAS ESPECIALES
    // ===================================================================

    public async Task<List<DocumentoLegal>> GetPendientesAdjuntarAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Where(d => d.ExpedienteLegalID == expedienteLegalId && !d.Adjuntado)
            .OrderBy(d => d.TipoDocumento)
            .ToListAsync();
    }

    public async Task<List<DocumentoLegal>> GetPendientesValidacionAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Where(d => d.ExpedienteLegalID == expedienteLegalId && d.Adjuntado && !d.Validado)
            .OrderBy(d => d.TipoDocumento)
            .ToListAsync();
    }

    public async Task<List<DocumentoLegal>> GetValidadosAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .Include(d => d.UsuarioAdjunto)
            .Include(d => d.UsuarioValidador)
            .Where(d => d.ExpedienteLegalID == expedienteLegalId && d.Validado)
            .OrderBy(d => d.TipoDocumento)
            .ToListAsync();
    }

    // ===================================================================
    // OPERACIONES DE ESCRITURA
    // ===================================================================

    public async Task<DocumentoLegal> CreateAsync(DocumentoLegal documento)
    {
        context.DocumentosLegales.Add(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task<DocumentoLegal> UpdateAsync(DocumentoLegal documento)
    {
        context.DocumentosLegales.Update(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task DeleteAsync(int documentoId)
    {
        var documento = await context.DocumentosLegales.FindAsync(documentoId);
        if (documento is not null)
        {
            context.DocumentosLegales.Remove(documento);
            await context.SaveChangesAsync();
        }
    }

    // ===================================================================
    // VALIDACIONES Y VERIFICACIONES
    // ===================================================================

    public async Task<bool> ExisteDocumentoTipoAsync(int expedienteLegalId, TipoDocumentoLegal tipo)
    {
        return await context.DocumentosLegales
            .AnyAsync(d => d.ExpedienteLegalID == expedienteLegalId && d.TipoDocumento == tipo);
    }

    public async Task<int> CountAdjuntadosAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .CountAsync(d => d.ExpedienteLegalID == expedienteLegalId && d.Adjuntado);
    }

    public async Task<int> CountValidadosAsync(int expedienteLegalId)
    {
        return await context.DocumentosLegales
            .CountAsync(d => d.ExpedienteLegalID == expedienteLegalId && d.Validado);
    }
}