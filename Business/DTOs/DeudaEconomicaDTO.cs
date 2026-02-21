namespace SisMortuorio.Business.DTOs
{
    // ═══════════════════════════════════════════════════════════
    // DTO PRINCIPAL - DEUDA ECONÓMICA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO completo de deuda económica.
    /// Incluye todos los campos para roles autorizados.
    /// Para Sup. Vigilancia, filtrar y mostrar solo el semáforo.
    /// </summary>
    public class DeudaEconomicaDTO
    {
        public int DeudaEconomicaID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;

        // Estado
        public string Estado { get; set; } = string.Empty;

        // Montos (NO mostrar a Sup. Vigilancia)
        public decimal MontoDeuda { get; set; }
        public decimal MontoExonerado { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoPendiente { get; set; }

        // Pago en Caja
        public string? NumeroBoleta { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? ObservacionesPago { get; set; }

        // Exoneración Servicio Social
        public string TipoExoneracion { get; set; } = string.Empty;
        public string? NumeroBoletaExoneracion { get; set; }
        public DateTime? FechaExoneracion { get; set; }
        public string? ObservacionesExoneracion { get; set; }
        public decimal PorcentajeExoneracion { get; set; }
        public string? RutaPDFSustento { get; set; }
        public string? NombreArchivoSustento { get; set; }
        public long? TamañoArchivoSustento { get; set; }
        public string? TamañoArchivoLegible { get; set; }
        public int? AsistentaSocialID { get; set; }
        public string? AsistentaSocialNombre { get; set; }

        // Auditoría
        public int UsuarioRegistroID { get; set; }
        public string UsuarioRegistroNombre { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public int? UsuarioActualizacionID { get; set; }
        public string? UsuarioActualizacionNombre { get; set; }
        public DateTime? FechaActualizacion { get; set; }

        // Métodos calculados
        public bool BloqueaRetiro { get; set; }
        public string SemaforoSupVigilancia { get; set; } = string.Empty;
        public string ResumenDetallado { get; set; } = string.Empty;
        public string ValidacionSustento { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // DTO SIMPLIFICADO PARA SUP. VIGILANCIA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO simplificado para Sup. Vigilancia.
    /// Solo muestra semáforo DEBE/NO DEBE sin montos.
    /// </summary>
    public class DeudaEconomicaSemaforoDTO
    {
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public bool TieneDeuda { get; set; }
        public string Semaforo { get; set; } = string.Empty;
        public string Instruccion { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // CREATE DTO - REGISTRO INICIAL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO para crear registro inicial de deuda económica.
    /// Usado por Sup. Vigilancia cuando Cuentas Pacientes informa verbalmente.
    /// </summary>
    public class CreateDeudaEconomicaDTO
    {
        public int ExpedienteID { get; set; }
        public decimal MontoDeuda { get; set; }
        public int UsuarioRegistroID { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // DTO MARCAR LIQUIDADO (PAGO EN CAJA)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO para marcar deuda como liquidada.
    /// Usado por Sup. Vigilancia cuando familiar muestra boleta de Caja.
    /// </summary>
    public class LiquidarDeudaEconomicaDTO
    {
        public string NumeroBoleta { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public int UsuarioActualizacionID { get; set; }
        public string? Observaciones { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // DTO APLICAR EXONERACIÓN (SERVICIO SOCIAL)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO para aplicar exoneración por Servicio Social.
    /// Usado por Asistenta Social con PDF de Ficha Socioeconómica.
    /// </summary>
    public class AplicarExoneracionDTO
    {
        public int ExpedienteID { get; set; }
        public decimal MontoExonerado { get; set; }
        public string TipoExoneracion { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string? NumeroBoletaExoneracion { get; set; }
        public int AsistentaSocialID { get; set; }

        // Archivo PDF
        public string? RutaPDFSustento { get; set; }
        public string? NombreArchivoSustento { get; set; }
        public long? TamañoArchivoSustento { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // DTO HISTORIAL (AUDITORÍA)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO para historial de cambios de la deuda económica.
    /// </summary>
    public class HistorialDeudaEconomicaDTO
    {
        public DateTime FechaHora { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string? IPOrigen { get; set; }
    }
}