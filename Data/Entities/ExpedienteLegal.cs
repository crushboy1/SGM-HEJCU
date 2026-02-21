using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Expediente Legal Digital - Modelo Híbrido.
    /// FLUJO: Vigilancia (Registro) → Admisión (Validación) → Jefe Guardia (Autorización)
    /// </summary>
    public class ExpedienteLegal
    {
        [Key]
        public int ExpedienteLegalID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIÓN CON EXPEDIENTE
        // ═══════════════════════════════════════════════════════════

        [Required]
        public int ExpedienteID { get; set; }
        public virtual Expediente Expediente { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // ESTADO DEL EXPEDIENTE (FLUJO HÍBRIDO)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual del expediente en el flujo híbrido.
        /// </summary>
        [Required]
        public EstadoExpedienteLegal Estado { get; set; } = EstadoExpedienteLegal.EnRegistro;

        // ═══════════════════════════════════════════════════════════
        // DATOS DE REFERENCIA EXTERNA (DEL OFICIO PNP)
        // ═══════════════════════════════════════════════════════════

        [MaxLength(100)]
        public string? NumeroOficioPNP { get; set; }

        [MaxLength(200)]
        public string? Comisaria { get; set; }

        [MaxLength(200)]
        public string? Fiscalia { get; set; }

        [MaxLength(200)]
        public string? Destino { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // VALIDACIÓN POR ADMISIÓN (NUEVO)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si Admisión validó que los documentos están completos y legibles.
        /// </summary>
        public bool ValidadoAdmision { get; set; } = false;

        /// <summary>
        /// Fecha en que Admisión validó la documentación.
        /// </summary>
        public DateTime? FechaValidacionAdmision { get; set; }

        /// <summary>
        /// Usuario de Admisión que validó.
        /// </summary>
        public int? UsuarioAdmisionID { get; set; }

        /// <summary>
        /// Navegación al usuario de Admisión.
        /// </summary>
        public virtual Usuario? UsuarioAdmision { get; set; }

        /// <summary>
        /// Observaciones de Admisión sobre la documentación.
        /// Ejemplo: "Documentación completa y legible", "Falta Acta de Levantamiento"
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesAdmision { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUTORIZACIÓN JEFE DE GUARDIA (FIRMA DEL OFICIO)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si el Jefe de Guardia firmó/visó el oficio PNP.
        /// </summary>
        public bool AutorizadoJefeGuardia { get; set; } = false;

        /// <summary>
        /// Fecha y hora en que el Jefe de Guardia firmó el oficio.
        /// </summary>
        public DateTime? FechaAutorizacion { get; set; }

        /// <summary>
        /// Jefe de Guardia que autorizó el levantamiento.
        /// </summary>
        public int? JefeGuardiaID { get; set; }

        /// <summary>
        /// Navegación al Jefe de Guardia.
        /// </summary>
        public virtual Usuario? JefeGuardia { get; set; }

        /// <summary>
        /// Observaciones del Jefe de Guardia.
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesJefeGuardia { get; set; }

        // ═══════════════════════════════════════════════════════════
        // COLECCIONES
        // ═══════════════════════════════════════════════════════════

        public virtual ICollection<AutoridadExterna> Autoridades { get; set; } = new List<AutoridadExterna>();
        public virtual ICollection<DocumentoLegal> Documentos { get; set; } = new List<DocumentoLegal>();

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        [Required]
        public int UsuarioRegistroID { get; set; }
        public virtual Usuario UsuarioRegistro { get; set; } = null!;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int? UsuarioActualizacionID { get; set; }
        public virtual Usuario? UsuarioActualizacion { get; set; }
        public DateTime? FechaUltimaActualizacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si todos los documentos mínimos están presentes.
        /// </summary>
        public bool DocumentosCompletos
        {
            get
            {
                var tieneEpicrisis = Documentos.Any(d =>
                    d.TipoDocumento == TipoDocumentoLegal.Epicrisis && d.Adjuntado);

                var tieneOficioPNP = Documentos.Any(d =>
                    d.TipoDocumento == TipoDocumentoLegal.OficioPolicial && d.Adjuntado);

                var tieneActaLevantamiento = Documentos.Any(d =>
                    d.TipoDocumento == TipoDocumentoLegal.ActaLevantamiento && d.Adjuntado);

                return tieneEpicrisis && tieneOficioPNP && tieneActaLevantamiento;
            }
        }

        public string? CalcularDocumentosPendientes()
        {
            var pendientes = new List<string>();

            if (!Documentos.Any(d => d.TipoDocumento == TipoDocumentoLegal.Epicrisis && d.Adjuntado))
                pendientes.Add("Epicrisis");

            if (!Documentos.Any(d => d.TipoDocumento == TipoDocumentoLegal.OficioPolicial && d.Adjuntado))
                pendientes.Add("Oficio PNP");

            if (!Documentos.Any(d => d.TipoDocumento == TipoDocumentoLegal.ActaLevantamiento && d.Adjuntado))
                pendientes.Add("Acta Levantamiento");

            return pendientes.Count > 0 ? string.Join(", ", pendientes) : null;
        }

        public DateTime? CalcularFechaLimitePendientes()
        {
            if (DocumentosCompletos || AutorizadoJefeGuardia)
                return null;

            return FechaCreacion.AddHours(48);
        }

        public int? CalcularDiasRestantesPendientes()
        {
            var fechaLimite = CalcularFechaLimitePendientes();
            if (fechaLimite == null) return null;

            var diferencia = fechaLimite.Value - DateTime.Now;
            return (int)Math.Ceiling(diferencia.TotalDays);
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE TRANSICIÓN DE ESTADO (FLUJO HÍBRIDO)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Vigilancia marca como listo para validación por Admisión.
        /// </summary>
        public void MarcarListoParaAdmision()
        {
            if (!DocumentosCompletos)
                throw new InvalidOperationException("No se puede enviar a Admisión sin documentos completos");

            Estado = EstadoExpedienteLegal.PendienteValidacionAdmision;
        }

        /// <summary>
        /// Admisión valida la documentación.
        /// </summary>
        public void ValidarPorAdmision(bool aprobado, int usuarioAdmisionID, string? observaciones)
        {
            if (Estado != EstadoExpedienteLegal.PendienteValidacionAdmision &&
                Estado != EstadoExpedienteLegal.RechazadoAdmision)
                throw new InvalidOperationException($"No se puede validar desde el estado {Estado}");

            if (aprobado)
            {
                ValidadoAdmision = true;
                Estado = EstadoExpedienteLegal.ValidadoAdmision;
            }
            else
            {
                ValidadoAdmision = false;
                Estado = EstadoExpedienteLegal.RechazadoAdmision;
            }

            UsuarioAdmisionID = usuarioAdmisionID;
            FechaValidacionAdmision = DateTime.Now;
            ObservacionesAdmision = observaciones;
        }

        /// <summary>
        /// Jefe de Guardia autoriza el levantamiento (firma oficio).
        /// </summary>
        public void AutorizarPorJefeGuardia(int jefeGuardiaID, string? observaciones)
        {
            if (Estado != EstadoExpedienteLegal.ValidadoAdmision)
                throw new InvalidOperationException("Solo se puede autorizar si Admisión validó previamente");

            AutorizadoJefeGuardia = true;
            JefeGuardiaID = jefeGuardiaID;
            FechaAutorizacion = DateTime.Now;
            ObservacionesJefeGuardia = observaciones;
            Estado = EstadoExpedienteLegal.AutorizadoJefeGuardia;
        }

        /// <summary>
        /// Obtiene el nombre del policía que entregó el oficio.
        /// </summary>
        public string? ObtenerNombrePolicia()
        {
            var policia = Autoridades.FirstOrDefault(a => a.TipoAutoridad == TipoAutoridadExterna.Policia);
            return policia != null ? $"{policia.ApellidoPaterno} {policia.ApellidoMaterno}, {policia.Nombres}" : null;
        }

        public string? ObtenerNombreFiscal()
        {
            var fiscal = Autoridades.FirstOrDefault(a => a.TipoAutoridad == TipoAutoridadExterna.Fiscal);
            return fiscal != null ? $"{fiscal.ApellidoPaterno} {fiscal.ApellidoMaterno}, {fiscal.Nombres}" : null;
        }

        public string? ObtenerNombreMedicoLegista()
        {
            var legista = Autoridades.FirstOrDefault(a => a.TipoAutoridad == TipoAutoridadExterna.MedicoLegista);
            return legista != null ? $"{legista.ApellidoPaterno} {legista.ApellidoMaterno}, {legista.Nombres}" : null;
        }
    }
}