/**
 * Modelos TypeScript para Expediente Legal
 * Mapea exactamente los DTOs del backend ExpedienteLegalDTO.cs
 * Última actualización: Diciembre 2024
 */

// ========================================
// INTERFACES PRINCIPALES
// ========================================

export interface ExpedienteLegalDTO {
  // ══════════════════════════════════════════════════════
  // IDENTIFICADORES
  // ══════════════════════════════════════════════════════
  expedienteLegalID: number;
  expedienteID: number;
  codigoExpediente: string;

  // ══════════════════════════════════════════════════════
  // DATOS DEL PACIENTE (Separados para filtros)
  // ══════════════════════════════════════════════════════
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  nombrePaciente: string; // Concatenado para display
  hc: string;
  numeroDocumento: string;

  // ══════════════════════════════════════════════════════
  // ESTADO DEL FLUJO
  // ══════════════════════════════════════════════════════

  /**
   * Estado actual del expediente.
   * Valores: EnRegistro | PendienteValidacionAdmision | RechazadoAdmision | 
   *          ValidadoAdmision | AutorizadoJefeGuardia
   */
  estado: string;

  /**
   * Texto descriptivo del estado para UI.
   */
  estadoDescripcion: string;

  // ══════════════════════════════════════════════════════
  // DATOS DE REFERENCIA LEGAL
  // ══════════════════════════════════════════════════════
  numeroOficioPNP?: string;
  comisaria?: string;
  fiscalia?: string;
  destino?: string;
  observaciones?: string;

  // ══════════════════════════════════════════════════════
  // VALIDACIÓN ADMISIÓN
  // ══════════════════════════════════════════════════════
  validadoAdmision: boolean;
  fechaValidacionAdmision?: Date;
  usuarioAdmisionID?: number;
  usuarioAdmisionNombre?: string;
  observacionesAdmision?: string;

  // ══════════════════════════════════════════════════════
  // AUTORIZACIÓN JEFE GUARDIA
  // ══════════════════════════════════════════════════════
  autorizadoJefeGuardia: boolean;
  fechaAutorizacion?: Date;
  jefeGuardiaID?: number;
  jefeGuardiaNombre?: string;
  observacionesJefeGuardia?: string;

  // ══════════════════════════════════════════════════════
  // ESTADO DOCUMENTARIO
  // ══════════════════════════════════════════════════════
  documentosCompletos: boolean;
  documentosPendientes?: string;
  tienePendientes: boolean;
  fechaLimitePendientes?: Date;
  diasRestantes?: number;

  // ══════════════════════════════════════════════════════
  // RESUMEN AUTORIDADES
  // ══════════════════════════════════════════════════════
  nombrePolicia?: string;
  nombreFiscal?: string;
  nombreMedicoLegista?: string;

  // ══════════════════════════════════════════════════════
  // COLECCIONES
  // ══════════════════════════════════════════════════════
  autoridades: AutoridadExternaDTO[];
  documentos: DocumentoLegalDTO[];

  // ══════════════════════════════════════════════════════
  // AUDITORÍA
  // ══════════════════════════════════════════════════════
  usuarioRegistroID: number;
  usuarioRegistroNombre: string;
  fechaCreacion: Date;
  usuarioActualizacionID?: number;
  usuarioActualizacionNombre?: string;
  fechaUltimaActualizacion?: Date;

  // ══════════════════════════════════════════════════════
  // CAMPOS CALCULADOS
  // ══════════════════════════════════════════════════════
  cantidadAutoridades: number;
  cantidadDocumentos: number;

  // ══════════════════════════════════════════════════════
  // PERMISOS DE UI
  // ══════════════════════════════════════════════════════

  /**
   * Indica si el usuario actual puede marcar como listo para Admisión.
   * Solo Vigilancia en estado EnRegistro.
   */
  puedeMarcarListo: boolean;

  /**
   * Indica si el usuario actual puede validar documentación.
   * Solo Admisión en estado PendienteValidacionAdmision.
   */
  puedeValidarAdmision: boolean;

