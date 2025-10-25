namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula la estructura de datos de SIGEM
    /// En producción, esto sería una vista o consulta a la BD real de SIGEM
    /// </summary>
    public class DiagnosticoSigem
    {
        public string HC { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public DateTime? FechaHoraFallecimiento { get; set; }
        public string? MedicoCMP { get; set; }
        public string? MedicoNombre { get; set; }
        public string? ServicioOrigen { get; set; }
        public string? NumeroCama { get; set; }
    }
}