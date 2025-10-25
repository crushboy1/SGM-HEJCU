namespace SisMortuorio.Data.Entities
{
    public class OcupacionBandeja
    {
        public int OcupacionID { get; set; }
        public string BandejaID { get; set; } = string.Empty; // "A-01" a "A-08"

        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public DateTime FechaHoraIngreso { get; set; } = DateTime.Now;
        public DateTime? FechaHoraSalida { get; set; }

        public int UsuarioAsignadorID { get; set; }
        public Usuario UsuarioAsignador { get; set; } = null!;
    }
}