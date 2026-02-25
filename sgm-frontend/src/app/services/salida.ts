import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * DTO para registrar salida de mortuorio.
 * Debe coincidir con RegistrarSalidaDTO.cs del backend.
 */
export interface RegistrarSalidaRequest {
  expedienteID: number;
  expedienteLegalID?: number;

  // Funeraria — solo TipoSalida = Familiar
  nombreFuneraria?: string;
  funerariaRUC?: string;
  funerariaTelefono?: string;
  conductorFuneraria?: string;
  dniConductor?: string;
  ayudanteFuneraria?: string;
  dniAyudante?: string;

  // Vehículo y destino
  placaVehiculo?: string;
  destino?: string;

  // Observaciones
  observaciones?: string;
}

/**
 * DTO de respuesta tras registrar salida.
 * Coincide con SalidaDTO.cs del backend.
 */
export interface SalidaDTO {
  salidaID: number;
  expedienteID: number;
  codigoExpediente: string;
  nombrePaciente: string;
  fechaHoraSalida: Date;
  tipoSalida: string;
  responsableNombre: string;
  responsableDocumento: string;
  vigilanteNombre: string;
  nombreFuneraria?: string;
  destino?: string;
  incidenteRegistrado: boolean;
  detalleIncidente?: string;
}

/**
 * Estadísticas de salidas del mortuorio.
 * Coincide con EstadisticasSalidaDTO.cs
 */
export interface EstadisticasSalidaDTO {
  totalSalidas: number;
  salidasFamiliar: number;
  salidasAutoridadLegal: number;
  salidasTrasladoHospital: number;
  salidasOtro: number;
  conIncidentes: number;
  conFuneraria: number;
  porcentajeIncidentes: number;
}

@Injectable({
  providedIn: 'root'
})
export class SalidaService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api/Salidas';

  /**
   * Registra la salida de un cuerpo del mortuorio.
   * Libera automáticamente la bandeja asignada y envía notificación SignalR.
   * 
   * @param data - Datos de la salida y responsable
   * @returns Observable con SalidaDTO del registro creado
   */
  registrarSalida(data: RegistrarSalidaRequest): Observable<SalidaDTO> {
    return this.http.post<SalidaDTO>(`${this.apiUrl}/registrar`, data);
  }

  /**
   * Obtiene la salida registrada de un expediente específico.
   * 
   * @param expedienteId - ID del expediente
   * @returns Observable con SalidaDTO o null si no existe
   */
  getSalidaPorExpediente(expedienteId: number): Observable<SalidaDTO | null> {
    return this.http.get<SalidaDTO | null>(`${this.apiUrl}/expediente/${expedienteId}`);
  }

  /**
   * Obtiene estadísticas de salidas en un rango de fechas.
   * 
   * @param fechaInicio - Fecha inicial (opcional)
   * @param fechaFin - Fecha final (opcional)
   * @returns Observable con EstadisticasSalidaDTO
   */
  getEstadisticas(fechaInicio?: Date, fechaFin?: Date): Observable<EstadisticasSalidaDTO> {
    let params: any = {};
    if (fechaInicio) params.fechaInicio = fechaInicio.toISOString();
    if (fechaFin) params.fechaFin = fechaFin.toISOString();

    return this.http.get<EstadisticasSalidaDTO>(`${this.apiUrl}/estadisticas`, { params });
  }

  /**
   * Obtiene lista de salidas en un rango de fechas.
   * 
   * @param fechaInicio - Fecha inicial
   * @param fechaFin - Fecha final
   * @returns Observable con array de SalidaDTO
   */
  getSalidasPorFechas(fechaInicio: Date, fechaFin: Date): Observable<SalidaDTO[]> {
    const params = {
      fechaInicio: fechaInicio.toISOString(),
      fechaFin: fechaFin.toISOString()
    };

    return this.http.get<SalidaDTO[]>(`${this.apiUrl}/rango`, { params });
  }
}
