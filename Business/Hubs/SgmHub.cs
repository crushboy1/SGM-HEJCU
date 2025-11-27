using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SisMortuorio.Business.Services;

namespace SisMortuorio.Business.Hubs
{
    /// <summary>
    /// Hub de SignalR para notificaciones en tiempo real del SGM.
    /// 
    /// Responsabilidades:
    /// 1. Gestionar conexiones de clientes (Angular) autenticados
    /// 2. Agrupar usuarios por Rol para mensajes dirigidos
    /// 3. Trackear conexiones activas para monitoreo
    /// 4. Permitir al backend enviar alertas push a través de IHubContext
    /// 
    /// Seguridad:
    /// - Requiere autenticación JWT ([Authorize])
    /// - Valida claims obligatorios (NameIdentifier, Name, Role)
    /// - Rechaza conexiones sin rol válido
    /// 
    /// Grupos:
    /// Los usuarios se agregan automáticamente al grupo de su rol al conectarse.
    /// Esto permite enviar mensajes dirigidos: Clients.Group("Admision").RecibirNotificacion(...)
    /// </summary>
    [Authorize] // Solo usuarios autenticados con JWT válido pueden conectarse
    public class SgmHub(ILogger<SgmHub> logger, IConnectionTrackerService connectionTracker) : Hub<ISgmClient> // ⭐ Constructor principal (.NET 9)
    {
        private readonly ILogger<SgmHub> _logger = logger;
        private readonly IConnectionTrackerService _connectionTracker = connectionTracker;

        /// <summary>
        /// Se invoca cuando un cliente (Angular) establece conexión con el Hub.
        /// 
        /// Flujo:
        /// 1. Extrae claims del JWT (userId, userName, rol)
        /// 2. Valida que el rol exista (rechaza conexión si no)
        /// 3. Agrega al usuario al grupo de su rol
        /// 4. Registra la conexión en el tracker
        /// 5. Loggea la conexión con metadata completa
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Extraer claims del token JWT
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User?.FindFirstValue(ClaimTypes.Name);
                var rol = Context.User?.FindFirstValue(ClaimTypes.Role);

                // Obtener metadata de la conexión
                var httpContext = Context.GetHttpContext();
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown";

                //  Rechazar si no tiene rol
                if (string.IsNullOrEmpty(rol))
                {
                    _logger.LogWarning(
                        "SignalR: Conexión rechazada - Usuario sin rol válido. " +
                        "UserId: {UserId}, UserName: {UserName}, IP: {IP}",
                        userId ?? "null",
                        userName ?? "null",
                        ipAddress
                    );

                    // Abortar conexión inmediatamente
                    Context.Abort();
                    return;
                }

                //  Rechazar si falta NameIdentifier o Name
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                {
                    _logger.LogWarning(
                        "SignalR: Conexión rechazada - Claims incompletos. " +
                        "UserId: {UserId}, UserName: {UserName}, Rol: {Rol}, IP: {IP}",
                        userId ?? "null",
                        userName ?? "null",
                        rol,
                        ipAddress
                    );

                    Context.Abort();
                    return;
                }

                // ✅ Agregar al grupo del rol (para mensajes dirigidos)
                await Groups.AddToGroupAsync(Context.ConnectionId, rol);

                // ✅ Registrar conexión en el tracker
                await _connectionTracker.AddConnectionAsync(
                    Context.ConnectionId,
                    userId,
                    userName,
                    rol,
                    ipAddress
                );

                // ✅ Log exitoso con metadata completa
                
                var userAgentShort = userAgent.Length > 100 ? userAgent[..100] + "..." : userAgent;

                _logger.LogInformation(
                    "SignalR: Usuario conectado exitosamente. " +
                    "ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, " +
                    "Rol: {Rol}, IP: {IP}, UserAgent: {UserAgent}",
                    Context.ConnectionId,
                    userId,
                    userName,
                    rol,
                    ipAddress,
                    userAgentShort
                );

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SignalR: Error inesperado en OnConnectedAsync. ConnectionId: {ConnectionId}",
                    Context.ConnectionId
                );

