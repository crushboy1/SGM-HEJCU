import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================

export interface Bandeja {
  bandejaID: number;
  codigo: string;
  estado: string;
  observaciones?: string;

  // Datos de ocupación 
  expedienteID?: number;
  codigoExpediente?: string;
  nombrePaciente?: string;
  usuarioAsignaNombre?: string;
  fechaHoraAsignacion?: string;
  tiempoOcupada?: string;
  tieneAlerta?: boolean;
}

export interface AsignarBandejaRequest {
  bandejaID: number;
  expedienteID: number;
  observaciones?: string;
}

export interface LiberarBandejaRequest {
  bandejaID: number;
  observaciones?: string;
}

// ===================================================================
// SERVICIO
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class BandejaService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api/Bandejas';


  getDashboard(): Observable<Bandeja[]> {
    return this.http.get<Bandeja[]>(`${this.apiUrl}/dashboard`);
  }

  getById(bandejaId: number): Observable<Bandeja> {
    return this.http.get<Bandeja>(`${this.apiUrl}/${bandejaId}`);
  }

  asignar(request: AsignarBandejaRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/asignar`, request);
  }

  /**
   * Liberar bandeja manualmente
   * ⚠️ DEUDA TÉCNICA: Requiere implementar endpoint [HttpPost("liberar")] en Backend.
   */
  liberar(request: LiberarBandejaRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/liberar`, request);
  }

  marcarMantenimiento(bandejaId: number, observaciones: string): Observable<any> {
    // Enviamos el string serializado para compatibilidad con [FromBody] string
    return this.http.put(`${this.apiUrl}/${bandejaId}/mantenimiento/iniciar`, JSON.stringify(observaciones), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  reactivar(bandejaId: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${bandejaId}/mantenimiento/finalizar`, {});
  }
}
