using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Implementación del servicio de notificaciones para Actas de Retiro.
/// Centraliza todas las notificaciones SignalR relacionadas con el flujo
/// de autorización y retiro físico de cuerpos del mortuorio.
/// </summary>
public class NotificacionActaRetiroService(
    IHubContext<SgmHub, ISgmClient> hubContext,
    ILogger<NotificacionActaRetiroService> logger) : INotificacionActaRetiroService
{
    // Roles que reciben alerta cuando un expediente está listo para retiro
    private static readonly List<string> RolesListoParaRetiro =
        ["VigilanciaMortuorio", "VigilanteSupervisor"];

    /// <inheritdoc/>
    public async Task NotificarExpedienteListoParaRetiroAsync(Expediente expediente, ActaRetiro acta)
    {
        // Construir detalle según tipo de salida
        var responsable = acta.TipoSalida == TipoSalida.Familiar
            ? $"Familiar: {acta.FamiliarNombreCompleto}"
            : $"Autoridad: {acta.AutoridadNombreCompleto} ({acta.TipoAutoridad})";

        await EnviarNotificacion(
            titulo: "Retiro Autorizado — Acción Requerida",
            mensaje: $"{expediente.CodigoExpediente} | {expediente.NombreCompleto} | " +
                     $"Bandeja: {expediente.BandejaActual?.Codigo ?? "—"} | {responsable}",
            roles: RolesListoParaRetiro,
            tipo: "success",
            categoriaNotificacion: "expediente_listo_para_retiro",
            expedienteId: expediente.ExpedienteID,
            codigoExpediente: expediente.CodigoExpediente,
            estadoAnterior: EstadoExpediente.EnBandeja.ToString(),
            estadoNuevo: EstadoExpediente.PendienteRetiro.ToString(),
            accionSugerida: "Registrar Salida",
            urlNavegacion: $"/salidas/registrar/{expediente.ExpedienteID}",
            requiereAccion: true
        );
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODO PRIVADO — construcción y envío de NotificacionDTO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Método centralizado para construir y enviar NotificacionDTO a grupos de roles.
    /// Patrón idéntico a NotificacionDeudaService para coherencia arquitectónica.
    /// Los errores se loggean como Warning — nunca bloquean el flujo principal.
    /// </summary>
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
        bool requiereAccion = false)
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
                Leida = false
            };

            await hubContext.Clients
                .Groups(roles)
                .RecibirNotificacion(notificacion);

            logger.LogDebug(
                "SignalR: Notificación enviada — Categoría: {Categoria}, Roles: {Roles}",
                categoriaNotificacion, string.Join(", ", roles)
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error al enviar notificación SignalR: {Titulo} a roles {Roles}",
                titulo, string.Join(", ", roles)
            );
        }
    }
}