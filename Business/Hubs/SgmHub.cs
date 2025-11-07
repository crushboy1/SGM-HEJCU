using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SisMortuorio.Business.Hubs
{
    /// <summary>
    /// Hub de SignalR para notificaciones en tiempo real del SGM.
    /// Gestiona conexiones de clientes (Angular) y permite al backend
    /// enviar alertas (push).
    /// </summary>
    [Authorize] // Asegura que solo usuarios autenticados puedan conectarse al Hub
    public class SgmHub : Hub<ISgmClient>
    {
        private readonly ILogger<SgmHub> _logger;

        public SgmHub(ILogger<SgmHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Se llama cuando un nuevo cliente (Angular) se conecta al Hub.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name);
            var rol = Context.User?.FindFirstValue(ClaimTypes.Role);

            // TODO: En el futuro, podemos agrupar conexiones por rol
            // await Groups.AddToGroupAsync(Context.ConnectionId, rol);

            _logger.LogInformation(
                "SignalR Client Conectado: ConnectionId {ConnectionId} | Usuario: {UserName} (ID: {UserId}) | Rol: {Rol}",
                Context.ConnectionId,
                userName,
                userId,
                rol);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Se llama cuando un cliente (Angular) se desconecta.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name);

            _logger.LogInformation(
                "SignalR Client Desconectado: ConnectionId {ConnectionId} | Usuario: {UserName} (ID: {UserId}) | Error: {Error}",
                Context.ConnectionId,
                userName,
                userId,
                exception?.Message ?? "Desconexión limpia");

            await base.OnDisconnectedAsync(exception);
        }
    }
}