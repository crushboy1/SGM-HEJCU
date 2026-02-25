namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Estados de verificación de un documento digitalizado del expediente.
    /// El Admisionista sube el documento y lo verifica físicamente contra el original.
    /// </summary>
    public enum EstadoDocumentoExpediente
    {
        /// <summary>
        /// Documento subido al sistema, pendiente de verificación física.
        /// El Admisionista debe comparar el archivo con el original presentado.
        /// </summary>
        PendienteVerificacion = 1,

        /// <summary>
        /// Admisionista verificó que el documento es legible y coincide con el original.
        /// Este estado habilita el conteo para DocumentosCompletosOK.
        /// </summary>
        Verificado = 2,

        /// <summary>
        /// Documento rechazado por ser ilegible, incompleto o no coincidir con el original.
        /// El familiar debe presentar nuevamente el documento.
        /// </summary>
        Rechazado = 3
    }
}