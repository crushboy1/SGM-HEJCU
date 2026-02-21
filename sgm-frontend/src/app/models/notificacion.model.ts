/**
 * Modelo de notificación en tiempo real recibida desde SignalR.
 * Debe coincidir exactamente con NotificacionDTO.cs del backend.
 * 
 * Changelog:
 * - v2.0: Agregado categoriaNotificacion, estadoAnterior, estadoNuevo
 * - v2.0: Tipo "exito" → "success" para consistencia con SweetAlert2
 */
export interface NotificacionDTO {
  /** ID único de la notificación (para tracking/persistencia) */
  id: string;
  /** Título de la notificación (breve, máx 50 caracteres) */
  titulo: string;
  /** Mensaje detallado de la notificación */
  mensaje: string;
  /** Tipo/severidad: "info" | "success" | "warning" | "error" */
  tipo: TipoNotificacion;
  /** 
   * Categoría de notificación para clasificación automática.
   * Valores: "nuevo_expediente" | "expediente_actualizado" | "bandeja_actualizada" | 
   *          "alerta_ocupacion" | "alerta_permanencia" | "solicitud_correccion" | "generico"
   */
  categoriaNotificacion?: string;
  /** Timestamp de cuando se generó la notificación */
  fechaHora: Date;
  /** Rol(es) destinatario(s) separados por coma (null = todos) */
  rolesDestino?: string;
  /** ID del expediente relacionado (si aplica) */
  expedienteId?: number;
  /** Código del expediente relacionado (si aplica) */
  codigoExpediente?: string;
  /** Estado anterior del expediente (para notificaciones de cambio de estado) */
  estadoAnterior?: string;
  /** Estado nuevo del expediente (para notificaciones de cambio de estado) */
  estadoNuevo?: string;
  /** Acción sugerida para el usuario */
  accionSugerida?: string;
  /** URL/ruta de navegación para la acción sugerida */
  urlNavegacion?: string;
  /** Indica si la notificación requiere confirmación/acción del usuario */
  requiereAccion: boolean;
  /** Indica si la notificación expira después de cierto tiempo */
  fechaExpiracion?: Date;
  /** Estado local (no viene del backend, se actualiza en el frontend) */
  leida?: boolean;
}

/**
 * Tipos de notificación con sus colores asociados.
 * Actualizado: "exito" → "success" para consistencia con SweetAlert2
 */
export type TipoNotificacion = 'info' | 'success' | 'warning' | 'error';

/**
 * Estadísticas de ocupación del mortuorio.
 * Debe coincidir con EstadisticasBandejaDTO.cs
 */
export interface EstadisticasBandejaDTO {
  total: number;
  disponibles: number;
  ocupadas: number;
  enMantenimiento: number;
  fueraDeServicio: number;
  porcentajeOcupacion: number;
  conAlerta24h: number;
  conAlerta48h: number;
}

/**
 * Información de una bandeja del mortuorio.
 * Debe coincidir con BandejaDTO.cs
 */
export interface BandejaDTO {
  bandejaID: number;
  codigo: string;
  estado: string;
  observaciones?: string;
  expedienteID?: number;
  codigoExpediente?: string;
  nombrePaciente?: string;
  usuarioAsignaNombre?: string;
  fechaHoraLiberacion?: Date;
  fechaHoraAsignacion?: Date;
  tiempoOcupada?: string;
  tieneAlerta?: boolean;
}


export interface BandejaDisponibleDTO {
  bandejaID: number;
  codigo: string;
}
export interface AsignarBandejaDTO {
  bandejaID: number;
  expedienteID: number;
  observaciones?: string;
}
/**
 * DTO para liberar bandeja manualmente (emergencia)
 */
export interface LiberarBandejaManualDTO {
  bandejaID: number;
  motivoLiberacion: string; // Mínimo 3 caracteres
  observaciones: string; // Mínimo 20 caracteres
  usuarioLiberaID: number;
}
// ========================================
// INTERFACES DEUDAS (para SignalR events)
// ========================================

export interface DeudaCreada {
  expedienteID: number;
  codigoExpediente: string;
  tipoDeuda: 'Sangre' | 'Economica';
  monto?: number;
  unidades?: number;
}

export interface DeudaResuelta {
  expedienteID: number;
  codigoExpediente: string;
  tipoDeuda: 'Sangre' | 'Economica';
  formaResolucion: 'Liquidado' | 'Anulado' | 'Exonerado';
}
/**
 * Enum de motivos comunes (sugerencias para frontend)
 */
export enum MotivoLiberacionManual {
  ErrorSistema = 'Error en sistema de registro',
  SalidaNoAutorizada = 'Salida sin completar trámites',
  Correccion = 'Corrección de asignación incorrecta',
  Emergencia = 'Emergencia o evacuación'
}
/**
 * Solicitud de corrección de expediente.
 * Debe coincidir con SolicitudCorreccionDTO.cs
 */
export interface SolicitudCorreccionDTO {
  solicitudID: number;
  expedienteID: number;
  codigoExpediente: string;
  fechaHoraSolicitud: Date;
  usuarioSolicitaNombre: string;
  usuarioResponsableNombre: string;
  descripcionProblema: string;
  datosIncorrectos: string;
  observacionesSolicitud?: string;
  resuelta: boolean;
  fechaHoraResolucion?: Date;
  descripcionResolucion?: string;
  brazaleteReimpreso: boolean;
  tiempoTranscurrido: string;
  superaTiempoAlerta: boolean;
}

/**
 * Configuración de colores por tipo de notificación (Tailwind CSS).
 * Actualizado: "exito" → "success"
 */
export const COLORES_NOTIFICACION: Record<TipoNotificacion, {
  bg: string;
  border: string;
  text: string;
  icon: string;
}> = {
  info: {
    bg: 'bg-blue-50',
    border: 'border-blue-200',
    text: 'text-blue-800',
    icon: 'text-blue-500'
  },
  success: {
    bg: 'bg-green-50',
    border: 'border-green-200',
    text: 'text-green-800',
    icon: 'text-green-500'
  },
  warning: {
    bg: 'bg-yellow-50',
    border: 'border-yellow-200',
    text: 'text-yellow-800',
    icon: 'text-yellow-500'
  },
  error: {
    bg: 'bg-red-50',
    border: 'border-red-200',
    text: 'text-red-800',
    icon: 'text-red-500'
  }
};

/**
 * Iconos por tipo de notificación (para app-icon component).
 * Actualizado: "exito" → "success"
 */
export const ICONOS_NOTIFICACION: Record<TipoNotificacion, string> = {
  info: 'info',
  success: 'circle-check',
  warning: 'alert-triangle',
  error: 'circle-x'
};
