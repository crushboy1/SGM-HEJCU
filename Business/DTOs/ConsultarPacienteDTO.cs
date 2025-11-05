namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO con datos combinados de Galenhos (HIS) y SIGEM
    /// Se usa para pre-llenar el formulario de creación de expediente
    /// </summary>
    public class ConsultarPacienteDTO
    {
        // ═══════════════════════════════════════════════════════════
        // DATOS DE GALENHOS (Sistema HIS)
        // ═══════════════════════════════════════════════════════════

        public string HC { get; set; } = string.Empty;
        public int TipoDocumentoID { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public int Edad { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string FuenteFinanciamiento { get; set; } = string.Empty; 

        // ═══════════════════════════════════════════════════════════
        // DATOS DE SIGEM (Sistema de Emergencias y Medicina)
        // ═══════════════════════════════════════════════════════════

        public string? ServicioFallecimiento { get; set; }
        public string? NumeroCama { get; set; }
        public DateTime? FechaHoraFallecimiento { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public string? CodigoCIE10 { get; set; }
        public string? MedicoCertificaNombre { get; set; }
        public string? MedicoCMP { get; set; }
        public string? MedicoRNE { get; set; }

        // ═══════════════════════════════════════════════════════════
        // INFORMACIÓN ADICIONAL
        // ═══════════════════════════════════════════════════════════

        public bool ExisteEnGalenhos { get; set; }
        public bool ExisteEnSigem { get; set; }
        public List<string> Advertencias { get; set; } = new();
    }
}