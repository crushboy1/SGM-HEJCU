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
export interface DeudaStats {
  sangrePendientes: number;
  sangreAnuladas: number;
  economicasPendientes: number;
  economicasExoneradas: number;
  montoTotalPendiente: number;
  montoTotalExonerado: number;
}

export interface DashboardKPIs {
  bandejas: BandejaStats;
  solicitudes: SolicitudStats;
  salidas: SalidaStats;
  deudas?: DeudaStats;
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
  getDashboardKPIs(incluirDeudas: boolean = false): Observable<DashboardKPIs> {
    const requests: any = {
      bandejas: this.getBandejaStats(),
      solicitudes: this.getSolicitudStats(),
      salidas: this.getSalidaStats()
    };

    if (incluirDeudas) {
      requests.deudas = this.getDeudaStats();
    }

    return forkJoin(requests) as Observable<DashboardKPIs>;  // ✅ Cast explícito
  }
  /**
 * Obtener estadísticas de deudas (Sangre + Económica)
 */
  getDeudaStats(): Observable<DeudaStats> {
    return this.http.get<DeudaStats>(`${this.apiUrl}/Deudas/estadisticas`);
  }
}
