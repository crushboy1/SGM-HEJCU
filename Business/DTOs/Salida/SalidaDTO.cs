namespace SisMortuorio.Business.DTOs.Salida
{
    /// <summary>
    /// DTO de salida (Output) que representa un registro de salida ya completado.
    /// </summary>
    public class SalidaDTO
    {
        public int SalidaID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public DateTime FechaHoraSalida { get; set; }
        public string TipoSalida { get; set; } = string.Empty; // "Familiar", "AutoridadLegal", etc.

        // Datos clave del retiro
        public string ResponsableNombre { get; set; } = string.Empty;
        public string ResponsableDocumento { get; set; } = string.Empty; // "DNI 12345678"
        public string VigilanteNombre { get; set; } = string.Empty;
        public string? NombreFuneraria { get; set; }
        public string? Destino { get; set; }

        // Incidentes
        public bool IncidenteRegistrado { get; set; }
        public string? DetalleIncidente { get; set; }
    }
}