namespace SisMortuorio.Business.DTOs.Solicitud
{
    /// <summary>
    /// DTO de salida (Output) con la información completa de una
    /// solicitud de corrección (para listados y detalles).
    /// </summary>
    public class SolicitudCorreccionDTO
    {
        public int SolicitudID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;

        // Solicitud
        public DateTime FechaHoraSolicitud { get; set; }
        public string UsuarioSolicitaNombre { get; set; } = string.Empty; // Vigilante
        public string UsuarioResponsableNombre { get; set; } = string.Empty; // Enfermera
        public string DescripcionProblema { get; set; } = string.Empty;
        public string DatosIncorrectos { get; set; } = string.Empty; // JSON
        public string? ObservacionesSolicitud { get; set; }

        // Resolución
        public bool Resuelta { get; set; }
        public DateTime? FechaHoraResolucion { get; set; }
        public string? DescripcionResolucion { get; set; }
        public bool BrazaleteReimpreso { get; set; }

        // Campos Calculados (se llenarán en el Mapper)
        public string TiempoTranscurrido { get; set; } = string.Empty; // "2h 15m" o "30m"
        public bool SuperaTiempoAlerta { get; set; } // > 2 horas
    }
}