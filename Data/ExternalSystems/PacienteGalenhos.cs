namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula la estructura de datos de GALENHOS
    /// En producción, esto sería una vista o consulta a la BD real de Galenhos
    /// </summary>
    public class PacienteGalenhos
    {
        public string HC { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;

        // Datos de identificación
        public int TipoDocumentoID { get; set; }  // 1=DNI, 2=Pasaporte, 3=CE, 4=Sin Doc, 5=NN
        public string NumeroDocumento { get; set; } = string.Empty;

        // Datos demográficos
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;  // "M" o "F"

        // Datos financieros
        public string FuenteFinanciamiento { get; set; } = string.Empty;  // "SIS" o "PARTICULAR"
    }
}