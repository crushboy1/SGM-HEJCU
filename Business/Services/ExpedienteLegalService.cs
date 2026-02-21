using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs.ExpedienteLegal;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services;

public class ExpedienteLegalService(
    IExpedienteLegalRepository expedienteLegalRepo,
    IDocumentoLegalRepository documentoLegalRepo,
    IAutoridadExternaRepository autoridadExternaRepo,
    IExpedienteRepository expedienteRepo,
    ApplicationDbContext context, // Solo para AuditLogs y Users
    ILogger<ExpedienteLegalService> logger,
    IHubContext<SgmHub, ISgmClient> hubContext) : IExpedienteLegalService
{
    // ===================================================================
    // CREAR EXPEDIENTE LEGAL
    // ===================================================================

    public async Task<ExpedienteLegalDTO> CrearExpedienteLegalAsync(CreateExpedienteLegalDTO dto)
    {
        var expediente = await expedienteRepo.GetByIdAsync(dto.ExpedienteID)
            ?? throw new KeyNotFoundException($"Expediente {dto.ExpedienteID} no encontrado");

        if (await expedienteLegalRepo.ExistsByExpedienteIdAsync(dto.ExpedienteID))
            throw new InvalidOperationException($"Ya existe un expediente legal para el expediente {dto.ExpedienteID}");

        _ = await context.Users.FindAsync(dto.UsuarioRegistroID)
            ?? throw new KeyNotFoundException($"Usuario {dto.UsuarioRegistroID} no encontrado");

        var expedienteLegal = new ExpedienteLegal
        {
            ExpedienteID = dto.ExpedienteID,
            NumeroOficioPNP = dto.NumeroActaPolicial,
            Observaciones = dto.Observaciones,
            Estado = EstadoExpedienteLegal.EnRegistro,
            UsuarioRegistroID = dto.UsuarioRegistroID,
            FechaCreacion = DateTime.Now
        };

        var creado = await expedienteLegalRepo.CreateAsync(expedienteLegal);

        logger.LogInformation(
            "Expediente legal {ExpedienteLegalID} creado para expediente {CodigoExpediente} por Usuario ID {UsuarioID}. Estado: EnRegistro",
            creado.ExpedienteLegalID, expediente.CodigoExpediente, dto.UsuarioRegistroID
        );

        await EnviarNotificacion(
            titulo: "Nuevo Expediente Legal Registrado",
            mensaje: $"Expediente {expediente.CodigoExpediente} registrado como caso externo. En proceso de completar documentación",
            roles: ["VigilanteSupervisor", "Administrador"],
            tipo: "info",
            categoriaNotificacion: "expediente_legal_creado",
            expedienteId: expediente.ExpedienteID,
            codigoExpediente: expediente.CodigoExpediente,
            estadoAnterior: null,
            estadoNuevo: "EnRegistro",
            accionSugerida: "Ver Expediente Legal",
            urlNavegacion: $"/expediente-legal/{creado.ExpedienteLegalID}",
            requiereAccion: false
        );

        await RegistrarAuditoriaAsync(
            "CrearExpedienteLegal",
            dto.UsuarioRegistroID,
            dto.ExpedienteID,
            null,
            new { ExpedienteLegalID = creado.ExpedienteLegalID, Estado = "EnRegistro" }
        );

        return MapToDTO(expedienteLegal);
    }

    // ===================================================================
    // CONSULTAS
    // ===================================================================

    public async Task<ExpedienteLegalDTO?> ObtenerPorIdAsync(int expedienteLegalId)
    {
        var expedienteLegal = await expedienteLegalRepo.GetCompletoByIdAsync(expedienteLegalId);
        return expedienteLegal is not null ? MapToDTO(expedienteLegal) : null;
    }

    public async Task<ExpedienteLegalDTO?> ObtenerPorExpedienteIdAsync(int expedienteId)
    {
        var expedienteLegal = await expedienteLegalRepo.GetByExpedienteIdAsync(expedienteId);
        return expedienteLegal is not null ? MapToDTO(expedienteLegal) : null;
    }

    // ===================================================================
    // ACTUALIZAR OBSERVACIONES
    // ===================================================================

    public async Task<ExpedienteLegalDTO> ActualizarObservacionesAsync(int expedienteLegalId, string observaciones, int usuarioId)
    {
        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(expedienteLegalId)
            ?? throw new KeyNotFoundException($"Expediente legal {expedienteLegalId} no encontrado");

        var observacionesAnterior = expedienteLegal.Observaciones;
        expedienteLegal.Observaciones = observaciones;
        expedienteLegal.UsuarioActualizacionID = usuarioId;
        expedienteLegal.FechaUltimaActualizacion = DateTime.Now;

        await expedienteLegalRepo.UpdateAsync(expedienteLegal);

        logger.LogInformation(
            "Observaciones actualizadas para ExpedienteLegal {ExpedienteLegalID} por Usuario ID {UsuarioID}",
            expedienteLegalId, usuarioId
        );

        await RegistrarAuditoriaAsync(
            "ActualizarObservaciones",
            usuarioId,
            expedienteLegal.ExpedienteID,
            new { ObservacionesAnterior = observacionesAnterior },
            new { ObservacionesNuevo = observaciones }
        );

        return MapToDTO(expedienteLegal);
    }

    // ===================================================================
    // MARCAR LISTO PARA ADMISIÓN
    // ===================================================================

    public async Task<ExpedienteLegalDTO> MarcarListoAdmisionAsync(MarcarListoAdmisionDTO dto)
    {
        var expedienteLegal = await expedienteLegalRepo.GetCompletoByIdAsync(dto.ExpedienteLegalID)
            ?? throw new KeyNotFoundException($"Expediente legal {dto.ExpedienteLegalID} no encontrado");

        if (expedienteLegal.Estado != EstadoExpedienteLegal.EnRegistro)
            throw new InvalidOperationException($"El expediente legal no está en estado EnRegistro. Estado actual: {expedienteLegal.Estado}");

        if (!expedienteLegal.DocumentosCompletos)
            throw new InvalidOperationException("No se puede marcar como listo. Documentos incompletos");

        var estadoAnterior = expedienteLegal.Estado;
        expedienteLegal.Estado = EstadoExpedienteLegal.PendienteValidacionAdmision;
        expedienteLegal.Observaciones = dto.Observaciones ?? expedienteLegal.Observaciones;
        expedienteLegal.FechaUltimaActualizacion = DateTime.Now;

        await expedienteLegalRepo.UpdateAsync(expedienteLegal);

        logger.LogInformation(
            "ExpedienteLegal {ExpedienteLegalID} marcado como listo para Admisión. {Estado} → PendienteValidacionAdmision",
            dto.ExpedienteLegalID, estadoAnterior
        );

        await EnviarNotificacion(
            titulo: "Expediente Legal Listo para Validación",
            mensaje: $"Expediente {expedienteLegal.Expediente?.CodigoExpediente ?? "N/A"} completó documentación y está listo para validación de Admisión",
            roles: ["Admision", "Administrador"],
            tipo: "info",
            categoriaNotificacion: "expediente_legal_listo_admision",
            expedienteId: expedienteLegal.ExpedienteID,
            codigoExpediente: expedienteLegal.Expediente?.CodigoExpediente,
            estadoAnterior: estadoAnterior.ToString(),
            estadoNuevo: "PendienteValidacionAdmision",
            accionSugerida: "Validar Documentación",
            urlNavegacion: $"/expediente-legal/{dto.ExpedienteLegalID}",
            requiereAccion: true
        );

        await RegistrarAuditoriaAsync(
            "MarcarListoParaAdmision",
            expedienteLegal.UsuarioRegistroID,
            expedienteLegal.ExpedienteID,
            new { EstadoAnterior = estadoAnterior },
            new { EstadoNuevo = expedienteLegal.Estado }
        );

        return MapToDTO(expedienteLegal);
    }

    // ===================================================================
    // VALIDAR POR ADMISIÓN
    // ===================================================================

    public async Task<ExpedienteLegalDTO> ValidarPorAdmisionAsync(ValidarDocumentacionAdmisionDTO dto)
    {
        var expedienteLegal = await expedienteLegalRepo.GetCompletoByIdAsync(dto.ExpedienteLegalID)
            ?? throw new KeyNotFoundException($"Expediente legal {dto.ExpedienteLegalID} no encontrado");

        if (expedienteLegal.Estado != EstadoExpedienteLegal.PendienteValidacionAdmision)
            throw new InvalidOperationException($"El expediente legal no está pendiente de validación. Estado actual: {expedienteLegal.Estado}");

        _ = await context.Users.FindAsync(dto.UsuarioAdmisionID)
            ?? throw new KeyNotFoundException($"Usuario Admisión {dto.UsuarioAdmisionID} no encontrado");

        var estadoAnterior = expedienteLegal.Estado;

        expedienteLegal.ValidadoAdmision = dto.Aprobado;
        expedienteLegal.FechaValidacionAdmision = DateTime.Now;
        expedienteLegal.UsuarioAdmisionID = dto.UsuarioAdmisionID;
        expedienteLegal.ObservacionesAdmision = dto.Observaciones;
        expedienteLegal.Estado = dto.Aprobado ? EstadoExpedienteLegal.ValidadoAdmision : EstadoExpedienteLegal.RechazadoAdmision;
        expedienteLegal.UsuarioActualizacionID = dto.UsuarioAdmisionID;
        expedienteLegal.FechaUltimaActualizacion = DateTime.Now;

        await expedienteLegalRepo.UpdateAsync(expedienteLegal);

        logger.LogInformation(
            "ExpedienteLegal {ExpedienteLegalID} {Accion} por Admisión - Usuario ID {UsuarioID}. {EstadoAnterior} → {EstadoNuevo}",
            dto.ExpedienteLegalID, dto.Aprobado ? "APROBADO" : "RECHAZADO", dto.UsuarioAdmisionID, estadoAnterior, expedienteLegal.Estado
        );

        if (dto.Aprobado)
        {
            await EnviarNotificacion(
                titulo: "Expediente Legal Validado por Admisión",
                mensaje: $"Expediente {expedienteLegal.Expediente?.CodigoExpediente ?? "N/A"} validado exitosamente. Pendiente de autorización Jefe Guardia",
                roles: ["JefeGuardia", "Administrador"],
                tipo: "success",
                categoriaNotificacion: "expediente_legal_validado_admision",
                expedienteId: expedienteLegal.ExpedienteID,
                codigoExpediente: expedienteLegal.Expediente?.CodigoExpediente,
                estadoAnterior: estadoAnterior.ToString(),
                estadoNuevo: "ValidadoAdmision",
                accionSugerida: "Autorizar Levantamiento",
                urlNavegacion: $"/expediente-legal/{dto.ExpedienteLegalID}",
                requiereAccion: true
            );
        }
        else
        {
            await EnviarNotificacion(
                titulo: "Expediente Legal Rechazado por Admisión",
                mensaje: $"Expediente {expedienteLegal.Expediente?.CodigoExpediente ?? "N/A"} rechazado. Requiere corrección: {dto.Observaciones}",
                roles: ["VigilanteSupervisor", "Administrador"],
                tipo: "warning",
                categoriaNotificacion: "expediente_legal_rechazado_admision",
                expedienteId: expedienteLegal.ExpedienteID,
                codigoExpediente: expedienteLegal.Expediente?.CodigoExpediente,
                estadoAnterior: estadoAnterior.ToString(),
                estadoNuevo: "RechazadoAdmision",
                accionSugerida: "Corregir Documentación",
                urlNavegacion: $"/expediente-legal/{dto.ExpedienteLegalID}",
                requiereAccion: true
            );
        }

        await RegistrarAuditoriaAsync(
            dto.Aprobado ? "ValidarAdmision" : "RechazarAdmision",
            dto.UsuarioAdmisionID,
            expedienteLegal.ExpedienteID,
            new { EstadoAnterior = estadoAnterior },
            new { EstadoNuevo = expedienteLegal.Estado, Observaciones = dto.Observaciones }
        );

        return MapToDTO(expedienteLegal);
    }

    // ===================================================================
    // AUTORIZAR POR JEFE GUARDIA
    // ===================================================================

    public async Task<ExpedienteLegalDTO> AutorizarPorJefeGuardiaAsync(ValidarExpedienteLegalDTO dto)
    {
        var expedienteLegal = await expedienteLegalRepo.GetCompletoByIdAsync(dto.ExpedienteLegalID)
            ?? throw new KeyNotFoundException($"Expediente legal {dto.ExpedienteLegalID} no encontrado");

        if (expedienteLegal.Estado != EstadoExpedienteLegal.ValidadoAdmision)
            throw new InvalidOperationException($"El expediente legal no está validado por Admisión. Estado actual: {expedienteLegal.Estado}");

        _ = await context.Users.FindAsync(dto.JefeGuardiaID)
            ?? throw new KeyNotFoundException($"Jefe de Guardia {dto.JefeGuardiaID} no encontrado");

        var estadoAnterior = expedienteLegal.Estado;

        expedienteLegal.AutorizadoJefeGuardia = dto.Validado;
        expedienteLegal.FechaAutorizacion = DateTime.Now;
        expedienteLegal.JefeGuardiaID = dto.JefeGuardiaID;
        expedienteLegal.ObservacionesJefeGuardia = dto.ObservacionesValidacion;
        expedienteLegal.Estado = dto.Validado ? EstadoExpedienteLegal.AutorizadoJefeGuardia : EstadoExpedienteLegal.RechazadoAdmision;
        expedienteLegal.UsuarioActualizacionID = dto.JefeGuardiaID;
        expedienteLegal.FechaUltimaActualizacion = DateTime.Now;

        await expedienteLegalRepo.UpdateAsync(expedienteLegal);

        logger.LogInformation(
            "ExpedienteLegal {ExpedienteLegalID} {Accion} por Jefe Guardia - Usuario ID {UsuarioID}. {EstadoAnterior} → {EstadoNuevo}",
            dto.ExpedienteLegalID, dto.Validado ? "AUTORIZADO" : "RECHAZADO", dto.JefeGuardiaID, estadoAnterior, expedienteLegal.Estado
        );

        if (dto.Validado)
        {
            await EnviarNotificacion(
                titulo: "Expediente Legal Autorizado - Listo para Levantamiento",
                mensaje: $"Expediente {expedienteLegal.Expediente?.CodigoExpediente ?? "N/A"} autorizado por Jefe de Guardia. Puede proceder con levantamiento de cadáver",
                roles: ["VigilanciaMortuorio", "Administrador"],
                tipo: "success",
                categoriaNotificacion: "expediente_legal_autorizado_jefe_guardia",
                expedienteId: expedienteLegal.ExpedienteID,
                codigoExpediente: expedienteLegal.Expediente?.CodigoExpediente,
                estadoAnterior: estadoAnterior.ToString(),
                estadoNuevo: "AutorizadoJefeGuardia",
                accionSugerida: "Coordinar Levantamiento",
                urlNavegacion: $"/expediente-legal/{dto.ExpedienteLegalID}",
                requiereAccion: false
            );
        }
        else
        {
            await EnviarNotificacion(
                titulo: "Expediente Legal Rechazado por Jefe Guardia",
                mensaje: $"Expediente {expedienteLegal.Expediente?.CodigoExpediente ?? "N/A"} rechazado. Devuelto a Admisión: {dto.ObservacionesValidacion}",
                roles: ["Admision", "Administrador"],
                tipo: "warning",
                categoriaNotificacion: "expediente_legal_rechazado_jefe_guardia",
                expedienteId: expedienteLegal.ExpedienteID,
                codigoExpediente: expedienteLegal.Expediente?.CodigoExpediente,
                estadoAnterior: estadoAnterior.ToString(),
                estadoNuevo: "RechazadoAdmision",
                accionSugerida: "Revisar y Reenviar",
                urlNavegacion: $"/expediente-legal/{dto.ExpedienteLegalID}",
                requiereAccion: true
            );
        }

        await RegistrarAuditoriaAsync(
            dto.Validado ? "AutorizarJefeGuardia" : "RechazarJefeGuardia",
            dto.JefeGuardiaID,
            expedienteLegal.ExpedienteID,
            new { EstadoAnterior = estadoAnterior },
            new { EstadoNuevo = expedienteLegal.Estado, Observaciones = dto.ObservacionesValidacion }
        );

        return MapToDTO(expedienteLegal);
    }

    // ===================================================================
    // AUTORIDADES
    // ===================================================================

    public async Task<AutoridadExternaDTO> RegistrarAutoridadAsync(CreateAutoridadExternaDTO dto)
    {
        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(dto.ExpedienteLegalID)
            ?? throw new KeyNotFoundException($"Expediente legal {dto.ExpedienteLegalID} no encontrado");

        _ = await context.Users.FindAsync(dto.UsuarioRegistroID)
            ?? throw new KeyNotFoundException($"Usuario {dto.UsuarioRegistroID} no encontrado");

        if (!Enum.TryParse<TipoAutoridadExterna>(dto.TipoAutoridad, out var tipoAutoridad))
            throw new ArgumentException($"Tipo de autoridad inválido: {dto.TipoAutoridad}");

        if (!Enum.TryParse<TipoDocumentoIdentidad>(dto.TipoDocumento, out var tipoDocumento))
            throw new ArgumentException($"Tipo de documento inválido: {dto.TipoDocumento}");

        var autoridad = new AutoridadExterna
        {
            ExpedienteLegalID = dto.ExpedienteLegalID,
            TipoAutoridad = tipoAutoridad,
            ApellidoPaterno = dto.ApellidoPaterno,
            ApellidoMaterno = dto.ApellidoMaterno,
            Nombres = dto.Nombres,
            TipoDocumento = tipoDocumento,
            NumeroDocumento = dto.NumeroDocumento,
            Institucion = dto.Institucion ?? string.Empty,
            Cargo = dto.Cargo,
            CodigoEspecial = dto.CodigoEspecial,
            PlacaVehiculo = dto.PlacaVehiculo,
            Telefono = dto.Telefono,
            UsuarioRegistroID = dto.UsuarioRegistroID,
            FechaRegistro = DateTime.Now
        };

        await expedienteLegalRepo.AddAutoridadAsync(autoridad);

        logger.LogInformation(
            "Autoridad externa {TipoAutoridad} registrada para ExpedienteLegal {ExpedienteLegalID} - {NombreCompleto}",
            tipoAutoridad, dto.ExpedienteLegalID, $"{dto.Nombres} {dto.ApellidoPaterno}"
        );

        await RegistrarAuditoriaAsync(
            "RegistrarAutoridad",
            dto.UsuarioRegistroID,
            expedienteLegal.ExpedienteID,
            null,
            new { AutoridadID = autoridad.AutoridadID, TipoAutoridad = tipoAutoridad, Nombre = $"{dto.Nombres} {dto.ApellidoPaterno}" }
        );

        return MapToAutoridadDTO(autoridad);
    }

    public async Task<List<AutoridadExternaDTO>> ObtenerAutoridadesAsync(int expedienteLegalId)
    {
        var autoridades = await autoridadExternaRepo.GetByExpedienteLegalIdAsync(expedienteLegalId);
        return autoridades.Select(MapToAutoridadDTO).ToList();
    }

    public async Task EliminarAutoridadAsync(int autoridadId)
    {
        var autoridad = await autoridadExternaRepo.GetByIdAsync(autoridadId)
            ?? throw new KeyNotFoundException($"Autoridad {autoridadId} no encontrada");

        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(autoridad.ExpedienteLegalID ?? 0)
            ?? throw new KeyNotFoundException("Expediente legal no encontrado");

        if (expedienteLegal.Estado != EstadoExpedienteLegal.EnRegistro)
            throw new InvalidOperationException($"No se puede eliminar autoridades. El expediente está en estado {expedienteLegal.Estado}");

        await autoridadExternaRepo.DeleteAsync(autoridadId);

        logger.LogInformation(
            "Autoridad {AutoridadID} eliminada del ExpedienteLegal {ExpedienteLegalID}",
            autoridadId, autoridad.ExpedienteLegalID
        );

        await RegistrarAuditoriaAsync(
            "EliminarAutoridad",
            autoridad.UsuarioRegistroID,
            expedienteLegal.ExpedienteID,
            new { AutoridadID = autoridadId, Nombre = $"{autoridad.Nombres} {autoridad.ApellidoPaterno}" },
            null
        );
    }

    // ===================================================================
    // DOCUMENTOS
    // ===================================================================

    public async Task<DocumentoLegalDTO> RegistrarDocumentoAsync(CreateDocumentoLegalDTO dto)
    {
        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(dto.ExpedienteLegalID)
            ?? throw new KeyNotFoundException($"Expediente legal {dto.ExpedienteLegalID} no encontrado");

        _ = await context.Users.FindAsync(dto.UsuarioSubeID)
            ?? throw new KeyNotFoundException($"Usuario {dto.UsuarioSubeID} no encontrado");

        if (!Enum.TryParse<TipoDocumentoLegal>(dto.TipoDocumento, out var tipoDocumento))
            throw new ArgumentException($"Tipo de documento inválido: {dto.TipoDocumento}");

        if (await documentoLegalRepo.ExisteDocumentoTipoAsync(dto.ExpedienteLegalID, tipoDocumento))
            throw new InvalidOperationException($"Ya existe un documento de tipo {tipoDocumento} para este expediente legal");

        var documento = new DocumentoLegal
        {
            ExpedienteLegalID = dto.ExpedienteLegalID,
            TipoDocumento = tipoDocumento,
            UsuarioAdjuntoID = dto.UsuarioSubeID,
            FechaAdjunto = DateTime.Now
        };

        await documentoLegalRepo.CreateAsync(documento);

        expedienteLegal.FechaUltimaActualizacion = DateTime.Now;
        await expedienteLegalRepo.UpdateAsync(expedienteLegal);

        logger.LogInformation(
            "Documento legal {TipoDocumento} registrado para ExpedienteLegal {ExpedienteLegalID}",
            tipoDocumento, dto.ExpedienteLegalID
        );

        await RegistrarAuditoriaAsync(
            "RegistrarDocumento",
            dto.UsuarioSubeID,
            expedienteLegal.ExpedienteID,
            null,
            new { DocumentoID = documento.DocumentoID, TipoDocumento = tipoDocumento }
        );

        return MapToDocumentoDTO(documento);
    }

    public async Task<DocumentoLegalDTO?> ObtenerDocumentoAsync(int expedienteLegalId, int documentoId)
    {
        var documento = await documentoLegalRepo.GetByIdAsync(documentoId);

        if (documento is null || documento.ExpedienteLegalID != expedienteLegalId)
            return null;

        return MapToDocumentoDTO(documento);
    }

    public async Task<DocumentoLegalDTO> ActualizarRutaArchivoAsync(
        int expedienteLegalId,
        int documentoId,
        string? rutaArchivo,
        string? nombreArchivo,
        long? tamañoArchivo,
        int usuarioActualizaId)
    {
        var documento = await documentoLegalRepo.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

        if (documento.ExpedienteLegalID != expedienteLegalId)
            throw new InvalidOperationException($"Documento {documentoId} no pertenece al expediente legal {expedienteLegalId}");

        documento.RutaArchivo = rutaArchivo ?? "";
        documento.NombreArchivo = nombreArchivo ?? "";
        documento.TamañoArchivo = tamañoArchivo ?? 0;
        documento.Adjuntado = !string.IsNullOrEmpty(rutaArchivo) && documento.TamañoArchivo > 0;

        if (documento.Adjuntado)
        {
            documento.UsuarioAdjuntoID = usuarioActualizaId;
            documento.FechaAdjunto = DateTime.Now;
        }

        await documentoLegalRepo.UpdateAsync(documento);

        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(expedienteLegalId);
        if (expedienteLegal is not null)
        {
            expedienteLegal.FechaUltimaActualizacion = DateTime.Now;

            if (expedienteLegal.Estado == EstadoExpedienteLegal.RechazadoAdmision && documento.Adjuntado)
            {
                expedienteLegal.Estado = EstadoExpedienteLegal.EnRegistro;
            }

            await expedienteLegalRepo.UpdateAsync(expedienteLegal);
        }

        logger.LogInformation(
            "Archivo actualizado DocID:{DocID}. Ruta:{Ruta}, Tamaño:{Size}, Estado:{Estado}",
            documentoId, rutaArchivo, documento.TamañoArchivo, expedienteLegal?.Estado
        );

        await RegistrarAuditoriaAsync(
            rutaArchivo is not null ? "ActualizarArchivoDocumento" : "EliminarArchivoDocumento",
            usuarioActualizaId,
            expedienteLegalId,
            new { DocumentoID = documentoId },
            new { RutaArchivo = rutaArchivo, NombreArchivo = nombreArchivo, TamañoArchivo = tamañoArchivo }
        );

        return MapToDocumentoDTO(documento);
    }

    public async Task<List<DocumentoLegalDTO>> ObtenerDocumentosAsync(int expedienteLegalId)
    {
        var documentos = await documentoLegalRepo.GetByExpedienteLegalIdAsync(expedienteLegalId);
        return documentos.Select(MapToDocumentoDTO).ToList();
    }

    // ===================================================================
    // CONSULTAS POR ESTADO
    // ===================================================================

    public async Task<List<ExpedienteLegalDTO>> ObtenerEnRegistroAsync()
    {
        var expedientes = await expedienteLegalRepo.GetEnRegistroAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerPendientesAdmisionAsync()
    {
        var expedientes = await expedienteLegalRepo.GetPendientesValidacionAdmisionAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerRechazadosAdmisionAsync()
    {
        var expedientes = await expedienteLegalRepo.GetRechazadosAdmisionAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerPendientesJefeGuardiaAsync()
    {
        var expedientes = await expedienteLegalRepo.GetPendientesAutorizacionJefeGuardiaAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerAutorizadosAsync()
    {
        var expedientes = await expedienteLegalRepo.GetAutorizadosAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerConDocumentosIncompletosAsync()
    {
        var expedientes = await expedienteLegalRepo.GetConDocumentosIncompletosAsync();
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ObtenerConAlertaTiempoAsync()
    {
        var fechaLimite = DateTime.Now.AddHours(-48);
        var expedientes = await expedienteLegalRepo.GetConAlertaTiempoAsync(fechaLimite);
        return MapToDTOList(expedientes);
    }

    public async Task<List<ExpedienteLegalDTO>> ListarTodosAsync()
    {
        var expedientes = await expedienteLegalRepo.GetAllAsync();
        return MapToDTOList(expedientes);
    }

    // ===================================================================
    // HISTORIAL
    // ===================================================================

    public async Task<List<HistorialExpedienteLegalDTO>> ObtenerHistorialAsync(int expedienteLegalId)
    {
        var expedienteLegal = await expedienteLegalRepo.GetByIdAsync(expedienteLegalId)
            ?? throw new KeyNotFoundException($"Expediente legal {expedienteLegalId} no encontrado");

        var logs = await context.AuditLogs
            .Where(a => a.ExpedienteID == expedienteLegal.ExpedienteID && a.Modulo == "ExpedienteLegal")
            .OrderBy(a => a.FechaHora)
            .Include(a => a.Usuario)
            .ToListAsync();

        return logs.Select(log => new HistorialExpedienteLegalDTO
        {
            FechaHora = log.FechaHora,
            Accion = log.Accion,
            UsuarioNombre = log.Usuario?.NombreCompleto ?? "Sistema",
            Detalle = log.DatosDespues ?? "Sin detalles",
            IPOrigen = log.IPOrigen
        }).ToList();
    }

    // ===================================================================
    // MAPPING
    // ===================================================================

    private List<ExpedienteLegalDTO> MapToDTOList(List<ExpedienteLegal> expedientes)
    {
        return expedientes.Select(MapToDTO).ToList();
    }

    private ExpedienteLegalDTO MapToDTO(ExpedienteLegal expedienteLegal)
    {
        return new ExpedienteLegalDTO
        {
            ExpedienteLegalID = expedienteLegal.ExpedienteLegalID,
            ExpedienteID = expedienteLegal.ExpedienteID,
            CodigoExpediente = expedienteLegal.Expediente?.CodigoExpediente ?? "N/A",
            ApellidoPaterno = expedienteLegal.Expediente?.ApellidoPaterno ?? "",
            ApellidoMaterno = expedienteLegal.Expediente?.ApellidoMaterno ?? "",
            Nombres = expedienteLegal.Expediente?.Nombres ?? "",
            HC = expedienteLegal.Expediente?.HC ?? "",
            NumeroDocumento = expedienteLegal.Expediente?.NumeroDocumento ?? "",
            NombrePaciente = expedienteLegal.Expediente?.NombreCompleto ?? "N/A",
            Estado = expedienteLegal.Estado.ToString(),
            EstadoDescripcion = ObtenerDescripcionEstado(expedienteLegal.Estado),
            NumeroOficioPNP = expedienteLegal.NumeroOficioPNP,
            Comisaria = expedienteLegal.Comisaria,
            Fiscalia = expedienteLegal.Fiscalia,
            Destino = expedienteLegal.Destino,
            Observaciones = expedienteLegal.Observaciones,
            ValidadoAdmision = expedienteLegal.ValidadoAdmision,
            FechaValidacionAdmision = expedienteLegal.FechaValidacionAdmision,
            UsuarioAdmisionID = expedienteLegal.UsuarioAdmisionID,
            UsuarioAdmisionNombre = expedienteLegal.UsuarioAdmision?.NombreCompleto,
            ObservacionesAdmision = expedienteLegal.ObservacionesAdmision,
            AutorizadoJefeGuardia = expedienteLegal.AutorizadoJefeGuardia,
            FechaAutorizacion = expedienteLegal.FechaAutorizacion,
            JefeGuardiaID = expedienteLegal.JefeGuardiaID,
            JefeGuardiaNombre = expedienteLegal.JefeGuardia?.NombreCompleto,
            ObservacionesJefeGuardia = expedienteLegal.ObservacionesJefeGuardia,
            DocumentosCompletos = expedienteLegal.DocumentosCompletos,
            DocumentosPendientes = expedienteLegal.CalcularDocumentosPendientes(),
            TienePendientes = !expedienteLegal.DocumentosCompletos,
            FechaLimitePendientes = expedienteLegal.CalcularFechaLimitePendientes(),
            DiasRestantes = expedienteLegal.CalcularDiasRestantesPendientes(),
            NombrePolicia = expedienteLegal.ObtenerNombrePolicia(),
            NombreFiscal = expedienteLegal.ObtenerNombreFiscal(),
            NombreMedicoLegista = expedienteLegal.ObtenerNombreMedicoLegista(),
            Autoridades = expedienteLegal.Autoridades.Select(MapToAutoridadDTO).ToList(),
            Documentos = expedienteLegal.Documentos.Select(MapToDocumentoDTO).ToList(),
            UsuarioRegistroID = expedienteLegal.UsuarioRegistroID,
            UsuarioRegistroNombre = expedienteLegal.UsuarioRegistro?.NombreCompleto ?? "N/A",
            FechaCreacion = expedienteLegal.FechaCreacion,
            UsuarioActualizacionID = expedienteLegal.UsuarioActualizacionID,
            UsuarioActualizacionNombre = expedienteLegal.UsuarioActualizacion?.NombreCompleto,
            FechaUltimaActualizacion = expedienteLegal.FechaUltimaActualizacion,
            CantidadAutoridades = expedienteLegal.Autoridades.Count,
            CantidadDocumentos = expedienteLegal.Documentos.Count,
            PuedeMarcarListo = expedienteLegal.Estado == EstadoExpedienteLegal.EnRegistro && expedienteLegal.DocumentosCompletos,
            PuedeValidarAdmision = expedienteLegal.Estado == EstadoExpedienteLegal.PendienteValidacionAdmision,
            PuedeAutorizarJefeGuardia = expedienteLegal.Estado == EstadoExpedienteLegal.ValidadoAdmision
        };
    }

    private static string ObtenerDescripcionEstado(EstadoExpedienteLegal estado) => estado switch
    {
        EstadoExpedienteLegal.EnRegistro => "En Registro (Vigilancia)",
        EstadoExpedienteLegal.PendienteValidacionAdmision => "Pendiente de Validación (Admisión)",
        EstadoExpedienteLegal.RechazadoAdmision => "Rechazado por Admisión (Corregir)",
        EstadoExpedienteLegal.ValidadoAdmision => "Validado (Pendiente Jefe Guardia)",
        EstadoExpedienteLegal.AutorizadoJefeGuardia => "Autorizado (Listo para Levantamiento)",
        _ => "Estado Desconocido"
    };

    private static AutoridadExternaDTO MapToAutoridadDTO(AutoridadExterna autoridad) => new()
    {
        AutoridadExternaID = autoridad.AutoridadID,
        ExpedienteLegalID = autoridad.ExpedienteLegalID ?? 0,
        TipoAutoridad = autoridad.TipoAutoridad.ToString(),
        ApellidoPaterno = autoridad.ApellidoPaterno,
        ApellidoMaterno = autoridad.ApellidoMaterno,
        Nombres = autoridad.Nombres,
        TipoDocumento = autoridad.TipoDocumento.ToString(),
        NumeroDocumento = autoridad.NumeroDocumento,
        Institucion = autoridad.Institucion,
        Cargo = autoridad.Cargo,
        CodigoEspecial = autoridad.CodigoEspecial,
        PlacaVehiculo = autoridad.PlacaVehiculo,
        Telefono = autoridad.Telefono,
        FechaRegistro = autoridad.FechaRegistro
    };

    private static DocumentoLegalDTO MapToDocumentoDTO(DocumentoLegal documento) => new()
    {
        DocumentoLegalID = documento.DocumentoID,
        ExpedienteLegalID = documento.ExpedienteLegalID ?? 0,
        TipoDocumento = documento.TipoDocumento.ToString(),
        NombreArchivo = documento.NombreArchivo,
        RutaArchivo = documento.RutaArchivo,
        TamañoArchivo = documento.TamañoArchivo,
        TamañoArchivoLegible = documento.ObtenerTamañoLegible(),
        UsuarioSubeID = documento.UsuarioAdjuntoID ?? 0,
        UsuarioSubeNombre = documento.UsuarioAdjunto?.NombreCompleto ?? "N/A",
        FechaSubida = documento.FechaAdjunto ?? DateTime.MinValue
    };

    // ===================================================================
    // HELPERS
    // ===================================================================

    private async Task RegistrarAuditoriaAsync(string accion, int usuarioId, int? expedienteId, object? datosAntes, object? datosDespues)
    {
        try
        {
            var log = AuditLog.CrearLogPersonalizado(
                "ExpedienteLegal",
                accion,
                usuarioId,
                expedienteId,
                datosAntes,
                datosDespues,
                null
            );

            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();

            logger.LogDebug(
                "AuditLog: Registrado {Accion} por Usuario ID {UsuarioID} para Expediente ID {ExpedienteID}",
                accion, usuarioId, expedienteId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al registrar auditoría para acción {Accion}", accion);
        }
    }

    private async Task EnviarNotificacion(
        string titulo,
        string mensaje,
        List<string> roles,
        string tipo = "info",
        string categoriaNotificacion = "generico",
        int? expedienteId = null,
        string? codigoExpediente = null,
        string? estadoAnterior = null,
        string? estadoNuevo = null,
        string? accionSugerida = null,
        string? urlNavegacion = null,
        bool requiereAccion = false,
        DateTime? fechaExpiracion = null)
    {
        try
        {
            var notificacion = new NotificacionDTO
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = tipo,
                CategoriaNotificacion = categoriaNotificacion,
                FechaHora = DateTime.Now,
                RolesDestino = string.Join(",", roles),
                ExpedienteId = expedienteId,
                CodigoExpediente = codigoExpediente,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                AccionSugerida = accionSugerida,
                UrlNavegacion = urlNavegacion,
                RequiereAccion = requiereAccion,
                FechaExpiracion = fechaExpiracion,
                Leida = false
            };

            await hubContext.Clients
                .Groups(roles)
                .RecibirNotificacion(notificacion);

            logger.LogDebug(
                "Notificación SignalR enviada - ID: {NotificacionId}, Categoría: {Categoria}, Tipo: {Tipo}, Roles: {Roles}",
                notificacion.Id, categoriaNotificacion, tipo, string.Join(", ", roles)
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error al enviar notificación SignalR: {Titulo} a roles {Roles}",
                titulo,
                string.Join(", ", roles)
            );
        }
    }
}