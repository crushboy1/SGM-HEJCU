namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipo de exoneración económica aplicada por Servicio Social
    /// </summary>
    public enum TipoExoneracion
    {
        /// <summary>
        /// Sin exoneración - Debe pagar monto completo
        /// </summary>
        SinExoneracion = 0,

        /// <summary>
        /// Servicio Social exoneró parte del monto
        /// Ej: Exonera S/ 800 de una deuda de S/ 1200
        /// </summary>
        Parcial = 1,

        /// <summary>
        /// Servicio Social exoneró el 100% de la deuda
        /// </summary>
        Total = 2
    }
}