namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO de respuesta al realizar un traspaso de custodia exitoso
    /// </summary>
    public class TraspasoRealizadoDTO
    {
        public int TransferenciaID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;

        // Información de custodia
        public int UsuarioOrigenID { get; set; }
        public string UsuarioOrigen { get; set; } = string.Empty;
        public string RolOrigen { get; set; } = string.Empty;

        public int UsuarioDestinoID { get; set; }
        public string UsuarioDestino { get; set; } = string.Empty;
        public string RolDestino { get; set; } = string.Empty;

        public DateTime FechaHoraTransferencia { get; set; }
        public string UbicacionOrigen { get; set; } = string.Empty;
        public string UbicacionDestino { get; set; } = string.Empty;

        // Cambio de estado
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;

        public string? Observaciones { get; set; }
    }
}