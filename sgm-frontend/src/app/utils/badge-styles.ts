/**
 * @module badgeStyles
 * Sistema centralizado de estilos para badges y estados del SGM
 * Basado en los colores del sistema hospitalario HEJCU
 * Usa Lucide Icons
 * 
 * @version 2.0.0
 * @changelog
 * - v2.0.0: Agregado tema 'orange', estado 'fueradeservicio', helper getBadgeTheme()
 */

// ===================================================================
// TIPOS
// ===================================================================

export interface BadgeConfig {
  theme: string;
  icon: string;
  label?: string;
}

// ===================================================================
// CONFIGURACIÓN MAESTRA DE ESTADOS DEL SGM
// ===================================================================

const estadosMap = new Map<string, BadgeConfig>([
  // ===================================================================
  // ESTADOS DEL FLUJO DE EXPEDIENTES
  // ===================================================================
  ['pendientedeexpediente', {
    theme: 'warning',
    icon: 'file-plus',
    label: 'Pendiente de Expediente'
  }],
  ['enpiso', {
    theme: 'info',
    icon: 'building-2',
    label: 'En Piso'
  }],
  ['pendientederecojo', {
    theme: 'warning',
    icon: 'clock',
    label: 'Pendiente de Recojo'
  }],
  ['entrasladomortuorio', {
    theme: 'purple',
    icon: 'truck',
    label: 'En Traslado'
  }],
  ['verificacionrechazadamortuorio', {
    theme: 'error',
    icon: 'circle-x',
    label: 'Verificación Rechazada'
  }],
  ['pendienteasignacionbandeja', {
    theme: 'warning',
    icon: 'clipboard-list',
    label: 'Pendiente Asignación'
  }],
  ['enbandeja', {
    theme: 'success',
    icon: 'archive',
    label: 'En Bandeja'
  }],
  ['pendienteretiro', {
    theme: 'cyan',
    icon: 'file-check',
    label: 'Pendiente Retiro'
  }],
  ['retirado', {
    theme: 'success',
    icon: 'circle-check',
    label: 'Retirado'
  }],

  // ===================================================================
  // ESTADOS DE BANDEJAS
  // ===================================================================
  ['disponible', {
    theme: 'success',
    icon: 'circle-check',
    label: 'Disponible'
  }],
  ['ocupada', {
    theme: 'error',
    icon: 'alert-triangle',
    label: 'Ocupada'
  }],
  ['mantenimiento', {
    theme: 'orange',
    icon: 'settings',
    label: 'Mantenimiento'
  }],
  ['fueradeservicio', {
    theme: 'neutral',
    icon: 'ban',
    label: 'Fuera de Servicio'
  }],

  // ===================================================================
  // ESTADOS GENERALES
  // ===================================================================
  ['activo', {
    theme: 'success',
    icon: 'circle-check',
    label: 'Activo'
  }],
  ['inactivo', {
    theme: 'neutral',
    icon: 'circle-x',
    label: 'Inactivo'
  }],

  // ===================================================================
  // ESTADOS DE NOTIFICACIONES SIGNALR
  // ===================================================================
  ['notif_info', {
    theme: 'info',
    icon: 'info',
    label: 'Información'
  }],
  ['notif_exito', {
    theme: 'success',
    icon: 'circle-check',
    label: 'Éxito'
  }],
  ['notif_advertencia', {
    theme: 'warning',
    icon: 'alert-triangle',
    label: 'Advertencia'
  }],
  ['notif_error', {
    theme: 'error',
    icon: 'circle-x',
    label: 'Error'
  }],
  ['notif_critico', {
    theme: 'urgent',
    icon: 'alert-circle',
    label: 'Crítico'
  }],
]);

const defaultConfig: BadgeConfig = {
  theme: 'neutral',
  icon: 'info',
  label: 'Desconocido'
};

// ===================================================================
// TEMAS DE BADGES (Clases de Tailwind)
// ===================================================================

