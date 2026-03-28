import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  ExpedienteVigilanciaDTO,
  DetalleVigilanciaDTO
} from '../models/notificacion.model';

/**
 * Service de consulta para el módulo Supervisor de Vigilancia.
 * Solo lectura — consulta expedientes con semáforo precalculado.
 *
 * Endpoints:
 *   GET /api/VigilanteSupervisor/expedientes?busqueda=xxx
 *   GET /api/VigilanteSupervisor/expedientes/{id}/detalle
 */
@Injectable({ providedIn: 'root' })
export class VigilanciaExpediente {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/VigilanteSupervisor';

  private get headers(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('sgm_token')}`
    });
  }

  /**
   * Obtiene expedientes con semáforo precalculado.
   * @param busqueda Texto libre — busca en HC, DNI y Nombre. null = todos.
   */
  obtenerExpedientes(busqueda?: string): Observable<ExpedienteVigilanciaDTO[]> {
    let params = new HttpParams();
    if (busqueda?.trim()) params = params.set('busqueda', busqueda.trim());

    return this.http.get<ExpedienteVigilanciaDTO[]>(
      `${this.apiUrl}/expedientes`,
      { headers: this.headers, params }
    ).pipe(
      map(data => data.map(e => this.mapearFechas(e))),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene detalle completo para el modal Ver.
   * Incluye semáforo expandido, responsable de retiro y Jefe de Guardia.
   */
  obtenerDetalle(expedienteId: number): Observable<DetalleVigilanciaDTO> {
    return this.http.get<DetalleVigilanciaDTO>(
      `${this.apiUrl}/expedientes/${expedienteId}/detalle`,
      { headers: this.headers }
    ).pipe(
      map(d => this.mapearFechasDetalle(d)),
      catchError(this.handleError)
    );
  }

  // ── Helpers ────────────────────────────────────────────────────

  private mapearFechas(e: ExpedienteVigilanciaDTO): ExpedienteVigilanciaDTO {
    return {
      ...e,
      fechaHoraFallecimiento: e.fechaHoraFallecimiento
        ? new Date(e.fechaHoraFallecimiento) : e.fechaHoraFallecimiento,
      fechaIngresoBandeja: e.fechaIngresoBandeja
        ? new Date(e.fechaIngresoBandeja) : undefined,
    };
  }

  private mapearFechasDetalle(d: DetalleVigilanciaDTO): DetalleVigilanciaDTO {
    return {
      ...this.mapearFechas(d),
      fechaNacimiento: d.fechaNacimiento
        ? new Date(d.fechaNacimiento) : d.fechaNacimiento,
    } as DetalleVigilanciaDTO;
  }

  private handleError(error: any): Observable<never> {
    const msg = error.error?.message || error.error?.title ||
      error.message || `Error ${error.status}`;
    console.error('[VigilanciaExpediente]', msg, error);
    return throwError(() => new Error(msg));
  }
}
