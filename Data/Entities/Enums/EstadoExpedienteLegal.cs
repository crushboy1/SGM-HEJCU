namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados del expediente legal en el flujo híbrido.
    /// Vigilancia → Admisión → Jefe Guardia
    /// </summary>
    public enum EstadoExpedienteLegal
    {
        /// <summary>
        /// Expediente creado por Vigilancia, en proceso de completar documentos.
        /// RESPONSABLE: Vigilancia
        /// </summary>
        EnRegistro = 1,

        /// <summary>
        /// Vigilancia marcó como completo, pendiente de revisión por Admisión.
        /// RESPONSABLE: Vigilancia → Admisión
        /// </summary>
        PendienteValidacionAdmision = 2,

        /// <summary>
        /// Admisión rechazó documentación (faltan docs o están ilegibles).
        /// RESPONSABLE: Vigilancia (corregir)
        /// </summary>
        RechazadoAdmision = 3,

        /// <summary>
        /// Admisión validó documentación completa, pendiente firma Jefe Guardia.
        /// RESPONSABLE: Admisión → Jefe Guardia
        /// </summary>
        ValidadoAdmision = 4,

        /// <summary>
        /// Jefe de Guardia firmó oficio, autorizado para levantamiento.
        /// ESTADO FINAL
        /// </summary>
        AutorizadoJefeGuardia = 5
    }
}