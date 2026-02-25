namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados del Acta de Retiro en el flujo administrativo.
    /// Admisión crea y firma → Vigilante registra salida física.
    /// </summary>
    public enum EstadoActaRetiro
    {
        /// <summary>
        /// Acta creada por Admisión, pendiente de firma física.
        /// El vigilante NO puede registrar salida en este estado.
        /// RESPONSABLE: Admisión
        /// </summary>
        Borrador = 1,

        /// <summary>
        /// Acta impresa y firmada físicamente por las partes.
        /// Habilita al vigilante para registrar la salida del cuerpo.
        /// RESPONSABLE: Admisión → Vigilante
        /// </summary>
        Firmada = 2,

        /// <summary>
        /// Acta anulada por error u otro motivo.
        /// Requiere crear una nueva acta para proceder.
        /// RESPONSABLE: Admisión / Supervisor
        /// </summary>
        Anulada = 3
    }
}