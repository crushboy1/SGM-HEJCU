import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================

export interface Expediente {
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  hc: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: string;
  estadoActual: string;
  numeroCama?: string;
  medicoCertificaNombre?: string;
  diagnosticoFinal?: string;
  tipoDocumento?: string;
  numeroDocumento?: string;
  tipoExpediente?: string;
  tipoSalidaPreliminar?: 'Familiar' | 'AutoridadLegal' | null;
  documentacionCompleta: boolean;
  fechaValidacionAdmision?: string;
  usuarioAdmisionNombre?: string;
  codigoBandeja?: string;
  numeroCertificadoSINADEF?: string;
  medicoCertificaCMP?: string;
  medicoCertificaRNE?: string;
}

export interface CreateExpedienteDTO {
  hc: string;
  tipoDocumento: number;
  numeroDocumento: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombres: string;
  fechaNacimiento: string;
  sexo: string;
  tipoSeguro: string;
  tipoExpediente: string;
  servicioFallecimiento: string;
  numeroCama?: string | null;
  fechaHoraFallecimiento: string;
  diagnosticoFinal: string;
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string | null;
  numeroCertificadoSINADEF?: string | null;
  pertenencias: CreatePertenenciaDTO[];
}

export interface CreatePertenenciaDTO {
  descripcion: string;
  observaciones?: string | null;
}

// ===================================================================
// SERVICIO FRONTEND
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class ExpedienteService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  /**
   * Obtiene todos los expedientes (para el Dashboard)
   */
  getAll(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes`);
  }

  /**
 * Obtiene un expediente por ID (Para ver detalles o editar)
 */
  getById(id: number): Observable<Expediente> {
    return this.http.get<any>(`${this.apiUrl}/Expedientes/${id}`).pipe(
      map(response => ({
        ...response,
        medicoCertificaCMP: response.medicoCMP || response.medicoCertificaCMP,
        medicoCertificaRNE: response.medicoRNE || response.medicoCertificaRNE
      } as Expediente))
    );
  }
  /**
 * Busca expedientes usando filtros (HC, DNI, etc.)
 * Se usa para que Banco de Sangre/Cuentas encuentren al paciente por sus datos históricos.
 */
  buscarExpedientes(filtros: { hc?: string, dni?: string, estado?: string }): Observable<Expediente[]> {
    let params = new HttpParams();

    if (filtros.hc) params = params.set('hc', filtros.hc);
    if (filtros.dni) params = params.set('dni', filtros.dni);
    if (filtros.estado) params = params.set('estado', filtros.estado);

    // Asumiendo que tu Controller expone el GetByFiltrosAsync en la raíz con query params
    // GET /api/Expedientes?hc=12345&dni=...
    return this.http.get<Expediente[]>(`${this.apiUrl}/Expedientes`, { params });
  }
  /**
 * Búsqueda simple por HC, DNI o Código
 * Retorna UN SOLO expediente
 */
  buscarSimple(filtros: {
    hc?: string;
    dni?: string;
    codigoExpediente?: string;
  }): Observable<Expediente> {
    let params = new HttpParams();

    if (filtros.hc) params = params.set('hc', filtros.hc);
    if (filtros.dni) params = params.set('dni', filtros.dni);
    if (filtros.codigoExpediente) params = params.set('codigoExpediente', filtros.codigoExpediente);

    return this.http.get<Expediente>(`${this.apiUrl}/expedientes/buscar-simple`, { params });
  }

  /**
   * Crea un nuevo expediente
   */
  create(data: CreateExpedienteDTO): Observable<Expediente> {
    return this.http.post<Expediente>(`${this.apiUrl}/Expedientes`, data);
  }

  /**
   * Actualiza un expediente existente (Para correcciones)
   */
  update(id: number, data: Partial<CreateExpedienteDTO>): Observable<Expediente> {
    return this.http.put<Expediente>(`${this.apiUrl}/Expedientes/${id}`, data);
  }

  // --- MÉTODOS QR Y BRAZALETE ---

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

  /**
   * Obtiene expedientes pendientes de validación por Admisión.
   * Estado: EnBandeja y DocumentacionCompleta = false
   */
  getPendientesValidacionAdmision(): Observable<Expediente[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/Expedientes/pendientes-validacion-admision`
    ).pipe(
      map(expedientes => expedientes.map(exp => ({
        ...exp,
        // Mapear campos del backend con nombres diferentes
        medicoCertificaCMP: exp.medicoCMP || exp.medicoCertificaCMP,
        medicoCertificaRNE: exp.medicoRNE || exp.medicoCertificaRNE
      } as Expediente)))
    );
  }
  /**
 * Establece el tipo de salida preliminar antes de crear el Acta de Retiro.
 * Define qué documentos son requeridos: Familiar (3 docs) o AutoridadLegal (1 doc).
 * Bloqueado si ya existe el Acta de Retiro.
 */
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
