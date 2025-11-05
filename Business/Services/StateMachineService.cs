using Microsoft.Extensions.Logging;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using Stateless;
using Stateless.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de máquina de estados centralizada para el expediente.
    /// Implementa IStateMachineService y es inyectado (Scoped) en otros servicios.
    /// </summary>
    public class StateMachineService : IStateMachineService
    {
        private readonly ILogger<StateMachineService> _logger;

        // El constructor ahora es inyectable por DI y solo recibe el Logger.
        public StateMachineService(ILogger<StateMachineService> logger)
        {
            _logger = logger;
            // ¡Toda la lógica de configuración se ha movido al método GetMachine!
        }

        /// <summary>
        /// Crea y configura una instancia de la máquina de estados VINCULADA a un expediente específico.
        /// </summary>
        private StateMachine<EstadoExpediente, TriggerExpediente> GetMachine(Expediente expediente)
        {
            var machine = new StateMachine<EstadoExpediente, TriggerExpediente>(
                () => expediente.EstadoActual,  // Getter: Lee el estado actual
                s => expediente.EstadoActual = s   // Setter: Escribe el nuevo estado
            );

            // --- Definición Completa de la Máquina de Estados ---
            // Esta lógica se aplica CADA VEZ que se obtiene una instancia de la máquina.

            // 1. ESTADO: EnPiso
            machine.Configure(EstadoExpediente.EnPiso)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → EnPiso", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.GenerarQR, EstadoExpediente.PendienteDeRecojo);

            // 2. ESTADO: PendienteDeRecojo
            machine.Configure(EstadoExpediente.PendienteDeRecojo)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → PendienteDeRecojo", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.AceptarCustodia, EstadoExpediente.EnTrasladoMortuorio);

            // 3. ESTADO: EnTrasladoMortuorio
            machine.Configure(EstadoExpediente.EnTrasladoMortuorio)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → EnTrasladoMortuorio", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.VerificarIngresoMortuorio, EstadoExpediente.PendienteAsignacionBandeja)
                .Permit(TriggerExpediente.RechazarVerificacion, EstadoExpediente.VerificacionRechazadaMortuorio);

            // 4. ESTADO: VerificacionRechazadaMortuorio
            machine.Configure(EstadoExpediente.VerificacionRechazadaMortuorio)
                .OnEntry(t => _logger.LogWarning("⚠️ Expediente {CodigoExpediente}: Estado → VerificacionRechazadaMortuorio", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.CorregirDatos, EstadoExpediente.EnTrasladoMortuorio); // Vuelve a traslado

            // 5. ESTADO: PendienteAsignacionBandeja
            machine.Configure(EstadoExpediente.PendienteAsignacionBandeja)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → PendienteAsignacionBandeja", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.AsignarBandeja, EstadoExpediente.EnBandeja);

            // 6. ESTADO: EnBandeja
            machine.Configure(EstadoExpediente.EnBandeja)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → EnBandeja", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.AutorizarRetiro, EstadoExpediente.PendienteRetiro);

            // 7. ESTADO: PendienteRetiro
            machine.Configure(EstadoExpediente.PendienteRetiro)
                .OnEntry(t => _logger.LogInformation("Expediente {CodigoExpediente}: Estado → PendienteRetiro", expediente.CodigoExpediente))
                .Permit(TriggerExpediente.RegistrarSalida, EstadoExpediente.Retirado);

            // 8. ESTADO: Retirado (Estado final)
            machine.Configure(EstadoExpediente.Retirado)
                .OnEntry(t => _logger.LogInformation("✅ Expediente {CodigoExpediente}: Estado → Retirado (FINAL)", expediente.CodigoExpediente));

            // --- Logging Global de Transición ---
            machine.OnTransitioned(transition =>
            {
                _logger.LogInformation(
                    "Expediente {CodigoExpediente}: Transición de estado. " +
                    "Trigger: {Trigger}, Estado anterior: {EstadoAnterior}, Estado nuevo: {EstadoNuevo}",
                    expediente.CodigoExpediente,
                    transition.Trigger,
                    transition.Source,
                    transition.Destination
                );
            });

            return machine;
        }

        /// <summary>
        /// Dispara un trigger en la máquina de estados de un expediente.
        /// </summary>
        public async Task FireAsync(Expediente expediente, TriggerExpediente trigger)
        {
            var machine = GetMachine(expediente);

            if (!machine.CanFire(trigger))
            {
                var permittedTriggers = await machine.GetPermittedTriggersAsync();
                throw new InvalidOperationException(
                    $"Acción no permitida: No se puede ejecutar la acción '{trigger}' " +
                    $"desde el estado actual '{expediente.EstadoActual}'. " +
                    $"Acciones válidas: {string.Join(", ", permittedTriggers)}");
            }

            // Dispara el trigger. El 'setter' actualizará 'expediente.EstadoActual' en memoria.
            await machine.FireAsync(trigger);
        }

        /// <summary>
        /// Verifica si una transición es válida sin ejecutarla.
        /// </summary>
        public bool CanFire(Expediente expediente, TriggerExpediente trigger)
        {
            return GetMachine(expediente).CanFire(trigger);
        }

        /// <summary>
        /// Obtiene los triggers permitidos desde el estado actual.
        /// </summary>
        public async Task<IEnumerable<TriggerExpediente>> GetPermittedTriggersAsync(Expediente expediente)
        {
            return await GetMachine(expediente).GetPermittedTriggersAsync();
        }

        /// <summary>
        /// Genera un diagrama visual de la máquina de estados (útil para documentación).
        /// </summary>
        public string GenerateDotGraph()
        {
            // Creamos una máquina temporal solo para generar el gráfico
            // (No necesita un expediente real)
            var machine = GetMachine(new Expediente());
            return UmlDotGraph.Format(machine.GetInfo());
        }
    }
}