namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Fuente de financiamiento del paciente fallecido.
    /// Reemplaza el campo TipoSeguro (string) en Expediente.
    /// Obtenido desde Galenhos; editable solo en registro manual.
    /// Default: PendientePago en todos los casos incluyendo NN.
    /// </summary>
    public enum FuenteFinanciamiento
    {
        /// <summary>
        /// Seguro Integral de Salud (Estado)
        /// </summary>
        SIS = 1,
        /// <summary>
        /// EsSalud (seguro social)
        /// </summary>
        EsSalud = 2,
        /// <summary>
        /// Pago directo sin seguro
        /// </summary>
        Particular = 3,
        /// <summary>
        /// Seguro Obligatorio de Accidentes de Tránsito
        /// </summary>
        SOAT = 4,
        /// <summary>
        /// Default para todos los casos al crear el expediente.
        /// Incluye pacientes NN. Se actualiza cuando Admisión confirma.
        /// </summary>
        PendientePago = 5,
        /// <summary>
        /// Otras coberturas no categorizadas
        /// </summary>
        Otros = 6
    }
}