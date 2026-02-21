using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs.ActaRetiro;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services;

public class ActaRetiroService(
    IActaRetiroRepository actaRetiroRepository,
    IExpedienteRepository expedienteRepository,
    IPdfGeneratorService pdfGeneratorService,
    ILogger<ActaRetiroService> logger) : IActaRetiroService
{
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════
    public async Task<ActaRetiroDTO?> GetByIdAsync(int actaRetiroId)
    {
        var acta = await actaRetiroRepository.GetByIdAsync(actaRetiroId);
        return acta is not null ? MapToDTO(acta) : null;
    }

    public async Task<ActaRetiroDTO?> GetByExpedienteIdAsync(int expedienteId)
    {
        var acta = await actaRetiroRepository.GetByExpedienteIdAsync(expedienteId);
        return acta is not null ? MapToDTO(acta) : null;
    }

    public async Task<List<ActaRetiroDTO>> GetPendientesFirmaAsync()
    {
        var actas = await actaRetiroRepository.GetPendientesFirmaAsync();
        return actas.Select(MapToDTO).ToList();
    }

    public async Task<List<ActaRetiroDTO>> GetByFechaRangoAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var actas = await actaRetiroRepository.GetByFechaRegistroAsync(fechaInicio, fechaFin);
        return actas.Select(MapToDTO).ToList();
    }

    public async Task<bool> ExisteActaParaExpedienteAsync(int expedienteId)
    {
        return await actaRetiroRepository.ExistsByExpedienteIdAsync(expedienteId);
    }
    public async Task<bool> ExisteByCertificadoSINADEFAsync(string numeroCertificado)
    {
        return await actaRetiroRepository.ExisteByCertificadoSINADEFAsync(numeroCertificado);
    }

    public async Task<bool> ExistsByOficioLegalAsync(string numeroOficio)
    {
        return await actaRetiroRepository.ExistsByOficioLegalAsync(numeroOficio);
    }
    // ═══════════════════════════════════════════════════════════
    // CREAR ACTA
    // ═══════════════════════════════════════════════════════════
    public async Task<ActaRetiroDTO> CreateAsync(CreateActaRetiroDTO dto)
    {
        logger.LogInformation(
            "Creando Acta de Retiro para expediente {ExpedienteID}, Tipo: {TipoSalida}",
            dto.ExpedienteID, dto.TipoSalida
        );

        // 1. Validar que el expediente existe
        var expediente = await expedienteRepository.GetByIdAsync(dto.ExpedienteID)
            ?? throw new InvalidOperationException($"Expediente {dto.ExpedienteID} no encontrado");

        // 2. Validar que no exista un acta previa
        if (await actaRetiroRepository.ExistsByExpedienteIdAsync(dto.ExpedienteID))
        {
            throw new InvalidOperationException($"Ya existe un Acta de Retiro para el expediente {dto.ExpedienteID}");
        }
       
        //2.1 VALIDAR UNICIDAD DE DOCUMENTOS LEGALES

        if (dto.TipoSalida == TipoSalida.Familiar)
        {
            // Validar que el certificado SINADEF no esté duplicado
            if (!string.IsNullOrWhiteSpace(dto.NumeroCertificadoDefuncion))
            {
                bool existeCertificado = await actaRetiroRepository
                    .ExisteByCertificadoSINADEFAsync(dto.NumeroCertificadoDefuncion);

                if (existeCertificado)
                {
                    throw new InvalidOperationException(
                        $"Ya existe un acta de retiro con el certificado SINADEF {dto.NumeroCertificadoDefuncion}. " +
                        "Cada certificado debe ser único."
                    );
                }
            }
        }
        else if (dto.TipoSalida == TipoSalida.AutoridadLegal)
        {
            // Validar que el oficio legal no esté duplicado
            if (!string.IsNullOrWhiteSpace(dto.NumeroOficioLegal))
            {
                bool existeOficio = await actaRetiroRepository
                    .ExistsByOficioLegalAsync(dto.NumeroOficioLegal);

                if (existeOficio)
                {
                    throw new InvalidOperationException(
                        $"Ya existe un acta de retiro con el oficio legal {dto.NumeroOficioLegal}. " +
                        "Cada oficio debe ser único."
                    );
                }
            }
        }
        // 3. Validar campos según tipo de salida
        ValidarCamposSegunTipo(dto);

        // 4. Crear entidad
        var acta = new ActaRetiro
        {
            ExpedienteID = dto.ExpedienteID,

            // Documento legal
            NumeroCertificadoDefuncion = dto.NumeroCertificadoDefuncion,
            NumeroOficioLegal = dto.NumeroOficioLegal,

            // Datos del fallecido
            NombreCompletoFallecido = dto.NombreCompletoFallecido,
            HistoriaClinica = dto.HistoriaClinica,
            TipoDocumentoFallecido = dto.TipoDocumentoFallecido,
            NumeroDocumentoFallecido = dto.NumeroDocumentoFallecido,
            ServicioFallecimiento = dto.ServicioFallecimiento,
            FechaHoraFallecimiento = dto.FechaHoraFallecimiento,

            // Médico certificante
            MedicoCertificaNombre = dto.MedicoCertificaNombre,
            MedicoCMP = dto.MedicoCMP,
            MedicoRNE = dto.MedicoRNE,

            // Jefe de Guardia
            JefeGuardiaNombre = dto.JefeGuardiaNombre,
            JefeGuardiaCMP = dto.JefeGuardiaCMP,

            // Tipo de salida
            TipoSalida = dto.TipoSalida,

            // Datos adicionales
            DatosAdicionales = dto.DatosAdicionales,
            Destino = dto.Destino,
            Observaciones = dto.Observaciones,

            // Auditoría
            UsuarioAdmisionID = dto.UsuarioAdmisionID,
            FechaRegistro = DateTime.Now
        };

        // 5. Mapear campos según tipo de salida
        if (dto.TipoSalida == TipoSalida.Familiar)
        {
            acta.FamiliarApellidoPaterno = dto.FamiliarApellidoPaterno;
            acta.FamiliarApellidoMaterno = dto.FamiliarApellidoMaterno;
            acta.FamiliarNombres = dto.FamiliarNombres;
            acta.FamiliarTipoDocumento = dto.FamiliarTipoDocumento;
            acta.FamiliarNumeroDocumento = dto.FamiliarNumeroDocumento;
            acta.FamiliarParentesco = dto.FamiliarParentesco;
            acta.FamiliarTelefono = dto.FamiliarTelefono;

            // Generar nombre completo
            acta.GenerarNombreCompletoFamiliar();
        }
        else if (dto.TipoSalida == TipoSalida.AutoridadLegal)
        {
            acta.AutoridadApellidoPaterno = dto.AutoridadApellidoPaterno;
            acta.AutoridadApellidoMaterno = dto.AutoridadApellidoMaterno;
            acta.AutoridadNombres = dto.AutoridadNombres;
            acta.TipoAutoridad = dto.TipoAutoridad;
            acta.AutoridadTipoDocumento = dto.AutoridadTipoDocumento;
            acta.AutoridadNumeroDocumento = dto.AutoridadNumeroDocumento;
            acta.AutoridadCargo = dto.AutoridadCargo;
            acta.AutoridadInstitucion = dto.AutoridadInstitucion;
            acta.AutoridadPlacaVehiculo = dto.AutoridadPlacaVehiculo;
            acta.AutoridadTelefono = dto.AutoridadTelefono;

            // Generar nombre completo
            acta.GenerarNombreCompletoAutoridad();
        }

        // 6. Validar completitud
        var validacion = acta.ValidarParaGenerarPDF();
        if (validacion != "OK")
        {
            throw new InvalidOperationException($"Validación fallida: {validacion}");
        }

        // 7. Guardar
        var actaCreada = await actaRetiroRepository.CreateAsync(acta);

        logger.LogInformation(
            "Acta de Retiro {ActaRetiroID} creada exitosamente para {TipoSalida}",
            actaCreada.ActaRetiroID, dto.TipoSalida
        );

        // 8. Recargar con navegaciones
        var actaCompleta = await actaRetiroRepository.GetByIdAsync(actaCreada.ActaRetiroID);
        return MapToDTO(actaCompleta!);
    }

    // ═══════════════════════════════════════════════════════════
    // GENERAR PDF
    // ═══════════════════════════════════════════════════════════
    public async Task<(byte[] PdfBytes, string FileName)> GenerarPDFSinFirmarAsync(int actaRetiroId)
    {
        logger.LogInformation("Generando PDF sin firmar para Acta {ActaRetiroID}", actaRetiroId);

        var acta = await actaRetiroRepository.GetByIdAsync(actaRetiroId)
            ?? throw new InvalidOperationException($"Acta de Retiro {actaRetiroId} no encontrada");

        // Validar que el acta está completa
        var validacion = acta.ValidarParaGenerarPDF();
        if (validacion != "OK")
        {
            throw new InvalidOperationException($"No se puede generar PDF: {validacion}");
        }

        // GENERAR PDF CON SERVICIO COMPARTIDO
        var pdfBytes = pdfGeneratorService.GenerarActaRetiro(acta);
        var fileName = $"ActaRetiro_{acta.Expediente?.CodigoExpediente ?? actaRetiroId.ToString()}.pdf";

        // Guardar en disco (opcional, para auditoría)
        var directorio = Path.Combine("wwwroot", "documentos-legales", "actas-retiro");
        Directory.CreateDirectory(directorio);

        var rutaCompleta = Path.Combine(directorio, fileName);
        await File.WriteAllBytesAsync(rutaCompleta, pdfBytes);

        // Actualizar ruta en BD
        var rutaPDF = $"documentos-legales/actas-retiro/{fileName}";
        acta.RutaPDFSinFirmar = rutaPDF;
        acta.NombreArchivoPDFSinFirmar = fileName;
        acta.TamañoPDFSinFirmar = pdfBytes.Length;

        await actaRetiroRepository.UpdateAsync(acta);

        logger.LogInformation("PDF sin firmar generado: {FileName} ({Size} bytes)", fileName, pdfBytes.Length);

        return (pdfBytes, fileName);
    }

    // ═══════════════════════════════════════════════════════════
    // SUBIR PDF FIRMADO
    // ═══════════════════════════════════════════════════════════
    public async Task<ActaRetiroDTO> SubirPDFFirmadoAsync(UpdateActaRetiroPDFDTO dto)
    {
        logger.LogInformation("Subiendo PDF firmado para Acta {ActaRetiroID}", dto.ActaRetiroID);

        var acta = await actaRetiroRepository.GetByIdAsync(dto.ActaRetiroID)
            ?? throw new InvalidOperationException($"Acta de Retiro {dto.ActaRetiroID} no encontrada");

        // Actualizar datos del PDF firmado
        acta.RutaPDFFirmado = dto.RutaPDFFirmado;
        acta.NombreArchivoPDFFirmado = dto.NombreArchivoPDFFirmado;
        acta.TamañoPDFFirmado = dto.TamañoPDFFirmado;

        // Marcar como firmado completo (usa FirmadoResponsable internamente)
        acta.MarcarFirmadoCompleto(dto.UsuarioSubidaPDFID);

        if (!string.IsNullOrWhiteSpace(dto.Observaciones))
        {
            acta.Observaciones = dto.Observaciones;
        }

        await actaRetiroRepository.UpdateAsync(acta);

        logger.LogInformation("PDF firmado subido exitosamente para Acta {ActaRetiroID}", dto.ActaRetiroID);

        // Recargar con navegaciones
        var actaActualizada = await actaRetiroRepository.GetByIdAsync(dto.ActaRetiroID);
        return MapToDTO(actaActualizada!);
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDACIONES PRIVADAS
    // ═══════════════════════════════════════════════════════════
    private static void ValidarCamposSegunTipo(CreateActaRetiroDTO dto)
    {
        if (dto.TipoSalida == TipoSalida.Familiar)
        {
            // Validar campos de Familiar
            if (string.IsNullOrWhiteSpace(dto.NumeroCertificadoDefuncion))
                throw new InvalidOperationException("El N° de Certificado SINADEF es obligatorio para retiros por familiar");

            if (string.IsNullOrWhiteSpace(dto.FamiliarApellidoPaterno))
                throw new InvalidOperationException("El apellido paterno del familiar es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.FamiliarNombres))
                throw new InvalidOperationException("Los nombres del familiar son obligatorios");

            if (string.IsNullOrWhiteSpace(dto.FamiliarNumeroDocumento))
                throw new InvalidOperationException("El documento del familiar es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.FamiliarParentesco))
                throw new InvalidOperationException("El parentesco es obligatorio");

            if (dto.FamiliarTipoDocumento == null)
                throw new InvalidOperationException("El tipo de documento del familiar es obligatorio");
        }
        else if (dto.TipoSalida == TipoSalida.AutoridadLegal)
        {
            // Validar campos de Autoridad Legal
            if (string.IsNullOrWhiteSpace(dto.NumeroOficioLegal))
                throw new InvalidOperationException("El N° de Oficio Legal es obligatorio para retiros por autoridades");

            if (string.IsNullOrWhiteSpace(dto.AutoridadApellidoPaterno))
                throw new InvalidOperationException("El apellido paterno de la autoridad es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.AutoridadNombres))
                throw new InvalidOperationException("Los nombres de la autoridad son obligatorios");

            if (string.IsNullOrWhiteSpace(dto.AutoridadNumeroDocumento))
                throw new InvalidOperationException("El documento de la autoridad es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.AutoridadInstitucion))
                throw new InvalidOperationException("La institución de la autoridad es obligatoria");

            if (dto.TipoAutoridad == null)
                throw new InvalidOperationException("El tipo de autoridad es obligatorio (PNP, Fiscal, Médico Legista)");

            if (dto.AutoridadTipoDocumento == null)
                throw new InvalidOperationException("El tipo de documento de la autoridad es obligatorio");
        }
    }
    // ═══════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════
    private static ActaRetiroDTO MapToDTO(ActaRetiro acta)
    {
        return new ActaRetiroDTO
        {
            ActaRetiroID = acta.ActaRetiroID,
            ExpedienteID = acta.ExpedienteID,
            CodigoExpediente = acta.Expediente?.CodigoExpediente ?? string.Empty,

            // Documento legal
            NumeroCertificadoDefuncion = acta.NumeroCertificadoDefuncion,
            NumeroOficioLegal = acta.NumeroOficioLegal,

            // Datos del fallecido
            NombreCompletoFallecido = acta.NombreCompletoFallecido ?? string.Empty,
            HistoriaClinica = acta.HistoriaClinica,
            TipoDocumentoFallecido = acta.TipoDocumentoFallecido.ToString(),
            NumeroDocumentoFallecido = acta.NumeroDocumentoFallecido,
            ServicioFallecimiento = acta.ServicioFallecimiento,
            FechaHoraFallecimiento = acta.FechaHoraFallecimiento,

            // Médico certificante
            MedicoCertificaNombre = acta.MedicoCertificaNombre,
            MedicoCMP = acta.MedicoCMP,
            MedicoRNE = acta.MedicoRNE,

            // Jefe de Guardia
            JefeGuardiaNombre = acta.JefeGuardiaNombre,
            JefeGuardiaCMP = acta.JefeGuardiaCMP,

            // Tipo de salida
            TipoSalida = acta.TipoSalida.ToString(),

            // Familiar
            FamiliarApellidoPaterno = acta.FamiliarApellidoPaterno,
            FamiliarApellidoMaterno = acta.FamiliarApellidoMaterno,
            FamiliarNombres = acta.FamiliarNombres,
            FamiliarNombreCompleto = acta.FamiliarNombreCompleto,
            FamiliarTipoDocumento = acta.FamiliarTipoDocumento?.ToString(),
            FamiliarNumeroDocumento = acta.FamiliarNumeroDocumento,
            FamiliarParentesco = acta.FamiliarParentesco,
            FamiliarTelefono = acta.FamiliarTelefono,

            // Autoridad Legal
            AutoridadApellidoPaterno = acta.AutoridadApellidoPaterno,
            AutoridadApellidoMaterno = acta.AutoridadApellidoMaterno,
            AutoridadNombres = acta.AutoridadNombres,
            AutoridadNombreCompleto = acta.AutoridadNombreCompleto,
            TipoAutoridad = acta.TipoAutoridad?.ToString(),
            AutoridadTipoDocumento = acta.AutoridadTipoDocumento?.ToString(),
            AutoridadNumeroDocumento = acta.AutoridadNumeroDocumento,
            AutoridadCargo = acta.AutoridadCargo,
            AutoridadInstitucion = acta.AutoridadInstitucion,
            AutoridadPlacaVehiculo = acta.AutoridadPlacaVehiculo,
            AutoridadTelefono = acta.AutoridadTelefono,

            // Datos adicionales
            DatosAdicionales = acta.DatosAdicionales,
            Destino = acta.Destino,

            // Firmas (usando campos actualizados)
            FirmadoResponsable = acta.FirmadoResponsable,
            FechaFirmaResponsable = acta.FechaFirmaResponsable,
            FirmadoAdmisionista = acta.FirmadoAdmisionista,
            FechaFirmaAdmisionista = acta.FechaFirmaAdmisionista,
            FirmadoSupervisorVigilancia = acta.FirmadoSupervisorVigilancia,
            FechaSupervisorVigilancia = acta.FechaSupervisorVigilancia,

            // Helper para UI
            NombreResponsableFirma = acta.ObtenerNombreResponsableFirma(),

            // PDFs
            RutaPDFSinFirmar = acta.RutaPDFSinFirmar,
            NombreArchivoPDFSinFirmar = acta.NombreArchivoPDFSinFirmar,
            TamañoPDFSinFirmarLegible = acta.ObtenerTamañoPDFSinFirmarLegible(),

            RutaPDFFirmado = acta.RutaPDFFirmado,
            NombreArchivoPDFFirmado = acta.NombreArchivoPDFFirmado,
            TamañoPDFFirmadoLegible = acta.ObtenerTamañoPDFFirmadoLegible(),

            // Estado
            EstaCompleta = acta.EstaCompleta(),
            TieneTodasLasFirmas = acta.TieneTodasLasFirmas(),
            TienePDFFirmado = acta.TienePDFFirmado(),

            // Observaciones
            Observaciones = acta.Observaciones,

            // Auditoría
            UsuarioAdmisionNombre = acta.UsuarioAdmision?.NombreCompleto ?? string.Empty,
            FechaRegistro = acta.FechaRegistro,
            UsuarioSubidaPDFNombre = acta.UsuarioSubidaPDF?.NombreCompleto,
            FechaSubidaPDF = acta.FechaSubidaPDF,

            // Salida asociada
            SalidaMortuorioID = acta.SalidaMortuorio?.SalidaID,
            FechaHoraSalida = acta.SalidaMortuorio?.FechaHoraSalida
        };
    }
}