  /**
   * Indica si el usuario actual puede autorizar levantamiento.
   * Solo Jefe Guardia en estado ValidadoAdmision.
   */
  puedeAutorizarJefeGuardia: boolean;
}

/**
 * DTO para autoridades externas (Policía, Fiscal, Médico Legista)
 */
export interface AutoridadExternaDTO {
  autoridadExternaID: number;
  expedienteLegalID: number;
  tipoAutoridad: string; // "Policia" | "Fiscal" | "MedicoLegista"
  nombreCompleto: string;
  numeroPlaca?: string; // Para policías
  numeroRegistro?: string; // Para fiscales/médicos
  institucion: string;
  telefono?: string;
  email?: string;
  fechaRegistro: Date;
  usuarioRegistroID: number;
  usuarioRegistroNombre: string;
}

/**
 * DTO para documentos legales asociados al expediente
 */
export interface DocumentoLegalDTO {
  documentoLegalID: number;
  expedienteLegalID: number;
  tipoDocumento: string; // "OficioPNP" | "ActaLevantamiento" | "CertificadoDefuncion" | "InformeFiscalia" | "Otro"
  numeroDocumento?: string;
  descripcion?: string;
  rutaArchivo: string;
  nombreArchivo: string;
  tamanoArchivo: number;
  extension: string;
  estaValidado: boolean;
  fechaValidacion?: Date;
  usuarioValidacionID?: number;
  usuarioValidacionNombre?: string;
  observacionesValidacion?: string;
  fechaCarga: Date;
  usuarioCargaID: number;
  usuarioCargaNombre: string;
  observaciones?: string;
}

/**
 * DTO para crear un nuevo expediente legal
 */
export interface CreateExpedienteLegalDTO {
  expedienteID: number;
  tipoCaso: string;
  usuarioRegistroID: number;
  numeroActaPolicial?: string;
  numeroOficioPNP?: string;
  comisaria?: string;
  fiscalia?: string;
  destino?: string;
  observaciones?: string;
}

/**
 * DTO para actualizar datos del expediente legal
 */
export interface UpdateExpedienteLegalDTO {
  numeroOficioPNP?: string;
  comisaria?: string;
  fiscalia?: string;
  destino?: string;
  observaciones?: string;
}

/**
 * DTO para marcar expediente como listo para Admisión (Vigilancia)
 */
export interface MarcarListoAdmisionDTO {
  expedienteLegalID: number;
  observaciones?: string;
}

/**
 * DTO para validar expediente (Admisión)
 */
export interface ValidarAdmisionDTO {
  expedienteLegalID: number;
  aprobar: boolean;
  observaciones?: string;
}

/**
 * DTO para autorizar levantamiento (Jefe Guardia)
 */
export interface AutorizarJefeGuardiaDTO {
  expedienteLegalID: number;
  observaciones?: string;
}

/**
 * DTO para subir documentos legales
 */
export interface UploadDocumentoLegalDTO {
  expedienteLegalID: number;
  tipoDocumento: string;
  numeroDocumento?: string;
  descripcion?: string;
  observaciones?: string;
}
/**
 * DTO de autoridad externa para frontend.
 * Representa una autoridad (PNP, Fiscal, Legista) registrada en el expediente legal.
 */
export interface AutoridadExternaDTO {
  autoridadExternaID: number;
  expedienteLegalID: number;
  tipoAutoridad: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  nombreCompleto: string; // ⭐ SIN '?'
  tipoDocumento: string;
  numeroDocumento: string;
  institucion: string; // ⭐ SIN '?'
  cargo?: string;
  codigoEspecial?: string;
  placaVehiculo?: string;
  telefono?: string;
  email?: string;
  fechaRegistro: Date; // ⭐ Solo Date, no string
}

/**
 * DTO para agregar autoridad externa
 */
export interface CreateAutoridadExternaDTO {
  expedienteLegalID: number;
  usuarioRegistroID?: number; // Opcional, se obtiene del token
  tipoAutoridad: 'Policia' | 'Fiscal' | 'MedicoLegista';
  nombreCompleto: string; // ⭐ Nombre completo directo (frontend lo concatena)
  numeroPlaca?: string; // ⭐ Para policías
  institucion: string;
  fechaHoraLlegada: string; // ISO format
}
/**
 * DTO para autorizar expediente legal por Jefe de Guardia.
 * Transición: ValidadoAdmision → AutorizadoJefeGuardia
 */
