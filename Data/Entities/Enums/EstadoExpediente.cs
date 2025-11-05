namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados del expediente en el flujo del mortuorio
    /// Representa la máquina de estados del sistema
    /// </summary>
    public enum EstadoExpediente
    {
        /// <summary>
        /// Expediente creado, cuerpo aún en el servicio
        /// </summary>
        EnPiso = 1,

        /// <summary>
        /// QR generado, esperando que Ambulancia recoja
        /// </summary>
        PendienteDeRecojo = 2,

        /// <summary>
        /// Ambulancia aceptó custodia, en traslado hacia mortuorio
        /// </summary>
        EnTrasladoMortuorio = 3,

        /// <summary>
        /// Vigilante rechazó verificación, esperando corrección de datos
        /// </summary>
        VerificacionRechazadaMortuorio = 4,

        /// <summary>
        /// Vigilante verificó ingreso. Cuerpo en puerta de mortuorio.
        /// Esperando asignación de bandeja por el técnico.
        /// </summary>
        PendienteAsignacionBandeja = 5,

        /// <summary>
        /// Técnico asignó bandeja. Cuerpo físicamente dentro del mortuorio.
        /// </summary>
        EnBandeja = 6,

        /// <summary>
        /// Cuerpo en bandeja, familiar autorizado, esperando retiro
        /// </summary>
        PendienteRetiro = 7,

        /// <summary>
        /// Cuerpo retirado del mortuorio, bandeja liberada
        /// Estado final
        /// </summary>
        Retirado = 8
    }
}