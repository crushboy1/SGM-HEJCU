using SisMortuorio.Data.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Expediente mortuorio - Entidad central del SGM.
    /// Representa el flujo completo desde fallecimiento hasta retiro.
    ///
    /// TIPOS DE INGRESO:
    /// - Interno: Fallecimiento durante hospitalización
    /// - Externo: Persona llega ya fallecida al hospital (DOA)
    /// </summary>
    public class Expediente
    {
        [Key]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Código único del expediente.
        /// Formato: SGM-YYYY-NNNNN. Generado automáticamente.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string CodigoExpediente { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de ingreso al mortuorio.
        /// Interno: fallece dentro del hospital.
        /// Externo: llega ya fallecido (DOA) — requiere Epicrisis como primer documento.
        /// NO determina TipoSalida, son dimensiones independientes.
        /// </summary>
        [Required]
        public TipoIngreso TipoExpediente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL PACIENTE FALLECIDO
        // ═══════════════════════════════════════════════════════════

        /// <summary>Historia Clínica. Fuente: Galenhos.</summary>
        [Required]
        [MaxLength(20)]
        public string HC { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento de identidad: DNI | Pasaporte | CarneExtranjeria | SinDocumento | NN
        /// </summary>
        [Required]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        /// <summary>Número de documento de identidad.</summary>
        [Required]
        [MaxLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo calculado: ApellidoPaterno + ApellidoMaterno + Nombres.
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        public DateTime FechaNacimiento { get; set; }

        /// <summary>Sexo: "M" o "F".</summary>
        [Required]
        [MaxLength(1)]
        public string Sexo { get; set; } = string.Empty;

        /// <summary>
        /// Fuente de financiamiento del paciente. Fuente: Galenhos.
        /// Default: PendientePago para todos los casos incluyendo NN.
        /// Readonly en flujo normal; editable solo en registro manual.
        /// </summary>
        [Required]
        public FuenteFinanciamiento FuenteFinanciamiento { get; set; } = FuenteFinanciamiento.PendientePago;

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL FALLECIMIENTO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Servicio donde ocurrió el fallecimiento.
        /// Ejemplos: "UCI", "Traumashock", "Hospitalización Medicina".
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServicioFallecimiento { get; set; } = string.Empty;

        /// <summary>Número de cama donde falleció el paciente (opcional).</summary>
        [MaxLength(20)]
        public string? NumeroCama { get; set; }

        /// <summary>Fecha y hora exacta del fallecimiento registrada por el médico.</summary>
        [Required]
        public DateTime FechaHoraFallecimiento { get; set; }

        /// <summary>Nombre completo del médico que certifica el fallecimiento.</summary>
        [Required]
        [MaxLength(200)]
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        /// <summary>CMP del médico certificador. 6 dígitos.</summary>
        [Required]
        [MaxLength(10)]
        public string MedicoCMP { get; set; } = string.Empty;

        /// <summary>
        /// RNE del médico certificador (opcional).
        /// Registro Nacional de Especialidades — solo si tiene especialidad registrada.
        /// </summary>
        [MaxLength(10)]
        public string? MedicoRNE { get; set; }

        /// <summary>
        /// Diagnóstico final (código CIE-10). Fuente: SIGEM.
        /// Opcional al crear — puede completarse después.
        /// </summary>
        [MaxLength(500)]
        public string? DiagnosticoFinal { get; set; }

        /// <summary>
        /// Paciente no identificado (NN).
        /// true → coordinar con Servicio Social para identificación vía RENIEC.
        /// FuenteFinanciamiento se mantiene PendientePago.
        /// </summary>
        public bool EsNN { get; set; } = false;

        /// <summary>
        /// Indica si la causa del fallecimiento es violenta o dudosa.
        /// true → TipoSalida debe ser AutoridadLegal sin excepción.
        /// true → bloquea médico externo y generación de SINADEF.
        /// Ejemplos: accidente, homicidio, suicidio, muerte inexplicable, caída de escaleras.
        /// </summary>
        public bool CausaViolentaODudosa { get; set; } = false;

        // ═══════════════════════════════════════════════════════════
        // MÉDICO EXTERNO (opcional)
        // Aplica cuando CausaViolentaODudosa = false, independientemente
        // del TipoExpediente. Casos:
        // - Interno <24h: familia trae médico de cabecera para evitar morgue
        // - Externo (DOA): familia trae médico de cabecera si lo tenía
        // NUNCA aplica cuando CausaViolentaODudosa = true.
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre del médico externo traído por la familia para certificar.
        /// Solo válido cuando CausaViolentaODudosa = false.
        /// </summary>
        [MaxLength(200)]
        public string? MedicoExternoNombre { get; set; }

        /// <summary>CMP del médico externo.</summary>
        [MaxLength(10)]
        public string? MedicoExternoCMP { get; set; }

        // ═══════════════════════════════════════════════════════════
        // VALIDACIÓN ADMISIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Todos los documentos requeridos según TipoSalida están verificados.
        /// Calculado por DocumentoExpedienteService.VerificarDocumentacionCompletaAsync().
        /// Cuando true habilita la creación del ActaRetiro.
        /// </summary>
        [Required]
        public bool DocumentacionCompleta { get; set; } = false;

        /// <summary>Fecha en que Admisión completó la validación documental.</summary>
        public DateTime? FechaValidacionAdmision { get; set; }

        /// <summary>ID del usuario de Admisión que validó los documentos.</summary>
        public int? UsuarioAdmisionID { get; set; }

        public virtual Usuario? UsuarioAdmision { get; set; }

        // ═══════════════════════════════════════════════════════════
        // TIPO DE SALIDA PRELIMINAR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tipo de salida definido por Admisión antes de crear el ActaRetiro.
        /// null → aún no definido.
        /// Familiar → DNI_Familiar + DNI_Fallecido + CertificadoDefuncion.
        /// AutoridadLegal → OficioPolicial únicamente.
        /// Bloqueado para roles no administrativos una vez creada el acta.
        /// </summary>
        public TipoSalida? TipoSalidaPreliminar { get; set; }

        // ═══════════════════════════════════════════════════════════
        // SEMÁFORO DE DEUDAS
        // NO hay flags redundantes aquí.
        // El semáforo se evalúa navegando las entidades relacionadas:
        //   DeudaEconomica?.BloqueaRetiro() → true si Estado = Pendiente y MontoPendiente > 0
        //   DeudaSangre?.BloqueaRetiro()    → true si Estado = Pendiente
        // null en ambos casos significa sin deuda registrada (verde por defecto).
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        // BYPASS DE DEUDA (AutoridadLegal únicamente)
        // Autorización excepcional de JG/Admin para retiros con deudas
        // cuando no hay familiar presente y PNP debe retirar el cuerpo.
        // Cubre AMBAS deudas (económica y sangre) ya que sin familiar
        // el hospital no puede recuperar ninguna de las dos.
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si JG o Admin autorizó el retiro con deudas pendientes.
        /// Solo aplica para TipoSalidaPreliminar = AutoridadLegal.
        /// Cuando true: ambos semáforos se consideran verdes para crear el acta.
        /// </summary>
        public bool BypassDeudaAutorizado { get; set; } = false;

        /// <summary>
        /// Justificación obligatoria del bypass.
        /// Ej: "PNP retira cuerpo sin familiar — caso legal urgente sin deudohabiente"
        /// </summary>
        [MaxLength(500)]
        public string? BypassDeudaJustificacion { get; set; }

        /// <summary>
        /// ID del usuario (JG o Admin) que autorizó el bypass.
        /// </summary>
        public int? BypassDeudaUsuarioID { get; set; }

        /// <summary>
        /// Navegación al usuario que autorizó el bypass.
        /// </summary>
        public virtual Usuario? BypassDeudaUsuario { get; set; }

        /// <summary>
        /// Fecha y hora en que se autorizó el bypass.
        /// </summary>
        public DateTime? BypassDeudaFecha { get; set; }

        // ═══════════════════════════════════════════════════════════
        // OBSERVACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Observaciones generales del expediente.
        /// Uso libre por Admisión o Enfermería para anotaciones relevantes.
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ESTADO Y QR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual en el flujo mortuorio.
        /// Gestionado por StateMachineService — no modificar directamente.
        /// </summary>
        [Required]
        public EstadoExpediente EstadoActual { get; set; } = EstadoExpediente.EnPiso;

        /// <summary>
        /// Código QR único. Formato: GUID sin guiones.
        /// Se genera una sola vez — no se regenera en reimpresiones.
        /// </summary>
        [MaxLength(50)]
        public string? CodigoQR { get; set; }

        public DateTime? FechaGeneracionQR { get; set; }

        // ═══════════════════════════════════════════════════════════
        // BANDEJA ACTUAL (DESNORMALIZADO)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID de la bandeja donde se encuentra el cuerpo actualmente.
        /// Solo tiene valor cuando EstadoActual = EnBandeja.
        /// Campo desnormalizado para queries rápidas.
        /// Fuente de verdad: BandejaHistorial.
        /// </summary>
        public int? BandejaActualID { get; set; }

        public virtual Bandeja? BandejaActual { get; set; }
        public virtual ActaRetiro? ActaRetiro { get; set; }
        public virtual SalidaMortuorio? SalidaMortuorio { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        [Required]
        public int UsuarioCreadorID { get; set; }

        public virtual Usuario UsuarioCreador { get; set; } = null!;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        /// <summary>Soft delete.</summary>
        public bool Eliminado { get; set; } = false;

        public DateTime? FechaEliminacion { get; set; }

        [MaxLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIONES DE NAVEGACIÓN
        // ═══════════════════════════════════════════════════════════

        public virtual ICollection<Pertenencia> Pertenencias { get; set; } = new List<Pertenencia>();

        public virtual ICollection<CustodiaTransferencia> CustodiaTransferencias { get; set; } = new List<CustodiaTransferencia>();

        public virtual DeudaSangre? DeudaSangre { get; set; }

        public virtual DeudaEconomica? DeudaEconomica { get; set; }

        public virtual VerificacionMortuorio? VerificacionMortuorio { get; set; }

        public virtual ICollection<BandejaHistorial> HistorialBandejas { get; set; } = new List<BandejaHistorial>();

        public virtual ICollection<DocumentoExpediente> Documentos { get; set; } = new List<DocumentoExpediente>();
    }
}