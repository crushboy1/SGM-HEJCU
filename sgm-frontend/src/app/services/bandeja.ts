import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  BandejaDTO,
  BandejaDisponibleDTO,
  AsignarBandejaDTO,
  EstadisticasBandejaDTO,
  LiberarBandejaManualDTO
} from '../models/notificacion.model';

/**
 * BandejaService
 * 
 * Gestiona todas las operaciones relacionadas con Bandejas del Mortuorio.
 * 
 * Endpoints disponibles:
 * - GET    /api/Bandejas/dashboard                     - Obtener todas las bandejas (mapa)
 * - GET    /api/Bandejas/{id}                          - Obtener bandeja por ID
 * - GET    /api/Bandejas/disponibles                   - Lista bandejas disponibles (dropdown)
 * - GET    /api/Bandejas/estadisticas                  - Estadísticas de ocupación
 * - POST   /api/Bandejas/asignar                       - Asignar expediente a bandeja
 * - PUT    /api/Bandejas/{id}/mantenimiento/iniciar    - Poner en mantenimiento
 * - PUT    /api/Bandejas/{id}/mantenimiento/finalizar  - Finalizar mantenimiento
 * * PUT /api/Bandejas/{id}/liberar-manualmente         - Liberar manualmente una bandeja
 * NOTA: BandejaDTO y EstadisticasBandejaDTO están definidos en notificacion.model.ts
 *       porque también se usan en notificaciones SignalR en tiempo real.
 */
