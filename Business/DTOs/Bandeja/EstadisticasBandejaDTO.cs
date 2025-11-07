namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO de salida (Output) con las estadísticas de ocupación del mortuorio.
    /// Refleja la clase 'BandejaEstadisticas' del repositorio.
    /// </summary>
    public class EstadisticasBandejaDTO
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public int Ocupadas { get; set; }
        public int EnMantenimiento { get; set; }
        public int FueraDeServicio { get; set; }
        public double PorcentajeOcupacion { get; set; }
        public int ConAlerta24h { get; set; }
        public int ConAlerta48h { get; set; }
    }
}