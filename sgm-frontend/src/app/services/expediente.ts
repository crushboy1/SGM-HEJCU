import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===================================================================
// INTERFACES PARA RESPUESTAS (GET)
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

// ===================================================================
// INTERFACES PARA CREAR EXPEDIENTE (POST)
// ===================================================================

export interface CreateExpedienteDTO {
  // Datos demográficos (de Galenhos - pre-llenados)
  hc: string;
  tipoDocumento: number;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  fechaNacimiento: string; // "YYYY-MM-DD"
  sexo: string;
  tipoSeguro: string;

  // Datos del fallecimiento (editables por Enfermería)
  tipoExpediente: string; // "Interno" | "Externo"
  servicioFallecimiento: string;
  numeroCama?: string | null;
  fechaHoraFallecimiento: string; // "YYYY-MM-DDTHH:mm"
  diagnosticoFinal: string;
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string | null;
  numeroCertificadoSINADEF?: string | null;

  // Pertenencias
  pertenencias: CreatePertenenciaDTO[];
}

export interface CreatePertenenciaDTO {
  descripcion: string;
  observaciones?: string | null;
}

// ===================================================================
// SERVICIO
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class ExpedienteService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  getAll(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes`);
  }

  create(data: CreateExpedienteDTO): Observable<Expediente> {
    return this.http.post<Expediente>(`${this.apiUrl}/Expedientes`, data);
  }
  generarQR(expedienteId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/QR/${expedienteId}/generar`, {});
  }
  // Imprimir (Primera vez)
  imprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/QR/${expedienteId}/imprimir-brazalete`,
      {},
      { responseType: 'blob' }
    );
  }
  // Reimprimir (Dashboard)
  reimprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/QR/${expedienteId}/reimprimir-brazalete`,
      { responseType: 'blob' }
    );
  }
}
