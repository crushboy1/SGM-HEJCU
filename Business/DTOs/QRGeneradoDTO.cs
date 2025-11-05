namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO de respuesta al generar un código QR
    /// </summary>
    public class QRGeneradoDTO
    {
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string CodigoQR { get; set; } = string.Empty;
        public string RutaImagenQR { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}