namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipos de documentos legales en casos de fallecimiento externo
    /// Documentos escaneados que conforman el "Expediente Legal Digital"
    /// </summary>
    public enum TipoDocumentoLegal
    {
        /// <summary>
        /// Documento médico para fallecimientos <24-48hrs
        /// Emitido por médico de Traumashock
        /// </summary>
        Epicrisis = 1,

        /// <summary>
        /// Solicitud oficial de entrega de cadáver
        /// Emitido por Comisaría del sector
        /// Requiere firma del Jefe de Guardia
        /// </summary>
        OficioPolicial = 2,

        /// <summary>
        /// Acta de levantamiento de cadáver
        /// Firmada por: Fiscal + Médico Legista + Policía
        /// </summary>
        ActaLevantamiento = 3,

        /// <summary>
        /// Certificado de defunción SINADEF (casos internos)
        /// Solo para referencia en casos externos donde también aplica
        /// </summary>
        CertificadoDefuncion = 4,

        /// <summary>
        /// Ficha de atención (Historia Clínica resumida)
        /// Documento de referencia para autoridades
        /// </summary>
        FichaAtencion = 5,

        /// <summary>
        /// Certificado emitido por médico externo contratado por familia
        /// Solo para casos <24-48hrs sin obligación SINADEF
        /// </summary>
        CertificadoMedicoExterno = 6,

        /// <summary>
        /// Otros documentos legales no clasificados
        /// Requiere descripción en observaciones
        /// </summary>
        Otros = 99
    }
}