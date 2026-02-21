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
/// Gestiona el registro de salida de cuerpos (casos internos y externos) y la liberación automática de bandejas.
/// 
/// Responsabilidades:
/// - Registrar salida según tipo (Familiar con ActaRetiro, AutoridadLegal con ExpedienteLegal)
/// - Validar documentación completa (validada por Admisión)
/// - Validar referencias polimórficas según tipo de salida
/// - Transicionar estado del expediente (PendienteRetiro → Retirado)
/// - Calcular tiempo de permanencia en mortuorio
/// - Liberar bandeja automáticamente (RN-34)
/// - Notificar cambios vía SignalR
/// </summary>
public class SalidaMortuorioService(
    ISalidaMortuorioRepository salidaRepo,
    IExpedienteRepository expedienteRepo,
    IBandejaService bandejaService,
    IStateMachineService stateMachine,
    IHubContext<SgmHub, ISgmClient> hubContext,
    ILogger<SalidaMortuorioService> logger) : ISalidaMortuorioService
{
    // ═══════════════════════════════════════════════════════════
    // REGISTRO DE SALIDA
    // ═══════════════════════════════════════════════════════════

    public async Task<SalidaDTO> RegistrarSalidaAsync(RegistrarSalidaDTO dto, int vigilanteId)
    {
        // 1. Validar expediente existe
        var expediente = await expedienteRepo.GetByIdAsync(dto.ExpedienteID)
            ?? throw new KeyNotFoundException($"Expediente ID {dto.ExpedienteID} no encontrado");

        // 2. Validar estado PendienteRetiro
        if (expediente.EstadoActual != EstadoExpediente.PendienteRetiro)
        {
            throw new InvalidOperationException(
                $"El expediente {expediente.CodigoExpediente} debe estar en estado 'Pendiente Retiro'. " +
                $"Estado actual: {expediente.EstadoActual}"
            );
        }

        // 3. Validar documentación completa (validada por Admisión)
        if (!expediente.DocumentacionCompleta)
        {
            throw new InvalidOperationException(
                $"Admisión debe completar la documentación antes del retiro. " +
                $"Expediente: {expediente.CodigoExpediente}"
            );
        }

        // 4. Validar transición en State Machine
        if (!stateMachine.CanFire(expediente, TriggerExpediente.RegistrarSalida))
        {
            throw new InvalidOperationException(
                $"Acción no permitida. El expediente {expediente.CodigoExpediente} está en estado '{expediente.EstadoActual}' " +
                $"y no puede registrarse su salida"
            );
        }

        // 5. Crear entidad y validar referencias polimórficas
        var salida = new SalidaMortuorio
        {
            ExpedienteID = dto.ExpedienteID,
            ActaRetiroID = dto.ActaRetiroID,
            ExpedienteLegalID = dto.ExpedienteLegalID,
            VigilanteID = vigilanteId,
            FechaHoraSalida = DateTime.Now,
            TipoSalida = dto.TipoSalida,
            ResponsableNombre = dto.ResponsableNombre,
            ResponsableTipoDocumento = dto.ResponsableTipoDocumento,
            ResponsableNumeroDocumento = dto.ResponsableNumeroDocumento,
            ResponsableParentesco = dto.ResponsableParentesco,
            ResponsableTelefono = dto.ResponsableTelefono,
            NumeroOficio = dto.NumeroOficio,
            NombreFuneraria = dto.NombreFuneraria,
            ConductorFuneraria = dto.ConductorFuneraria,
            DNIConductor = dto.DNIConductor,
            AyudanteFuneraria = dto.AyudanteFuneraria,
            DNIAyudante = dto.DNIAyudante,
            PlacaVehiculo = dto.PlacaVehiculo,
            Destino = dto.Destino,
            Observaciones = dto.Observaciones,
            IncidenteRegistrado = false
        };

        // 6. Validar referencias polimórficas
        var validacionReferencias = salida.ValidarReferencias();
        if (validacionReferencias != "OK")
        {
            throw new InvalidOperationException(validacionReferencias);
        }

        // 7. Validar documentación según tipo
        var validacionDocumentacion = salida.ValidarDocumentacion();
        if (validacionDocumentacion != "Documentación completa")
        {
            throw new InvalidOperationException(validacionDocumentacion);
        }

        // 8. Calcular tiempo de permanencia
        var fechaIngresoMortuorio = await ObtenerFechaIngresoMortuorioAsync(expediente.ExpedienteID);
        salida.CalcularTiempoPermanencia(fechaIngresoMortuorio);

        // 9. Guardar registro de salida
        var salidaCreada = await salidaRepo.CreateAsync(salida);

        var estadoAnterior = expediente.EstadoActual;

        // 10. Disparar State Machine (PendienteRetiro → Retirado)
        await stateMachine.FireAsync(expediente, TriggerExpediente.RegistrarSalida);
        await expedienteRepo.UpdateAsync(expediente);

        // 11. Liberar bandeja automáticamente (RN-34)
        await bandejaService.LiberarBandejaAsync(expediente.ExpedienteID, vigilanteId);

        logger.LogInformation(
            "Salida registrada para Expediente {CodigoExpediente} (Tipo: {TipoSalida}) por Usuario ID {UsuarioID}. " +
            "Estado: {EstadoAnterior} → {EstadoNuevo}. Tiempo permanencia: {TiempoPermanencia}. Bandeja liberada.",
            expediente.CodigoExpediente, dto.TipoSalida, vigilanteId, estadoAnterior, expediente.EstadoActual, salida.TiempoPermanencia
        );

        // 12. Notificar cambio de estado vía SignalR
        await NotificarSalidaRegistradaAsync(expediente, estadoAnterior, dto.TipoSalida);

        return MapToSalidaDTO(salidaCreada);
    }
    // ═══════════════════════════════════════════════════════════
    // PRE-LLENADO DE FORMULARIO
    // ═══════════════════════════════════════════════════════════

    public async Task<DatosPreLlenadoSalidaDTO?> GetDatosParaPrellenarAsync(int expedienteId)
    {
        // 1. Obtener datos desde el repositorio
        var datos = await salidaRepo.GetDatosParaPrellenarAsync(expedienteId);

        if (datos == null)
        {
            logger.LogWarning(
                "No se pudieron obtener datos de pre-llenado para expediente {ExpedienteID}. " +
                "Posibles causas: expediente no existe, no está en PendienteRetiro, o no tiene acta firmada.",
                expedienteId
            );
            return null;
        }

        logger.LogInformation(
            "Datos de pre-llenado obtenidos para expediente {CodigoExpediente}. " +
            "Tipo Salida: {TipoSalida}, DocumentosOK: {DocumentosOK}, PagosOK: {PagosOK}",
            datos.CodigoExpediente, datos.TipoSalida, datos.DocumentosOK, datos.PagosOK
        );

        return datos;
    }
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════

    public async Task<SalidaDTO?> GetByExpedienteIdAsync(int expedienteId)
    {
        var salida = await salidaRepo.GetByExpedienteIdAsync(expedienteId);
        return salida is not null ? MapToSalidaDTO(salida) : null;
    }

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

    public async Task<List<SalidaDTO>> GetSalidasPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasPorRangoFechasAsync(fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    public async Task<List<SalidaDTO>> GetSalidasExcedieronLimiteAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasExcedieronLimiteAsync(fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    public async Task<List<SalidaDTO>> GetSalidasPorTipoAsync(TipoSalida tipo, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var salidas = await salidaRepo.GetSalidasPorTipoAsync(tipo, fechaInicio, fechaFin);
        return salidas.Select(MapToSalidaDTO).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    private async Task NotificarSalidaRegistradaAsync(Expediente expediente, EstadoExpediente estadoAnterior, TipoSalida tipoSalida)
    {
        try
        {
            var mensaje = tipoSalida switch
            {
                TipoSalida.Familiar => $"Expediente {expediente.CodigoExpediente} retirado por familiar. Destino: Funeraria",
                TipoSalida.AutoridadLegal => $"Expediente {expediente.CodigoExpediente} retirado por autoridades. Destino: Morgue Central",
                _ => $"Expediente {expediente.CodigoExpediente} retirado del mortuorio"
            };

            var notificacion = new NotificacionDTO
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = "Salida Registrada",
                Mensaje = mensaje,
                Tipo = "success",
                CategoriaNotificacion = "salida_mortuorio",
                FechaHora = DateTime.Now,
                RolesDestino = "JefeGuardia,VigilanteSupervisor",
                ExpedienteId = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                EstadoAnterior = estadoAnterior.ToString(),
                EstadoNuevo = expediente.EstadoActual.ToString(),
                AccionSugerida = "Ver Estadísticas",
                UrlNavegacion = $"/salidas-mortuorio",
                RequiereAccion = false,
                FechaExpiracion = DateTime.Now.AddHours(24),
                Leida = false
            };

            await hubContext.Clients
                .Groups(["JefeGuardia", "VigilanteSupervisor"])
                .RecibirNotificacion(notificacion);

            logger.LogDebug(
                "Notificación SignalR enviada - Salida registrada: {CodigoExpediente} (Tipo: {TipoSalida})",
                expediente.CodigoExpediente, tipoSalida
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

    private SalidaDTO MapToSalidaDTO(SalidaMortuorio salida)
    {
        string? tiempoLegible = null;
        if (salida.TiempoPermanencia.HasValue)
        {
            var tiempo = salida.TiempoPermanencia.Value;
            tiempoLegible = tiempo.TotalDays >= 1
                ? $"{(int)tiempo.TotalDays} días {tiempo.Hours} horas"
                : $"{tiempo.Hours} horas {tiempo.Minutes} minutos";
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
            TipoSalida = salida.TipoSalida.ToString(),
            ResponsableNombre = salida.ResponsableNombre,
            ResponsableDocumento = $"{salida.ResponsableTipoDocumento} {salida.ResponsableNumeroDocumento}",
            ResponsableParentesco = salida.ResponsableParentesco,
            ResponsableTelefono = salida.ResponsableTelefono,
            NumeroOficio = salida.NumeroOficio,
            NombreFuneraria = salida.NombreFuneraria,
            ConductorFuneraria = salida.ConductorFuneraria,
            DNIConductor = salida.DNIConductor,
            PlacaVehiculo = salida.PlacaVehiculo,
            Destino = salida.Destino,
            VigilanteNombre = salida.Vigilante?.NombreCompleto ?? "N/A",
            Observaciones = salida.Observaciones,
            IncidenteRegistrado = salida.IncidenteRegistrado,
            DetalleIncidente = salida.DetalleIncidente,
            TiempoPermanencia = salida.TiempoPermanencia,
            TiempoPermanenciaLegible = tiempoLegible,
            ExcedioLimite = salida.ExcedioLimitePermanencia()
        };
    }
    /// <summary>
    /// Obtiene la fecha/hora real de ingreso físico al mortuorio.
    /// Usa FechaHoraAsignacion de la bandeja (momento en que el cuerpo es colocado físicamente).
    /// Fallback: FechaCreacion del expediente si no tiene bandeja asignada.
    /// </summary>
    /// <param name="expedienteId">ID del expediente</param>
    /// <returns>Fecha/hora de ingreso al mortuorio</returns>
    private async Task<DateTime> ObtenerFechaIngresoMortuorioAsync(int expedienteId)
    {
        var expediente = await expedienteRepo.GetByIdAsync(expedienteId);

        // Prioridad: Usar fecha de asignación de bandeja (momento real de ingreso físico)
        if (expediente?.BandejaActual?.FechaHoraAsignacion is not null)
        {
            return (DateTime)expediente.BandejaActual.FechaHoraAsignacion;
        }

        // Fallback: Si aún no tiene bandeja, usar fecha de creación
        // (Casos excepcionales donde se registra salida antes de asignar bandeja)
        logger.LogWarning(
            "Expediente {ExpedienteID} no tiene bandeja asignada. Usando FechaCreacion como aproximación.",
            expedienteId
        );

        return expediente?.FechaCreacion ?? DateTime.Now;
    }
}