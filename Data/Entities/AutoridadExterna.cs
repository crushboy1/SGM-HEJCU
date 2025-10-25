namespace SisMortuorio.Data.Entities
{
    public class AutoridadExterna
    {
        public int AutoridadID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public TipoAutoridad TipoAutoridad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public TipoDocumentoIdentidad TipoDocumento { get; set; }  // ← NUEVO
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? CodigoEspecial { get; set; } // CMP, Código Fiscal, etc.
        public string Institucion { get; set; } = string.Empty; // "Comisaría X", "Fiscalía Y"
        public string? PlacaVehiculo { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public int UsuarioRegistroID { get; set; }
        public Usuario UsuarioRegistro { get; set; } = null!;
    }

    public enum TipoAutoridad
    {
        Policia,
        Fiscal,
        MedicoLegista,
        Otros
    }
}