namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Define el estado actual de una bandeja en el mortuorio.
    /// Una bandeja solo puede estar en uno de estos estados a la vez.
    /// </summary>
    public enum EstadoBandeja
    {
        /// <summary>
        /// Bandeja libre, lista para asignar
        /// </summary>
        Disponible = 1,

        /// <summary>
        /// Bandeja ocupada con un cuerpo
        /// </summary>
        Ocupada = 2,

        /// <summary>
        /// Bandeja en proceso de limpieza/desinfección (temporalmente no disponible)
        /// </summary>
        Mantenimiento = 3,

        /// <summary>
        /// Bandeja fuera de servicio por daños o reparaciones mayores
        /// </summary>
        FueraDeServicio = 4
    }

    /// <summary>
    /// Define el tipo de acción registrada en OcupacionBandeja.
    /// Usado para auditoría y reportes de movimientos.
    /// </summary>
    public enum AccionBandeja
    {
        /// <summary>
        /// Primera asignación de un cuerpo a la bandeja
        /// </summary>
        Asignacion = 1,

        /// <summary>
        /// Liberación de la bandeja (cuerpo retirado del mortuorio)
        /// </summary>
        Liberacion = 2,

        /// <summary>
        /// Liberación de la bandeja (cuerpo retirado del mortuorio manualmente)
        /// </summary>
        LiberacionManual = 3,

        /// <summary>
        /// Movimiento interno del cuerpo a otra bandeja
        /// </summary>
        Reasignacion = 4,

        /// <summary>
        /// Inicio de mantenimiento/limpieza de la bandeja
        /// </summary>
        InicioMantenimiento = 5,

        /// <summary>
        /// Fin de mantenimiento, bandeja lista para usar
        /// </summary>
        FinMantenimiento = 6
    }

}