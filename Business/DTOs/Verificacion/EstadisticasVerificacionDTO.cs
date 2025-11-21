namespace SisMortuorio.Business.DTOs.Verificacion
{
    /// <summary>
    /// DTO de salida (Output) con estadísticas de verificaciones.
    /// </summary>
    public class EstadisticasVerificacionDTO
    {
        public int TotalVerificaciones { get; set; }
        public int Aprobadas { get; set; }
        public int Rechazadas { get; set; }
        public double PorcentajeAprobacion { get; set; }
        public int ConDiscrepanciaHC { get; set; }
        public int ConDiscrepanciaDocumento { get; set; }
        public int ConDiscrepanciaNombre { get; set; }
    }
}