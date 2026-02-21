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
    }
}