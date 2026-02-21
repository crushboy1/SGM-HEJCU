/**
 * Modelos TypeScript para Deuda Económica
 * Mapea exactamente los DTOs del backend DeudaEconomicaDTO.cs
 * 
 * NOTA: Sup. Vigilancia NO ve montos, solo semáforo DEBE/NO DEBE
 */

// ========================================
// INTERFACES PRINCIPALES
// ========================================

export interface DeudaEconomicaDTO {
  deudaEconomicaID: number;
  expedienteID: number;
  codigoExpediente: string;
  estado: string; // "SinDeuda" | "Liquidado" | "Exonerado" | "Pendiente"

  // Montos (NO mostrar a Sup. Vigilancia)
  montoDeuda: number;
  montoExonerado: number;
  montoPagado: number;
  montoPendiente: number;

  // Pago en Caja
  numeroBoleta?: string;
  fechaPago?: Date;
  observacionesPago?: string;

  // Exoneración Servicio Social
  tipoExoneracion: string; // "SinExoneracion" | "Parcial" | "Total"
  numeroBoletaExoneracion?: string;
  fechaExoneracion?: Date;
  observacionesExoneracion?: string;
  porcentajeExoneracion: number;
  rutaPDFSustento?: string;
  nombreArchivoSustento?: string;
  tamañoArchivoSustento?: number;
  tamañoArchivoLegible?: string;
  asistentaSocialID?: number;
  asistentaSocialNombre?: string;

  // Auditoría
  usuarioRegistroID: number;
  usuarioRegistroNombre: string;
  fechaRegistro: Date;
  usuarioActualizacionID?: number;
  usuarioActualizacionNombre?: string;
  fechaActualizacion?: Date;

  // Métodos calculados
  bloqueaRetiro: boolean;
  semaforoSupVigilancia: string; // "DEBE" | "NO DEBE"
  resumenDetallado: string;
  validacionSustento: string;
}

/**
 * DTO simplificado para Sup. Vigilancia
 * Solo muestra semáforo sin montos
 */
export interface DeudaEconomicaSemaforoDTO {
  expedienteID: number;
  codigoExpediente: string;
  tieneDeuda: boolean;
  semaforo: string; // "DEBE" | "NO DEBE"
  instruccion: string;
}

export interface CreateDeudaEconomicaDTO {
  expedienteID: number;
  montoDeuda: number;
  usuarioRegistroID: number;
}

export interface LiquidarDeudaEconomicaDTO {
  numeroBoleta: string;
  montoPagado: number;
  usuarioActualizacionID: number;
  observaciones?: string;
}

/**
 * DTO para aplicar exoneración (Servicio Social)
 */
export interface AplicarExoneracionDTO {
  expedienteID: number;
  montoExonerado: number;
  tipoExoneracion: string; // "Parcial" | "Total"
  observaciones: string;
  numeroBoletaExoneracion?: string;
  asistentaSocialID: number;
  // Archivo PDF
  rutaPDFSustento?: string;
  nombreArchivoSustento?: string;
  tamañoArchivoSustento?: number;
}

export interface HistorialDeudaEconomicaDTO {
  fechaHora: Date;
  accion: string;
  usuarioNombre: string;
  detalle: string;
  ipOrigen?: string;
}

/**
 * DTO de estadísticas generales
 */
export interface EstadisticasDeudaEconomicaDTO {
  totalDeudas: number;
  deudasPendientes: number;
  deudasLiquidadas: number;
  deudasExoneradas: number;
  montoTotalDeudas: number;
  montoTotalExonerado: number;
  montoTotalPagado: number;
  montoTotalPendiente: number;
  promedioExoneracion: number;
}

// ========================================
// ENUMS
// ========================================

export enum EstadoDeudaEconomica {
  SinDeuda = 'SinDeuda',
  Liquidado = 'Liquidado',
  Exonerado = 'Exonerado',
  Pendiente = 'Pendiente'
}

export enum TipoExoneracion {
  SinExoneracion = 'SinExoneracion',
  Parcial = 'Parcial',
  Total = 'Total'
}

