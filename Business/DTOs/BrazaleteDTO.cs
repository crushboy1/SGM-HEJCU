namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO con información para generar brazalete
    /// </summary>
    public class BrazaleteDTO
    {
        public string CodigoExpediente { get; set; } = string.Empty;
        public string CodigoQR { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public DateTime FechaHoraFallecimiento { get; set; }
        public string ServicioFallecimiento { get; set; } = string.Empty;
        public string? NumeroCama { get; set; }
        public string RutaImagenQR { get; set; } = string.Empty;
        public byte[]? PDFBytes { get; set; }
        public string? RutaPDF { get; set; }
    }
}