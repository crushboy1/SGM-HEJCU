namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados de la deuda de sangre del fallecido.
    /// Determina si se bloquea retiro según RN-21.
    /// </summary>
    public enum EstadoDeudaSangre
    {
        /// <summary>
        /// Paciente no usó unidades de sangre durante hospitalización.
        /// NO BLOQUEA retiro.
        /// </summary>
        SinDeuda = 1,

        /// <summary>
        /// Familiar firmó compromiso de reposición futura.
        /// Firmado por: Familiar, Médico Banco Sangre, Vigilante.
        /// NO BLOQUEA retiro según RN-21.
        /// </summary>
        Liquidado = 2,

        /// <summary>
        /// Médico de Banco de Sangre autorizó salida sin reposición.
        /// Caso social o excepcional. Requiere justificación documentada.
        /// NO BLOQUEA retiro.
        /// </summary>
        Anulado = 3,

        /// <summary>
        /// Deuda pendiente de resolución.
        /// Familiar aún no firma compromiso ni hay anulación médica.
        /// BLOQUEA retiro.
        /// </summary>
        Pendiente = 4
    }
}