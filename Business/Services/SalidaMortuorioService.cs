using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Implementación del servicio de Salida de Mortuorio.
/// Gestiona el registro de salida de cuerpos y la liberación automática de bandejas.
///
/// Responsabilidades:
/// - Registrar salida validando ActaRetiro firmada (maneja Familiar y AutoridadLegal)
/// - Validar estado del expediente (PendienteRetiro)
/// - Transicionar estado (PendienteRetiro → Retirado) vía State Machine
/// - Calcular tiempo de permanencia en mortuorio
/// - Liberar bandeja automáticamente (RN-34)
/// - Notificar cambios vía SignalR
///
/// NOTA: TipoSalida, datos del responsable y NumeroOficio
/// se leen siempre desde ActaRetiro — no se duplican en SalidaMortuorio.
/// </summary>
public class SalidaMortuorioService(
    ISalidaMortuorioRepository salidaRepo,
    IExpedienteRepository expedienteRepo,
    IActaRetiroRepository actaRetiroRepo,
    IBandejaService bandejaService,
    IStateMachineService stateMachine,
    IHubContext<SgmHub, ISgmClient> hubContext,
    ILogger<SalidaMortuorioService> logger) : ISalidaMortuorioService
{
    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE SALIDA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Registra la salida física de un cuerpo del mortuorio.
    /// Valida ActaRetiro firmada, dispara State Machine y libera bandeja.
    /// </summary>
    /// <param name="dto">Datos capturados por el Vigilante (funeraria, placa, destino)</param>
    /// <param name="registradoPorId">ID del registrador obtenido desde el token JWT</param>
    /// <returns>SalidaDTO con datos completos incluyendo tiempo de permanencia</returns>
    public async Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int registradoPorId)
    {
        // 1. Validar expediente existe
        var expediente = await expedienteRepo.GetByIdAsync(dto.ExpedienteID)
            ?? throw new KeyNotFoundException($"Expediente ID {dto.ExpedienteID} no encontrado");

        // 2. Validar estado PendienteRetiro
        if (expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
            throw new InvalidOperationException(
                $"El expediente {expediente.CodigoExpediente} debe estar en 'Pendiente Retiro'. " +
                $"Estado actual: {expediente.EstadoActual}");

        // 3. Resolver ActaRetiro desde ExpedienteID — backend es dueño de la relación
        var acta = await actaRetiroRepo.GetByExpedienteIdAsync(dto.ExpedienteID)
            ?? throw new InvalidOperationException(
                $"El expediente {expediente.CodigoExpediente} no tiene Acta de Retiro registrada");

        // 4. Validar PDF firmado
        if (!acta.TienePDFFirmado())
            throw new InvalidOperationException(
                $"El Acta de Retiro del expediente {expediente.CodigoExpediente} " +
                $"debe tener el PDF firmado cargado antes de registrar la salida");

        // 5. Validar transición en State Machine
        if (!stateMachine.CanFire(expediente, TriggerExpediente.RegistrarSalida))
            throw new InvalidOperationException(
                $"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado " +
                $"'{expediente.EstadoActual}' y no puede registrarse su salida");

        // 6. Crear entidad — solo campos capturados por el Vigilante
        // TipoSalida y datos del responsable se leen desde ActaRetiro
        var salida = new SalidaMortuorio
        {
            ExpedienteID = dto.ExpedienteID,
            ActaRetiroID = acta.ActaRetiroID,   // resuelto internamente, no desde el DTO
            ExpedienteLegalID = dto.ExpedienteLegalID,
            RegistradoPorID = registradoPorId,
            FechaHoraSalida = DateTime.Now,
            NombreFuneraria = dto.NombreFuneraria,
            FunerariaRUC = dto.FunerariaRUC,
            FunerariaTelefono = dto.FunerariaTelefono,
            ConductorFuneraria = dto.ConductorFuneraria,
            DNIConductor = dto.DNIConductor,
            AyudanteFuneraria = dto.AyudanteFuneraria,
            DNIAyudante = dto.DNIAyudante,
            PlacaVehiculo = dto.PlacaVehiculo,
            Destino = dto.Destino,
            Observaciones = dto.Observaciones,
            IncidenteRegistrado = false
        };

        // 7. Validar referencias y documentación polimórfica
        var validacionReferencias = salida.ValidarReferencias();
        if (validacionReferencias != "OK")
            throw new InvalidOperationException(validacionReferencias);

        if (acta.TipoSalida == TipoSalida.AutoridadLegal &&
         string.IsNullOrWhiteSpace(salida.PlacaVehiculo))
        {
            salida.PlacaVehiculo = acta.AutoridadPlacaVehiculo;
        }
        salida.ActaRetiro = acta;
        var validacionDocumentacion = salida.ValidarDocumentacion();
        if (validacionDocumentacion != "Documentación completa")
            throw new InvalidOperationException(validacionDocumentacion);

        // 8. Calcular tiempo de permanencia
        var fechaIngresoMortuorio = ObtenerFechaIngresoMortuorio(expediente);
        salida.CalcularTiempoPermanencia(fechaIngresoMortuorio);

        // 9. Guardar registro de salida
        var salidaCreada = await salidaRepo.CreateAsync(salida);
        var estadoAnterior = expediente.EstadoActual;

        // 10. Disparar State Machine (PendienteRetiro → Retirado)
        await stateMachine.FireAsync(expediente, TriggerExpediente.RegistrarSalida);
        await expedienteRepo.UpdateAsync(expediente);

        // 11. Liberar bandeja automáticamente (RN-34)
        await bandejaService.LiberarBandejaAsync(expediente.ExpedienteID, registradoPorId);

        logger.LogInformation(
            "Salida registrada — Expediente: {CodigoExpediente}, TipoSalida: {TipoSalida}, " +
            "RegistradoPorID: {RegistradoPorID}, Estado: {EstadoAnterior} → {EstadoNuevo}, " +
            "Permanencia: {TiempoPermanencia}, Bandeja liberada.",
            expediente.CodigoExpediente,
            acta.TipoSalida,
            registradoPorId,
            estadoAnterior,
            expediente.EstadoActual,
            salida.TiempoPermanencia);

        // 12. Notificar vía SignalR
        await NotificarSalidaRegistradaAsync(expediente, estadoAnterior, acta);

        return MapToSalidaDTO(salidaCreada);
    }

    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos pre-llenados desde ActaRetiro para el formulario del Vigilante.
    /// Retorna null si el expediente no cumple los requisitos para registrar salida.
    /// </summary>
    public async Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId)
    {
        var datos = await salidaRepo.GetDatosParaPrellenarAsync(expedienteId);

        if (datos == null)
        {
            logger.LogWarning(
                "No se pudieron obtener datos de pre-llenado para expediente {ExpedienteID}. " +
                "Causas posibles: no existe, no está en PendienteRetiro, o no tiene PDF firmado.",
                expedienteId
            );
            return null;
        }

        logger.LogInformation(
            "Datos de pre-llenado obtenidos — Expediente: {CodigoExpediente}, " +
            "TipoSalida: {TipoSalida}, ActaListaParaSalida: {ActaListaParaSalida}, " +
            "PuedeRegistrarSalida: {PuedeRegistrarSalida}",
            datos.CodigoExpediente,
            datos.TipoSalida,
            datos.ActaListaParaSalida,
            datos.PuedeRegistrarSalida
        );

        return datos;
    }

    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene el registro de salida de un expediente específico.
    /// </summary>
    public async Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId)
    {
        var salida = await salidaRepo.GetByExpedienteIdAsync(expedienteId);
        return salida is not null ? MapToSalidaDTO(salida) : null;
    }

    /// <summary>
    /// Obtiene estadísticas consolidadas de salidas en un rango de fechas.
    /// </summary>
    public async Task<EstadisticasSalidaDTO> GetEstadisticasAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var stats = await salidaRepo.GetEstadisticasAsync(fechaInicio, fechaFin);

        return new EstadisticasSalidaDTO
        {
            TotalSalidas = stats.TotalSalidas,
            SalidasFamiliar = stats.SalidasFamiliar,
            SalidasAutoridadLegal = stats.SalidasAutoridadLegal,
            ConIncidentes = stats.ConIncidentes,
            ConFuneraria = stats.ConFuneraria,
            PorcentajeIncidentes = stats.PorcentajeIncidentes
        };
    }

    /// <summary>
    /// Obtiene historial de salidas por rango de fechas.
    /// </summary>
    public async Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    /// <summary>
    /// Obtiene salidas que excedieron el límite de permanencia de 48 horas.
    /// </summary>
    public async Task<List<SalidaDTO>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasExcedieronLimiteAsync(fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    /// <summary>
    /// Obtiene salidas filtradas por tipo (Familiar o AutoridadLegal).
    /// El filtro se aplica sobre ActaRetiro.TipoSalida en el repositorio.
    /// </summary>
    public async Task<List<SalidaDTO>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasPorTipoAsync(tipo, fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene la fecha/hora real de ingreso físico al mortuorio.
    /// Usa FechaHoraAsignacion de la bandeja (momento real de ingreso físico).
    /// Fallback: FechaCreacion del expediente si no tiene bandeja asignada.
    /// No hace llamada a BD — usa el expediente ya cargado en memoria.
    /// </summary>
    private DateTime ObtenerFechaIngresoMortuorio(Expediente expediente)
    {
        if (expediente.BandejaActual?.FechaHoraAsignacion is not null)
            return (DateTime)expediente.BandejaActual.FechaHoraAsignacion;

        logger.LogWarning(
            "Expediente {ExpedienteID} no tiene bandeja asignada. " +
            "Usando FechaCreacion como aproximación del ingreso al mortuorio.",
            expediente.ExpedienteID
        );

        return expediente.FechaCreacion;
    }

    /// <summary>
    /// Notifica la salida registrada vía SignalR.
    /// Destinatarios: Admision (bandeja se actualiza), JefeGuardia y VigilanteSupervisor (estadísticas).
    /// El acta se pasa para leer TipoSalida sin consulta adicional a BD.
    /// </summary>
    private async Task NotificarSalidaRegistradaAsync(
        Expediente expediente,
        EstadoExpediente estadoAnterior,
        ActaRetiro acta)
    {
        try
        {
            var mensaje = acta.TipoSalida switch
            {
                TipoSalida.Familiar =>
                    $"Expediente {expediente.CodigoExpediente} retirado por familiar",
                TipoSalida.AutoridadLegal =>
                    $"Expediente {expediente.CodigoExpediente} retirado por autoridad legal",
                _ =>
                    $"Expediente {expediente.CodigoExpediente} retirado del mortuorio"
            };

            var notificacion = new NotificacionDTO
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = "Salida Registrada",
                Mensaje = mensaje,
                Tipo = "success",
                CategoriaNotificacion = "salida_mortuorio",
                FechaHora = DateTime.Now,
                ExpedienteId = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                EstadoAnterior = estadoAnterior.ToString(),
                EstadoNuevo = expediente.EstadoActual.ToString(),
                AccionSugerida = "Ver Estadísticas",
                UrlNavegacion = "/salidas-mortuorio",
                RequiereAccion = false,
                FechaExpiracion = DateTime.Now.AddHours(24),
                Leida = false
            };

            // Admisión: para que el expediente desaparezca de su bandeja
            // JefeGuardia y VigilanteSupervisor: para estadísticas y auditoría
            await hubContext.Clients
                .Groups(["Admision", "JefeGuardia", "VigilanteSupervisor", "Administrador", "VigilanciaMortuorio"])
                .RecibirNotificacion(notificacion);

            logger.LogDebug(
                "Notificación SignalR enviada — Salida registrada: {CodigoExpediente}, TipoSalida: {TipoSalida}",
                expediente.CodigoExpediente,
                acta.TipoSalida
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error al enviar notificación SignalR para salida del expediente {CodigoExpediente}",
                expediente.CodigoExpediente
            );
        }
    }

    /// <summary>
    /// Mapea entidad SalidaMortuorio a DTO de respuesta.
    /// Lee TipoSalida y datos del responsable desde ActaRetiro (sin duplicación).
    /// Requiere que salida.ActaRetiro esté cargado (Include en repositorio).
    /// </summary>
    /// <summary>
    /// Mapea entidad SalidaMortuorio a DTO de respuesta.
    /// Lee TipoSalida y datos del responsable desde ActaRetiro (sin duplicación).
    /// Requiere que salida.ActaRetiro esté cargado (Include en repositorio).
    /// </summary>
    private SalidaDTO MapToSalidaDTO(SalidaMortuorio salida)
    {
        var acta = salida.ActaRetiro;

        string responsableDocumento = string.Empty;
        string responsableNombre = string.Empty;
        string? responsableParentesco = null;
        string? responsableTelefono = null;
        string? numeroOficio = null;
        string? tipoAutoridad = null;
        string? autoridadInstitucion = null;
        string tipoSalida = string.Empty;

        if (acta is not null)
        {
            tipoSalida = acta.TipoSalida.ToString();

            if (acta.TipoSalida == TipoSalida.Familiar)
            {
                responsableNombre = acta.FamiliarNombreCompleto
                    ?? $"{acta.FamiliarApellidoPaterno} {acta.FamiliarApellidoMaterno}, {acta.FamiliarNombres}".Trim();
                responsableDocumento = $"{acta.FamiliarTipoDocumento} {acta.FamiliarNumeroDocumento}".Trim();
                responsableParentesco = acta.FamiliarParentesco;
                responsableTelefono = acta.FamiliarTelefono;
            }
            else if (acta.TipoSalida == TipoSalida.AutoridadLegal)
            {
                responsableNombre = acta.AutoridadNombreCompleto
                    ?? $"{acta.AutoridadApellidoPaterno} {acta.AutoridadApellidoMaterno}, {acta.AutoridadNombres}".Trim();
                responsableDocumento = $"{acta.AutoridadTipoDocumento} {acta.AutoridadNumeroDocumento}".Trim();
                responsableTelefono = acta.AutoridadTelefono;
                numeroOficio = acta.NumeroOficioLegal;
                tipoAutoridad = acta.TipoAutoridad?.ToString();
                autoridadInstitucion = acta.AutoridadInstitucion;
            }
        }

        // Formato legible desde minutos —
        string? tiempoLegible = null;
        if (salida.TiempoPermanenciaMinutos.HasValue)
        {
            var totalMin = salida.TiempoPermanenciaMinutos.Value;
            var dias = totalMin / (60 * 24);
            var horas = (totalMin % (60 * 24)) / 60;
            var mins = totalMin % 60;

            tiempoLegible = dias > 0
                ? $"{dias}d {horas}h {mins}m"
                : horas > 0
                    ? $"{horas}h {mins}m"
                    : $"{mins}m";
        }

        return new SalidaDTO
        {
            SalidaID = salida.SalidaID,
            ExpedienteID = salida.ExpedienteID,
            CodigoExpediente = salida.Expediente?.CodigoExpediente ?? "N/A",
            NombrePaciente = salida.Expediente?.NombreCompleto ?? "N/A",
            ActaRetiroID = salida.ActaRetiroID,
            ExpedienteLegalID = salida.ExpedienteLegalID,
            FechaHoraSalida = salida.FechaHoraSalida,

            // Desde ActaRetiro
            TipoSalida = tipoSalida,
            ResponsableNombre = responsableNombre,
            ResponsableDocumento = responsableDocumento,
            ResponsableParentesco = responsableParentesco,
            ResponsableTelefono = responsableTelefono,
            NumeroOficio = numeroOficio,
            TipoAutoridad = tipoAutoridad,
            AutoridadInstitucion = autoridadInstitucion,

            // Capturado por el Vigilante
            NombreFuneraria = salida.NombreFuneraria,
            FunerariaRUC = salida.FunerariaRUC,
            FunerariaTelefono = salida.FunerariaTelefono,
            ConductorFuneraria = salida.ConductorFuneraria,
            DNIConductor = salida.DNIConductor,
            AyudanteFuneraria = salida.AyudanteFuneraria,
            DNIAyudante = salida.DNIAyudante,
            PlacaVehiculo = salida.PlacaVehiculo,
            Destino = salida.Destino,

            RegistradoPorID = salida.RegistradoPorID,
            RegistradoPorNombre = salida.RegistradoPor?.NombreCompleto ?? "N/A",
            Observaciones = salida.Observaciones,

            IncidenteRegistrado = salida.IncidenteRegistrado,
            DetalleIncidente = salida.DetalleIncidente,

            TiempoPermanenciaMinutos = salida.TiempoPermanenciaMinutos,
            TiempoPermanenciaLegible = tiempoLegible,
            ExcedioLimite = salida.ExcedioLimitePermanencia()
        };
    }
}