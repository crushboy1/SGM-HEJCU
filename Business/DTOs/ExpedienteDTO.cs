namespace SisMortuorio.Business.DTOs
{
    public class ExpedienteDTO
    {
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string TipoExpediente { get; set; } = string.Empty;

        // Datos del Paciente
        public string HC { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public int Edad { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string TipoSeguro { get; set; } = string.Empty;

        // Datos del Fallecimiento
        public string ServicioFallecimiento { get; set; } = string.Empty;
        public string? NumeroCama { get; set; }
        public DateTime FechaHoraFallecimiento { get; set; }
        public string MedicoCertificaNombre { get; set; } = string.Empty;
        public string MedicoCMP { get; set; } = string.Empty;
        public string? MedicoRNE { get; set; }
        public string? NumeroCertificadoSINADEF { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public bool DocumentacionCompleta { get; set; }
        public DateTime? FechaValidacionAdmision { get; set; }
        public string? UsuarioAdmisionNombre { get; set; }

        // Estado y QR
        public string EstadoActual { get; set; } = string.Empty;
        public string? CodigoQR { get; set; }
        public DateTime? FechaGeneracionQR { get; set; }

        // Auditoría
        public string UsuarioCreador { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Pertenencias
        public List<PertenenciaDTO>? Pertenencias { get; set; }
        /// <summary>
        /// Código de la bandeja donde está ubicado el cuerpo actualmente.
        /// Se llena dinámicamente en QRService.ConsultarPorQRAsync.
        /// </summary>
        public string? CodigoBandeja { get; set; }
        /// <summary>
        /// ID de la bandeja ocupada.
        /// </summary>
        public int? BandejaID { get; set; }
        public int? BandejaActualID { get; set; }
    }

    public class PertenenciaDTO
    {
        public int PertenenciaID { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}