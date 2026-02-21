namespace SisMortuorio.Business.DTOs
{
    public class GenerarCompromisoDTO
    {
        public int ExpedienteID { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;

        // Datos del Familiar (El que firma)
        public string NombreFamiliar { get; set; } = string.Empty;
        public string DNIFamiliar { get; set; } = string.Empty;

        // Datos de la Deuda
        public int CantidadUnidades { get; set; }
    }
}