const badgeThemes: Record<string, string> = {
  success: 'bg-green-100 text-green-800 border-green-300',
  error: 'bg-red-100 text-red-800 border-red-300',
  warning: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  info: 'bg-blue-100 text-blue-800 border-blue-300',
  purple: 'bg-purple-100 text-purple-800 border-purple-300',
  cyan: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  orange: 'bg-orange-100 text-orange-800 border-orange-300',
  neutral: 'bg-gray-100 text-gray-600 border-gray-300',
  urgent: 'bg-red-100 text-red-800 border-red-400 animate-pulse',
  default: 'bg-gray-100 text-gray-500 border-gray-200',
};

// ===================================================================
// FUNCIONES PÚBLICAS
// ===================================================================

/**
 * Normaliza un estado a lowercase sin espacios para búsqueda en el Map.
 */
function normalizeEstado(estado: string): string {
  return estado.toLowerCase().replace(/\s+/g, '');
}

/**
 * Obtiene la configuración completa de un estado.
 */
export function getEstadoConfig(estado: string): BadgeConfig {
  if (!estado) return defaultConfig;

  const normalized = normalizeEstado(estado);
  return estadosMap.get(normalized) || defaultConfig;
}

/**
 * Obtiene las clases CSS completas para un badge normal.
 */
export function getBadgeClasses(estado: string): string {
  const baseClasses = 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border';
  const config = getEstadoConfig(estado);
  const themeClasses = badgeThemes[config.theme] || badgeThemes.default;

  return `${baseClasses} ${themeClasses}`;
}

/**
 * Obtiene las clases CSS completas para un badge grande.
 */
export function getBadgeClassesLg(estado: string): string {
  const baseClasses = 'inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold border-2';
  const config = getEstadoConfig(estado);
  const themeClasses = badgeThemes[config.theme] || badgeThemes.default;

  return `${baseClasses} ${themeClasses}`;
}

/**
 * Obtiene el nombre del ícono para un estado.
 */
export function getEstadoIcon(estado: string): string {
  const config = getEstadoConfig(estado);
  return config.icon;
}

/**
 * Obtiene la etiqueta legible de un estado.
 */
export function getEstadoLabel(estado: string): string {
  const config = getEstadoConfig(estado);
  return config.label || estado;
}

/**
 * Obtiene solo el tema de un estado.
 * Útil cuando solo necesitas el color sin las clases completas.
 */
export function getBadgeTheme(estado: string): string {
  const config = getEstadoConfig(estado);
  return config.theme;
}

/**
 * Obtiene un objeto completo con clases, ícono, label y tema.
 * Útil para uso en templates Angular.
 */
export function getBadgeWithIcon(estado: string): {
  classes: string;
  icon: string;
  label: string;
  theme: string;
} {
  const config = getEstadoConfig(estado);
  return {
    classes: getBadgeClasses(estado),
    icon: config.icon,
    label: config.label || estado,
    theme: config.theme
  };
}

/**
 * Obtiene la clase de color de texto para un estado.
 */
export function getEstadoTextColor(estado: string): string {
  const config = getEstadoConfig(estado);

  const textColors: Record<string, string> = {
    success: 'text-green-800',
    error: 'text-red-800',
    warning: 'text-yellow-800',
    info: 'text-blue-800',
    purple: 'text-purple-800',
    cyan: 'text-cyan-800',
    orange: 'text-orange-800',
    neutral: 'text-gray-600',
    urgent: 'text-red-800',
    default: 'text-gray-500',
  };

  return textColors[config.theme] || textColors.default;
}

/**
 * Obtiene las clases de fondo y borde para cards basadas en el estado.
 */
export function getCardBgClasses(estado: string): string {
  const config = getEstadoConfig(estado);

  const cardBgs: Record<string, string> = {
    success: 'bg-green-50 border-green-200',
    error: 'bg-red-50 border-red-200',
    warning: 'bg-yellow-50 border-yellow-200',
    info: 'bg-blue-50 border-blue-200',
    purple: 'bg-purple-50 border-purple-200',
    cyan: 'bg-cyan-50 border-cyan-200',
    orange: 'bg-orange-50 border-orange-200',
    neutral: 'bg-gray-50 border-gray-200',
    urgent: 'bg-red-50 border-red-300',
    default: 'bg-white border-gray-200',
  };

  return cardBgs[config.theme] || cardBgs.default;
}

