import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================

/**
 * Modelo de bandeja del mortuorio.
 * Coincide con BandejaDTO.cs del backend.
 */
export interface Bandeja {
  bandejaID: number;
  codigo: string;
  estado: string; // "Disponible" | "Ocupada" | "Mantenimiento"
  observaciones?: string;

  // Datos de ocupación (solo si estado = "Ocupada")
  expedienteID?: number;
  codigoExpediente?: string;
  nombrePaciente?: string;
  usuarioAsignaNombre?: string;
  fechaHoraAsignacion?: string;
  tiempoOcupada?: string; // Ej: "2h 30m"
  tieneAlerta?: boolean;
}

/**
 * Request para asignar un expediente a una bandeja.
 */
export interface AsignarBandejaRequest {
  bandejaID: number;
  expedienteID: number;
  observaciones?: string;
}

/**
 * Request para liberar una bandeja.
 */
export interface LiberarBandejaRequest {
  bandejaID: number;
  observaciones?: string;
}

/**
 * Estadísticas del dashboard de bandejas.
 */
export interface EstadisticasBandejaDTO {
  total: number;
  disponibles: number;
  ocupadas: number;
  enMantenimiento: number;
  fueraDeServicio: number;
  porcentajeOcupacion: number;
  conAlerta24h: number;
  conAlerta48h: number;
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

  /**
   * Obtiene todas las bandejas con su estado actual.
   * Usado por el componente mapa-mortuorio.
   */
  getDashboard(): Observable<Bandeja[]> {
    return this.http.get<Bandeja[]>(`${this.apiUrl}/dashboard`);
  }

  /**
   * Obtiene los detalles de una bandeja específica.
   */
  getById(bandejaId: number): Observable<Bandeja> {
    return this.http.get<Bandeja>(`${this.apiUrl}/${bandejaId}`);
  }

  /**
   * Obtiene estadísticas generales del mortuorio.
   */
  getEstadisticas(): Observable<EstadisticasBandejaDTO> {
    return this.http.get<EstadisticasBandejaDTO>(`${this.apiUrl}/estadisticas`);
  }

  /**
   * Asigna un expediente a una bandeja disponible.
   */
  asignar(request: AsignarBandejaRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/asignar`, request);
  }

  /**
   * Libera una bandeja ocupada (registra salida del cuerpo).
   * 
   * ⚠️ DEUDA TÉCNICA: Backend debe implementar endpoint [HttpPost("liberar")]
   * o usar endpoint de SalidaMortuorio que libera automáticamente.
   */
  liberar(request: LiberarBandejaRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/liberar`, request);
  }

  /**
   * Marca una bandeja como "En Mantenimiento".
   */
  marcarMantenimiento(bandejaId: number, observaciones: string): Observable<any> {
    // Backend espera [FromBody] string, por eso usamos JSON.stringify
    return this.http.put(
      `${this.apiUrl}/${bandejaId}/mantenimiento/iniciar`,
      JSON.stringify(observaciones),
      {
        headers: { 'Content-Type': 'application/json' }
      }
    );
  }

  /**
   * Reactiva una bandeja que estaba en mantenimiento.
   */
  reactivar(bandejaId: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${bandejaId}/mantenimiento/finalizar`, {});
  }
}
