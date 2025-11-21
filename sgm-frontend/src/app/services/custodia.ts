import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RealizarTraspasoRequest {
  codigoQR: string;
  observaciones?: string;
}

export interface TraspasoResponse {
  transferenciaID: number;
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  estadoAnterior: string;
  estadoNuevo: string;
}

@Injectable({
  providedIn: 'root'
})
export class CustodiaService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api/Custodia';

  /**
   * Realiza el traspaso de custodia (Enfermería -> Ambulancia)
   * Se llama al escanear el QR
   */
  realizarTraspaso(data: RealizarTraspasoRequest): Observable<TraspasoResponse> {
    return this.http.post<TraspasoResponse>(`${this.apiUrl}/traspasos`, data);
  }
}
