namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Modelo del último episodio médico según estructura de SIGEM.
    /// Representa la atención del paciente al momento del fallecimiento.
    /// En producción: vista o consulta a BD real de SIGEM.
    /// </summary>
    public class EpisodioSigem
    {
        /// <summary>Historia Clínica del paciente.</summary>
        public string HC { get; set; } = string.Empty;

        /// <summary>Servicio donde ocurrió el fallecimiento.</summary>
        public string ServicioFallecimiento { get; set; } = string.Empty;

        /// <summary>Número de cama o ubicación específica.</summary>
        public string NumeroCama { get; set; } = string.Empty;

        /// <summary>Fecha y hora exacta del fallecimiento.</summary>
        public DateTime FechaHoraFallecimiento { get; set; }

        /// <summary>Diagnóstico final (texto descriptivo).</summary>
        public string DiagnosticoFinal { get; set; } = string.Empty;

        /// <summary>Código CIE-10 del diagnóstico.</summary>
        public string CodigoCIE10 { get; set; } = string.Empty;

        /// <summary>Nombre completo del médico certificante.</summary>
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        /// <summary>
        /// CMP del médico certificante.
        /// Colegio Médico del Perú — 5 dígitos.
        /// </summary>
        public string? MedicoCMP { get; set; }

        /// <summary>
        /// RNE del médico certificante (opcional).
        /// Registro Nacional de Especialidades — solo si tiene especialidad registrada.
        /// </summary>
        public string? MedicoRNE { get; set; }
    }
}