namespace SisMortuorio.Data.Entities
{
    public class Expediente
    {
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty; // SGM-2025-00001
        public string TipoExpediente { get; set; } = string.Empty; // Interno, Externo

        // Datos del Paciente
        public string HC { get; set; } = string.Empty;
        public TipoDocumentoIdentidad TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty; // Denormalizado
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty; // M, F
        public string TipoSeguro { get; set; } = string.Empty; // SIS, PARTICULAR

        // Datos del Fallecimiento
        public string ServicioFallecimiento { get; set; } = string.Empty;
        public string? NumeroCama { get; set; }
        public DateTime FechaHoraFallecimiento { get; set; }
        public string MedicoCertificaNombre { get; set; } = string.Empty;
        public string MedicoCMP { get; set; } = string.Empty;
        public string? MedicoRNE { get; set; }
        public string? NumeroCertificadoSINADEF { get; set; }
        public string? CausaMuerte { get; set; }

        // Estado y QR
        public string EstadoActual { get; set; } = string.Empty; // En Piso, Pendiente de Recojo, etc.
        public string? CodigoQR { get; set; }
        public DateTime? FechaGeneracionQR { get; set; }

        // Auditoría
        public int UsuarioCreadorID { get; set; }
        public Usuario UsuarioCreador { get; set; } = null!;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        public bool Eliminado { get; set; } = false;
        public DateTime? FechaEliminacion { get; set; }
        public string? MotivoEliminacion { get; set; }

        // Relaciones
        public ICollection<Pertenencia> Pertenencias { get; set; } = new List<Pertenencia>();
        public ICollection<CustodiaTransferencia> CustodiaTransferencias { get; set; } = new List<CustodiaTransferencia>();
    }
}