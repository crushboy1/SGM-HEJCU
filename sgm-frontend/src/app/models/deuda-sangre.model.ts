/**
 * Modelos TypeScript para Deuda de Sangre
 * Mapea exactamente los DTOs del backend DeudaSangreDTO.cs
 */

// ========================================
// INTERFACES PRINCIPALES
// ========================================

export interface DeudaSangreDTO {
  deudaSangreID: number;
  expedienteID: number;
  codigoExpediente: string;
  estado: string; // "SinDeuda" | "Liquidado" | "Anulado" | "Pendiente"
  cantidadUnidades: number;
  tipoSangre?: string;
  nombreFamiliarCompromiso?: string;
  dniFamiliarCompromiso?: string;
  fechaLiquidacion?: Date;
  rutaPDFCompromiso?: string;
  anuladaPorMedico: boolean;
  medicoAnulaID?: number;
  fechaAnulacion?: Date;
  justificacionAnulacion?: string;
  usuarioRegistroID: number;
  usuarioRegistroNombre: string;
  fechaRegistro: Date;
  bloqueaRetiro: boolean;
  semaforo: string; // "SIN DEUDA" | "PENDIENTE (X unidades)" | "LIQUIDADO" | "ANULADO POR MEDICO"
}

export interface CreateDeudaSangreDTO {
  expedienteID: number;
  cantidadUnidades: number;
  tipoSangre?: string;
  usuarioRegistroID: number;
}

export interface LiquidarDeudaSangreDTO {
  nombreFamiliarCompromiso: string;
  dniFamiliarCompromiso: string;
  rutaPDFCompromiso: string;
  usuarioActualizacionID: number;
  observaciones?: string;
}

export interface AnularDeudaSangreDTO {
  expedienteID: number;
  medicoAnulaID: number;
  justificacionAnulacion: string;
}

export interface HistorialDeudaSangreDTO {
  fechaHora: Date;
  accion: string;
  usuarioNombre: string;
  detalle: string;
  ipOrigen?: string;
}

// ========================================
// ENUMS
// ========================================

export enum EstadoDeudaSangre {
  SinDeuda = 'SinDeuda',
  Liquidado = 'Liquidado',
  Anulado = 'Anulado',
  Pendiente = 'Pendiente'
}

export enum TipoSangre {
  OPositivo = 'O+',
  ONegativo = 'O-',
  APositivo = 'A+',
  ANegativo = 'A-',
  BPositivo = 'B+',
  BNegativo = 'B-',
  ABPositivo = 'AB+',
  ABNegativo = 'AB-'
}

// ========================================
// HELPERS Y UTILIDADES
// ========================================

export class DeudaSangreHelper {
  /**
   * Obtiene el color del semáforo según el estado
   */
  static getSemaforoColor(estado: string): string {
    switch (estado) {
      case EstadoDeudaSangre.SinDeuda:
        return 'text-green-600';
      case EstadoDeudaSangre.Liquidado:
        return 'text-blue-600';
      case EstadoDeudaSangre.Anulado:
        return 'text-gray-600';
      case EstadoDeudaSangre.Pendiente:
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
      case EstadoDeudaSangre.SinDeuda:
        return 'circle-check';
      case EstadoDeudaSangre.Liquidado:
        return 'file-check';
      case EstadoDeudaSangre.Anulado:
        return 'ban';
      case EstadoDeudaSangre.Pendiente:
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
      case EstadoDeudaSangre.SinDeuda:
        return 'Sin Deuda';
      case EstadoDeudaSangre.Liquidado:
        return 'Compromiso Firmado';
      case EstadoDeudaSangre.Anulado:
        return 'Anulada por Médico';
      case EstadoDeudaSangre.Pendiente:
        return 'Pendiente';
      default:
        return estado;
    }
  }

  /**
   * Verifica si el estado bloquea el retiro
   */
  static bloqueaRetiro(estado: string): boolean {
    return estado === EstadoDeudaSangre.Pendiente;
  }
}