                // Re-throw para que SignalR maneje el error correctamente
                throw;
            }
        }

        /// <summary>
        /// Se invoca cuando un cliente (Angular) se desconecta del Hub.
        /// 
        /// Causas de desconexión:
        /// - Usuario cierra el navegador/pestaña
        /// - Usuario hace logout
        /// - Timeout por inactividad
        /// - Error de red
        /// - Cliente llama a connection.stop()
        /// 
        /// Flujo:
        /// 1. Extrae rol del token JWT
        /// 2. ⭐ CRÍTICO: Remueve al usuario del grupo de su rol
        /// 3. Remueve del tracker
        /// 4. Loggea la desconexión con metadata
        /// </summary>
        /// <param name="exception">Excepción si la desconexión fue abrupta (null si fue normal)</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                // Extraer claims
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User?.FindFirstValue(ClaimTypes.Name);
                var rol = Context.User?.FindFirstValue(ClaimTypes.Role);

                // ⭐ FIX CRÍTICO: Remover del grupo del rol
                if (!string.IsNullOrEmpty(rol))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, rol);
                }

                // Remover del tracker
                await _connectionTracker.RemoveConnectionAsync(Context.ConnectionId);

                //  Log con información de desconexión
                if (exception != null)
                {
                   
                    _logger.LogWarning(
                        exception,
                        "SignalR: Usuario desconectado con error. " +
                        "ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, Rol: {Rol}",
                        Context.ConnectionId,
                        userId ?? "Unknown",
                        userName ?? "Unknown",
                        rol ?? "Unknown"
                    );
                }
                else
                {
                    // Desconexión normal
                    _logger.LogInformation(
                        "SignalR: Usuario desconectado normalmente. " +
                        "ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, Rol: {Rol}",
                        Context.ConnectionId,
                        userId ?? "Unknown",
                        userName ?? "Unknown",
                        rol ?? "Unknown"
                    );
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SignalR: Error inesperado en OnDisconnectedAsync. ConnectionId: {ConnectionId}",
                    Context.ConnectionId
                );
            }
        }

        /// <summary>
        /// Método invocable desde el cliente para verificar conectividad (healthcheck).
        /// El cliente puede llamar a connection.invoke("Ping") para validar que el Hub responde.
        /// 
        /// Uso en Angular:
        /// const latency = await connection.invoke("Ping");
        /// console.log(`Latency: ${latency}ms`);
        /// </summary>
        /// <returns>Timestamp del servidor para calcular latencia</returns>
        public Task<long> Ping()
        {
            return Task.FromResult(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// Método invocable desde el cliente para obtener información de su conexión actual.
        /// Útil para debugging y soporte técnico.
        /// 
        /// Uso en Angular:
        /// const info = await connection.invoke("GetConnectionInfo");
        /// console.log(info); // { connectionId: "abc", rol: "Admision", ... }
        /// </summary>
        /// <returns>Objeto con metadata de la conexión actual (propiedades en camelCase)</returns>
        public Task<object> GetConnectionInfo()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name);
            var rol = Context.User?.FindFirstValue(ClaimTypes.Role);
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var connectionId = Context.ConnectionId;
            var connectedAt = DateTime.UtcNow;

            // Objeto anónimo con sintaxis simplificada (camelCase para JavaScript/TypeScript)
            object info = new
            {
                connectionId,
                userId,
                userName,
                rol,
                ipAddress,
                connectedAt
            };

            return Task.FromResult(info);
        }

        /// <summary>
        /// Método invocable desde el cliente para obtener estadísticas del Hub.
        /// Solo disponible para roles administrativos.
        /// 
        /// Uso en Angular (Admin):
        /// const stats = await connection.invoke("GetHubStatistics");
        /// console.log(stats); // { totalConnections: 15, connectionsByRole: {...} }
        /// </summary>
        /// <returns>Estadísticas de conexiones activas</returns>
        [Authorize(Roles = "Administrador,JefeGuardia")]
        public async Task<object> GetHubStatistics()
        {
            try
            {
                var stats = await _connectionTracker.GetStatisticsAsync();

                _logger.LogInformation(
                    "SignalR: Estadísticas solicitadas por {UserName} ({Rol})",
                    Context.User?.FindFirstValue(ClaimTypes.Name),
                    Context.User?.FindFirstValue(ClaimTypes.Role)
                );

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Error al obtener estadísticas del Hub");
                throw new HubException("No se pudieron obtener las estadísticas del Hub");
            }
        }
    }
}