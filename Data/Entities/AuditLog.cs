namespace SisMortuorio.Data.Entities
{
    public class AuditLog
    {
        public int LogID { get; set; }

        public int? ExpedienteID { get; set; }
        public Expediente? Expediente { get; set; }

        public int UsuarioID { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public string Modulo { get; set; } = string.Empty; // "Expedientes", "Custodia", etc.
        public string Accion { get; set; } = string.Empty; // "Crear", "Actualizar", "Eliminar", etc.

        public string? DatosAntes { get; set; } // JSON
        public string? DatosDespues { get; set; } // JSON

        public string? IPOrigen { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string? Observaciones { get; set; }
    }
}