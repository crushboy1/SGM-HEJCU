namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO de autoridad externa asociada a un expediente legal.
    /// </summary>
    public class AutoridadExternaDTO
    {
        public int AutoridadExternaID { get; set; }
        public int ExpedienteLegalID { get; set; }

        // Tipo de autoridad
        public string TipoAutoridad { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;

        // Documento de identidad
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;

        // Datos de institución
        public string? Institucion { get; set; }
        public string? Cargo { get; set; }

        // Código especial (CMP para médicos legistas, Código Fiscal)
        public string? CodigoEspecial { get; set; }

        // Contacto
        public string? PlacaVehiculo { get; set; }
        public string? Telefono { get; set; }

        // Auditoría
        public DateTime FechaRegistro { get; set; }
    }
}