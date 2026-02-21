namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipos de documentos de identidad válidos en el sistema
    /// Soporta tanto nacionales como extranjeros
    /// </summary>
    public enum TipoDocumentoIdentidad
    {
        /// <summary>
        /// Documento Nacional de Identidad (Perú)
        /// 8 dígitos numéricos
        /// </summary>
        DNI = 1,

        /// <summary>
        /// Pasaporte internacional
        /// Formato alfanumérico según país emisor
        /// </summary>
        Pasaporte = 2,

        /// <summary>
        /// Carné de Extranjería (emitido por Migraciones Perú)
        /// Formato alfanumérico
        /// </summary>
        CarneExtranjeria = 3,

        /// <summary>
        /// Persona sin documento de identidad
        /// Casos excepcionales (indigentes, situaciones de emergencia)
        /// </summary>
        SinDocumento = 4,

        /// <summary>
        /// No Identificado / Paciente NN
        /// Se asigna HC temporal hasta identificación
        /// Formato: "NN-DDMMYYYY"
        /// </summary>
        NN = 5
    }
}