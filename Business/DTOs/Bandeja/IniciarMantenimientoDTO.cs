namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO para iniciar el mantenimiento de una bandeja.
    /// Captura todos los datos del modal de mantenimiento del Mapa Mortuorio.
    /// Roles autorizados: Administrador, JefeGuardia, VigilanteSupervisor.
    /// </summary>
    public class IniciarMantenimientoDTO
    {
        /// <summary>
        /// Motivo principal del mantenimiento.
        /// Valores válidos: Limpieza | Reparacion | InspeccionSanitaria | FallaTecnica | Otro
        /// </summary>
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Descripción libre del mantenimiento (opcional).
        /// Ejemplo: "Limpieza profunda por derrame. Coordinado con Servicios Generales."
        /// </summary>
        public string? Detalle { get; set; }

        /// <summary>
        /// Fecha y hora de inicio del mantenimiento.
        /// El frontend la envía en formato ISO 8601 (hora local → UTC).
        /// Si no se envía, el backend usa DateTime.Now.
        /// </summary>
        public DateTime? FechaInicio { get; set; }

        /// <summary>
        /// Fecha y hora estimada de finalización (opcional).
        /// Útil para planificación del turno.
        /// </summary>
        public DateTime? FechaEstimadaFin { get; set; }

        /// <summary>
        /// Nombre del responsable externo que ejecutará el mantenimiento (opcional).
        /// Ejemplo: "García - Servicios Generales"
        /// </summary>
        public string? ResponsableExterno { get; set; }
    }

    /// <summary>
    /// Valores válidos para el campo Motivo de IniciarMantenimientoDTO.
    /// Usados tanto en backend (validación) como en frontend (select).
    /// </summary>
    public static class MotivoMantenimiento
    {
        public const string Limpieza = "Limpieza";
        public const string Reparacion = "Reparacion";
        public const string InspeccionSanitaria = "InspeccionSanitaria";
        public const string FallaTecnica = "FallaTecnica";
        public const string Otro = "Otro";

        public static readonly string[] Valores =
        [
            Limpieza, Reparacion, InspeccionSanitaria, FallaTecnica, Otro
        ];

        public static bool EsValido(string motivo) =>
            Valores.Contains(motivo, StringComparer.OrdinalIgnoreCase);
    }
}