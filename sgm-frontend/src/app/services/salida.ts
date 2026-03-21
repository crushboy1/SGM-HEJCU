import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * DTO para registrar salida de mortuorio.
 * Debe coincidir con RegistrarSalidaDTO.cs del backend.
 */
export interface RegistrarSalidaRequest {
  expedienteID: number;

  /**
   * ID del Acta de Retiro asociada. OBLIGATORIO.
   * El backend lo requiere para vincular la salida con el acta firmada.
   */
  actaRetiroID: number;

  expedienteLegalID?: number;

  // Funeraria — solo TipoSalida = Familiar
  nombreFuneraria?: string;
  funerariaRUC?: string;
  funerariaTelefono?: string;
  conductorFuneraria?: string;
  dniConductor?: string;
  ayudanteFuneraria?: string;
  dniAyudante?: string;

  // Vehículo y destino — ambos tipos de salida
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
  tiempoPermanenciaMinutos?: number;
  bandejaLiberadaID?: number;
  incidenteRegistrado: boolean;
  detalleIncidente?: string;
}

/**
 * Estadísticas de salidas del mortuorio.
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
   * Requiere actaRetiroID para vincular con el acta firmada.
   * Libera automáticamente la bandeja y envía notificación SignalR.
   */
  registrarSalida(data: RegistrarSalidaRequest): Observable<SalidaDTO> {
    return this.http.post<SalidaDTO>(`${this.apiUrl}/registrar`, data);
  }

  /**
   * Obtiene la salida registrada de un expediente específico.
   */
  getSalidaPorExpediente(expedienteId: number): Observable<SalidaDTO | null> {
    return this.http.get<SalidaDTO | null>(
      `${this.apiUrl}/expediente/${expedienteId}`
    );
  }

  /**
   * Obtiene estadísticas de salidas en un rango de fechas.
   */
  getEstadisticas(
    fechaInicio?: Date,
    fechaFin?: Date
  ): Observable<EstadisticasSalidaDTO> {
    let params: any = {};
    if (fechaInicio) params.fechaInicio = fechaInicio.toISOString();
    if (fechaFin) params.fechaFin = fechaFin.toISOString();
    return this.http.get<EstadisticasSalidaDTO>(
      `${this.apiUrl}/estadisticas`, { params }
    );
  }

  /**
   * Obtiene lista de salidas en un rango de fechas.
   */
  getSalidasPorFechas(
    fechaInicio: Date,
    fechaFin: Date
  ): Observable<SalidaDTO[]> {
    const params = {
      fechaInicio: fechaInicio.toISOString(),
      fechaFin: fechaFin.toISOString()
    };
    return this.http.get<SalidaDTO[]>(`${this.apiUrl}/rango`, { params });
  }
}
