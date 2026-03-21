import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/** DTO de entrada — campos del brazalete pre-llenados por el sistema + confirmación manual */
export interface VerificacionRequest {
  codigoExpedienteBrazalete: string;
  // Campos informativos (pre-llenados automáticamente desde datosExpediente)
  hcBrazalete: string;
  tipoDocumentoBrazalete: string;
  numeroDocumentoBrazalete: string;
  nombreCompletoBrazalete: string;
  servicioBrazalete: string;
  // Única confirmación manual del Vigilante
  brazaletePresente: boolean;
  observaciones?: string;
}

/** DTO de respuesta */
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

  /** Consulta datos del expediente por código QR para mostrarlos en pantalla */
  consultarPorQR(codigoQR: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/QR/consultar/${codigoQR}`);
  }

  /** Registra la verificación de ingreso físico al mortuorio */
  registrarIngreso(data: VerificacionRequest): Observable<VerificacionResultado> {
    return this.http.post<VerificacionResultado>(`${this.apiUrl}/Verificacion/ingreso`, data);
  }
}