@Injectable({
  providedIn: 'root'
})
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

  // ========================================
  // CONSULTAS
  // ========================================

  /**
   * Obtiene todas las bandejas con su estado actual (mapa mortuorio)
   * GET /api/Bandejas/dashboard
   * Roles: Todos autenticados
   */
  getDashboard(): Observable<BandejaDTO[]> {
    return this.http.get<BandejaDTO[]>(
      `${this.apiUrl}/dashboard`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(b => this.mapearFechas(b))),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene los detalles de una bandeja específica
   * GET /api/Bandejas/{id}
   * Roles: Todos autenticados
   */
  getById(bandejaId: number): Observable<BandejaDTO> {
    return this.http.get<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene lista de bandejas disponibles (para dropdown en asignación)
   * GET /api/Bandejas/disponibles
   * Roles: Ambulancia, Administrador
   */
  getDisponibles(): Observable<BandejaDisponibleDTO[]> {
    return this.http.get<BandejaDisponibleDTO[]>(
      `${this.apiUrl}/disponibles`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene estadísticas generales del mortuorio
   * GET /api/Bandejas/estadisticas
   * Roles: Administrador, JefeGuardia, VigilanteSupervisor, Enfermería
   */
  getEstadisticas(): Observable<EstadisticasBandejaDTO> {
    return this.http.get<EstadisticasBandejaDTO>(
      `${this.apiUrl}/estadisticas`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  // ========================================
  // OPERACIONES
  // ========================================

  /**
   * Asigna un expediente a una bandeja disponible
   * POST /api/Bandejas/asignar
   * Roles: Ambulancia, Administrador
   */
  asignar(dto: AsignarBandejaDTO): Observable<BandejaDTO> {
    return this.http.post<BandejaDTO>(
      `${this.apiUrl}/asignar`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Pone una bandeja en estado de Mantenimiento
   * PUT /api/Bandejas/{id}/mantenimiento/iniciar
   * Roles: Administrador, JefeGuardia, VigilanteSupervisor
   */
  marcarMantenimiento(bandejaId: number, observaciones: string): Observable<BandejaDTO> {
    // Backend espera [FromBody] string, por eso usamos comillas dobles escapadas
    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/mantenimiento/iniciar`,
      `"${observaciones}"`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Finaliza el mantenimiento de una bandeja (la pone Disponible)
   * PUT /api/Bandejas/{id}/mantenimiento/finalizar
   * Roles: Administrador, JefeGuardia, VigilanteSupervisor
   */
  finalizarMantenimiento(bandejaId: number): Observable<BandejaDTO> {
    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/mantenimiento/finalizar`,
      {},
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }
  /**
 * Libera manualmente una bandeja ocupada (emergencia/corrección)
 * PUT /api/Bandejas/{id}/liberar-manualmente
 * Roles: Administrador, JefeGuardia, VigilanteSupervisor
 */
  liberarManualmente(
    bandejaId: number,
    motivoLiberacion: string,
    observaciones: string
  ): Observable<BandejaDTO> {
    const dto: LiberarBandejaManualDTO = {
      bandejaID: bandejaId,
      motivoLiberacion,
      observaciones,
      usuarioLiberaID: 0 // Backend lo sobrescribe con usuario autenticado
    };

    return this.http.put<BandejaDTO>(
      `${this.apiUrl}/${bandejaId}/liberar-manualmente`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }
  // ========================================
  // HELPERS PRIVADOS
  // ========================================

  /**
   * Mapea las fechas de string a Date
   */
  private mapearFechas(bandeja: BandejaDTO): BandejaDTO {
    return {
      ...bandeja,
      fechaHoraAsignacion: bandeja.fechaHoraAsignacion
        ? new Date(bandeja.fechaHoraAsignacion)
        : undefined,
      fechaHoraLiberacion: bandeja.fechaHoraLiberacion
        ? new Date(bandeja.fechaHoraLiberacion)
        : undefined
    };
  }

  /**
   * Manejo centralizado de errores
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'Error desconocido';

    if (error.error instanceof ErrorEvent) {
      // Error del lado del cliente
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Error del lado del servidor
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.error?.title) {
        errorMessage = error.error.title;
      } else if (error.message) {
        errorMessage = error.message;
      } else {
        errorMessage = `Error ${error.status}: ${error.statusText}`;
      }
    }

    console.error('[BandejaService] Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }

  // ========================================
  // UTILIDADES
  // ========================================

  /**
   * Obtiene el color del badge según el estado de la bandeja
   */
  getEstadoColor(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible':
        return 'bg-green-100 text-green-800 border-green-300';
      case 'ocupada':
        return 'bg-red-100 text-red-800 border-red-300';
      case 'mantenimiento':
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case 'fueradeservicio':
        return 'bg-gray-100 text-gray-800 border-gray-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  /**
   * Obtiene el icono según el estado de la bandeja
   */
  getEstadoIcon(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible':
        return 'archive';
      case 'ocupada':
        return 'alert-circle';
      case 'mantenimiento':
        return 'wrench';
      case 'fueradeservicio':
        return 'ban';
      default:
        return 'help-circle';
    }
  }

  /**
   * Obtiene el texto legible del estado
   */
  getEstadoLabel(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'disponible':
        return 'Disponible';
      case 'ocupada':
        return 'Ocupada';
      case 'mantenimiento':
        return 'En Mantenimiento';
      case 'fueradeservicio':
        return 'Fuera de Servicio';
      default:
        return estado;
    }
  }

  /**
   * Calcula el porcentaje de ocupación
   */
  calcularPorcentajeOcupacion(ocupadas: number, total: number): number {
    if (total === 0) return 0;
    return Math.round((ocupadas / total) * 100);
  }

  /**
   * Verifica si hay alerta de ocupación crítica (>70%)
   */
  tieneAlertaOcupacion(porcentaje: number): boolean {
    return porcentaje > 70;
  }

  /**
   * Formatea el tiempo ocupada en formato legible
   * Ejemplo: "2d 5h" o "3h 30m"
   */
  formatearTiempoOcupada(tiempoString?: string): string {
    if (!tiempoString) return '-';
    return tiempoString;
  }

  /**
   * Valida si el usuario puede asignar bandejas
   */
  puedeAsignarBandeja(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('Ambulancia') || roles?.includes('Administrador') || false;
  }

  /**
   * Valida si el usuario puede gestionar mantenimiento
   */
  puedeGestionarMantenimiento(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('Administrador') ||
      roles?.includes('JefeGuardia') ||
      roles?.includes('VigilanteSupervisor') ||
      false;
  }
}
