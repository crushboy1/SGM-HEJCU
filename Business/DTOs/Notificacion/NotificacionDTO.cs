using System;

namespace SisMortuorio.Business.DTOs.Notificacion
{
    /// <summary>
    /// DTO para notificaciones en tiempo real a través de SignalR.
    /// Representa una notificación genérica con metadata completa.
    /// Soporta clasificación automática en el frontend (NuevoExpediente, ExpedienteActualizado).
    /// 
    /// Changelog:
    /// - v2.0: Agregado EstadoAnterior y EstadoNuevo para cambios de estado
    /// - v2.0: Agregado CategoriaNotificacion para clasificación automática
    /// </summary>
    public class NotificacionDTO
    {
        /// <summary>
        /// ID único de la notificación (para tracking/persistencia).
        /// Generado automáticamente en el constructor.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Título de la notificación (breve, máx 50 caracteres).
        /// Ejemplo: "Nuevo Expediente Creado"
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje detallado de la notificación.
        /// Ejemplo: "Se ha creado el expediente SGM-2025-00015 para el paciente Juan Pérez"
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Tipo/severidad de la notificación.
        /// Valores: "info", "success", "warning", "error"
        /// Mapea a los colores de toasts en el frontend.
        /// </summary>
        public string Tipo { get; set; } = "info";

        /// <summary>
        /// ⭐ NUEVO: Categoría de notificación para clasificación automática.
        /// Valores: "nuevo_expediente", "expediente_actualizado", "bandeja_actualizada", 
        ///          "alerta_ocupacion", "alerta_permanencia", "solicitud_correccion", "generico"
        /// 
        /// El frontend usa esto para emitir en el observable correcto:
        /// - "nuevo_expediente" → onNuevoExpediente
        /// - "expediente_actualizado" → onExpedienteActualizado
        /// - otros → onNotificacionGenerica
        /// </summary>
        public string CategoriaNotificacion { get; set; } = "generico";

        /// <summary>
        /// Timestamp de cuando se generó la notificación.
        /// Se usa para ordenar y filtrar notificaciones antiguas.
        /// </summary>
        public DateTime FechaHora { get; set; } = DateTime.Now;

        /// <summary>
        /// Rol(es) destinatario(s) separados por coma.
        /// Ejemplo: "Admision", "Admision,JefeGuardia"
        /// null = notificación para todos los conectados
        /// </summary>
        public string? RolesDestino { get; set; }

        /// <summary>
        /// ID del expediente relacionado (si aplica).
        /// Permite al frontend navegar directamente al expediente.
        /// </summary>
        public int? ExpedienteId { get; set; }

        /// <summary>
        /// Código del expediente relacionado (si aplica).
        /// Ejemplo: "SGM-2025-00015"
        /// </summary>
        public string? CodigoExpediente { get; set; }

        /// <summary>
        /// ⭐ NUEVO: Estado anterior del expediente (para notificaciones de cambio de estado).
        /// Ejemplo: "PendienteDeRecojo"
        /// null = no aplica (no es un cambio de estado)
        /// </summary>
        public string? EstadoAnterior { get; set; }

        /// <summary>
        /// ⭐ NUEVO: Estado nuevo del expediente (para notificaciones de cambio de estado).
        /// Ejemplo: "EnTrasladoMortuorio"
        /// null = no aplica (no es un cambio de estado)
        /// </summary>
        public string? EstadoNuevo { get; set; }

        /// <summary>
        /// Acción sugerida para el usuario.
        /// Ejemplo: "Ver Expediente", "Autorizar Retiro", "Regularizar Deuda"
        /// null = notificación solo informativa
        /// </summary>
        public string? AccionSugerida { get; set; }

        /// <summary>
        /// URL/ruta de navegación para la acción sugerida.
        /// Ejemplo: "/expediente/15", "/autorizacion-retiro/15"
        /// null = sin navegación
        /// </summary>
        public string? UrlNavegacion { get; set; }

        /// <summary>
        /// Indica si la notificación requiere confirmación/acción del usuario.
        /// true = el usuario debe hacer algo (aparecer destacado)
        /// false = solo informativo
        /// </summary>
        public bool RequiereAccion { get; set; } = false;

        /// <summary>
        /// Indica si la notificación expira después de cierto tiempo.
        /// null = no expira (permanece hasta que el usuario la marque como leída)
        /// Ejemplo: DateTime.Now.AddHours(24) para notificaciones temporales
        /// </summary>
        public DateTime? FechaExpiracion { get; set; }

        /// <summary>
        /// ⭐ NUEVO: Indica si la notificación ha sido leída por el usuario.
        /// Se usa en el frontend para el contador de "no leídas".
        /// El backend siempre envía false, el frontend la actualiza localmente.
        /// </summary>
        public bool Leida { get; set; } = false;
    }
}