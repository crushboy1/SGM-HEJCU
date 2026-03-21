import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ── Alineado con BandejaEntradaDTO (backend) ─────────────────────────────────

/** Paciente fallecido pendiente de generar expediente (bandeja de Enfermería) */
export interface PacientePendiente {
  // Galenhos — identificación
  hc: string;
  tipoDocumentoID: number;       // TipoDocumentoIdentidad (enum int)
  numeroDocumento: string;
  nombreCompleto: string;
  edad: number;
  sexo: string;
  fuenteFinanciamiento: string;
  esNN: boolean;

  // SIGEM (opcional)
  servicioFallecimiento?: string;
  numeroCama?: string;
  fechaHoraFallecimiento?: string;
  diagnosticoFinal?: string;
  medicoCertificaNombre?: string;

  // Estado de integración
  tieneDatosSigem: boolean;
  advertencias: string[];
}

/** Datos completos del paciente para pre-llenar el formulario de expediente */
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
  esNN: boolean;
  causaViolentaODudosa: boolean;
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

@Injectable({ providedIn: 'root' })
export class IntegracionService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api';

  /** Lista de pacientes fallecidos sin expediente (bandeja Enfermería) */
  getPendientes(): Observable<PacientePendiente[]> {
    return this.http.get<PacientePendiente[]>(`${this.apiUrl}/Integracion/pendientes`);
  }

  /** Datos completos para pre-llenar formulario de nuevo expediente */
  consultarPaciente(hc: string): Observable<PacienteParaForm> {
    return this.http.get<PacienteParaForm>(`${this.apiUrl}/Integracion/consultar-paciente/${hc}`);
  }
}
