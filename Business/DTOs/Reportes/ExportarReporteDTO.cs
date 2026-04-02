namespace SisMortuorio.Business.DTOs.Reportes
{
    /// <summary>
    /// DTO para solicitar exportación de reportes.
    /// Usado en los endpoints POST /api/Reportes/exportar/*.
    /// </summary>
    public class ExportarReporteDTO
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        /// <summary>Solo para permanencia — retorna únicamente activos.</summary>
        public bool SoloActivos { get; set; } = false;
    }
}