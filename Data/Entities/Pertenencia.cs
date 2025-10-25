namespace SisMortuorio.Data.Entities
{
    public class Pertenencia
    {
        public int PertenenciaID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // ConCuerpo, Entregado
        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}