export interface AutorizarJefeGuardiaDTO {
  expedienteLegalID: number;
  validado: boolean;
  observacionesValidacion?: string;
}
// ========================================
// ENUMS
// ========================================

/**
 * Estados posibles del expediente legal en el flujo híbrido
 */
export enum EstadoExpedienteLegal {
  EnRegistro = 'EnRegistro',
  PendienteValidacionAdmision = 'PendienteValidacionAdmision',
  RechazadoAdmision = 'RechazadoAdmision',
  ValidadoAdmision = 'ValidadoAdmision',
  AutorizadoJefeGuardia = 'AutorizadoJefeGuardia'
}

/**
 * Tipos de documentos legales
 */
export enum TipoDocumentoLegal {
  OficioPNP = 'OficioPNP',
  ActaLevantamiento = 'ActaLevantamiento',
  CertificadoDefuncion = 'CertificadoDefuncion',
  InformeFiscalia = 'InformeFiscalia',
  ProtocoloAutopsia = 'ProtocoloAutopsia',
  Otro = 'Otro'
}

/**
 * Tipos de autoridades externas
 */
export enum TipoAutoridad {
  Policia = 'Policia',
  Fiscal = 'Fiscal',
  MedicoLegista = 'MedicoLegista'
}

// ========================================
// HELPERS Y UTILIDADES
// ========================================

