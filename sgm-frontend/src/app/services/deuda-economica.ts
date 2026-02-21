import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import {
  DeudaEconomicaDTO,
  DeudaEconomicaSemaforoDTO,
  CreateDeudaEconomicaDTO,
  LiquidarDeudaEconomicaDTO,
  AplicarExoneracionDTO,
  HistorialDeudaEconomicaDTO,
  EstadisticasDeudaEconomicaDTO
} from '../models/deuda-economica.model';

/**
 * DeudaEconomicaService
 * Gestiona todas las operaciones relacionadas con Deudas Económicas
 */
@Injectable({
  providedIn: 'root'
})
export class DeudaEconomica {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/Deudas/economica';

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('sgm_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  registrar(dto: CreateDeudaEconomicaDTO): Observable<DeudaEconomicaDTO> {
    return this.http.post<DeudaEconomicaDTO>(
      this.apiUrl,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  obtenerPorExpediente(expedienteId: number): Observable<DeudaEconomicaDTO | null> {
    return this.http.get<DeudaEconomicaDTO>(
      `${this.apiUrl}/expediente/${expedienteId}`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response ? this.mapearFechas(response) : null),
      catchError(error => {
        if (error.status === 404) return [null];
        return this.handleError(error);
      })
    );
  }

  marcarSinDeuda(expedienteId: number): Observable<DeudaEconomicaDTO> {
    return this.http.put<DeudaEconomicaDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/sin-deuda`,
      {},
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  liquidar(expedienteId: number, dto: LiquidarDeudaEconomicaDTO): Observable<DeudaEconomicaDTO> {
    return this.http.put<DeudaEconomicaDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/liquidar`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  exonerar(expedienteId: number, dto: AplicarExoneracionDTO): Observable<DeudaEconomicaDTO> {
    return this.http.put<DeudaEconomicaDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/exonerar`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  bloqueaRetiro(expedienteId: number): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/expediente/${expedienteId}/bloquea-retiro`,
      { headers: this.getHeaders() }
    ).pipe(catchError(this.handleError));
  }

  obtenerSemaforo(expedienteId: number): Observable<DeudaEconomicaSemaforoDTO> {
    return this.http.get<DeudaEconomicaSemaforoDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/semaforo`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  obtenerPendientes(): Observable<DeudaEconomicaDTO[]> {
    return this.http.get<DeudaEconomicaDTO[]>(
      `${this.apiUrl}/pendientes`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(d => this.mapearFechas(d))),
      catchError(this.handleError)
    );
  }

  obtenerExoneradas(): Observable<DeudaEconomicaDTO[]> {
    return this.http.get<DeudaEconomicaDTO[]>(
      `${this.apiUrl}/exoneradas`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(d => this.mapearFechas(d))),
      catchError(this.handleError)
    );
  }

  obtenerHistorial(expedienteId: number): Observable<HistorialDeudaEconomicaDTO[]> {
    return this.http.get<HistorialDeudaEconomicaDTO[]>(
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

  obtenerEstadisticas(): Observable<EstadisticasDeudaEconomicaDTO> {
    return this.http.get<EstadisticasDeudaEconomicaDTO>(
      `${this.apiUrl}/estadisticas`,
      { headers: this.getHeaders() }
    ).pipe(catchError(this.handleError));
  }

  uploadPDF(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('sgm_token');
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

    return this.http.post<{ rutaArchivo: string }>(
      `${this.apiUrl}/upload-pdf`,
      formData,
      { headers }
    ).pipe(
      map(response => response.rutaArchivo),
      catchError(this.handleError)
    );
  }

  downloadPDF(rutaArchivo: string): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/download-pdf?ruta=${encodeURIComponent(rutaArchivo)}`,
      { headers: this.getHeaders(), responseType: 'blob' }
    ).pipe(catchError(this.handleError));
  }

  descargarYAbrirPDF(rutaArchivo: string): void {
    this.downloadPDF(rutaArchivo).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => window.URL.revokeObjectURL(url), 100);
      },
      error: (error) => {
        console.error('[DeudaEconomicaService] Error al descargar PDF:', error);
        alert('Error al descargar el archivo PDF');
      }
    });
  }

  private mapearFechas(dto: DeudaEconomicaDTO): DeudaEconomicaDTO {
    return {
      ...dto,
      fechaRegistro: new Date(dto.fechaRegistro),
      fechaPago: dto.fechaPago ? new Date(dto.fechaPago) : undefined,
      fechaExoneracion: dto.fechaExoneracion ? new Date(dto.fechaExoneracion) : undefined,
      fechaActualizacion: dto.fechaActualizacion ? new Date(dto.fechaActualizacion) : undefined
    };
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'Error desconocido';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.error instanceof Blob) {
      errorMessage = 'Error al descargar el archivo PDF';
    } else {
      if (error.error?.message) errorMessage = error.error.message;
      else if (error.error?.title) errorMessage = error.error.title;
      else if (error.message) errorMessage = error.message;
      else errorMessage = `Error ${error.status}: ${error.statusText}`;
    }

    console.error('[DeudaEconomicaService] Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }

  esCuentasPacientes(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('CuentasPacientes') || roles?.includes('Administrador') || false;
  }

  esServicioSocial(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('ServicioSocial') || roles?.includes('Administrador') || false;
  }

  puedeVerificarSupervisor(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('VigilanteSupervisor') ||
      roles?.includes('CuentasPacientes') ||
      roles?.includes('Administrador') || false;
  }

  validarMonto(monto: number): { valido: boolean; error?: string } {
    if (monto < 0) return { valido: false, error: 'El monto no puede ser negativo' };
    if (monto === 0) return { valido: false, error: 'El monto debe ser mayor a cero' };
    if (monto > 100000) return { valido: false, error: 'El monto excede el límite (S/ 100,000)' };
    return { valido: true };
  }
}
