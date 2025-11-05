using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Define el contrato para el servicio de máquina de estados.
    /// Es responsable de validar y ejecutar todas las transiciones
    /// de estado de un expediente.
    /// </summary>
    public interface IStateMachineService
    {
        /// <summary>
        /// Dispara un trigger en la máquina de estados de un expediente.
        /// Automáticamente valida si la transición es permitida y 
        /// actualiza el estado en la entidad (en memoria).
        /// </summary>
        /// <param name="expediente">La entidad Expediente a modificar.</param>
        /// <param name="trigger">La acción que se desea ejecutar.</param>
        /// <exception cref="InvalidOperationException">Si la transición no es válida.</exception>
        Task FireAsync(Expediente expediente, TriggerExpediente trigger);

        /// <summary>
        /// Verifica si una transición es válida sin ejecutarla.
        /// </summary>
        /// <param name="expediente">El expediente en su estado actual.</param>
        /// <param name="trigger">La acción que se desea verificar.</param>
        bool CanFire(Expediente expediente, TriggerExpediente trigger);

        /// <summary>
        /// Obtiene una lista de los triggers permitidos desde el estado actual del expediente.
        /// </summary>
        /// <param name="expediente">El expediente en su estado actual.</param>
        Task<IEnumerable<TriggerExpediente>> GetPermittedTriggersAsync(Expediente expediente);

        /// <summary>
        /// Genera un diagrama visual de la máquina de estados en formato DOT (Graphviz).
        /// Útil para documentación.
        /// </summary>
        string GenerateDotGraph();
    }
}