// ========================================
// HELPERS Y UTILIDADES
// ========================================

export class DeudaEconomicaHelper {
  /**
   * Obtiene el color del semáforo según el estado
   */
  static getSemaforoColor(estado: string): string {
    switch (estado) {
      case EstadoDeudaEconomica.SinDeuda:
        return 'text-green-600';
      case EstadoDeudaEconomica.Liquidado:
        return 'text-blue-600';
      case EstadoDeudaEconomica.Exonerado:
        return 'text-purple-600';
      case EstadoDeudaEconomica.Pendiente:
        return 'text-red-600';
      default:
        return 'text-gray-400';
    }
  }

  /**
   * Obtiene el icono del semáforo según el estado
   */
  static getSemaforoIcon(estado: string): string {
    switch (estado) {
      case EstadoDeudaEconomica.SinDeuda:
        return 'circle-check';
      case EstadoDeudaEconomica.Liquidado:
        return 'credit-card';
      case EstadoDeudaEconomica.Exonerado:
        return 'file-badge';
      case EstadoDeudaEconomica.Pendiente:
        return 'alert-circle';
      default:
        return 'help-circle';
    }
  }

  /**
   * Obtiene el texto legible del estado
   */
  static getEstadoLabel(estado: string): string {
    switch (estado) {
      case EstadoDeudaEconomica.SinDeuda:
        return 'Sin Deuda';
      case EstadoDeudaEconomica.Liquidado:
        return 'Pagado';
      case EstadoDeudaEconomica.Exonerado:
        return 'Exonerado';
      case EstadoDeudaEconomica.Pendiente:
        return 'Pendiente de Pago';
      default:
        return estado;
    }
  }

  /**
   * Obtiene el texto legible del tipo de exoneración
   */
  static getTipoExoneracionLabel(tipo: string): string {
    switch (tipo) {
      case TipoExoneracion.SinExoneracion:
        return 'Sin Exoneración';
      case TipoExoneracion.Parcial:
        return 'Exoneración Parcial';
      case TipoExoneracion.Total:
        return 'Exoneración Total';
      default:
        return tipo;
    }
  }

  /**
   * Verifica si el estado bloquea el retiro
   */
  static bloqueaRetiro(estado: string): boolean {
    return estado === EstadoDeudaEconomica.Pendiente;
  }

  /**
   * Formatea un monto a moneda peruana
   */
  static formatearMonto(monto: number): string {
    return new Intl.NumberFormat('es-PE', {
      style: 'currency',
      currency: 'PEN',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(monto);
  }

  /**
   * Calcula el monto pendiente
   */
  static calcularMontoPendiente(montoDeuda: number, montoExonerado: number, montoPagado: number): number {
    return Math.max(0, montoDeuda - montoExonerado - montoPagado);
  }

  /**
   * Calcula el porcentaje de exoneración
   */
  static calcularPorcentajeExoneracion(montoDeuda: number, montoExonerado: number): number {
    if (montoDeuda <= 0) return 0;
    return Math.round((montoExonerado / montoDeuda) * 100);
  }

  /**
   * Valida si un monto de exoneración es válido
   */
  static validarMontoExoneracion(montoDeuda: number, montoExonerado: number, montoPagado: number): {
    valido: boolean;
    error?: string;
  } {
    if (montoExonerado < 0) {
      return { valido: false, error: 'El monto exonerado no puede ser negativo' };
    }

    if (montoExonerado > montoDeuda) {
      return { valido: false, error: 'El monto exonerado no puede ser mayor a la deuda total' };
    }

    const montoDisponible = montoDeuda - montoPagado;
    if (montoExonerado > montoDisponible) {
      return {
        valido: false,
        error: `Solo se puede exonerar hasta ${this.formatearMonto(montoDisponible)} (ya se pagó ${this.formatearMonto(montoPagado)})`
      };
    }

    return { valido: true };
  }

  /**
   * Formatea tamaño de archivo
   */
  static formatearTamanoArchivo(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
