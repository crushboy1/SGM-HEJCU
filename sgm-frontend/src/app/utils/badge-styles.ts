/**
 * @module badgeStyles
 * Sistema centralizado de estilos para badges y estados del SGM
 * Basado en los colores del sistema hospitalario HEJCU
 * Usa Lucide Icons
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
  // Estados del flujo de expedientes
  ['enpiso', {
    theme: 'info',
    icon: 'building-2',  // ✅ Existe en icon.component
    label: 'En Piso'
  }],
  ['pendientederecojo', {
    theme: 'warning',
    icon: 'clock',  // ✅ Existe
    label: 'Pendiente de Recojo'
  }],
  ['entrasladomortuorio', {
    theme: 'purple',
    icon: 'truck',  // ✅ Existe
    label: 'En Traslado'
  }],
  ['verificacionrechazadamortuorio', {
    theme: 'error',
    icon: 'circle-x',  // ✅ Existe
    label: 'Verificación Rechazada'
  }],
  ['pendienteasignacionbandeja', {
    theme: 'warning',
    icon: 'clipboard-list',  // ✅ Existe
    label: 'Pendiente Asignación'
  }],
  ['enbandeja', {
    theme: 'success',
    icon: 'archive',  // ✅ Existe
    label: 'En Bandeja'
  }],
  ['pendienteretiro', {
    theme: 'cyan',
    icon: 'file-check',  // ✅ Existe
    label: 'Pendiente Retiro'
  }],
  ['retirado', {
    theme: 'success',
    icon: 'circle-check',  // ✅ Existe
    label: 'Retirado'
  }],

  // Estados de bandejas
  ['disponible', {
    theme: 'success',
    icon: 'circle-check',  // ✅ Existe
    label: 'Disponible'
  }],
  ['ocupada', {
    theme: 'error',
    icon: 'alert-triangle',  // ✅ Existe
    label: 'Ocupada'
  }],
  ['mantenimiento', {
    theme: 'warning',
    icon: 'settings',  // ✅ Existe
    label: 'Mantenimiento'
  }],

  // Estados generales
  ['activo', {
    theme: 'success',
    icon: 'circle-check',  // ✅ Existe
    label: 'Activo'
  }],
  ['inactivo', {
    theme: 'neutral',
    icon: 'circle-x',  // ✅ Existe
    label: 'Inactivo'
  }],
]);

const defaultConfig: BadgeConfig = {
  theme: 'neutral',
  icon: 'info',  // ✅ Existe
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
  neutral: 'bg-gray-100 text-gray-600 border-gray-300',
  urgent: 'bg-red-100 text-red-800 border-red-400 animate-pulse',
  default: 'bg-gray-100 text-gray-500 border-gray-200',
};

// ===================================================================
// FUNCIONES PÚBLICAS
// ===================================================================

function normalizeEstado(estado: string): string {
  return estado.toLowerCase().replace(/\s+/g, '');
}

export function getEstadoConfig(estado: string): BadgeConfig {
  if (!estado) return defaultConfig;

  const normalized = normalizeEstado(estado);
  return estadosMap.get(normalized) || defaultConfig;
}

export function getBadgeClasses(estado: string): string {
  const baseClasses = 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border';
  const config = getEstadoConfig(estado);
  const themeClasses = badgeThemes[config.theme] || badgeThemes.default;

  return `${baseClasses} ${themeClasses}`;
}

export function getBadgeClassesLg(estado: string): string {
  const baseClasses = 'inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold border-2';
  const config = getEstadoConfig(estado);
  const themeClasses = badgeThemes[config.theme] || badgeThemes.default;

  return `${baseClasses} ${themeClasses}`;
}

export function getEstadoIcon(estado: string): string {
  const config = getEstadoConfig(estado);
  return config.icon;
}

export function getEstadoLabel(estado: string): string {
  const config = getEstadoConfig(estado);
  return config.label || estado;
}

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

export function getEstadoTextColor(estado: string): string {
  const config = getEstadoConfig(estado);

  const textColors: Record<string, string> = {
    success: 'text-green-800',
    error: 'text-red-800',
    warning: 'text-yellow-800',
    info: 'text-blue-800',
    purple: 'text-purple-800',
    cyan: 'text-cyan-800',
    neutral: 'text-gray-600',
    default: 'text-gray-500',
  };

  return textColors[config.theme] || textColors.default;
}

export function getCardBgClasses(estado: string): string {
  const config = getEstadoConfig(estado);

  const cardBgs: Record<string, string> = {
    success: 'bg-green-50 border-green-200',
    error: 'bg-red-50 border-red-200',
    warning: 'bg-yellow-50 border-yellow-200',
    info: 'bg-blue-50 border-blue-200',
    purple: 'bg-purple-50 border-purple-200',
    cyan: 'bg-cyan-50 border-cyan-200',
    neutral: 'bg-gray-50 border-gray-200',
    default: 'bg-white border-gray-200',
  };

  return cardBgs[config.theme] || cardBgs.default;
}

export function isEstadoUrgente(estado: string): boolean {
  const normalized = normalizeEstado(estado);
  const estadosUrgentes = [
    'verificacionrechazadamortuorio',
    'pendienteretiro'
  ];

  return estadosUrgentes.includes(normalized);
}

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
