namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipos de salida del mortuorio
    /// Determina quién retira el cuerpo y qué documentación se requiere
    /// </summary>
    public enum TipoSalida
    {
        /// <summary>
        /// Retiro por familiar directo (padre, madre, hijo, cónyuge, hermano)
        /// Destino: Cementerio o Campo Santo (especificado por familia)
        /// Requiere: Acta de Retiro tripartita firmada
        /// </summary>
        Familiar = 1,

        /// <summary>
        /// Retiro por autoridades (PNP, Fiscalía, Médico Legista)
        /// Destino: Morgue Central (implícito)
        /// Requiere: Oficio policial/fiscal, Expediente Legal
        /// Casos: Muerte < 24h, muerte violenta, sospecha criminal
        /// </summary>
        AutoridadLegal = 2
    }
}