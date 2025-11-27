using SisMortuorio.Business.Services;

namespace SisMortuorio.Business.Workers
{
    /// <summary>
    /// Worker para limpiar conexiones SignalR "zombies" periódicamente.
    /// 
    /// Problema que resuelve:
    /// Si un cliente (Angular) se desconecta abruptamente (crash, pérdida de red),
    /// SignalR podría no detectarlo inmediatamente. Esto causa que el ConnectionTracker
    /// mantenga conexiones "fantasma" que consumen memoria y falsean estadísticas.
    /// 
    /// Solución:
    /// Cada 30 minutos, este worker consulta al ConnectionTracker y limpia conexiones
    /// con más de 60 minutos de antigüedad sin actividad.
    /// 
    /// Configuración:
    /// - Intervalo de ejecución: 30 minutos (configurable)
    /// - Timeout de conexión: 60 minutos (configurable)
    /// - Registro: Singleton (Program.cs) con AddHostedService
    /// </summary>
    public class ConnectionCleanupWorker(
        ILogger<ConnectionCleanupWorker> logger,
        IConnectionTrackerService connectionTracker) : BackgroundService 
    {
        private readonly ILogger<ConnectionCleanupWorker> _logger = logger;
        private readonly IConnectionTrackerService _connectionTracker = connectionTracker;

        // Configuración del worker
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);  // Ejecutar cada 30 min
        private readonly int _connectionTimeoutMinutes = 60;                    // Considerar zombie tras 60 min

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "ConnectionCleanupWorker iniciado. Intervalo: {Interval} min, Timeout: {Timeout} min",
                _cleanupInterval.TotalMinutes,
                _connectionTimeoutMinutes
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupStaleConnectionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "ConnectionCleanupWorker: Error inesperado durante limpieza"
                    );
                }

                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("ConnectionCleanupWorker: Cancelación solicitada, deteniendo...");
                    break;
                }
            }

            _logger.LogInformation("ConnectionCleanupWorker detenido");
        }

        /// <summary>
        /// Ejecuta la limpieza de conexiones zombies.
        /// Accede directamente al ConnectionTracker (Singleton).
        /// </summary>
        private async Task CleanupStaleConnectionsAsync()
        {
            _logger.LogDebug("ConnectionCleanupWorker: Iniciando limpieza de conexiones zombies");

            try
            {
                // Obtener estadísticas antes de la limpieza
                var statsBefore = await _connectionTracker.GetStatisticsAsync();
                var totalBefore = GetTotalFromStats(statsBefore);

                _logger.LogInformation(
                    "ConnectionCleanupWorker: Conexiones antes de limpieza: {Total}",
                    totalBefore
                );

                // Ejecutar limpieza
                var removedCount = await _connectionTracker.CleanupStaleConnectionsAsync(_connectionTimeoutMinutes);

                if (removedCount > 0)
                {
                    var statsAfter = await _connectionTracker.GetStatisticsAsync();
                    var totalAfter = GetTotalFromStats(statsAfter);

                    _logger.LogWarning(
                        "ConnectionCleanupWorker: Limpieza completada. " +
                        "Conexiones removidas: {RemovedCount}, Total restante: {TotalAfter}",
                        removedCount,
                        totalAfter
                    );
                }
                else
                {
                    _logger.LogDebug(
                        "ConnectionCleanupWorker: No se encontraron conexiones zombies para limpiar"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ConnectionCleanupWorker: Error al ejecutar limpieza de conexiones"
                );
            }
        }

        /// <summary>
        /// Extrae el total de conexiones del objeto de estadísticas dinámico.
        /// </summary>
        private static int GetTotalFromStats(object stats)
        {
            try
            {
                var statsType = stats.GetType();
                var totalProp = statsType.GetProperty("TotalConnections");

                if (totalProp != null)
                {
                    var value = totalProp.GetValue(stats);
                    return value is int total ? total : 0;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Se ejecuta cuando la aplicación se detiene.
        /// Permite cleanup final antes de apagar el worker.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "ConnectionCleanupWorker: Deteniendo worker (ejecutando limpieza final)..."
            );

            try
            {
                await CleanupStaleConnectionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConnectionCleanupWorker: Error en limpieza final");
            }

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("ConnectionCleanupWorker: Worker detenido completamente");
        }
    }
}