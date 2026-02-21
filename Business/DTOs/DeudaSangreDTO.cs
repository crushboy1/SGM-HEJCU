namespace SisMortuorio.Business.DTOs
{
    // ═══════════════════════════════════════════════════════════
    // DTO PRINCIPAL
    // ═══════════════════════════════════════════════════════════

    public class DeudaSangreDTO
    {
        public int DeudaSangreID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // "Pendiente", "Liquidado", etc.
        public int CantidadUnidades { get; set; }
        public string? TipoSangre { get; set; }
        public string? NombreFamiliarCompromiso { get; set; }
        public string? DNIFamiliarCompromiso { get; set; }
        public DateTime? FechaLiquidacion { get; set; }
        public string? RutaPDFCompromiso { get; set; }
        public bool AnuladaPorMedico { get; set; }
        public int? MedicoAnulaID { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string? JustificacionAnulacion { get; set; }
        public int UsuarioRegistroID { get; set; }
        public string UsuarioRegistroNombre { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool BloqueaRetiro { get; set; }
        public string Semaforo { get; set; } = string.Empty; // "PENDIENTE", " LIQUIDADO"
    }

    // ═══════════════════════════════════════════════════════════
    // CREATE DTO
    // ═══════════════════════════════════════════════════════════

    public class CreateDeudaSangreDTO
    {
        public int ExpedienteID { get; set; }
        public int CantidadUnidades { get; set; }
        public string? TipoSangre { get; set; }
        public int UsuarioRegistroID { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // LIQUIDAR DTO
    // ═══════════════════════════════════════════════════════════

    public class LiquidarDeudaSangreDTO
    {
        public string NombreFamiliarCompromiso { get; set; } = string.Empty;
        public string DNIFamiliarCompromiso { get; set; } = string.Empty;
        public string RutaPDFCompromiso { get; set; } = string.Empty;
        public int UsuarioActualizacionID { get; set; }
        public string? Observaciones { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // ANULAR DTO
    // ═══════════════════════════════════════════════════════════

    public class AnularDeudaSangreDTO
    {
        public int MedicoAnulaID { get; set; }
        public string JustificacionAnulacion { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // HISTORIAL DTO
    // ═══════════════════════════════════════════════════════════

    public class HistorialDeudaSangreDTO
    {
        public DateTime FechaHora { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string? IPOrigen { get; set; }
    }
}