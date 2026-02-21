import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

// Services
import { ExpedienteService, Expediente } from './expediente';

/**
 * Servicio centralizado para búsqueda de expedientes
 * Elimina código duplicado en componentes de deudas
 * 
 * Usado por:
 * - RegistrarDeudaEconomica
 * - GestionarExoneracion
 * - LiquidarDeudaEconomica
 * - RegistrarDeudaSangre
 * - LiquidarDeudaSangre
 */
@Injectable({
  providedIn: 'root'
})
export class BusquedaExpediente {
  private expedienteService = inject(ExpedienteService);

  /**
   * Busca un expediente por término y tipo
   * 
   * @param termino - Valor a buscar (HC, DNI o Código)
   * @param tipo - Tipo de búsqueda: 'HC' | 'DNI' | 'CODIGO'
   * @returns Observable con el expediente encontrado
   * @throws Error si no se encuentra o si el código no es numérico
   * 
   * @example
   * ```typescript
   * this.busquedaService.buscar('12345', 'HC').subscribe({
   *   next: (expediente) => console.log(expediente),
   *   error: (err) => console.error(err.message)
   * });
   * ```
   */
  buscar(termino: string, tipo: 'HC' | 'DNI' | 'CODIGO'): Observable<Expediente> {
    const terminoLimpio = this.normalizarTermino(termino);

    if (!terminoLimpio) {
      return throwError(() => new Error('Término de búsqueda vacío'));
    }

    // CASO 1: Búsqueda por CÓDIGO SGM
    if (tipo === 'CODIGO') {
      if (!/^SGM-\d{4}-\d{5}$/i.test(terminoLimpio)) {
        return throwError(() => new Error('Formato de código inválido. Use: SGM-YYYY-NNNNN'));
      }

      return this.expedienteService.buscarSimple({
        codigoExpediente: terminoLimpio.toUpperCase()
      }).pipe(
        catchError(err => {
          const mensaje = err.error?.message || `Expediente ${terminoLimpio} no encontrado`;
          return throwError(() => new Error(mensaje));
        })
      );
    }

    // CASO 2: Búsqueda por HC
    if (tipo === 'HC') {
      if (!this.esNumerico(terminoLimpio)) {
        return throwError(() => new Error('HC debe contener solo números'));
      }

      if (!this.validarHC(terminoLimpio)) {
        return throwError(() => new Error('HC debe tener entre 5 y 8 dígitos'));
      }

      return this.expedienteService.buscarSimple({ hc: terminoLimpio }).pipe(
        catchError(err => {
          const mensaje = err.error?.message || `Paciente con HC ${terminoLimpio} no encontrado`;
          return throwError(() => new Error(mensaje));
        })
      );
    }

    // CASO 3: Búsqueda por DNI
    if (tipo === 'DNI') {
      if (!this.esNumerico(terminoLimpio)) {
        return throwError(() => new Error('DNI debe contener solo números'));
      }

      if (!this.validarDNI(terminoLimpio)) {
        return throwError(() => new Error('DNI debe tener 8 dígitos'));
      }

      return this.expedienteService.buscarSimple({ dni: terminoLimpio }).pipe(
        catchError(err => {
          const mensaje = err.error?.message || `Paciente con DNI ${terminoLimpio} no encontrado`;
          return throwError(() => new Error(mensaje));
        })
      );
    }

    return throwError(() => new Error('Tipo de búsqueda no válido'));
  }

  /**
   * Maneja errores de búsqueda con mensajes específicos
   * @private
   */
  private manejarError(error: any, tipo: string): Observable<never> {
    let mensaje = 'Error al buscar expediente';

    // Manejo según código HTTP
    if (error.status === 404) {
      mensaje = tipo === 'CODIGO'
        ? 'No se encontró el expediente con ese código'
        : `No se encontró ningún paciente con ese ${tipo}`;
    } else if (error.status === 400) {
      mensaje = error.error?.message || 'Datos de búsqueda inválidos';
    } else if (error.status === 0) {
      mensaje = 'No hay conexión con el servidor. Verifique su internet.';
    } else if (error.message) {
      mensaje = error.message;
    }

    console.error('[BusquedaExpediente] Error:', {
      tipo,
      status: error.status,
      mensaje,
      error
    });

    return throwError(() => new Error(mensaje));
  }

  /**
   * Valida si un término es numérico
   * Útil para validaciones en componentes
   */
  esNumerico(termino: string): boolean {
    return !isNaN(Number(termino.trim()));
  }

  /**
   * Limpia y normaliza un término de búsqueda
   * Elimina espacios, caracteres especiales, etc.
   */
  normalizarTermino(termino: string): string {
    return termino
      .trim()
      .toUpperCase()
      .replace(/[^\w\s-]/g, ''); // Elimina caracteres especiales excepto guiones
  }

  /**
   * Valida formato de DNI 
   */
  validarDNI(dni: string): boolean {
    const dniLimpio = dni.trim();
    return /^\d{8}$/.test(dniLimpio);
  }

  /**
   * Valida formato de HC (Historia Clínica)
   * Puede ser numérico de 5-8 dígitos
   */
  validarHC(hc: string): boolean {
    const hcLimpio = hc.trim();
    return /^\d{5,8}$/.test(hcLimpio);
  }
}
