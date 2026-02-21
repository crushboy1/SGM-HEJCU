namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Tipos de autoridades externas en casos de fallecimiento externo
    /// Registradas cuando hay intervención legal (PNP, Fiscalía, Medicina Legal)
    /// </summary>
    public enum TipoAutoridadExterna
    {
        /// <summary>
        /// Efectivo de la Policía Nacional del Perú
        /// Responsable del levantamiento inicial y custodia del cadáver
        /// Requiere: Comisaría de origen, Placa de patrullero
        /// </summary>
        Policia = 1,

        /// <summary>
        /// Fiscal del Ministerio Público
        /// Autoridad legal que supervisa el levantamiento de cadáver
        /// Requiere: Código de Fiscal, Fiscalía de turno
        /// </summary>
        Fiscal = 2,

        /// <summary>
        /// Médico Legista de la Morgue Central de Lima
        /// Responsable del retiro del cadáver para necropsia
        /// Requiere: CMP (Colegio Médico del Perú)
        /// </summary>
        MedicoLegista = 3
    }
}