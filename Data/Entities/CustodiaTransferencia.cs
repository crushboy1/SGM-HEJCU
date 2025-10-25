namespace SisMortuorio.Data.Entities
{
    public class CustodiaTransferencia
    {
        public int TransferenciaID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public int UsuarioOrigenID { get; set; }
        public Usuario UsuarioOrigen { get; set; } = null!;

        public int UsuarioDestinoID { get; set; }
        public Usuario UsuarioDestino { get; set; } = null!;

        public DateTime FechaHoraTransferencia { get; set; } = DateTime.Now;
        public string UbicacionOrigen { get; set; } = string.Empty;
        public string UbicacionDestino { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}