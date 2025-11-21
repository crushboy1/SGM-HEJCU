import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// DTO de Salida (Lo que envía el Vigilante)
export interface VerificacionRequest {
  codigoExpedienteBrazalete: string;
  hcBrazalete: string;
  tipoDocumentoBrazalete: string;   
  numeroDocumentoBrazalete: string;
  nombreCompletoBrazalete: string;
  servicioBrazalete: string;
  observaciones?: string;
}

// DTO de Respuesta
export interface VerificacionResultado {
  verificacionID: number;
  aprobada: boolean;
  mensajeResultado: string;
  estadoExpedienteNuevo: string;
  motivoRechazo?: string;
  solicitudCorreccionID?: number;
}

@Injectable({
  providedIn: 'root'
})
export class VerificacionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  /**
   * Consulta datos del QR para mostrarlos en la tablet del vigilante
   */
  consultarPorQR(codigoQR: string): Observable<any> {
    // Usamos el endpoint público de consulta QR
    return this.http.get<any>(`${this.apiUrl}/QR/consultar/${codigoQR}`);
  }

  /**
   * Registra la verificación (Ingreso físico al mortuorio)
   */
  registrarIngreso(data: VerificacionRequest): Observable<VerificacionResultado> {
    return this.http.post<VerificacionResultado>(`${this.apiUrl}/Verificacion/ingreso`, data);
  }
}
