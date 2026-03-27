import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  BandejaDTO,
  BandejaDisponibleDTO,
  AsignarBandejaDTO,
  EstadisticasBandejaDTO,
  LiberarBandejaManualDTO,
  IniciarMantenimientoDTO
} from '../models/notificacion.model';

/**
 * BandejaService
 *
 * CAMBIOS v2 (Fase 5 — Modal Mantenimiento):
 * - marcarMantenimiento(): envía IniciarMantenimientoDTO completo en lugar de string
 * - IniciarMantenimientoDTO importado desde notificacion.model.ts
 */
@Injectable({ providedIn: 'root' })
export class BandejaService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/Bandejas';

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('sgm_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // ── Consultas ────────────────────────────────────────────────────

  getDashboard(): Observable<BandejaDTO[]> {
    return this.http.get<BandejaDTO[]>(
      `${this.apiUrl}/dashboard`, { headers: this.getHeaders() }
    ).pipe(
      map(r => r.map(b => this.mapearFechas(b))),
      catchError(this.handleError)
    );
  }

  getById(bandejaId: number): Observable<BandejaDTO> {
    return this.http.get<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}`, { headers: this.getHeaders() }
    ).pipe(
      map(r => this.mapearFechas(r)),
      catchError(this.handleError)
    );
  }

  getDisponibles(): Observable<BandejaDisponibleDTO[]> {
    return this.http.get<BandejaDisponibleDTO[]>(
      `${this.apiUrl}/disponibles`, { headers: this.getHeaders() }
    ).pipe(catchError(this.handleError));
  }

  getEstadisticas(): Observable<EstadisticasBandejaDTO> {
    return this.http.get<EstadisticasBandejaDTO>(
      `${this.apiUrl}/estadisticas`, { headers: this.getHeaders() }
    ).pipe(catchError(this.handleError));
  }

  // ── Operaciones ───────────────────────────────────────────────────

  asignar(dto: AsignarBandejaDTO): Observable<BandejaDTO> {
    return this.http.post<BandejaDTO>(
      `${this.apiUrl}/asignar`, dto, { headers: this.getHeaders() }
    ).pipe(
      map(r => this.mapearFechas(r)),
      catchError(this.handleError)
    );
  }

  /**
   * Pone una bandeja en mantenimiento con datos completos del modal.
   * CAMBIOS v2: acepta IniciarMantenimientoDTO en lugar de string.
   * PUT /api/Bandejas/{id}/mantenimiento/iniciar
   */
  marcarMantenimiento(bandejaId: number, dto: IniciarMantenimientoDTO): Observable<BandejaDTO> {
    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/mantenimiento/iniciar`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(r => this.mapearFechas(r)),
      catchError(this.handleError)
    );
  }

  finalizarMantenimiento(bandejaId: number): Observable<BandejaDTO> {
    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/mantenimiento/finalizar`,
      {},
      { headers: this.getHeaders() }
    ).pipe(
      map(r => this.mapearFechas(r)),
      catchError(this.handleError)
    );
  }

  liberarManualmente(
    bandejaId: number,
    motivoLiberacion: string,
    observaciones: string
  ): Observable<BandejaDTO> {
    const dto: LiberarBandejaManualDTO = {
      bandejaID: bandejaId,
      motivoLiberacion,
      observaciones,
      usuarioLiberaID: 0 // backend sobrescribe con usuario autenticado
    };
    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/liberar-manualmente`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(r => this.mapearFechas(r)),
      catchError(this.handleError)
    );
  }

  // ── Utilidades ─────────────────────────────────────────────────────

  getEstadoColor(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible': return 'bg-green-100 text-green-800 border-green-300';
      case 'ocupada': return 'bg-red-100 text-red-800 border-red-300';
      case 'mantenimiento': return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      default: return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  getEstadoIcon(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible': return 'archive';
      case 'ocupada': return 'alert-circle';
      case 'mantenimiento': return 'wrench';
      case 'fueradeservicio': return 'ban';
      default: return 'help-circle';
    }
  }

  getEstadoLabel(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible': return 'Disponible';
      case 'ocupada': return 'Ocupada';
      case 'mantenimiento': return 'En Mantenimiento';
      case 'fueradeservicio': return 'Fuera de Servicio';
      default: return estado;
    }
  }

  formatearTiempoOcupada(tiempoString?: string): string {
    if (!tiempoString) return '—';
    return tiempoString; // Ya viene formateado del backend (Xd Yh Zm)
  }

  puedeAsignarBandeja(): boolean {
    const rol = localStorage.getItem('sgm_role');
    return rol === 'Ambulancia' || rol === 'Administrador';
  }

  puedeGestionarMantenimiento(): boolean {
    const rol = localStorage.getItem('sgm_role');
    return rol === 'Administrador' ||
      rol === 'JefeGuardia' ||
      rol === 'VigilanteSupervisor';
  }

  // ── Privados ──────────────────────────────────────────────────────

  private mapearFechas(b: BandejaDTO): BandejaDTO {
    return {
      ...b,
      fechaHoraAsignacion: b.fechaHoraAsignacion
        ? new Date(b.fechaHoraAsignacion) : undefined,
      fechaHoraLiberacion: b.fechaHoraLiberacion
        ? new Date(b.fechaHoraLiberacion) : undefined,
      fechaInicioMantenimiento: b.fechaInicioMantenimiento
        ? new Date(b.fechaInicioMantenimiento) : undefined,
      fechaEstimadaFinMantenimiento: b.fechaEstimadaFinMantenimiento
        ? new Date(b.fechaEstimadaFinMantenimiento) : undefined,
    };
  }

  private handleError(error: any): Observable<never> {
    const msg = error.error?.message || error.error?.title ||
      error.message || `Error ${error.status}`;
    console.error('[BandejaService] Error:', msg, error);
    return throwError(() => new Error(msg));
  }
}
