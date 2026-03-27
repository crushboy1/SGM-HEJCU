namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO con la información completa de una bandeja.
    /// Usado para el mapa visual del mortuorio.
    /// </summary>
    public class BandejaDTO
    {
        public int BandejaID { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // Disponible, Ocupada, Mantenimiento
        public string? Observaciones { get; set; }

        // Datos de Ocupación (si Estado == Ocupada)
        public int? ExpedienteID { get; set; }
        public string? CodigoExpediente { get; set; }
        public string? NombrePaciente { get; set; }
        public string? UsuarioAsignaNombre { get; set; }
        public DateTime? FechaHoraAsignacion { get; set; }
        public string? TiempoOcupada { get; set; } // Ej: "2h 30m"
        public bool TieneAlerta { get; set; } = false; // > 24h

        // Datos de Liberación (si ya fue liberada)
        public string? UsuarioLiberaNombre { get; set; }
        public DateTime? FechaHoraLiberacion { get; set; }

        // Auditoría
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        // Solo tienen valor cuando Estado = "Mantenimiento"

        /// <summary>Motivo: Limpieza | Reparacion | InspeccionSanitaria | FallaTecnica | Otro</summary>
        public string? MotivoMantenimiento { get; set; }

        /// <summary>Descripción libre del mantenimiento.</summary>
        public string? DetalleMantenimiento { get; set; }

        /// <summary>Fecha y hora de inicio del mantenimiento.</summary>
        public DateTime? FechaInicioMantenimiento { get; set; }

        /// <summary>Fecha estimada de fin (para planificación del turno).</summary>
        public DateTime? FechaEstimadaFinMantenimiento { get; set; }

        /// <summary>Nombre del responsable externo (texto libre).</summary>
        public string? ResponsableMantenimiento { get; set; }

        /// <summary>Nombre del usuario SGM que registró el mantenimiento.</summary>
        public string? UsuarioRegistraMantenimientoNombre { get; set; }
    }
}