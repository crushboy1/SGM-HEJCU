import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  DeudaSangreDTO,
  CreateDeudaSangreDTO,
  LiquidarDeudaSangreDTO,
  AnularDeudaSangreDTO,
  HistorialDeudaSangreDTO
} from '../models/deuda-sangre.model';

/**
 * DeudaSangreService
 * 
 * Gestiona todas las operaciones relacionadas con Deudas de Sangre.
 * 
 * Endpoints disponibles:
 * - POST   /api/Deudas/sangre                           - Registrar deuda
 * - GET    /api/Deudas/sangre/expediente/{id}          - Obtener por expediente
 * - PUT    /api/Deudas/sangre/expediente/{id}/sin-deuda - Marcar sin deuda
 * - PUT    /api/Deudas/sangre/expediente/{id}/liquidar  - Liquidar (compromiso)
 * - PUT    /api/Deudas/sangre/expediente/{id}/anular    - Anular (médico)
 * - GET    /api/Deudas/sangre/expediente/{id}/bloquea-retiro - Verificar bloqueo
 * - GET    /api/Deudas/sangre/expediente/{id}/semaforo  - Obtener semáforo
 * - GET    /api/Deudas/sangre/pendientes                - Lista pendientes
 * - GET    /api/Deudas/sangre/expediente/{id}/historial - Historial
 */
export interface GenerarCompromisoDTO {
  expedienteID: number;
  nombrePaciente: string;
  nombreFamiliar: string;
  dniFamiliar: string;
  cantidadUnidades: number;
}
@Injectable({
  providedIn: 'root'
})
export class DeudaSangre {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/Deudas/sangre';

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('sgm_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // ========================================
  // CRUD BÁSICO
  // ========================================

  /**
   * Registra una nueva deuda de sangre
   * POST /api/Deudas/sangre
   * Roles: BancoSangre, Administrador
   */
  registrar(dto: CreateDeudaSangreDTO): Observable<DeudaSangreDTO> {
    return this.http.post<DeudaSangreDTO>(
      this.apiUrl,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene la deuda de sangre de un expediente
   * GET /api/Deudas/sangre/expediente/{id}
   * Roles: Todos autenticados
   */
  obtenerPorExpediente(expedienteId: number): Observable<DeudaSangreDTO | null> {
    return this.http.get<DeudaSangreDTO>(
      `${this.apiUrl}/expediente/${expedienteId}`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response ? this.mapearFechas(response) : null),
      catchError(error => {
        if (error.status === 404) {
          return [null];
        }
        return this.handleError(error);
      })
    );
  }

  // ========================================
  // OPERACIONES DE ESTADO
  // ========================================

  /**
   * Marca como sin deuda (paciente no usó sangre)
   * PUT /api/Deudas/sangre/expediente/{id}/sin-deuda
   * Roles: BancoSangre, Administrador
   */
  marcarSinDeuda(expedienteId: number): Observable<DeudaSangreDTO> {
    return this.http.put<DeudaSangreDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/sin-deuda`,
      {},  //  Body vacío
      { headers: this.getHeaders() }
    )
  }

  /**
   * Liquida la deuda (familiar firma compromiso de reposición)
   * PUT /api/Deudas/sangre/expediente/{id}/liquidar
   * Roles: VigilanteSupervisor, BancoSangre, Administrador
   */
  liquidar(expedienteId: number, dto: LiquidarDeudaSangreDTO): Observable<DeudaSangreDTO> {
    return this.http.put<DeudaSangreDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/liquidar`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Anula la deuda por decisión médica (caso excepcional)
   * PUT /api/Deudas/sangre/expediente/{id}/anular
   * Roles: BancoSangre, Administrador
   */
  anular(expedienteId: number, dto: AnularDeudaSangreDTO): Observable<DeudaSangreDTO> {
    return this.http.put<DeudaSangreDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/anular`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  // ========================================
  // CONSULTAS Y VALIDACIONES
  // ========================================

  /**
   * Verifica si la deuda bloquea el retiro del cuerpo
   * GET /api/Deudas/sangre/expediente/{id}/bloquea-retiro
   * Roles: Todos autenticados
   */
  bloqueaRetiro(expedienteId: number): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/expediente/${expedienteId}/bloquea-retiro`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
 * Obtiene el semáforo visual de la deuda
 * GET /api/Deudas/sangre/expediente/{id}/semaforo
 * Roles: Todos autenticados
 */
  obtenerSemaforo(expedienteId: number): Observable<string> {
    return this.http.get<{ semaforo: string }>(
      `${this.apiUrl}/expediente/${expedienteId}/semaforo`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.semaforo),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene lista de deudas pendientes
   * GET /api/Deudas/sangre/pendientes
   * Roles: BancoSangre, VigilanteSupervisor, Administrador
   */
  obtenerPendientes(): Observable<DeudaSangreDTO[]> {
    return this.http.get<DeudaSangreDTO[]>(
      `${this.apiUrl}/pendientes`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(d => this.mapearFechas(d))),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene el historial de cambios de una deuda
   * GET /api/Deudas/sangre/expediente/{id}/historial
   * Roles: BancoSangre, Administrador
   */
  obtenerHistorial(expedienteId: number): Observable<HistorialDeudaSangreDTO[]> {
    return this.http.get<HistorialDeudaSangreDTO[]>(
      `${this.apiUrl}/expediente/${expedienteId}/historial`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(h => ({
        ...h,
        fechaHora: new Date(h.fechaHora)
      }))),
      catchError(this.handleError)
    );
  }

  // ========================================
  // HELPERS PRIVADOS
  // ========================================

  /**
   * Mapea las fechas de string a Date
   */
  private mapearFechas(dto: DeudaSangreDTO): DeudaSangreDTO {
    return {
      ...dto,
      fechaRegistro: new Date(dto.fechaRegistro),
      fechaLiquidacion: dto.fechaLiquidacion ? new Date(dto.fechaLiquidacion) : undefined,
      fechaAnulacion: dto.fechaAnulacion ? new Date(dto.fechaAnulacion) : undefined
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

    console.error('[DeudaSangreService] Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }

  // ========================================
  // UTILIDADES
  // ========================================

  /**
   * Valida si el usuario tiene rol de Banco de Sangre
   */
  esBancoSangre(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('BancoSangre') || roles?.includes('Administrador') || false;
  }

  /**
   * Valida si el usuario puede liquidar deudas
   */
  puedeVerificarSupervisor(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('VigilanteSupervisor') ||
      roles?.includes('BancoSangre') ||
      roles?.includes('Administrador') ||
      false;
  }
  generarCompromisoPDF(dto: GenerarCompromisoDTO): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/generar-compromiso`,
      dto,
      { headers: this.getHeaders(), responseType: 'blob' }
    ).pipe(
      catchError(this.handleError)
    );
  }

  uploadCompromiso(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('sgm_token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    return this.http.post<{ rutaArchivo: string }>(
      `${this.apiUrl}/upload-compromiso`,
      formData,
      { headers }
    ).pipe(
      map(response => response.rutaArchivo),
      catchError(this.handleError)
    );
  }
}
