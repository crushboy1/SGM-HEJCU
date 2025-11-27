using System.Collections.Concurrent;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación thread-safe del servicio de tracking de conexiones SignalR.
    /// 
    /// Estructura de datos:
    /// - ConcurrentDictionary<ConnectionId, ConnectionInfo>
    /// - Thread-safe para múltiples conexiones/desconexiones simultáneas
    /// - Almacenamiento en memoria (se pierde al reiniciar el servidor)
    /// 
    /// Registro de servicio: Singleton (Program.cs)
    /// </summary>
    public class ConnectionTrackerService(ILogger<ConnectionTrackerService> logger) : IConnectionTrackerService
    {
        // Diccionario thread-safe de conexiones activas
        // Key: ConnectionId, Value: ConnectionInfo
        private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
        private readonly ILogger<ConnectionTrackerService> _logger = logger;

        /// <summary>
        /// Registra una nueva conexión.
        /// </summary>
        public Task AddConnectionAsync(string connectionId, string userId, string userName, string rol, string ipAddress)
        {
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = connectionId,
                UserId = userId,
                UserName = userName,
                Rol = rol,
                IpAddress = ipAddress,
                ConnectedAt = DateTime.UtcNow
            };

            // Agregar al diccionario (thread-safe)
            if (_connections.TryAdd(connectionId, connectionInfo))
            {
                _logger.LogDebug(
                    "ConnectionTracker: Conexión agregada. Total: {Total}, ConnectionId: {ConnectionId}, User: {UserName}",
                    _connections.Count,
                    connectionId,
                    userName
                );
            }
            else
            {
                _logger.LogWarning(
                    "ConnectionTracker: Intento de agregar conexión duplicada. ConnectionId: {ConnectionId}",
                    connectionId
                );
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Remueve una conexión cuando el usuario se desconecta.
        /// </summary>
        public Task RemoveConnectionAsync(string connectionId)
        {
            // Remover del diccionario (thread-safe)
            if (_connections.TryRemove(connectionId, out var removedConnection))
            {
                _logger.LogDebug(
                    "ConnectionTracker: Conexión removida. Total: {Total}, ConnectionId: {ConnectionId}, User: {UserName}",
                    _connections.Count,
                    connectionId,
                    removedConnection.UserName
                );
            }
            else
            {
                _logger.LogWarning(
                    "ConnectionTracker: Intento de remover conexión inexistente. ConnectionId: {ConnectionId}",
                    connectionId
                );
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtiene todas las conexiones de un usuario específico.
        /// </summary>
        public Task<List<string>> GetConnectionsByUserIdAsync(string userId)
        {
            var connections = _connections.Values
                .Where(c => c.UserId == userId)
                .Select(c => c.ConnectionId)
                .ToList();

            return Task.FromResult(connections);
        }

        /// <summary>
        /// Obtiene todas las conexiones de usuarios con un rol específico.
        /// </summary>
        public Task<List<string>> GetConnectionsByRoleAsync(string rol)
        {
            var connections = _connections.Values
                .Where(c => c.Rol.Equals(rol, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ConnectionId)
                .ToList();

            return Task.FromResult(connections);
        }

        /// <summary>
        /// Obtiene el total de usuarios únicos conectados.
        /// </summary>
        public Task<int> GetTotalUniqueUsersAsync()
        {
            var uniqueUsers = _connections.Values
                .Select(c => c.UserId)
                .Distinct()
                .Count();

            return Task.FromResult(uniqueUsers);
        }

        /// <summary>
        /// Obtiene el total de conexiones activas.
        /// </summary>
        public Task<int> GetTotalConnectionsAsync()
        {
            return Task.FromResult(_connections.Count);
        }

        /// <summary>
        /// Obtiene estadísticas completas para dashboard.
        /// </summary>
        public Task<object> GetStatisticsAsync()
        {
            var connectionsByRole = _connections.Values
                .GroupBy(c => c.Rol)
                .ToDictionary(g => g.Key, g => g.Count());

            var uniqueUsersByRole = _connections.Values
                .GroupBy(c => c.Rol)
                .ToDictionary(g => g.Key, g => g.Select(c => c.UserId).Distinct().Count());

            var stats = new
            {
                TotalConnections = _connections.Count,
                UniqueUsers = _connections.Values.Select(c => c.UserId).Distinct().Count(),
                ConnectionsByRole = connectionsByRole,
                UniqueUsersByRole = uniqueUsersByRole,
                Timestamp = DateTime.UtcNow
            };

            return Task.FromResult<object>(stats);
        }

        /// <summary>
        /// Verifica si un usuario está conectado.
        /// </summary>
        public Task<bool> IsUserConnectedAsync(string userId)
        {
            var isConnected = _connections.Values.Any(c => c.UserId == userId);
            return Task.FromResult(isConnected);
        }

        /// <summary>
        /// Limpia conexiones "zombies" que superan el timeout.
        /// Debe ser llamado periódicamente por un Worker.
        /// </summary>
        public Task<int> CleanupStaleConnectionsAsync(int timeoutMinutes = 60)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            var staleConnections = _connections.Values
                .Where(c => c.ConnectedAt < cutoffTime)
                .ToList();

            int removedCount = 0;

            foreach (var staleConnection in staleConnections)
            {
                if (_connections.TryRemove(staleConnection.ConnectionId, out _))
                {
                    removedCount++;
                    _logger.LogInformation(
                        "ConnectionTracker: Conexión zombie limpiada. ConnectionId: {ConnectionId}, User: {UserName}, Antigüedad: {Minutes} min",
                        staleConnection.ConnectionId,
                        staleConnection.UserName,
                        (DateTime.UtcNow - staleConnection.ConnectedAt).TotalMinutes
                    );
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation(
                    "ConnectionTracker: Limpieza completada. Conexiones removidas: {Count}",
                    removedCount
                );
            }

            return Task.FromResult(removedCount);
        }

        /// <summary>
        /// Clase interna para almacenar información de cada conexión.
        /// </summary>
        private class ConnectionInfo
        {
            public required string ConnectionId { get; set; } // ⭐ required para init-only
            public required string UserId { get; set; }
            public required string UserName { get; set; }
            public required string Rol { get; set; }
            public required string IpAddress { get; set; }
            public required DateTime ConnectedAt { get; set; }
        }
    }
}