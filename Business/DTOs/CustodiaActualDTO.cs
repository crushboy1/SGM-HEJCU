namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO simplificado para mostrar quién tiene la custodia actualmente
    /// </summary>
    public class CustodiaActualDTO
    {
        public int TransferenciaID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;

        // Usuario que tiene la custodia actualmente
        public int UsuarioActualID { get; set; }
        public string UsuarioActualNombre { get; set; } = string.Empty;
        public string UsuarioActualRol { get; set; } = string.Empty;

        // Información temporal
        public DateTime FechaHoraRecepcion { get; set; }

        // Ubicación actual
        public string UbicacionActual { get; set; } = string.Empty;

        // Estado actual del expediente
        public string EstadoActual { get; set; } = string.Empty;
    }
}