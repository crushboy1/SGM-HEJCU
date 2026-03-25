namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO para la bandeja de entrada de Enfermería.
    /// Combina datos de Galenhos y SIGEM para mostrar pacientes
    /// fallecidos pendientes de generar expediente.
    /// </summary>
    public class BandejaEntradaDTO
    {
        // ── Galenhos ────────────────────────────────────────────────
        public string HC { get; set; } = string.Empty;
        public int TipoDocumentoID { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int Edad { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string FuenteFinanciamiento { get; set; } = string.Empty;
        public bool EsNN { get; set; }

        // ── SIGEM (null si no encontró episodio) ─────────────────────
        public string? ServicioFallecimiento { get; set; }
        public string? NumeroCama { get; set; }
        public DateTime? FechaHoraFallecimiento { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public string? MedicoCertificaNombre { get; set; }

        // ── Estado de integración ────────────────────────────────────
        /// <summary>Si false → Enfermería debe completar datos manualmente.</summary>
        public bool TieneDatosSigem { get; set; }
        public bool CausaViolentaODudosa { get; set; }
        public List<string> Advertencias { get; set; } = new();
    }
}