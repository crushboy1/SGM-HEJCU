namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados posibles de la deuda económica del fallecido
    /// Determina si se bloquea retiro según RN-19 y RN-20
    /// </summary>
    public enum EstadoDeudaEconomica
    {
        /// <summary>
        /// Paciente SIS o sin consumos - Sin deuda
        /// NO BLOQUEA retiro
        /// </summary>
        SinDeuda = 1,

        /// <summary>
        /// Familiar pagó en Caja - Deuda liquidada
        /// NO BLOQUEA retiro
        /// </summary>
        Liquidado = 2,

        /// <summary>
        /// Servicio Social exoneró total o parcialmente
        /// NO BLOQUEA retiro (si monto final = 0)
        /// </summary>
        Exonerado = 3,

        /// <summary>
        /// Deuda pendiente de pago/exoneración
        /// BLOQUEA retiro
        /// </summary>
        Pendiente = 4
    }
}