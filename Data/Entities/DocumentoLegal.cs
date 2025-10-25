namespace SisMortuorio.Data.Entities
{
    public class DocumentoLegal
    {
        public int DocumentoID { get; set; }
        public int ExpedienteID { get; set; }
        public Expediente Expediente { get; set; } = null!;

        public TipoDocumento TipoDocumento { get; set; }
        public string? RutaArchivo { get; set; } // "uploads/documentos/EXP-001-epicrisis.pdf"
        public bool EstaCompleto { get; set; }

        public DateTime? FechaAdjunto { get; set; }
        public int? UsuarioAdjuntoID { get; set; }
        public Usuario? UsuarioAdjunto { get; set; }
    }

    public enum TipoDocumento
    {
        Epicrisis,
        OficioPNP,
        ActaLevantamiento,
        CertificadoDefuncion,
        Otros
    }
}