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
        /// Movimiento interno del cuerpo a otra bandeja
        /// </summary>
        Reasignacion = 3,

        /// <summary>
        /// Inicio de mantenimiento/limpieza de la bandeja
        /// </summary>
        InicioMantenimiento = 4,

        /// <summary>
        /// Fin de mantenimiento, bandeja lista para usar
        /// </summary>
        FinMantenimiento = 5
    }

    /// <summary>
    /// Define el tipo de salida del cuerpo del mortuorio.
    /// Determina el flujo de autorización y documentación requerida.
    /// </summary>
    public enum TipoSalida
    {
        /// <summary>
        /// Retiro por familiares autorizados (caso normal)
        /// </summary>
        Familiar = 1,

        /// <summary>
        /// Retiro por autoridades legales (fiscalía, PNP, medicina legal)
        /// </summary>
        AutoridadLegal = 2,

        /// <summary>
        /// Traslado a otro establecimiento de salud
        /// </summary>
        TrasladoHospital = 3,

        /// <summary>
        /// Otros casos no contemplados
        /// </summary>
        Otro = 4
    }
}