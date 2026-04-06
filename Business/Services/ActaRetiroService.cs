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
    IDocumentoExpedienteService documentoExpedienteService,
    IStateMachineService stateMachineService,
    INotificacionActaRetiroService notificacionService,
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
        => await actaRetiroRepository.ExistsByExpedienteIdAsync(expedienteId);

    public async Task<bool> ExisteByCertificadoSINADEFAsync(string numeroCertificado)
        => await actaRetiroRepository.ExisteByCertificadoSINADEFAsync(numeroCertificado);

    public async Task<bool> ExistsByOficioLegalAsync(string numeroOficio)
        => await actaRetiroRepository.ExistsByOficioLegalAsync(numeroOficio);

    // ═══════════════════════════════════════════════════════════
    // CREAR ACTA
    // ═══════════════════════════════════════════════════════════

    public async Task<ActaRetiroDTO> CreateAsync(CreateActaRetiroDTO dto)
    {
        logger.LogInformation(
            "Creando Acta de Retiro para expediente {ExpedienteID}, Tipo: {TipoSalida}",
            dto.ExpedienteID, dto.TipoSalida);

        // 1. Validar que el expediente existe
        var expediente = await expedienteRepository.GetByIdAsync(dto.ExpedienteID)
            ?? throw new InvalidOperationException(
                $"Expediente {dto.ExpedienteID} no encontrado");

        // 2. Validar estado del expediente — debe estar en EnBandeja o PendienteRetiro
        if (expediente.EstadoActual != EstadoExpediente.EnBandeja &&
            expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
        {
            throw new InvalidOperationException(
                $"El expediente está en estado '{expediente.EstadoActual}' y no puede " +
                "recibir un Acta de Retiro. El expediente debe estar en 'EnBandeja' o 'PendienteRetiro'.");
        }

        // 3. Validar que no exista un acta previa
        if (await actaRetiroRepository.ExistsByExpedienteIdAsync(dto.ExpedienteID))
        {
            throw new InvalidOperationException(
                $"Ya existe un Acta de Retiro para el expediente {dto.ExpedienteID}");
        }

        // 4. Validar unicidad de documentos legales
        if (dto.TipoSalida == TipoSalida.Familiar &&
            !string.IsNullOrWhiteSpace(dto.NumeroCertificadoDefuncion))
        {
            if (await actaRetiroRepository.ExisteByCertificadoSINADEFAsync(dto.NumeroCertificadoDefuncion))
                throw new InvalidOperationException(
                    $"Ya existe un acta con el certificado SINADEF {dto.NumeroCertificadoDefuncion}. " +
                    "Cada certificado debe ser único.");
        }

        if (dto.TipoSalida == TipoSalida.AutoridadLegal &&
            !string.IsNullOrWhiteSpace(dto.NumeroOficioPolicial))
        {
            if (await actaRetiroRepository.ExistsByOficioLegalAsync(dto.NumeroOficioPolicial))
                throw new InvalidOperationException(
                    $"Ya existe un acta con el oficio legal {dto.NumeroOficioPolicial}. " +
                    "Cada oficio debe ser único.");
        }

        // 5. Validar semáforo de deudas
        // Para AutoridadLegal: si hay bypass autorizado en el expediente se omite.
        // Para Familiar: siempre debe cumplir (no existe bypass para familiar).
        await ValidarSemaforoDeudas(expediente, dto.TipoSalida);

        // 6. Validar campos según tipo de salida
        ValidarCamposSegunTipo(dto);

        // 7. Validar documentación como gate (documentos subidos y verificados)
        var documentacionOK = await documentoExpedienteService
            .VerificarDocumentacionCompletaAsync(dto.ExpedienteID);

        if (!documentacionOK)
            throw new InvalidOperationException(
                "No se puede crear el Acta de Retiro sin documentación completa. " +
                "Verifique que todos los documentos requeridos estén subidos y verificados.");

        // 8. Construir entidad
        var acta = new ActaRetiro
        {
            ExpedienteID = dto.ExpedienteID,
            NumeroCertificadoDefuncion = dto.NumeroCertificadoDefuncion,
            NumeroOficioPolicial = dto.NumeroOficioPolicial,
            NombreCompletoFallecido = dto.NombreCompletoFallecido,
            HistoriaClinica = dto.HistoriaClinica,
            TipoDocumentoFallecido = dto.TipoDocumentoFallecido,
            NumeroDocumentoFallecido = dto.NumeroDocumentoFallecido,
            ServicioFallecimiento = dto.ServicioFallecimiento,
            FechaHoraFallecimiento = dto.FechaHoraFallecimiento,
            MedicoCertificaNombre = dto.MedicoCertificaNombre,
            MedicoCMP = dto.MedicoCMP,
            MedicoRNE = dto.MedicoRNE,
            MedicoExternoNombre = dto.MedicoExternoNombre,
            MedicoExternoCMP = dto.MedicoExternoCMP,
            JefeGuardiaNombre = dto.JefeGuardiaNombre,
            JefeGuardiaCMP = dto.JefeGuardiaCMP,
            TipoSalida = dto.TipoSalida,
            DatosAdicionales = dto.DatosAdicionales,
            Destino = dto.Destino,
            Observaciones = dto.Observaciones,
            UsuarioAdmisionID = dto.UsuarioAdmisionID,
            FechaRegistro = DateTime.Now
        };

        // 9. Propagar bypass si fue autorizado previamente en el expediente
        // El bypass se registra en ActaRetiro para trazabilidad del documento
        if (expediente.BypassDeudaAutorizado)
        {
            acta.BypassDeudaAutorizado = true;
            acta.BypassDeudaJustificacion = expediente.BypassDeudaJustificacion;
            acta.BypassDeudaUsuarioID = expediente.BypassDeudaUsuarioID;
            acta.BypassDeudaFecha = expediente.BypassDeudaFecha;
        }

        // 10. Mapear datos según tipo de salida
        if (dto.TipoSalida == TipoSalida.Familiar)
        {
            acta.FamiliarApellidoPaterno = dto.FamiliarApellidoPaterno;
            acta.FamiliarApellidoMaterno = dto.FamiliarApellidoMaterno;
            acta.FamiliarNombres = dto.FamiliarNombres;
            acta.FamiliarTipoDocumento = dto.FamiliarTipoDocumento;
            acta.FamiliarNumeroDocumento = dto.FamiliarNumeroDocumento;
            acta.FamiliarParentesco = dto.FamiliarParentesco;
            acta.FamiliarTelefono = dto.FamiliarTelefono;
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
            acta.AutoridadTelefono = dto.AutoridadTelefono;
            acta.GenerarNombreCompletoAutoridad();
        }

        // 11. Validar completitud final
        var validacion = acta.ValidarParaGenerarPDF();
        if (validacion != "OK")
            throw new InvalidOperationException($"Validación fallida: {validacion}");

        // 12. Guardar
        var actaCreada = await actaRetiroRepository.CreateAsync(acta);

        logger.LogInformation(
            "Acta de Retiro {ActaRetiroID} creada exitosamente para {TipoSalida}",
            actaCreada.ActaRetiroID, dto.TipoSalida);

        // 13. Recargar con navegaciones completas
        var actaCompleta = await actaRetiroRepository.GetByIdAsync(actaCreada.ActaRetiroID);
        return MapToDTO(actaCompleta!);
    }

    // ═══════════════════════════════════════════════════════════
    // AUTORIZAR BYPASS DE DEUDA
    // Solo roles: Admin, JefeGuardia, SoporteInformatica
    // El UsuarioAutorizaID viene del JWT en el controller
    // ═══════════════════════════════════════════════════════════

    public async Task AutorizarBypassDeudaAsync(
        AutorizarBypassDeudaDTO dto, int usuarioAutorizaID, string rolUsuario)
    {
        logger.LogInformation(
            "Solicitud de bypass de deuda para expediente {ExpedienteID} por usuario {UsuarioID} rol {Rol}",
            dto.ExpedienteID, usuarioAutorizaID, rolUsuario);

        // 1. Validar rol — solo JG y Admin pueden autorizar bypass
        var rolesPermitidos = new[] { "JefeGuardia", "Administrador" };
        if (!rolesPermitidos.Contains(rolUsuario))
            throw new UnauthorizedAccessException(
                "Solo el Jefe de Guardia o el Administrador pueden autorizar " +
                "el bypass de deuda económica.");

        // 2. Obtener expediente
        var expediente = await expedienteRepository.GetByIdAsync(dto.ExpedienteID)
            ?? throw new InvalidOperationException(
                $"Expediente {dto.ExpedienteID} no encontrado");

        // 3. Validar que el tipo de salida preliminar sea AutoridadLegal
        // El bypass solo tiene sentido para casos PNP — los familiares deben cancelar siempre
        if (expediente.TipoSalidaPreliminar != TipoSalida.AutoridadLegal)
            throw new InvalidOperationException(
                "El bypass de deuda solo aplica para retiros por Autoridad Legal (PNP/Fiscal). " +
                "Los retiros por familiar deben regularizar la deuda antes de proceder.");

        // 4. Validar que no tenga ya un bypass activo
        if (expediente.BypassDeudaAutorizado)
            throw new InvalidOperationException(
                "Este expediente ya tiene un bypass de deuda autorizado. " +
                $"Fue autorizado el {expediente.BypassDeudaFecha:dd/MM/yyyy HH:mm}.");

        // 5. Registrar bypass en expediente
        expediente.BypassDeudaAutorizado = true;
        expediente.BypassDeudaJustificacion = dto.Justificacion;
        expediente.BypassDeudaUsuarioID = usuarioAutorizaID;
        expediente.BypassDeudaFecha = DateTime.Now;
        expediente.FechaModificacion = DateTime.Now;

        await expedienteRepository.UpdateAsync(expediente);

        logger.LogWarning(
            "Bypass de deuda autorizado para expediente {CodigoExpediente} " +
            "por usuario {UsuarioID}. Justificación: {Justificacion}",
            expediente.CodigoExpediente, usuarioAutorizaID, dto.Justificacion);
    }

    // ═══════════════════════════════════════════════════════════
    // GENERAR PDF
    // ═══════════════════════════════════════════════════════════

    public async Task<(byte[] PdfBytes, string FileName)> GenerarPDFSinFirmarAsync(int actaRetiroId)
    {
        logger.LogInformation("Generando PDF sin firmar para Acta {ActaRetiroID}", actaRetiroId);

        var acta = await actaRetiroRepository.GetByIdAsync(actaRetiroId)
            ?? throw new InvalidOperationException(
                $"Acta de Retiro {actaRetiroId} no encontrada");

        var validacion = acta.ValidarParaGenerarPDF();
        if (validacion != "OK")
            throw new InvalidOperationException($"No se puede generar PDF: {validacion}");

        var pdfBytes = pdfGeneratorService.GenerarActaRetiro(acta);
        var fileName = $"ActaRetiro_{acta.Expediente?.CodigoExpediente ?? actaRetiroId.ToString()}.pdf";

        var directorio = Path.Combine("wwwroot", "documentos-legales", "actas-retiro");
        Directory.CreateDirectory(directorio);

        var rutaCompleta = Path.Combine(directorio, fileName);
        await File.WriteAllBytesAsync(rutaCompleta, pdfBytes);

        acta.RutaPDFSinFirmar = $"documentos-legales/actas-retiro/{fileName}";
        acta.NombreArchivoPDFSinFirmar = fileName;
        acta.TamañoPDFSinFirmar = pdfBytes.Length;

        await actaRetiroRepository.UpdateAsync(acta);

        logger.LogInformation(
            "PDF sin firmar generado: {FileName} ({Size} bytes)", fileName, pdfBytes.Length);

        return (pdfBytes, fileName);
    }

    // ═══════════════════════════════════════════════════════════
    // SUBIR PDF FIRMADO
    // ═══════════════════════════════════════════════════════════

    public async Task<ActaRetiroDTO> SubirPDFFirmadoAsync(UpdateActaRetiroPDFDTO dto)
    {
        logger.LogInformation("Subiendo PDF firmado para Acta {ActaRetiroID}", dto.ActaRetiroID);

        var acta = await actaRetiroRepository.GetByIdAsync(dto.ActaRetiroID)
            ?? throw new InvalidOperationException(
                $"Acta de Retiro {dto.ActaRetiroID} no encontrada");

        acta.RutaPDFFirmado = dto.RutaPDFFirmado;
        acta.NombreArchivoPDFFirmado = dto.NombreArchivoPDFFirmado;
        acta.TamañoPDFFirmado = dto.TamañoPDFFirmado;
        acta.MarcarFirmadoCompleto(dto.UsuarioSubidaPDFID);

        if (!string.IsNullOrWhiteSpace(dto.Observaciones))
            acta.Observaciones = dto.Observaciones;

        await actaRetiroRepository.UpdateAsync(acta);

        // Disparar transición EnBandeja → PendienteRetiro
        var expediente = await expedienteRepository.GetByIdAsync(acta.ExpedienteID)
            ?? throw new InvalidOperationException(
                $"Expediente {acta.ExpedienteID} no encontrado");

        if (expediente.EstadoActual == EstadoExpediente.EnBandeja)
        {
            await stateMachineService.FireAsync(expediente, TriggerExpediente.AutorizarRetiro);
            expediente.FechaModificacion = DateTime.Now;
            await expedienteRepository.UpdateAsync(expediente);

            logger.LogInformation(
                "Expediente {ExpedienteID} transitó a PendienteRetiro al subir PDF firmado",
                expediente.ExpedienteID);

            await notificacionService.NotificarExpedienteListoParaRetiroAsync(expediente, acta);
        }

        var actaActualizada = await actaRetiroRepository.GetByIdAsync(dto.ActaRetiroID);
        return MapToDTO(actaActualizada!);
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDACIONES PRIVADAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Valida el semáforo de deudas antes de crear el acta.
    /// Para AutoridadLegal: si el expediente tiene bypass autorizado, se omite la deuda económica.
    /// La deuda de sangre siempre debe estar resuelta (no existe bypass para sangre).
    /// Para Familiar: ambas deudas deben estar verdes sin excepción.
    /// </summary>
    private static async Task ValidarSemaforoDeudas(
        Expediente expediente, TipoSalida tipoSalida)
    {
        // Para AutoridadLegal (PNP): si hay bypass autorizado cubre AMBAS deudas.
        // Fundamento: si el familiar nunca aparece, el hospital pierde las unidades de
        // sangre igual que la deuda económica. No tiene sentido bloquear al PNP por
        // una deuda que el hospital ya no puede recuperar.
        bool bypassValido = tipoSalida == TipoSalida.AutoridadLegal &&
                            expediente.BypassDeudaAutorizado;

        // Evaluar semáforo navegando entidades relacionadas — no hay flags en Expediente.
        // null significa sin deuda registrada → verde por defecto.
        bool bloqueaSangre = expediente.DeudaSangre?.BloqueaRetiro() ?? false;
        bool bloqueaEconomica = expediente.DeudaEconomica?.BloqueaRetiro() ?? false;

        // Deuda de sangre
        if (bloqueaSangre)
        {
            if (bypassValido)
            {
                // Bypass cubre también deuda de sangre para AutoridadLegal
            }
            else if (tipoSalida == TipoSalida.Familiar)
            {
                throw new InvalidOperationException(
                    "El expediente tiene deuda de sangre pendiente. " +
                    "El familiar debe firmar el compromiso en Banco de Sangre antes de proceder.");
            }
            else
            {
                throw new InvalidOperationException(
                    "El expediente tiene deuda de sangre pendiente. " +
                    "Solicite al Jefe de Guardia o Administrador que autorice el bypass de deuda " +
                    "(cubre deuda económica y de sangre para retiros por autoridad legal).");
            }
        }

        // Deuda económica
        if (bloqueaEconomica)
        {
            if (bypassValido)
            {
                // Bypass cubre deuda económica para AutoridadLegal
                return;
            }

            throw new InvalidOperationException(
                "El expediente tiene deuda económica pendiente. " +
                (tipoSalida == TipoSalida.AutoridadLegal
                    ? "Solicite al Jefe de Guardia o Administrador que autorice el bypass de deuda."
                    : "La deuda debe ser cancelada o exonerada antes de proceder con el retiro familiar."));
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Valida campos obligatorios según el tipo de salida.
    /// Para Familiar: SINADEF obligatorio solo si no hay médico externo.
    /// Para AutoridadLegal: oficio + datos completos de autoridad.
    /// </summary>
    private static void ValidarCamposSegunTipo(CreateActaRetiroDTO dto)
    {
        if (dto.TipoSalida == TipoSalida.Familiar)
        {
            // SINADEF obligatorio solo si no hay médico externo
            bool tieneSinadef = !string.IsNullOrWhiteSpace(dto.NumeroCertificadoDefuncion);
            bool tieneMedicoExterno = !string.IsNullOrWhiteSpace(dto.MedicoExternoNombre);

            if (!tieneSinadef && !tieneMedicoExterno)
                throw new InvalidOperationException(
                    "Para retiro familiar debe proporcionar el N° de Certificado SINADEF " +
                    "o los datos del médico externo que certifica.");

            if (tieneMedicoExterno && string.IsNullOrWhiteSpace(dto.MedicoExternoCMP))
                throw new InvalidOperationException(
                    "El CMP del médico externo es obligatorio cuando se indica médico externo.");

            if (string.IsNullOrWhiteSpace(dto.FamiliarApellidoPaterno))
                throw new InvalidOperationException(
                    "El apellido paterno del familiar es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.FamiliarNombres))
                throw new InvalidOperationException(
                    "Los nombres del familiar son obligatorios.");

            if (dto.FamiliarTipoDocumento == TipoDocumentoIdentidad.DNI &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    dto.FamiliarNumeroDocumento ?? string.Empty, @"^\d{8}$"))
                throw new InvalidOperationException(
                    "El DNI del familiar debe tener exactamente 8 dígitos numéricos.");

            if (string.IsNullOrWhiteSpace(dto.FamiliarParentesco))
                throw new InvalidOperationException(
                    "El parentesco es obligatorio.");

            if (dto.FamiliarTipoDocumento == null)
                throw new InvalidOperationException(
                    "El tipo de documento del familiar es obligatorio.");
        }
        else if (dto.TipoSalida == TipoSalida.AutoridadLegal)
        {
            if (string.IsNullOrWhiteSpace(dto.NumeroOficioPolicial))
                throw new InvalidOperationException(
                    "El N° de Oficio Legal es obligatorio para retiros por autoridades.");

            if (string.IsNullOrWhiteSpace(dto.AutoridadApellidoPaterno))
                throw new InvalidOperationException(
                    "El apellido paterno de la autoridad es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.AutoridadNombres))
                throw new InvalidOperationException(
                    "Los nombres de la autoridad son obligatorios.");

            if (dto.AutoridadTipoDocumento == TipoDocumentoIdentidad.DNI &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    dto.AutoridadNumeroDocumento ?? string.Empty, @"^\d{8}$"))
                throw new InvalidOperationException(
                    "El DNI de la autoridad debe tener exactamente 8 dígitos numéricos.");

            if (string.IsNullOrWhiteSpace(dto.AutoridadInstitucion))
                throw new InvalidOperationException(
                    "La institución/comisaría de la autoridad es obligatoria.");

            if (dto.TipoAutoridad == null)
                throw new InvalidOperationException(
                    "El tipo de autoridad es obligatorio (PNP, Fiscal, Médico Legista).");

            if (dto.AutoridadTipoDocumento == null)
                throw new InvalidOperationException(
                    "El tipo de documento de la autoridad es obligatorio.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════

    private static ActaRetiroDTO MapToDTO(ActaRetiro acta)
    {
        // Calcular edad desde expediente (no denormalizada en ActaRetiro)
        int edad = 0;
        if (acta.Expediente is not null)
        {
            var fechaRef = acta.Expediente.FechaHoraFallecimiento;
            edad = fechaRef.Year - acta.Expediente.FechaNacimiento.Year;
            if (fechaRef < acta.Expediente.FechaNacimiento.AddYears(edad)) edad--;
        }

        return new ActaRetiroDTO
        {
            ActaRetiroID = acta.ActaRetiroID,
            ExpedienteID = acta.ExpedienteID,
            CodigoExpediente = acta.Expediente?.CodigoExpediente ?? string.Empty,

            // Documento legal
            NumeroCertificadoDefuncion = acta.NumeroCertificadoDefuncion,
            NumeroOficioPolicial = acta.NumeroOficioPolicial,

            // Datos del fallecido
            NombreCompletoFallecido = acta.NombreCompletoFallecido ?? string.Empty,
            HistoriaClinica = acta.HistoriaClinica,
            TipoDocumentoFallecido = acta.TipoDocumentoFallecido.ToString(),
            NumeroDocumentoFallecido = acta.NumeroDocumentoFallecido,
            ServicioFallecimiento = acta.ServicioFallecimiento,
            FechaHoraFallecimiento = acta.FechaHoraFallecimiento,

            // Campos del expediente (digitaliza cuaderno VigSup)
            Edad = edad,
            DiagnosticoFinal = acta.Expediente?.DiagnosticoFinal,
            CausaViolentaODudosa = acta.Expediente?.CausaViolentaODudosa ?? false,
            TipoExpediente = acta.Expediente?.TipoExpediente.ToString() ?? string.Empty,

            // Médico certificante
            MedicoCertificaNombre = acta.MedicoCertificaNombre,
            MedicoCMP = acta.MedicoCMP,
            MedicoRNE = acta.MedicoRNE,

            // Médico externo
            MedicoExternoNombre = acta.MedicoExternoNombre,
            MedicoExternoCMP = acta.MedicoExternoCMP,

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
            AutoridadTelefono = acta.AutoridadTelefono,

            // Bypass deuda
            BypassDeudaAutorizado = acta.BypassDeudaAutorizado,
            BypassDeudaJustificacion = acta.BypassDeudaJustificacion,
            BypassDeudaUsuarioNombre = acta.BypassDeudaUsuario?.NombreCompleto,
            BypassDeudaFecha = acta.BypassDeudaFecha,

            // Datos adicionales
            DatosAdicionales = acta.DatosAdicionales,
            Destino = acta.Destino,

            // Firmas
            FirmadoResponsable = acta.FirmadoResponsable,
            FechaFirmaResponsable = acta.FechaFirmaResponsable,
            FirmadoAdmisionista = acta.FirmadoAdmisionista,
            FechaFirmaAdmisionista = acta.FechaFirmaAdmisionista,
            FirmadoSupervisorVigilancia = acta.FirmadoSupervisorVigilancia,
            FechaSupervisorVigilancia = acta.FechaSupervisorVigilancia,
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