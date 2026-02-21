using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa un documento legal escaneado y adjunto a un expediente
    /// Parte del "Expediente Legal Digital" para casos de fallecimiento externo
    /// </summary>
    public class DocumentoLegal
    {
        /// <summary>
        /// Identificador único del documento
        /// </summary>
        [Key]
        public int DocumentoID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIÓN CON EXPEDIENTE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente al que pertenece este documento
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public virtual Expediente Expediente { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // TIPO Y CLASIFICACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tipo de documento legal
        /// Epicrisis | OficioPolicial | ActaLevantamiento | FichaAtencion | CertificadoMedicoExterno | Otros
        /// </summary>
        [Required]
        public TipoDocumentoLegal TipoDocumento { get; set; }

        /// <summary>
        /// Descripción adicional del documento (opcional)
        /// Útil para tipo "Otros" o aclaraciones específicas
        /// </summary>
        [MaxLength(500)]
        public string? Descripcion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ARCHIVO FÍSICO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Ruta completa del archivo en servidor
        /// Ej: "uploads/documentos/2025/01/SGM-2025-00152-epicrisis.pdf"
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string RutaArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre original del archivo (para mostrar en UI)
        /// Ej: "Epicrisis_Juan_Perez_28112024.pdf"
        /// No incluye ruta, solo nombre + extensión
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del archivo en bytes
        /// Útil para validaciones y reportes de almacenamiento
        /// </summary>
        public long TamañoArchivo { get; set; }

        /// <summary>
        /// Extensión del archivo (sin punto)
        /// Ej: "pdf", "jpg", "png"
        /// Validación: Solo PDF permitido en producción
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // ESTADO Y VALIDACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si el archivo fue adjuntado correctamente
        /// false = Pendiente de escaneo/adjuntar
        /// true = Archivo presente en servidor
        /// </summary>
        [Required]
        public bool Adjuntado { get; set; } = false;

        /// <summary>
        /// Indica si el documento fue validado/revisado por usuario autorizado
        /// Solo Jefe de Guardia o Sup. Vigilancia pueden validar
        /// </summary>
        public bool Validado { get; set; } = false;

        /// <summary>
        /// Usuario que validó el documento (Jefe de Guardia, Sup. Vigilancia)
        /// </summary>
        public int? UsuarioValidadorID { get; set; }

        /// <summary>
        /// Navegación al usuario validador
        /// </summary>
        public virtual Usuario? UsuarioValidador { get; set; }

        /// <summary>
        /// Fecha en que se validó el documento
        /// </summary>
        public DateTime? FechaValidacion { get; set; }

        /// <summary>
        /// Observaciones del validador
        /// Ej: "Oficio firmado correctamente", "Falta sello de Fiscalía"
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesValidacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Fecha y hora en que se adjuntó el archivo
        /// </summary>
        public DateTime? FechaAdjunto { get; set; }

        /// <summary>
        /// Usuario que adjuntó el documento (Sup. Vigilancia, Admisión)
        /// </summary>
        public int? UsuarioAdjuntoID { get; set; }

        /// <summary>
        /// Navegación al usuario que adjuntó
        /// </summary>
        public virtual Usuario? UsuarioAdjunto { get; set; }


        public int? ExpedienteLegalID { get; set; }
        public virtual ExpedienteLegal? ExpedienteLegal { get; set; }


        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si el documento está completo y listo para usar
        /// </summary>
        public bool EstaCompleto()
        {
            return Adjuntado &&
                   !string.IsNullOrWhiteSpace(RutaArchivo) &&
                   TamañoArchivo > 0;
        }

        /// <summary>
        /// Extrae solo el nombre del archivo sin la ruta
        /// </summary>
        public string ObtenerNombreSoloArchivo()
        {
            return Path.GetFileName(RutaArchivo);
        }

        /// <summary>
        /// Valida que la extensión sea PDF
        /// </summary>
        public bool EsPDF()
        {
            return Extension.ToLower() == "pdf";
        }

        /// <summary>
        /// Marca el documento como validado
        /// </summary>
        public void MarcarValidado(int usuarioValidadorID, string? observaciones = null)
        {
            Validado = true;
            UsuarioValidadorID = usuarioValidadorID;
            FechaValidacion = DateTime.Now;
            ObservacionesValidacion = observaciones;
        }

        /// <summary>
        /// Convierte el tamaño en bytes a formato legible
        /// </summary>
        public string ObtenerTamañoLegible()
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = TamañoArchivo;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}