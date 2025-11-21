import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PacientePendiente {
  hc: string;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  sexo: string;
  tipoDocumentoID: number;
  servicioFallecimiento?: string; 
  fechaHoraFallecimiento?: string; 
}

export interface PacienteParaForm {
  hc: string;
  tipoDocumentoID: number;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  fechaNacimiento: string;
  edad: number;
  sexo: string;
  fuenteFinanciamiento: string;
  servicioFallecimiento: string | null;
  numeroCama: string | null;
  fechaHoraFallecimiento: string | null;
  diagnosticoFinal: string | null;
  codigoCIE10: string | null;
  medicoCertificaNombre: string | null;
  medicoCMP: string | null;
  medicoRNE: string | null;
  existeEnGalenhos: boolean;
  existeEnSigem: boolean;
  advertencias: string[];
}

@Injectable({
  providedIn: 'root'
})
export class IntegracionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api/Integracion';

  getPendientes(): Observable<PacientePendiente[]> {
    return this.http.get<PacientePendiente[]>(`${this.apiUrl}/pendientes`);
  }

  consultarParaForm(hc: string): Observable<PacienteParaForm> {
    return this.http.get<PacienteParaForm>(`${this.apiUrl}/consultar-paciente/${hc}`);
  }
}
