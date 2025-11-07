namespace SisMortuorio.Business.DTOs.Solicitud
{
    /// <summary>
    /// DTO de salida (Output) con estadísticas de solicitudes de corrección.
    /// Mapea 1:1 con la clase SolicitudEstadisticas del repositorio.
    /// </summary>
    public class EstadisticasSolicitudDTO
    {
        public int TotalSolicitudes { get; set; }
        public int Pendientes { get; set; }
        public int Resueltas { get; set; }
        public int ConAlerta { get; set; } // Pendientes > 2 horas
        public double TiempoPromedioResolucionHoras { get; set; }
    }
}