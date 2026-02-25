using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Repositories;

/// <summary>
/// Implementación del repositorio de documentos digitalizados del expediente.
/// </summary>
public class DocumentoExpedienteRepository : IDocumentoExpedienteRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentoExpedienteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<DocumentoExpediente>> GetByExpedienteIdAsync(int expedienteId)
    {
        return await _context.DocumentosExpediente
            .Include(d => d.UsuarioSubio)
            .Include(d => d.UsuarioVerifico)
            .Where(d => d.ExpedienteID == expedienteId)
            .OrderByDescending(d => d.FechaHoraSubida)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<DocumentoExpediente?> GetByIdAsync(int documentoId)
    {
        return await _context.DocumentosExpediente
            .Include(d => d.Expediente)
            .Include(d => d.UsuarioSubio)
            .Include(d => d.UsuarioVerifico)
            .FirstOrDefaultAsync(d => d.DocumentoExpedienteID == documentoId);
    }

    /// <inheritdoc/>
    public async Task<DocumentoExpediente?> GetByExpedienteIdYTipoAsync(
        int expedienteId,
        TipoDocumentoExpediente tipo)
    {
        return await _context.DocumentosExpediente
            .Include(d => d.UsuarioSubio)
            .Include(d => d.UsuarioVerifico)
            .Where(d => d.ExpedienteID == expedienteId && d.TipoDocumento == tipo)
            .OrderByDescending(d => d.FechaHoraSubida)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<List<DocumentoExpediente>> GetVerificadosByExpedienteIdAsync(int expedienteId)
    {
        return await _context.DocumentosExpediente
            .Include(d => d.UsuarioSubio)
            .Include(d => d.UsuarioVerifico)
            .Where(d => d.ExpedienteID == expedienteId
                     && d.Estado == EstadoDocumentoExpediente.Verificado)
            .OrderByDescending(d => d.FechaHoraSubida)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteDocumentoTipoAsync(
        int expedienteId,
        TipoDocumentoExpediente tipo)
    {
        return await _context.DocumentosExpediente
            .AnyAsync(d => d.ExpedienteID == expedienteId
                        && d.TipoDocumento == tipo);
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteDocumentoVerificadoTipoAsync(
        int expedienteId,
        TipoDocumentoExpediente tipo)
    {
        return await _context.DocumentosExpediente
            .AnyAsync(d => d.ExpedienteID == expedienteId
                        && d.TipoDocumento == tipo
                        && d.Estado == EstadoDocumentoExpediente.Verificado);
    }

    /// <inheritdoc/>
    public async Task<DocumentoExpediente> CreateAsync(DocumentoExpediente documento)
    {
        await _context.DocumentosExpediente.AddAsync(documento);
        await _context.SaveChangesAsync();
        return documento;
    }

    /// <inheritdoc/>
    public async Task<DocumentoExpediente> UpdateAsync(DocumentoExpediente documento)
    {
        _context.DocumentosExpediente.Update(documento);
        await _context.SaveChangesAsync();
        return documento;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int documentoId)
    {
        var documento = await _context.DocumentosExpediente
            .FirstOrDefaultAsync(d => d.DocumentoExpedienteID == documentoId)
            ?? throw new KeyNotFoundException(
                $"Documento con ID {documentoId} no encontrado.");

        _context.DocumentosExpediente.Remove(documento);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetCountVerificadosAsync(int expedienteId)
    {
        return await _context.DocumentosExpediente
            .CountAsync(d => d.ExpedienteID == expedienteId
                          && d.Estado == EstadoDocumentoExpediente.Verificado);
    }
}