/**
 * Verifica si un estado es considerado urgente.
 */
export function isEstadoUrgente(estado: string): boolean {
  const normalized = normalizeEstado(estado);
  const estadosUrgentes = [
    'verificacionrechazadamortuorio',
    'pendienteretiro'
  ];

  return estadosUrgentes.includes(normalized);
}

/**
 * Obtiene el nivel de prioridad de un estado (mayor = más urgente).
 * Útil para ordenamiento de listas.
 */
export function getEstadoPriority(estado: string): number {
  const normalized = normalizeEstado(estado);

  const priorities: Record<string, number> = {
    'verificacionrechazadamortuorio': 10,
    'pendienteretiro': 9,
    'pendientederecojo': 8,
    'entrasladomortuorio': 7,
    'pendienteasignacionbandeja': 6,
    'enbandeja': 5,
    'enpiso': 4,
    'retirado': 1,
  };

  return priorities[normalized] || 0;
}
// ===================================================================
// HELPERS PARA SEMÁFORO DE DEUDAS
// ===================================================================

/**
 * Obtiene las clases completas para badge de semáforo de deudas
 * @param bloqueaRetiro - Si true, muestra estado "DEBE" (rojo), si false "NO DEBE" (verde)
 * @param compacto - Si true, usa tamaño reducido
 * @returns Clases Tailwind completas para el badge
 */
export function getSemaforoDeudaClasses(bloqueaRetiro: boolean, compacto: boolean = false): string {
  const baseClasses = 'inline-flex items-center gap-1.5 rounded-lg font-semibold border-2 transition-all';
  const sizeClasses = compacto ? 'text-xs px-2 py-1' : 'px-3 py-1.5';

  if (bloqueaRetiro) {
    return `${baseClasses} ${sizeClasses} bg-red-50 border-red-300 text-red-700 shadow-sm`;
  }

  return `${baseClasses} ${sizeClasses} bg-green-50 border-green-300 text-green-700 shadow-sm`;
}

/**
 * Obtiene el ícono apropiado para semáforo de deudas
 * @param bloqueaRetiro - Si true, retorna ícono de alerta, si false de check
 * @returns Nombre del ícono Lucide
 */
export function getSemaforoDeudaIcon(bloqueaRetiro: boolean): string {
  return bloqueaRetiro ? 'alert-circle' : 'circle-check';
}

/**
 * Obtiene el color del texto para semáforo de deudas
 * @param bloqueaRetiro - Si true, retorna color rojo, si false verde
 * @returns Clase Tailwind de color de texto
 */
export function getSemaforoDeudaTextColor(bloqueaRetiro: boolean): string {
  return bloqueaRetiro ? 'text-red-700' : 'text-green-700';
}

/**
 * Obtiene el texto legible del semáforo
 * @param bloqueaRetiro - Estado de bloqueo de retiro
 * @returns "DEBE" o "NO DEBE"
 */
export function getSemaforoDeudaLabel(bloqueaRetiro: boolean): string {
  return bloqueaRetiro ? 'DEBE' : 'NO DEBE';
}

/**
 * Obtiene un objeto completo con toda la configuración del semáforo
 * Útil para uso en templates que necesitan múltiples propiedades
 * @param bloqueaRetiro - Estado de bloqueo de retiro
 * @param compacto - Si true, usa versión reducida
 * @returns Objeto con classes, icon, textColor y label
 */
export function getSemaforoDeudaConfig(bloqueaRetiro: boolean, compacto: boolean = false): {
  classes: string;
  icon: string;
  textColor: string;
  label: string;
} {
  return {
    classes: getSemaforoDeudaClasses(bloqueaRetiro, compacto),
    icon: getSemaforoDeudaIcon(bloqueaRetiro),
    textColor: getSemaforoDeudaTextColor(bloqueaRetiro),
    label: getSemaforoDeudaLabel(bloqueaRetiro)
  };
}
