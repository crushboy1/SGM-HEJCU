import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================

/** Mapea ExpedienteDTO (lectura) */
export interface Expediente {
  expedienteID: number;
  codigoExpediente: string;
  hc: string;
  tipoExpediente: 'Interno' | 'Externo';
  tipoDocumento: string;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  nombreCompleto: string;
  edad: number;
  sexo: string;
  fuenteFinanciamiento: string;
  esNN: boolean;
  causaViolentaODudosa: boolean;
  servicioFallecimiento: string;
  numeroCama?: string;
  fechaHoraFallecimiento: string;
  diagnosticoFinal?: string;
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string;
  medicoExternoNombre?: string;
  medicoExternoCMP?: string;
  observaciones?: string;
  estadoActual: string;
  codigoQR?: string;
  bandejaActualID?: number;
  codigoBandeja?: string;
  tipoSalidaPreliminar?: 'Familiar' | 'AutoridadLegal' | null;
  documentacionCompleta: boolean;
  fechaValidacionAdmision?: string;
  usuarioAdmisionNombre?: string;

  // ─── BYPASS DE DEUDA ─────────────────────────────────────
  /** true si JG/Admin autorizó retiro con deudas pendientes.
   * Solo aplica para TipoSalidaPreliminar = AutoridadLegal. */
  bypassDeudaAutorizado?: boolean;
  bypassDeudaJustificacion?: string;
  /** Nombre del usuario que autorizó el bypass */
  bypassDeudaUsuarioNombre?: string;
  bypassDeudaFecha?: string;
}

/** Mapea CreateExpedienteDTO del backend */
export interface CreateExpedienteDTO {
  hc: string;
  tipoDocumento: number;           // TipoDocumentoIdentidad (enum int)
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  fechaNacimiento: string;
  sexo: string;
  fuenteFinanciamiento: number;    // FuenteFinanciamiento (enum int)
  tipoExpediente: number;          // TipoIngreso (enum int: Interno=1, Externo=2)
  esNN: boolean;
  causaViolentaODudosa: boolean;
  servicioFallecimiento: string;
  numeroCama?: string | null;
  fechaHoraFallecimiento: string;
  diagnosticoFinal?: string | null;
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string | null;
  medicoExternoNombre?: string | null;
  medicoExternoCMP?: string | null;
  observaciones?: string | null;
  pertenencias?: CreatePertenenciaDTO[] | null;
}

/** Mapea UpdateExpedienteDTO del backend */
export interface UpdateExpedienteDTO {
  diagnosticoFinal?: string | null;
  medicoCertificaNombre?: string | null;
  medicoCMP?: string | null;
  medicoRNE?: string | null;
  causaViolentaODudosa?: boolean;
  medicoExternoNombre?: string | null;
  medicoExternoCMP?: string | null;
  fuenteFinanciamiento?: number | null;
  observaciones?: string | null;
}

export interface CreatePertenenciaDTO {
  descripcion: string;
  observaciones?: string | null;
}

/** Parámetros para buscar expedientes */
export interface BuscarExpedientesParams {
  hc?: string;
  numeroDocumento?: string;
  servicio?: string;
  fechaDesde?: string;
  fechaHasta?: string;
  estado?: number;
}

// ===================================================================
// SERVICIO
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class ExpedienteService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api';

  getAll(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes`);
  }

  getById(id: number): Observable<Expediente> {
    return this.http.get<Expediente>(`${this.apiUrl}/Expedientes/${id}`);
  }

  buscarExpedientes(filtros: BuscarExpedientesParams): Observable<Expediente[]> {
    let params = new HttpParams();
    if (filtros.hc) params = params.set('hc', filtros.hc);
    if (filtros.numeroDocumento) params = params.set('numeroDocumento', filtros.numeroDocumento);
    if (filtros.servicio) params = params.set('servicio', filtros.servicio);
    if (filtros.fechaDesde) params = params.set('fechaDesde', filtros.fechaDesde);
    if (filtros.fechaHasta) params = params.set('fechaHasta', filtros.fechaHasta);
    if (filtros.estado != null) params = params.set('estado', String(filtros.estado));
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes/buscar`, { params });
  }

  buscarSimple(filtros: {
    hc?: string;
    dni?: string;
    codigoExpediente?: string;
  }): Observable<Expediente> {
    let params = new HttpParams();
    if (filtros.hc) params = params.set('hc', filtros.hc);
    if (filtros.dni) params = params.set('dni', filtros.dni);
    if (filtros.codigoExpediente) params = params.set('codigoExpediente', filtros.codigoExpediente);
    return this.http.get<Expediente>(`${this.apiUrl}/Expedientes/buscar-simple`, { params });
  }

  create(data: CreateExpedienteDTO): Observable<Expediente> {
    return this.http.post<Expediente>(`${this.apiUrl}/Expedientes`, data);
  }

  update(id: number, data: UpdateExpedienteDTO): Observable<Expediente> {
    return this.http.put<Expediente>(`${this.apiUrl}/Expedientes/${id}`, data);
  }

  generarQR(expedienteId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/QR/${expedienteId}/generar`, {});
  }

  imprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/QR/${expedienteId}/imprimir-brazalete`,
      {},
      { responseType: 'blob' }
    );
  }

  reimprimirBrazalete(expedienteId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/QR/${expedienteId}/reimprimir-brazalete`,
      { responseType: 'blob' }
    );
  }

  validarDocumentacion(expedienteId: number): Observable<Expediente> {
    return this.http.post<Expediente>(
      `${this.apiUrl}/Expedientes/${expedienteId}/validar-documentacion`,
      {}
    );
  }

  getPendientesValidacionAdmision(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(
      `${this.apiUrl}/Expedientes/pendientes-validacion-admision`
    );
  }

  getPendientesRecojo(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(
      `${this.apiUrl}/Expedientes/pendientes-recojo`
    );
  }

  establecerTipoSalidaPreliminar(
    expedienteId: number,
    tipoSalida: 'Familiar' | 'AutoridadLegal'
  ): Observable<Expediente> {
    return this.http.patch<Expediente>(
      `${this.apiUrl}/Expedientes/${expedienteId}/tipo-salida-preliminar`,
      JSON.stringify(tipoSalida),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  limpiarTipoSalidaPreliminar(expedienteId: number): Observable<Expediente> {
    return this.http.patch<Expediente>(
      `${this.apiUrl}/Expedientes/${expedienteId}/tipo-salida-preliminar`,
      JSON.stringify(null),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }
}
