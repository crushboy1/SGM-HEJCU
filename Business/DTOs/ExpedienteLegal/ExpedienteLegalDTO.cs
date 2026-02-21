namespace SisMortuorio.Business.DTOs.ExpedienteLegal
{
    public class ExpedienteLegalDTO
    {
        public int ExpedienteLegalID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        // ══════════════════════════════════════════════════════
        // ESTADO DEL FLUJO HÍBRIDO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual del expediente.
        /// EnRegistro | PendienteValidacionAdmision | RechazadoAdmision | ValidadoAdmision | AutorizadoJefeGuardia
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Texto descriptivo del estado para UI.
        /// </summary>
        public string EstadoDescripcion { get; set; } = string.Empty;

        // Datos de referencia
        public string? NumeroOficioPNP { get; set; }
        public string? Comisaria { get; set; }
        public string? Fiscalia { get; set; }
        public string? Destino { get; set; }
        public string? Observaciones { get; set; }

        // ══════════════════════════════════════════════════════
        // VALIDACIÓN ADMISIÓN
        // ══════════════════════════════════════════════════════

        public bool ValidadoAdmision { get; set; }
        public DateTime? FechaValidacionAdmision { get; set; }
        public int? UsuarioAdmisionID { get; set; }
        public string? UsuarioAdmisionNombre { get; set; }
        public string? ObservacionesAdmision { get; set; }

        // ══════════════════════════════════════════════════════
        // AUTORIZACIÓN JEFE GUARDIA
        // ══════════════════════════════════════════════════════

        public bool AutorizadoJefeGuardia { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public int? JefeGuardiaID { get; set; }
        public string? JefeGuardiaNombre { get; set; }
        public string? ObservacionesJefeGuardia { get; set; }

        // ══════════════════════════════════════════════════════
        // ESTADO DOCUMENTARIO
        // ══════════════════════════════════════════════════════

        public bool DocumentosCompletos { get; set; }
        public string? DocumentosPendientes { get; set; }
        public bool TienePendientes { get; set; }
        public DateTime? FechaLimitePendientes { get; set; }
        public int? DiasRestantes { get; set; }

        // ══════════════════════════════════════════════════════
        // RESUMEN AUTORIDADES
        // ══════════════════════════════════════════════════════

        public string? NombrePolicia { get; set; }
        public string? NombreFiscal { get; set; }
        public string? NombreMedicoLegista { get; set; }

        // Colecciones
        public List<AutoridadExternaDTO> Autoridades { get; set; } = new();
        public List<DocumentoLegalDTO> Documentos { get; set; } = new();

        // Auditoría
        public int UsuarioRegistroID { get; set; }
        public string UsuarioRegistroNombre { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public int? UsuarioActualizacionID { get; set; }
        public string? UsuarioActualizacionNombre { get; set; }
        public DateTime? FechaUltimaActualizacion { get; set; }

        // Campos calculados
        public int CantidadAutoridades { get; set; }
        public int CantidadDocumentos { get; set; }

        // ══════════════════════════════════════════════════════
        // CAMPOS PARA UI (PERMISOS)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Indica si el usuario actual puede marcar como listo para Admisión.
        /// Solo Vigilancia en estado EnRegistro.
        /// </summary>
        public bool PuedeMarcarListo { get; set; }

        /// <summary>
        /// Indica si el usuario actual puede validar documentación.
        /// Solo Admisión en estado PendienteValidacionAdmision.
        /// </summary>
        public bool PuedeValidarAdmision { get; set; }

        /// <summary>
        /// Indica si el usuario actual puede autorizar levantamiento.
        /// Solo Jefe Guardia en estado ValidadoAdmision.
        /// </summary>
        public bool PuedeAutorizarJefeGuardia { get; set; }
    }
}