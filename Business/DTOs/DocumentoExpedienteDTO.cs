using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// DTO de respuesta con los datos completos de un documento digitalizado
    /// </summary>
    public class DocumentoExpedienteDTO
    {
        public int DocumentoExpedienteID { get; set; }
        public int ExpedienteID { get; set; }

        // Clasificación
        public TipoDocumentoExpediente TipoDocumento { get; set; }
        public string TipoDocumentoDescripcion { get; set; } = string.Empty;
        public EstadoDocumentoExpediente Estado { get; set; }
        public string EstadoDescripcion { get; set; } = string.Empty;

        // Archivo
        public string NombreArchivo { get; set; } = string.Empty;
        public string ExtensionArchivo { get; set; } = string.Empty;
        public string TamanioLegible { get; set; } = string.Empty;

        // Auditoría - Subida
        public string UsuarioSubioNombre { get; set; } = string.Empty;
        public DateTime FechaHoraSubida { get; set; }

        // Auditoría - Verificación
        public string? UsuarioVerificoNombre { get; set; }
        public DateTime? FechaHoraVerificacion { get; set; }

        // Observaciones
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para subir un nuevo documento digitalizado
    /// </summary>
    public class SubirDocumentoDTO
    {
        public int ExpedienteID { get; set; }
        public TipoDocumentoExpediente TipoDocumento { get; set; }
        public string? Observaciones { get; set; }

        /// <summary>
        /// ID del usuario admisionista que sube el documento
        /// Se obtiene del JWT en el controller, no lo envía el frontend
        /// </summary>
        public int UsuarioSubioID { get; set; }
    }

    /// <summary>
    /// DTO para verificar un documento contra el original físico
    /// </summary>
    public class VerificarDocumentoDTO
    {
        public int DocumentoExpedienteID { get; set; }
        public string? Observaciones { get; set; }

        /// <summary>
        /// ID del usuario que verifica (del JWT)
        /// </summary>
        public int UsuarioVerificoID { get; set; }
    }

    /// <summary>
    /// DTO para rechazar un documento
    /// </summary>
    public class RechazarDocumentoDTO
    {
        public int DocumentoExpedienteID { get; set; }

        /// <summary>
        /// Motivo del rechazo — obligatorio para trazabilidad
        /// Ejemplo: "Imagen borrosa", "DNI vencido", "Firma ilegible"
        /// </summary>
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// ID del usuario que rechaza (del JWT)
        /// </summary>
        public int UsuarioVerificoID { get; set; }
    }

    /// <summary>
    /// Resumen del estado de documentación del expediente.
    /// Indica qué documentos están presentes según TipoSalida.
    /// Usado por Admisión para habilitar/bloquear la creación del ActaRetiro.
    /// </summary>
    public class ResumenDocumentosDTO
    {
        public int ExpedienteID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ESTADO GENERAL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si todos los documentos requeridos según TipoSalida están verificados.
        /// true → habilita botón "Crear Acta" en frontend
        /// </summary>
        public bool DocumentacionCompleta { get; set; }

        /// <summary>
        /// TipoSalida del ActaRetiro si existe, null si aún no se creó.
        /// Determina qué documentos son requeridos.
        /// </summary>
        public TipoSalida? TipoSalida { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DOCUMENTOS CASO FAMILIAR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado del DNI del familiar (requerido si TipoSalida == Familiar)
        /// </summary>
        public EstadoDocumentoItem DNIFamiliar { get; set; } = new();

        /// <summary>
        /// Estado del DNI del fallecido (requerido si TipoSalida == Familiar)
        /// </summary>
        public EstadoDocumentoItem DNIFallecido { get; set; } = new();

        /// <summary>
        /// Estado del Certificado de Defunción SINADEF (requerido si TipoSalida == Familiar)
        /// </summary>
        public EstadoDocumentoItem CertificadoDefuncion { get; set; } = new();

        // ═══════════════════════════════════════════════════════════
        // DOCUMENTOS CASO AUTORIDAD LEGAL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado del Oficio Legal PNP/Fiscal (requerido si TipoSalida == AutoridadLegal)
        /// </summary>
        public EstadoDocumentoItem OficioLegal { get; set; } = new();

        // ═══════════════════════════════════════════════════════════
        // LISTA COMPLETA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Todos los documentos subidos al expediente (cualquier estado)
        /// </summary>
        public List<DocumentoExpedienteDTO> Documentos { get; set; } = new();
    }

    /// <summary>
    /// Estado individual de un tipo de documento requerido.
    /// Usado en ResumenDocumentosDTO para el semáforo visual del frontend.
    /// </summary>
    public class EstadoDocumentoItem
    {
        /// <summary>
        /// Indica si el documento fue subido (cualquier estado)
        /// </summary>
        public bool Subido { get; set; }

        /// <summary>
        /// Indica si el documento está verificado
        /// </summary>
        public bool Verificado { get; set; }

        /// <summary>
        /// Indica si el documento fue rechazado
        /// </summary>
        public bool Rechazado { get; set; }

        /// <summary>
        /// ID del documento para acciones (descargar, reemplazar)
        /// </summary>
        public int? DocumentoID { get; set; }

        /// <summary>
        /// Nombre del archivo subido
        /// </summary>
        public string? NombreArchivo { get; set; }

        /// <summary>
        /// Observaciones del admisionista
        /// </summary>
        public string? Observaciones { get; set; }
    }
}