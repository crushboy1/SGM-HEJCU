namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Origen del ingreso al mortuorio.
    /// Interno: paciente fallece dentro del hospital.
    /// Externo: persona llega ya fallecida 
    /// NO determina TipoSalida — son dimensiones independientes.
    /// </summary>
    public enum TipoIngreso
    {
        /// <summary>
        /// Paciente hospitalizado que fallece dentro del hospital.
        /// Notificación origen: SIGEM/Hospitalización.
        /// </summary>
        Interno = 1,
        /// <summary>
        /// Persona que llega ya fallecida al hospital
        /// Notificación origen: SIGEM2/Traumashock.
        /// Requiere Epicrisis como primer documento.
        /// </summary>
        Externo = 2
    }
}