using Microsoft.AspNetCore.SignalR;
using SisMortuorio.Business.DTOs.Notificacion;
using SisMortuorio.Business.Hubs;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones de deudas.
    /// Centraliza lógica de construcción de NotificacionDTO y envío SignalR.
    /// </summary>
    public class NotificacionDeudaService : INotificacionDeudaService
    {
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext;
        private readonly ILogger<NotificacionDeudaService> _logger;

        private static readonly List<string> RolesNotificacionDeudas =
            ["VigilanteSupervisor", "Admision", "Administrador"];

        private static readonly List<string> RolesNotificacionCreacion =
            ["VigilanteSupervisor", "Admision", "CuentasPacientes", "BancoSangre", "Administrador"];

        public NotificacionDeudaService(
            IHubContext<SgmHub, ISgmClient> hubContext,
            ILogger<NotificacionDeudaService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotificarDeudaCreadaAsync(
            string tipoDeuda,
            int expedienteId,
            string codigoExpediente,
            string detalle)
        {
            var categoria = tipoDeuda == "Sangre" ? "deuda_sangre_creada" : "deuda_economica_creada";
            var titulo = tipoDeuda == "Sangre"
                ? "Nueva Deuda de Sangre Registrada"
                : "Nueva Deuda Económica Registrada";

            await EnviarNotificacion(
                titulo: titulo,
                mensaje: $"{detalle} para {codigoExpediente}. RETIRO BLOQUEADO",
                roles: RolesNotificacionCreacion,
                tipo: "warning",
                categoriaNotificacion: categoria,
                expedienteId: expedienteId,
                codigoExpediente: codigoExpediente,
                estadoAnterior: "Liberado",
                estadoNuevo: "Bloqueado",
                accionSugerida: "Ver Deuda",
                urlNavegacion: $"/expediente/{expedienteId}/deudas",
                requiereAccion: true
            );
        }

        public async Task NotificarDeudaResueltaAsync(
            string tipoDeuda,
            string accionRealizada,
            int expedienteId,
            string codigoExpediente)
        {
            var categoria = tipoDeuda == "Sangre"
                ? (accionRealizada == "Anulada" ? "deuda_sangre_anulada" : "deuda_sangre_cumplida")
                : (accionRealizada == "Exonerada" ? "deuda_economica_exonerada" : "deuda_economica_saldada");

            var mensaje = tipoDeuda == "Sangre"
                ? $"Deuda de sangre {accionRealizada.ToLower()} para {codigoExpediente}"
                : $"Deuda económica {accionRealizada.ToLower()} para {codigoExpediente}";

            await EnviarNotificacion(
                titulo: $"Deuda de {tipoDeuda} {accionRealizada}",
                mensaje: mensaje,
                roles: RolesNotificacionDeudas,
                tipo: "success",
                categoriaNotificacion: categoria,
                expedienteId: expedienteId,
                codigoExpediente: codigoExpediente,
                estadoAnterior: "Bloqueado",
                estadoNuevo: "Bloqueado",
                accionSugerida: "Ver Expediente",
                urlNavegacion: $"/expediente/{expedienteId}/deudas",
                requiereAccion: false
            );
        }

        public async Task NotificarDesbloqueoTotalAsync(
            int expedienteId,
            string codigoExpediente,
            int deudasResueltas,
            int totalDeudas)
        {
            await EnviarNotificacion(
                titulo: "Expediente Totalmente Liberado",
                mensaje: $"Expediente {codigoExpediente} totalmente liberado. Todas las deudas resueltas ({deudasResueltas}/{totalDeudas}). Puede proceder con el retiro",
                roles: RolesNotificacionDeudas,
                tipo: "success",
                categoriaNotificacion: "expediente_totalmente_liberado",
                expedienteId: expedienteId,
                codigoExpediente: codigoExpediente,
                estadoAnterior: "Bloqueado",
                estadoNuevo: "Liberado",
                accionSugerida: "Autorizar Retiro",
                urlNavegacion: $"/expediente/{expedienteId}/retiro",
                requiereAccion: true
            );

            _logger.LogInformation(
                "Expediente {CodigoExpediente} totalmente liberado - Todas las deudas resueltas ({Resueltas}/{Total})",
                codigoExpediente, deudasResueltas, totalDeudas
            );
        }

        public async Task NotificarDesbloqueoParcialAsync(
            int expedienteId,
            string codigoExpediente,
            string tipoDeudaResuelto,
            int deudasResueltas,
            int totalDeudas)
        {
            await EnviarNotificacion(
                titulo: "Expediente Parcialmente Liberado",
                mensaje: $"Expediente {codigoExpediente} liberado parcialmente: Deuda {tipoDeudaResuelto} resuelta. {deudasResueltas} de {totalDeudas} deudas completadas",
                roles: RolesNotificacionDeudas,
                tipo: "info",
                categoriaNotificacion: "expediente_parcialmente_liberado",
                expedienteId: expedienteId,
                codigoExpediente: codigoExpediente,
                estadoAnterior: "Bloqueado",
                estadoNuevo: "ParcialmenteLiberado",
                accionSugerida: "Ver Deudas Pendientes",
                urlNavegacion: $"/expediente/{expedienteId}/deudas",
                requiereAccion: false
            );

            _logger.LogInformation(
                "Expediente {CodigoExpediente} parcialmente liberado - Deuda {TipoDeuda} resuelta ({Resueltas}/{Total})",
                codigoExpediente, tipoDeudaResuelto, deudasResueltas, totalDeudas
            );
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

                await _hubContext.Clients
                    .Groups(roles)
                    .RecibirNotificacion(notificacion);

                _logger.LogDebug(
                    "Notificación SignalR enviada - Categoría: {Categoria}, Tipo: {Tipo}, Roles: {Roles}",
                    categoriaNotificacion, tipo, string.Join(", ", roles)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error al enviar notificación SignalR: {Titulo} a roles {Roles}",
                    titulo,
                    string.Join(", ", roles)
                );
            }
        }
    }
}