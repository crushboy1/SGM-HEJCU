namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipos de documentos digitalizados que se adjuntan al expediente mortuorio.
    /// Reemplaza los "juegos de copias físicas" del proceso manual.
    /// </summary>
    public enum TipoDocumentoExpediente
    {
        /// <summary>
        /// Copia del DNI del familiar o responsable del retiro.
        /// Requerido cuando TipoSalida == Familiar.
        /// Respalda la identidad de quien figura en el ActaRetiro.
        /// </summary>
        DNI_Familiar = 1,

        /// <summary>
        /// Copia del DNI u otro documento de identidad del fallecido.
        /// Requerido en todos los tipos de salida.
        /// </summary>
        DNI_Fallecido = 2,

        /// <summary>
        /// Certificado de Defunción / SINADEF generado por el médico.
        /// Requerido cuando TipoSalida == Familiar y muerte >= 24h.
        /// Usado posteriormente por el familiar para trámites en RENIEC.
        /// </summary>
        CertificadoDefuncion = 3,

        /// <summary>
        /// Oficio legal emitido por PNP, Fiscalía o Legista.
        /// Requerido cuando TipoSalida == AutoridadLegal (muerte violenta o menor a 24h).
        /// Reemplaza al CertificadoDefuncion en estos casos.
        /// </summary>
        OficioLegal = 4,

        /// <summary>
        /// Acta de levantamiento del cuerpo emitida por autoridad competente.
        /// Casos de muerte violenta o bajo investigación.
        /// Vinculado al ExpedienteLegal.
        /// </summary>
        ActaLevantamiento = 5,

        /// <summary>
        /// Documento adicional no categorizado.
        /// Usar campo Observaciones para describir el contenido.
        /// </summary>
        Otro = 6
    }
}