namespace SisMortuorio.Data.Entities
{
    public class DeudaSangre
    {
        public int DeudaSangreID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public bool TieneDeuda { get; set; }
        public string? Detalle { get; set; } // "3 unidades pendientes"
        public bool CompromisoFirmado { get; set; } // RN-21: No bloquea si hay compromiso

        // Auditoría
        public int UsuarioRegistroID { get; set; }
        public Usuario UsuarioRegistro { get; set; } = null!;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public int? UsuarioActualizacionID { get; set; }
        public Usuario? UsuarioActualizacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }
}