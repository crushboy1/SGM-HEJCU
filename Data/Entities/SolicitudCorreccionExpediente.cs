using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa una solicitud de corrección de datos de un expediente.
    /// Se genera cuando el vigilante rechaza la verificación por datos incorrectos
    /// y necesita que enfermería corrija la información y reemita el brazalete.
    /// </summary>
    public class SolicitudCorreccionExpediente
    {
        /// <summary>
        /// Identificador único de la solicitud de corrección
        /// </summary>
        [Key]
        public int SolicitudID { get; set; }

        /// <summary>
        /// ID del expediente que requiere corrección
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// Fecha y hora en que se generó la solicitud
        /// </summary>
        [Required]
        public DateTime FechaHoraSolicitud { get; set; } = DateTime.Now;

        /// <summary>
        /// ID del usuario que solicita la corrección (Vigilante)
        /// </summary>
        [Required]
        public int UsuarioSolicitaID { get; set; }

        /// <summary>
        /// Navegación al usuario solicitante (Vigilante)
        /// </summary>
        public Usuario UsuarioSolicita { get; set; } = null!;

        /// <summary>
        /// ID del usuario responsable de realizar la corrección (Técnico de Enfermería)
        /// </summary>
        [Required]
        public int UsuarioResponsableID { get; set; }

        /// <summary>
        /// Navegación al usuario responsable (Técnico de Enfermería del servicio)
        /// </summary>
        public Usuario UsuarioResponsable { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // DATOS INCORRECTOS Y CORRECCIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// JSON con los campos que tienen discrepancias
        /// Formato: {"HC": {"Expediente": "12345", "Brazalete": "12346"}, ...}
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string DatosIncorrectos { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del problema encontrado
        /// Ej: "El nombre en el brazalete dice 'Juan Perez' pero debería ser 'José Perez'"
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string DescripcionProblema { get; set; } = string.Empty;

        /// <summary>
        /// Observaciones adicionales del vigilante al momento de rechazar
        /// Ej: "Brazalete ilegible", "Código QR dañado", "Datos borrosos"
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesSolicitud { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ESTADO DE LA SOLICITUD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si la solicitud fue resuelta
        /// </summary>
        [Required]
        public bool Resuelta { get; set; } = false;

        /// <summary>
        /// Fecha y hora en que se resolvió la solicitud
        /// </summary>
        public DateTime? FechaHoraResolucion { get; set; }

        /// <summary>
        /// Descripción de las correcciones realizadas por enfermería
        /// Ej: "Se corrigió el nombre de 'Juan' a 'José'. Brazalete reimpreso."
        /// </summary>
        [MaxLength(1000)]
        public string? DescripcionResolucion { get; set; }

        /// <summary>
        /// Observaciones de enfermería al resolver
        /// Ej: "Error en transcripción manual", "Dato incorrecto en SIGEM"
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesResolucion { get; set; }

        /// <summary>
        /// Indica si se reimprimió el brazalete
        /// </summary>
        public bool BrazaleteReimpreso { get; set; } = false;

        /// <summary>
        /// Fecha y hora en que se reimprimió el brazalete
        /// </summary>
        public DateTime? FechaHoraReimpresion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ALERTAS Y NOTIFICACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si se notificó a la supervisora de enfermería
        /// (Todas las correcciones deben notificar a supervisión)
        /// </summary>
        public bool NotificadoSupervisora { get; set; } = false;

        /// <summary>
        /// Fecha y hora de la notificación a supervisora
        /// </summary>
        public DateTime? FechaHoraNotificacionSupervisora { get; set; }

        /// <summary>
        /// Indica si se notificó al jefe de guardia
        /// (Solo se notifica si la solicitud supera las 2 horas sin resolver)
        /// </summary>
        public bool NotificadoJefeGuardia { get; set; } = false;

        /// <summary>
        /// Fecha y hora de la notificación a jefe de guardia
        /// </summary>
        public DateTime? FechaHoraNotificacionJefeGuardia { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula el tiempo transcurrido desde la solicitud
        /// </summary>
        public TimeSpan TiempoTranscurrido()
        {
            var fechaFin = Resuelta ? FechaHoraResolucion!.Value : DateTime.Now;
            return fechaFin - FechaHoraSolicitud;
        }

        /// <summary>
        /// Verifica si la solicitud superó las 2 horas sin resolver (alerta crítica)
        /// </summary>
        public bool SuperaTiempoAlerta()
        {
            return !Resuelta && TiempoTranscurrido().TotalHours >= 2;
        }

        /// <summary>
        /// Verifica si la solicitud está pendiente
        /// </summary>
        public bool EstaPendiente()
        {
            return !Resuelta;
        }

        /// <summary>
        /// Marca la solicitud como resuelta
        /// </summary>
        public void Resolver(string descripcionResolucion, bool brazaleteReimpreso, string? observaciones = null)
        {
            if (Resuelta)
                throw new InvalidOperationException("Esta solicitud ya fue resuelta");

            if (string.IsNullOrWhiteSpace(descripcionResolucion))
                throw new ArgumentException("Debe proporcionar una descripción de la resolución", nameof(descripcionResolucion));

            Resuelta = true;
            FechaHoraResolucion = DateTime.Now;
            DescripcionResolucion = descripcionResolucion;
            BrazaleteReimpreso = brazaleteReimpreso;
            ObservacionesResolucion = observaciones;

            if (brazaleteReimpreso)
            {
                FechaHoraReimpresion = DateTime.Now;
            }
        }

        /// <summary>
        /// Registra la notificación a la supervisora de enfermería
        /// </summary>
        public void NotificarSupervisora()
        {
            if (NotificadoSupervisora)
                return; // Ya fue notificada

            NotificadoSupervisora = true;
            FechaHoraNotificacionSupervisora = DateTime.Now;
        }

        /// <summary>
        /// Registra la notificación al jefe de guardia
        /// </summary>
        public void NotificarJefeGuardia()
        {
            if (NotificadoJefeGuardia)
                return; // Ya fue notificado

            NotificadoJefeGuardia = true;
            FechaHoraNotificacionJefeGuardia = DateTime.Now;
        }

        /// <summary>
        /// Genera un resumen de la solicitud para notificaciones
        /// </summary>
        public string GenerarResumen()
        {
            var estado = Resuelta ? "RESUELTA" : "PENDIENTE";
            var tiempo = TiempoTranscurrido();
            var tiempoTexto = tiempo.TotalHours >= 1
                ? $"{tiempo.Hours}h {tiempo.Minutes}m"
                : $"{tiempo.Minutes}m";

            return $"Solicitud #{SolicitudID} - {estado} - Tiempo: {tiempoTexto}\n" +
                   $"Expediente: {Expediente?.CodigoExpediente ?? "N/A"}\n" +
                   $"Problema: {DescripcionProblema}";
        }
    }
}