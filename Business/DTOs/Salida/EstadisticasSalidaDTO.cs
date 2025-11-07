namespace SisMortuorio.Business.DTOs.Salida
{
    /// <summary>
    /// DTO de salida (Output) con estadísticas de salidas del mortuorio.
    /// Mapea 1:1 con la clase SalidaEstadisticas del repositorio.
    /// </summary>
    public class EstadisticasSalidaDTO
    {
        public int TotalSalidas { get; set; }
        public int SalidasFamiliar { get; set; }
        public int SalidasAutoridadLegal { get; set; }
        public int SalidasTrasladoHospital { get; set; }
        public int SalidasOtro { get; set; }
        public int ConIncidentes { get; set; }
        public int ConFuneraria { get; set; }
        public double PorcentajeIncidentes { get; set; }
    }
}