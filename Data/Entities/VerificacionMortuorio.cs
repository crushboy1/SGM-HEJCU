using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa el registro de verificación de ingreso al mortuorio por parte del vigilante.
    /// Cada intento de verificación (exitoso o rechazado) se registra para auditoría.
    /// </summary>
    public class VerificacionMortuorio
    {
        /// <summary>
        /// Identificador único del registro de verificación
        /// </summary>
        [Key]
        public int VerificacionID { get; set; }

        /// <summary>
        /// ID del expediente que se está verificando
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// ID del vigilante que realiza la verificación
        /// </summary>
        [Required]
        public int VigilanteID { get; set; }

        /// <summary>
        /// Navegación al usuario vigilante
        /// </summary>
        public Usuario Vigilante { get; set; } = null!;

        /// <summary>
        /// ID del técnico de ambulancia que trae el cuerpo
        /// </summary>
        [Required]
        public int TecnicoAmbulanciaID { get; set; }

        /// <summary>
        /// Navegación al técnico de ambulancia
        /// </summary>
        public Usuario TecnicoAmbulancia { get; set; } = null!;

        /// <summary>
        /// Fecha y hora en que se realizó la verificación
        /// </summary>
        [Required]
        public DateTime FechaHoraVerificacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si la verificación fue aprobada (true) o rechazada (false)
        /// </summary>
        [Required]
        public bool Aprobada { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CAMPOS DE VERIFICACIÓN (Comparación de datos)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de Historia Clínica escaneado del brazalete
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string HCBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento leído del brazalete (DNI, Pasaporte, NN, etc.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TipoDocumentoBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento leído del brazalete
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string NumeroDocumentoBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo escaneado del brazalete
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string NombreCompletoBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Servicio de fallecimiento escaneado del brazalete
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServicioBrazalete { get; set; } = string.Empty;

        /// <summary>
        /// Código del expediente escaneado del QR del brazalete
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CodigoExpedienteBrazalete { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // CAMPOS DE RESULTADO DE VERIFICACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si la HC coincide
        /// </summary>
        public bool HCCoincide { get; set; }

        /// <summary>
        /// Indica si el DNI coincide
        /// </summary>
        public bool DocumentoCoincide { get; set; }

        /// <summary>
        /// Indica si el nombre completo coincide
        /// </summary>
        public bool NombreCoincide { get; set; }

        /// <summary>
        /// Indica si el servicio coincide
        /// </summary>
        public bool ServicioCoincide { get; set; }

        /// <summary>
        /// Indica si el código de expediente coincide
        /// </summary>
        public bool CodigoExpedienteCoincide { get; set; }

        /// <summary>
        /// Motivo del rechazo (si Aprobada = false)
        /// Ej: "Nombre no coincide", "HC incorrecta", "Brazalete dañado"
        /// </summary>
        [MaxLength(500)]
        public string? MotivoRechazo { get; set; }

        /// <summary>
        /// Observaciones adicionales del vigilante
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si todos los campos críticos coinciden
        /// </summary>
        public bool TodosLosCamposCoinciden()
        {
            return HCCoincide && DocumentoCoincide && NombreCoincide &&
                   ServicioCoincide && CodigoExpedienteCoincide;
        }

        /// <summary>
        /// Cuenta cuántos campos NO coinciden
        /// </summary>
        public int ContarDiscrepancias()
        {
            int discrepancias = 0;

            if (!HCCoincide) discrepancias++;
            if (!DocumentoCoincide) discrepancias++;
            if (!NombreCoincide) discrepancias++;
            if (!ServicioCoincide) discrepancias++;
            if (!CodigoExpedienteCoincide) discrepancias++;

            return discrepancias;
        }

        /// <summary>
        /// Genera un resumen de las discrepancias encontradas
        /// </summary>
        public string GenerarResumenDiscrepancias()
        {
            if (TodosLosCamposCoinciden())
                return "Todos los campos coinciden correctamente";

            var discrepancias = new System.Collections.Generic.List<string>();

            if (!HCCoincide) discrepancias.Add("HC");
            if (!DocumentoCoincide) discrepancias.Add("Documento de Identidad");
            if (!NombreCoincide) discrepancias.Add("Nombre");
            if (!ServicioCoincide) discrepancias.Add("Servicio");
            if (!CodigoExpedienteCoincide) discrepancias.Add("Código Expediente");

            return $"Discrepancias en: {string.Join(", ", discrepancias)}";
        }

        /// <summary>
        /// Marca la verificación como aprobada
        /// </summary>
        public void Aprobar(string? observaciones = null)
        {
            if (!TodosLosCamposCoinciden())
                throw new InvalidOperationException("No se puede aprobar la verificación. Existen discrepancias en los datos");

            Aprobada = true;
            MotivoRechazo = null;
            Observaciones = observaciones;
        }

        /// <summary>
        /// Marca la verificación como rechazada
        /// </summary>
        public void Rechazar(string motivoRechazo, string? observaciones = null)
        {
            if (string.IsNullOrWhiteSpace(motivoRechazo))
                throw new ArgumentException("Debe proporcionar un motivo de rechazo", nameof(motivoRechazo));

            Aprobada = false;
            MotivoRechazo = motivoRechazo;
            Observaciones = observaciones;
        }
    }
}