export class ExpedienteLegalHelper {
  /**
   * Obtiene el color del badge según el estado
   */
  static getEstadoColor(estado: string): string {
    switch (estado) {
      case EstadoExpedienteLegal.EnRegistro:
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case EstadoExpedienteLegal.PendienteValidacionAdmision:
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case EstadoExpedienteLegal.RechazadoAdmision:
        return 'bg-red-100 text-red-800 border-red-300';
      case EstadoExpedienteLegal.ValidadoAdmision:
        return 'bg-green-100 text-green-800 border-green-300';
      case EstadoExpedienteLegal.AutorizadoJefeGuardia:
        return 'bg-purple-100 text-purple-800 border-purple-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  /**
   * Obtiene el icono según el estado
   */
  static getEstadoIcon(estado: string): string {
    switch (estado) {
      case EstadoExpedienteLegal.EnRegistro:
        return 'file-edit';
      case EstadoExpedienteLegal.PendienteValidacionAdmision:
        return 'clock';
      case EstadoExpedienteLegal.RechazadoAdmision:
        return 'file-x';
      case EstadoExpedienteLegal.ValidadoAdmision:
        return 'file-check';
      case EstadoExpedienteLegal.AutorizadoJefeGuardia:
        return 'file-badge';
      default:
        return 'file';
    }
  }

  /**
   * Obtiene el texto legible del estado
   */
  static getEstadoLabel(estado: string): string {
    switch (estado) {
      case EstadoExpedienteLegal.EnRegistro:
        return 'En Registro';
      case EstadoExpedienteLegal.PendienteValidacionAdmision:
        return 'Pendiente Validación Admisión';
      case EstadoExpedienteLegal.RechazadoAdmision:
        return 'Rechazado por Admisión';
      case EstadoExpedienteLegal.ValidadoAdmision:
        return 'Validado por Admisión';
      case EstadoExpedienteLegal.AutorizadoJefeGuardia:
        return 'Autorizado Jefe Guardia';
      default:
        return estado;
    }
  }

  /**
   * Obtiene el texto legible del tipo de documento
   */
  static getTipoDocumentoLabel(tipo: string): string {
    switch (tipo) {
      case TipoDocumentoLegal.OficioPNP:
        return 'Oficio PNP';
      case TipoDocumentoLegal.ActaLevantamiento:
        return 'Acta de Levantamiento';
      case TipoDocumentoLegal.CertificadoDefuncion:
        return 'Certificado de Defunción';
      case TipoDocumentoLegal.InformeFiscalia:
        return 'Informe Fiscalía';
      case TipoDocumentoLegal.ProtocoloAutopsia:
        return 'Protocolo de Autopsia';
      case TipoDocumentoLegal.Otro:
        return 'Otro Documento';
      default:
        return tipo;
    }
  }

  /**
   * Obtiene el icono según tipo de documento
   */
  static getTipoDocumentoIcon(tipo: string): string {
    switch (tipo) {
      case TipoDocumentoLegal.OficioPNP:
        return 'shield';
      case TipoDocumentoLegal.ActaLevantamiento:
        return 'file-text';
      case TipoDocumentoLegal.CertificadoDefuncion:
        return 'file-certificate';
      case TipoDocumentoLegal.InformeFiscalia:
        return 'file-badge';
      case TipoDocumentoLegal.ProtocoloAutopsia:
        return 'microscope';
      default:
        return 'file';
    }
  }

  /**
   * Obtiene el texto legible del tipo de autoridad
   */
  static getTipoAutoridadLabel(tipo: string): string {
    switch (tipo) {
      case TipoAutoridad.Policia:
        return 'Policía';
      case TipoAutoridad.Fiscal:
        return 'Fiscal';
      case TipoAutoridad.MedicoLegista:
        return 'Médico Legista';
      default:
        return tipo;
    }
  }

  /**
   * Obtiene el icono según tipo de autoridad
   */
  static getTipoAutoridadIcon(tipo: string): string {
    switch (tipo) {
      case TipoAutoridad.Policia:
        return 'shield';
      case TipoAutoridad.Fiscal:
        return 'scale';
      case TipoAutoridad.MedicoLegista:
        return 'stethoscope';
      default:
        return 'user';
    }
  }

  /**
   * Obtiene el color del badge según tipo de autoridad
   */
  static getTipoAutoridadColor(tipo: string): string {
    switch (tipo) {
      case TipoAutoridad.Policia:
        return 'bg-blue-100 text-blue-800';
      case TipoAutoridad.Fiscal:
        return 'bg-purple-100 text-purple-800';
      case TipoAutoridad.MedicoLegista:
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Formatea el tamaño de archivo en formato legible
   */
  static formatearTamanoArchivo(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Valida si el expediente puede ser marcado como listo (Vigilancia)
   */
  static puedeMarcarListo(estado: string): boolean {
    return estado === EstadoExpedienteLegal.EnRegistro;
  }

  /**
   * Valida si el expediente puede ser validado por admisión
   */
  static puedeValidarAdmision(estado: string): boolean {
    return estado === EstadoExpedienteLegal.PendienteValidacionAdmision;
  }

  /**
   * Valida si el expediente puede ser autorizado por jefe de guardia
   */
  static puedeAutorizarJefeGuardia(estado: string): boolean {
    return estado === EstadoExpedienteLegal.ValidadoAdmision;
  }

  /**
   * Obtiene el color de alerta según días restantes
   */
  static getColorAlertaDias(diasRestantes?: number): string {
    if (!diasRestantes) return 'text-gray-500';
    if (diasRestantes <= 1) return 'text-red-600';
    if (diasRestantes <= 3) return 'text-orange-600';
    return 'text-yellow-600';
  }

  /**
   * Valida extensión de archivo PDF
   */
  static esArchivoValido(nombreArchivo: string): boolean {
    const extension = nombreArchivo.split('.').pop()?.toLowerCase();
    return extension === 'pdf';
  }

  /**
   * Valida tamaño máximo de archivo (10MB)
   */
  static esTamanoValido(bytes: number): boolean {
    const maxBytes = 10 * 1024 * 1024; // 10MB
    return bytes <= maxBytes;
  }

  /**
   * Genera resumen de estado documentario
   */
  static getResumenDocumentario(expediente: ExpedienteLegalDTO): string {
    if (expediente.documentosCompletos) {
      return 'Documentación completa';
    }
    if (expediente.tienePendientes && expediente.diasRestantes !== undefined) {
      return `${expediente.diasRestantes} días restantes`;
    }
    return 'Documentación incompleta';
  }
}

// ========================================
// TIPOS DE WORKFLOW
// ========================================

export interface TimelineEstadoExpediente {
  estado: string;
  label: string;
  icono: string;
  color: string;
  activo: boolean;
  completado: boolean;
  fecha?: Date;
  usuario?: string;
  observaciones?: string;
}

/**
 * Genera la línea de tiempo de estados para el expediente legal
 */
export function generarTimelineExpedienteLegal(expediente: ExpedienteLegalDTO): TimelineEstadoExpediente[] {
  const estadoActual = expediente.estado;

  // 1. Definimos el flujo base (Camino Feliz)
  let estadosVisuales = [
    EstadoExpedienteLegal.EnRegistro,
    EstadoExpedienteLegal.PendienteValidacionAdmision,
    EstadoExpedienteLegal.ValidadoAdmision,
    EstadoExpedienteLegal.AutorizadoJefeGuardia
  ];

  // 2. Ajuste Dinámico: Si está RECHAZADO, reemplazamos el paso de validación visualmente
  if (estadoActual === EstadoExpedienteLegal.RechazadoAdmision) {
    estadosVisuales = [
      EstadoExpedienteLegal.EnRegistro,
      EstadoExpedienteLegal.PendienteValidacionAdmision,
      EstadoExpedienteLegal.RechazadoAdmision, // Reemplaza a Validado
      EstadoExpedienteLegal.AutorizadoJefeGuardia
    ];
  }

  // Encontrar en qué paso estamos
  const indexActual = estadosVisuales.indexOf(estadoActual as EstadoExpedienteLegal);

  return estadosVisuales.map((estado, index) => {
    let fecha: Date | undefined;
    let usuario: string | undefined;
    let observaciones: string | undefined;

    // 3. Asignar datos según el estado específico (Mapeo DTO Backend -> Timeline)
    switch (estado) {
      case EstadoExpedienteLegal.EnRegistro:
        fecha = expediente.fechaCreacion;
        usuario = expediente.usuarioRegistroNombre;
        break;

      case EstadoExpedienteLegal.PendienteValidacionAdmision:
        // Estado de espera: No tiene fecha/usuario propios aún, es un puente.
        break;

      case EstadoExpedienteLegal.ValidadoAdmision:
        fecha = expediente.fechaValidacionAdmision;
        usuario = expediente.usuarioAdmisionNombre;
        observaciones = expediente.observacionesAdmision;
        break;

      case EstadoExpedienteLegal.RechazadoAdmision:
        // Si fue rechazado, usamos los datos de validación (que contienen el rechazo)
        fecha = expediente.fechaValidacionAdmision;
        usuario = expediente.usuarioAdmisionNombre;
        observaciones = expediente.observacionesAdmision;
        break;

      case EstadoExpedienteLegal.AutorizadoJefeGuardia:
        fecha = expediente.fechaAutorizacion;
        usuario = expediente.jefeGuardiaNombre;
        observaciones = expediente.observacionesJefeGuardia;
        break;
    }

    return {
      estado,
      label: ExpedienteLegalHelper.getEstadoLabel(estado),
      icono: ExpedienteLegalHelper.getEstadoIcon(estado),
      color: ExpedienteLegalHelper.getEstadoColor(estado),
      activo: estado === estadoActual,
      // Está completado si su índice es menor al actual, 
      // O si es el estado actual y es un estado final/hito (como Autorizado)
      completado: index < indexActual || (estado === EstadoExpedienteLegal.AutorizadoJefeGuardia && estado === estadoActual),
      fecha,
      usuario,
      observaciones
    };
  });
}

/**
 * Interface para filtros de búsqueda
 */
export interface FiltrosExpedienteLegal {
  estado?: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  nombres?: string;
  numeroDocumento?: string;
  hc?: string;
  fechaDesde?: Date;
  fechaHasta?: Date;
  tieneDocumentosPendientes?: boolean;
  comisaria?: string;
  fiscalia?: string;
}

/**
 * Interface para respuesta paginada
 */
export interface ExpedienteLegalPagedResponse {
  items: ExpedienteLegalDTO[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
