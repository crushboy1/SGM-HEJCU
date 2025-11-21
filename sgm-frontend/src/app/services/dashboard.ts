import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';

// ===================================================================
// INTERFACES BASADAS EN DTOs DEL BACKEND
// ===================================================================

export interface BandejaStats {
  total: number;
  disponibles: number;
  ocupadas: number;
  enMantenimiento: number;
  porcentajeOcupacion: number;
  conAlerta24h: number;
  conAlerta48h: number;
}

export interface SolicitudStats {
  totalSolicitudes: number;
  pendientes: number;
  resueltas: number;
  conAlerta: number;
}

export interface SalidaStats {
  totalSalidas: number;
  salidasFamiliar: number;
  salidasAutoridadLegal: number; 
  salidasTrasladoHospital: number; 
  salidasOtro: number;         
  conIncidentes: number;
}

export interface DashboardKPIs {
  bandejas: BandejaStats;
  solicitudes: SolicitudStats;
  salidas: SalidaStats;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  /**
   * Obtener estadísticas de bandejas del mortuorio
   */
  getBandejaStats(): Observable<BandejaStats> {
    return this.http.get<BandejaStats>(`${this.apiUrl}/Bandejas/estadisticas`);
  }

  /**
   * Obtener estadísticas de solicitudes de corrección
   */
  getSolicitudStats(): Observable<SolicitudStats> {
    return this.http.get<SolicitudStats>(`${this.apiUrl}/Solicitudes-correccion/estadisticas`);
  }

  /**
   * Obtener estadísticas de salidas del mortuorio
   */
  getSalidaStats(): Observable<SalidaStats> {
    return this.http.get<SalidaStats>(`${this.apiUrl}/Salidas/estadisticas`);
  }

  /**
   * Carga combinada de todos los KPIs del Dashboard
   * Optimizado con forkJoin para ejecutar en paralelo
   */
  getDashboardKPIs(): Observable<DashboardKPIs> {
    return forkJoin({
      bandejas: this.getBandejaStats(),
      solicitudes: this.getSolicitudStats(),
      salidas: this.getSalidaStats() // ← AGREGADO
    });
  }
}
