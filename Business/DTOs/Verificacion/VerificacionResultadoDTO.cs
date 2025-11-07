namespace SisMortuorio.Business.DTOs.Verificacion
{
    /// <summary>
    /// DTO de salida (Output) que informa el resultado de un intento de verificación.
    /// </summary>
    public class VerificacionResultadoDTO
    {
        public int VerificacionID { get; set; }
        public DateTime FechaHoraVerificacion { get; set; }

        // Resultado General
        public bool Aprobada { get; set; }
        public string MensajeResultado { get; set; } = string.Empty;
        public string EstadoExpedienteNuevo { get; set; } = string.Empty;

        // Resultado Detallado
        public bool HCCoincide { get; set; }
        public bool DNICoincide { get; set; }
        public bool NombreCoincide { get; set; }
        public bool ServicioCoincide { get; set; }
        public bool CodigoExpedienteCoincide { get; set; }

        // En caso de Rechazo
        public string? MotivoRechazo { get; set; }
        public int? SolicitudCorreccionID { get; set; }
    }
}