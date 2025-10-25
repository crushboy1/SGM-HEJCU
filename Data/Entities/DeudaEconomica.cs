namespace SisMortuorio.Data.Entities
{
    public class DeudaEconomica
    {
        public int DeudaEconomicaID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public decimal MontoDeuda { get; set; }
        public EstadoDeuda Estado { get; set; } = EstadoDeuda.Pendiente;
        public string? NumeroBoleta { get; set; }

        // Exoneración
        public decimal? PorcentajeExoneracion { get; set; }
        public string? ObservacionesExoneracion { get; set; }

        // Auditoría
        public int UsuarioRegistroID { get; set; }
        public Usuario UsuarioRegistro { get; set; } = null!;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public int? UsuarioActualizacionID { get; set; }
        public Usuario? UsuarioActualizacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public enum EstadoDeuda
    {
        Pendiente,
        Cancelado,
        Exonerado
    }
}