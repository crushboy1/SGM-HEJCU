namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    /// <summary>
    /// DTO de documento legal asociado a un expediente legal.
    /// </summary>
    public class DocumentoLegalDTO
    {
        public int DocumentoLegalID { get; set; }
        public int ExpedienteLegalID { get; set; }

        public string TipoDocumento { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public long TamañoArchivo { get; set; }
        public string TamañoArchivoLegible { get; set; } = string.Empty;
        public string? Observaciones { get; set; }

        public int UsuarioSubeID { get; set; }
        public string UsuarioSubeNombre { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; }
    }
}