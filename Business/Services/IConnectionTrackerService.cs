namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para trackear conexiones activas de SignalR.
    /// 
    /// Responsabilidades:
    /// 1. Mantener registro de qué usuarios están conectados
    /// 2. Permitir consultas de conexiones por usuario/rol
    /// 3. Proveer estadísticas para dashboard administrativo
    /// 4. Limpiar conexiones "zombies" (timeout)
    /// 
    /// Implementación: Thread-safe usando ConcurrentDictionary
    /// Scope: Singleton (una instancia compartida para toda la app)
    /// </summary>
    public interface IConnectionTrackerService
    {
        /// <summary>
        /// Registra una nueva conexión cuando un usuario se conecta al Hub.
        /// </summary>
        /// <param name="connectionId">ID único de la conexión de SignalR</param>
        /// <param name="userId">ID del usuario (NameIdentifier claim)</param>
        /// <param name="userName">Nombre del usuario (Name claim)</param>
        /// <param name="rol">Rol del usuario (Role claim)</param>
        /// <param name="ipAddress">IP del cliente</param>
        Task AddConnectionAsync(string connectionId, string userId, string userName, string rol, string ipAddress);

        /// <summary>
        /// Remueve una conexión cuando un usuario se desconecta del Hub.
        /// </summary>
        /// <param name="connectionId">ID único de la conexión de SignalR</param>
        Task RemoveConnectionAsync(string connectionId);

        /// <summary>
        /// Obtiene todos los ConnectionIds de un usuario específico.
        /// Un usuario puede tener múltiples conexiones (múltiples tabs/dispositivos).
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de ConnectionIds activos de ese usuario</returns>
        Task<List<string>> GetConnectionsByUserIdAsync(string userId);

        /// <summary>
        /// Obtiene todos los ConnectionIds de usuarios con un rol específico.
        /// Útil para enviar mensajes masivos a un rol sin usar Groups.
        /// </summary>
        /// <param name="rol">Nombre del rol</param>
        /// <returns>Lista de ConnectionIds de ese rol</returns>
        Task<List<string>> GetConnectionsByRoleAsync(string rol);

        /// <summary>
        /// Obtiene el total de usuarios únicos conectados actualmente.
        /// (Un usuario con 3 tabs abiertas cuenta como 1)
        /// </summary>
        /// <returns>Cantidad de usuarios únicos conectados</returns>
        Task<int> GetTotalUniqueUsersAsync();

        /// <summary>
        /// Obtiene el total de conexiones activas.
        /// (Un usuario con 3 tabs abiertas cuenta como 3)
        /// </summary>
        /// <returns>Cantidad de conexiones activas</returns>
        Task<int> GetTotalConnectionsAsync();

        /// <summary>
        /// Obtiene estadísticas completas para dashboard administrativo.
        /// </summary>
        /// <returns>Objeto con estadísticas detalladas</returns>
        Task<object> GetStatisticsAsync();

        /// <summary>
        /// Verifica si un usuario específico está conectado actualmente.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>true si tiene al menos una conexión activa</returns>
        Task<bool> IsUserConnectedAsync(string userId);

        /// <summary>
        /// Limpia conexiones antiguas que superan el timeout (conexiones zombies).
        /// Este método debe ser llamado periódicamente por un Worker.
        /// </summary>
        /// <param name="timeoutMinutes">Minutos de inactividad para considerar zombie</param>
        /// <returns>Cantidad de conexiones limpiadas</returns>
        Task<int> CleanupStaleConnectionsAsync(int timeoutMinutes = 60);
    }
}