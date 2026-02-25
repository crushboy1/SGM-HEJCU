using SisMortuorio.Data.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Expediente mortuorio - Entidad central del SGM
    /// Representa el flujo completo desde fallecimiento hasta retiro
    /// 
    /// TIPOS DE EXPEDIENTE:
    /// - Interno: Fallecimiento en hospitalización (flujo normal)
    /// - Externo: Fallecimiento fuera del hospital o < 24-48hrs (requiere docs legales)
    /// </summary>
    public class Expediente
    {
        /// <summary>
        /// Identificador único del expediente
        /// </summary>
        [Key]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Código único del expediente (formato: SGM-YYYY-NNNNN)
        /// Ejemplo: SGM-2025-00001
        /// Generado automáticamente por el sistema
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string CodigoExpediente { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de expediente: "Interno" o "Externo"
        /// Interno: Fallecimiento durante hospitalización
        /// Externo: Fallecimiento < 24-48hrs o fuera del hospital
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TipoExpediente { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL PACIENTE FALLECIDO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Historia Clínica del paciente
        /// Fuente: Sistema Galenhos
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string HC { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento de identidad
        /// DNI | Pasaporte | CarneExtranjeria | SinDocumento | NN
        /// </summary>
        [Required]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        /// <summary>
        /// Número de documento de identidad
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Apellido paterno del paciente
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        /// <summary>
        /// Apellido materno del paciente
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        /// <summary>
        /// Nombres del paciente
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del paciente (calculado)
        /// Formato: ApellidoPaterno + ApellidoMaterno + Nombres
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de nacimiento del paciente
        /// </summary>
        [Required]
        public DateTime FechaNacimiento { get; set; }

        /// <summary>
        /// Sexo del paciente: "M" (Masculino) o "F" (Femenino)
        /// </summary>
        [Required]
        [MaxLength(1)]
        public string Sexo { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de seguro del paciente
        /// 
        /// TODO REFACTORING: Renombrar a "FuenteFinanciamiento" para alinearse 
        ///                   con nomenclatura de Galenhos (Fase de Refactorización)
        /// 
        /// Fuente: Campo "FuenteFinanciamiento" de Galenhos
        /// Valores posibles: SIS, EsSalud, Particular, SOAT, etc.
        /// 
        /// IMPACTO EN DEUDA ECONÓMICA:
        /// - SIS → Sin deuda económica (cubierto 100%)
        /// - Particular → Verificación económica obligatoria
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TipoSeguro { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL FALLECIMIENTO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Servicio donde ocurrió el fallecimiento
        /// Ejemplos: "UCI", "Traumashock", "Hospitalización Medicina", etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServicioFallecimiento { get; set; } = string.Empty;

        /// <summary>
        /// Número de cama donde falleció el paciente (opcional)
        /// Ejemplo: "401", "B-03", etc.
        /// </summary>
        [MaxLength(20)]
        public string? NumeroCama { get; set; }

        /// <summary>
        /// Fecha y hora exacta del fallecimiento
        /// Registrada por el médico que certifica
        /// </summary>
        [Required]
        public DateTime FechaHoraFallecimiento { get; set; }

        /// <summary>
        /// Nombre completo del médico que certifica el fallecimiento
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        /// <summary>
        /// CMP (Colegio Médico del Perú) del médico certificador
        /// 6 dígitos numéricos
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string MedicoCMP { get; set; } = string.Empty;

        /// <summary>
        /// RNE (Registro Nacional de Especialidades) del médico certificador
        /// 5 dígitos - Solo si el médico tiene especialidad registrada
        /// Aclaración: NO es para extranjeros, es para especialistas médicos
        /// </summary>
        [MaxLength(10)]
        public string? MedicoRNE { get; set; }

        /// <summary>
        /// Número de Certificado de Defunción emitido por SINADEF
        /// Solo aplica si fallecimiento > 24-48hrs
        /// </summary>
        [MaxLength(50)]
        public string? NumeroCertificadoSINADEF { get; set; }

        /// <summary>
        /// Diagnóstico final del fallecimiento (código CIE-10)
        /// Fuente: Sistema SIGEM
        /// Renombrado de "CausaMuerte" para alinearse con SIGEM
        /// </summary>
        [MaxLength(500)]
        public string? DiagnosticoFinal { get; set; }

        // ═══════════════════════════════════════════════════════════
        // VALIDACIÓN ADMISIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si todos los documentos requeridos según TipoSalida están verificados.
        /// Calculado por DocumentoExpedienteService.VerificarDocumentosCompletosAsync()
        /// Reemplaza el proceso manual de "3 juegos de copias físicas"
        /// </summary>
        [Required]
        public bool DocumentacionCompleta { get; set; } = false;

        /// <summary>
        /// Fecha y hora en que Admisión validó la documentación.
        /// </summary>
        public DateTime? FechaValidacionAdmision { get; set; }

        /// <summary>
        /// ID del usuario de Admisión que validó los documentos.
        /// </summary>
        public int? UsuarioAdmisionID { get; set; }

        /// <summary>
        /// Navegación al usuario de Admisión que validó.
        /// </summary>
        public virtual Usuario? UsuarioAdmision { get; set; }
        // ═══════════════════════════════════════════════════════════
        // TIPO DE SALIDA PRELIMINAR
        // Definido por Admisión antes de crear el Acta de Retiro
        // ═══════════════════════════════════════════════════════════
        /// <summary>
        /// Tipo de salida definido por Admisión al gestionar documentos.
        /// Se establece ANTES de crear el Acta de Retiro.
        /// Una vez creada el acta, este campo queda bloqueado para roles no administrativos.
        /// Familiar → requiere DNI Familiar + DNI Fallecido + Cert. Defunción
        /// AutoridadLegal → requiere solo Oficio Legal (PNP/Fiscal/Legista)
        /// null → aún no definido (Admisión debe seleccionarlo)
        /// </summary>
        public TipoSalida? TipoSalidaPreliminar { get; set; }
        // ═══════════════════════════════════════════════════════════
        // ESTADO Y QR
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual del expediente en el flujo
        /// Estados: EnPiso | PendienteDeRecojo | EnTrasladoMortuorio | 
        ///          VerificacionRechazadaMortuorio | PendienteAsignacionBandeja |
        ///          EnBandeja | PendienteRetiro | Retirado
        /// </summary>
        [Required]
        public EstadoExpediente EstadoActual { get; set; } = EstadoExpediente.EnPiso;

        /// <summary>
        /// Código QR único generado para el expediente
        /// Formato: GUID sin guiones
        /// Ejemplo: "A1B2C3D4E5F6G7H8I9J0"
        /// </summary>
        [MaxLength(50)]
        public string? CodigoQR { get; set; }

        /// <summary>
        /// Fecha y hora de generación del QR
        /// Se genera una sola vez (no se regenera en reimpresiones)
        /// </summary>
        public DateTime? FechaGeneracionQR { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CAMPOS PARA CASOS EXTERNOS
        // Solo aplican cuando TipoExpediente = "Externo"
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre completo del médico externo que trae la familia
        /// Solo para TipoExpediente = "Externo"
        /// Aplica en fallecimientos < 24-48hrs donde familia contrata médico privado
        /// </summary>
        [MaxLength(200)]
        public string? MedicoExternoNombre { get; set; }

        /// <summary>
        /// CMP del médico externo
        /// Solo para TipoExpediente = "Externo"
        /// </summary>
        [MaxLength(10)]
        public string? MedicoExternoCMP { get; set; }

        /// <summary>
        /// Indica si caso requiere intervención legal (PNP, Fiscal, Médico Legista)
        /// Solo para TipoExpediente = "Externo"
        /// 
        /// true → Se creará ExpedienteLegal + AutoridadesExternas + DocumentosLegales
        /// false → Flujo normal con médico externo contratado por familia
        /// </summary>
        public bool RequiereIntervencionLegal { get; set; } = false;

        // ═══════════════════════════════════════════════════════════
        // BANDEJA ACTUAL (DESNORMALIZADO)
        // Campo para queries rápidas - Fuente de verdad: BandejaHistorial
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID de la bandeja donde se encuentra actualmente el cuerpo
        /// Solo tiene valor cuando EstadoActual = EnBandeja
        /// 
        /// IMPORTANTE: Campo desnormalizado para queries rápidas
        /// También existe en BandejaHistorial (fuente de verdad)
        /// Se actualiza cuando:
        /// - Se asigna bandeja → BandejaActualID = X
        /// - Se retira cuerpo → BandejaActualID = null
        /// </summary>
        public int? BandejaActualID { get; set; }

        /// <summary>
        /// Navegación a la bandeja actual
        /// </summary>
        public virtual Bandeja? BandejaActual { get; set; }
        public virtual ActaRetiro? ActaRetiro { get; set; }
        public virtual SalidaMortuorio? SalidaMortuorio { get; set; }
        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Usuario que creó el expediente (Enfermería)
        /// </summary>
        [Required]
        public int UsuarioCreadorID { get; set; }

        /// <summary>
        /// Navegación al usuario creador
        /// </summary>
        public virtual Usuario UsuarioCreador { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de creación del expediente
        /// </summary>
        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de última modificación
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// Soft delete - Indica si el expediente fue eliminado lógicamente
        /// </summary>
        public bool Eliminado { get; set; } = false;

        /// <summary>
        /// Fecha y hora de eliminación lógica
        /// </summary>
        public DateTime? FechaEliminacion { get; set; }

        /// <summary>
        /// Motivo de la eliminación lógica
        /// </summary>
        [MaxLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIONES DE NAVEGACIÓN - FASE 1-3
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Pertenencias del fallecido registradas por Enfermería
        /// Relación 1:N
        /// </summary>
        public virtual ICollection<Pertenencia> Pertenencias { get; set; } = new List<Pertenencia>();

        /// <summary>
        /// Historial de transferencias de custodia del cuerpo
        /// Relación 1:N
        /// Registra traspaso: Enfermería → Ambulancia → Mortuorio
        /// </summary>
        public virtual ICollection<CustodiaTransferencia> CustodiaTransferencias { get; set; } = new List<CustodiaTransferencia>();

        // ═══════════════════════════════════════════════════════════
        // RELACIONES DE NAVEGACIÓN - FASE 4-5
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Deuda de sangre del expediente
        /// Relación 1:1 (opcional)
        /// Solo existe si paciente/familia tiene deuda con Banco de Sangre
        /// </summary>
        public virtual DeudaSangre? DeudaSangre { get; set; }

        /// <summary>
        /// Deuda económica del expediente
        /// Relación 1:1 (opcional)
        /// Solo existe si paciente es PARTICULAR (no SIS)
        /// </summary>
        public virtual DeudaEconomica? DeudaEconomica { get; set; }

        /// <summary>
        /// Verificación de ingreso al mortuorio
        /// Relación 1:1 (opcional)
        /// Se crea cuando Vigilante verifica ingreso
        /// </summary>
        public virtual VerificacionMortuorio? VerificacionMortuorio { get; set; }

        /// <summary>
        /// Historial completo de asignaciones de bandejas
        /// Relación 1:N
        /// Registra todas las asignaciones/reasignaciones/liberaciones
        /// Incluye tiempo de permanencia y alertas
        /// </summary>
        public virtual ICollection<BandejaHistorial> HistorialBandejas { get; set; } = new List<BandejaHistorial>();

        /// <summary>
        /// Documentos digitalizados adjuntos al expediente
        /// Reemplaza los juegos de copias físicas del proceso manual
        /// Relación 1:N
        /// </summary>
        public virtual ICollection<DocumentoExpediente> Documentos { get; set; } = new List<DocumentoExpediente>();
        // ═══════════════════════════════════════════════════════════
        // RELACIONES DE NAVEGACIÓN - CASOS EXTERNOS
        // Solo aplican cuando TipoExpediente = "Externo"
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Documentos legales adjuntos (Epicrisis, Oficios PNP, Actas)
        /// Relación 1:N
        /// Solo para casos externos con RequiereIntervencionLegal = true
        /// </summary>
        public virtual ICollection<DocumentoLegal> DocumentosLegales { get; set; } = new List<DocumentoLegal>();

        /// <summary>
        /// Autoridades externas registradas (Policía, Fiscal, Médico Legista)
        /// Relación 1:N
        /// Solo para casos externos con RequiereIntervencionLegal = true
        /// </summary>
        public virtual ICollection<AutoridadExterna> AutoridadesExternas { get; set; } = new List<AutoridadExterna>();

        /// <summary>
        /// Expediente legal digital (reemplaza cuaderno físico Sup. Vigilancia)
        /// Relación 1:1 (opcional)
        /// Solo para casos externos con RequiereIntervencionLegal = true
        /// Consolida toda la documentación legal y validaciones
        /// </summary>
        public virtual ExpedienteLegal? ExpedienteLegal { get; set; }
    }
}