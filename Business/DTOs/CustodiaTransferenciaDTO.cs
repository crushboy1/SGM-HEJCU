namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO para consultar historial de transferencias de custodia
    /// </summary>
    public class CustodiaTransferenciaDTO
    {
        public int TransferenciaID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;

        // Usuario origen
        public int UsuarioOrigenID { get; set; }
        public string UsuarioOrigenNombre { get; set; } = string.Empty;
        public string UsuarioOrigenRol { get; set; } = string.Empty;

        // Usuario destino
        public int UsuarioDestinoID { get; set; }
        public string UsuarioDestinoNombre { get; set; } = string.Empty;
        public string UsuarioDestinoRol { get; set; } = string.Empty;

        // Información de la transferencia
        public DateTime FechaHoraTransferencia { get; set; }
        public string UbicacionOrigen { get; set; } = string.Empty;
        public string UbicacionDestino { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}