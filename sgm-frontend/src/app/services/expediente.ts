import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================

export interface Expediente {
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  hc: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: string;
  estadoActual: string;
  numeroCama?: string;
  medicoCertificaNombre?: string;
  diagnosticoFinal?: string;
  tipoDocumento?: string;
  numeroDocumento?: string;
}

export interface CreateExpedienteDTO {
  hc: string;
  tipoDocumento: number;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  fechaNacimiento: string;
  sexo: string;
  tipoSeguro: string;
  tipoExpediente: string;
  servicioFallecimiento: string;
  numeroCama?: string | null;
  fechaHoraFallecimiento: string;
  diagnosticoFinal: string;
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string | null;
  numeroCertificadoSINADEF?: string | null;
  pertenencias: CreatePertenenciaDTO[];
}

export interface CreatePertenenciaDTO {
  descripcion: string;
  observaciones?: string | null;
}

// ===================================================================
// SERVICIO FRONTEND
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class ExpedienteService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  /**
   * Obtiene todos los expedientes (para el Dashboard)
   */
  getAll(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes`);
  }

  /**
   * Obtiene un expediente por ID (Para ver detalles o editar)
   */
  getById(id: number): Observable<Expediente> {
    return this.http.get<Expediente>(`${this.apiUrl}/Expedientes/${id}`);
  }

  /**
   * Crea un nuevo expediente
   */
  create(data: CreateExpedienteDTO): Observable<Expediente> {
    return this.http.post<Expediente>(`${this.apiUrl}/Expedientes`, data);
  }

  /**
   * Actualiza un expediente existente (Para correcciones)
   */
  update(id: number, data: Partial<CreateExpedienteDTO>): Observable<Expediente> {
    return this.http.put<Expediente>(`${this.apiUrl}/Expedientes/${id}`, data);
  }

  // --- MÉTODOS QR Y BRAZALETE ---

  generarQR(expedienteId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/QR/${expedienteId}/generar`, {});
  }

  imprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/QR/${expedienteId}/imprimir-brazalete`,
      {},
      { responseType: 'blob' }
    );
  }

  reimprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/QR/${expedienteId}/reimprimir-brazalete`,
      { responseType: 'blob' }
    );
  }
}
