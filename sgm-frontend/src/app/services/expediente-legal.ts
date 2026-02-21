import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, switchMap, map, catchError, throwError } from 'rxjs';
import {
  ExpedienteLegalDTO,
  DocumentoLegalDTO,
  CreateExpedienteLegalDTO,
  UpdateExpedienteLegalDTO,
  ValidarAdmisionDTO,
  AutorizarJefeGuardiaDTO,
  AutoridadExternaDTO,    
  CreateAutoridadExternaDTO
} from '../models/expediente-legal.model';

/**
 * ExpedienteLegalService
 * 
 * Gestiona todas las operaciones relacionadas con Expedientes Legales.
 * 
 * Endpoints disponibles:
 * - POST   /api/ExpedienteLegal                                    - Crear expediente legal
 * - GET    /api/ExpedienteLegal/{id}                               - Obtener por ID
 * - GET    /api/ExpedienteLegal/expediente/{expedienteId}          - Obtener por expediente
 * - PUT    /api/ExpedienteLegal/{id}                               - Actualizar datos
 * - PUT    /api/ExpedienteLegal/{id}/validar-admision              - Validar (Admisión)
 * - PUT    /api/ExpedienteLegal/{id}/autorizar-jefe-guardia        - Autorizar (Jefe Guardia)
 * - POST   /api/ExpedienteLegal/{id}/documentos/upload             - Upload documento PDF
 * - GET    /api/ExpedienteLegal/{id}/documentos                    - Listar documentos
 * - GET    /api/ExpedienteLegal/documentos/{documentoId}/download  - Descargar PDF
 * - DELETE /api/ExpedienteLegal/documentos/{documentoId}           - Eliminar documento
 * - GET    /api/ExpedienteLegal/pendientes-validacion              - Lista pendientes validación
 * - GET    /api/ExpedienteLegal/pendientes-autorizacion            - Lista pendientes autorización
 */
@Injectable({
  providedIn: 'root'
})
export class ExpedienteLegal {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/ExpedienteLegal';

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('sgm_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // ========================================
  // CRUD BÁSICO EXPEDIENTE LEGAL
  // ========================================

  /**
   * Crea un nuevo expediente legal
   * POST /api/ExpedienteLegal
   * Roles: Admision, Administrador
   */
  /**
   * Obtiene TODOS los expedientes legales (para la Bandeja/Lista).
   
   */
  listarExpedientes(): Observable<ExpedienteLegalDTO[]> {
    return this.http.get<ExpedienteLegalDTO[]>(
      this.apiUrl,
      { headers: this.getHeaders() }
    ).pipe(
      // Mapeamos cada elemento de la lista para corregir las fechas
      map(lista => lista.map(item => this.mapearFechas(item))),
      catchError(this.handleError)
    );
  }
  crear(dto: CreateExpedienteLegalDTO): Observable<ExpedienteLegalDTO> {
    return this.http.post<ExpedienteLegalDTO>(
      this.apiUrl,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene un expediente legal por su ID
   * GET /api/ExpedienteLegal/{id}
   * Roles: Todos autenticados
   */
  obtenerPorId(id: number): Observable<ExpedienteLegalDTO> {
    return this.http.get<ExpedienteLegalDTO>(
      `${this.apiUrl}/${id}`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene el expediente legal asociado a un expediente
   * GET /api/ExpedienteLegal/expediente/{expedienteId}
   * Roles: Todos autenticados
   */
  obtenerPorExpediente(expedienteId: number): Observable<ExpedienteLegalDTO | null> {
    return this.http.get<ExpedienteLegalDTO>(
      `${this.apiUrl}/expediente/${expedienteId}`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response ? this.mapearFechas(response) : null),
      catchError(error => {
        if (error.status === 404) {
          return [null];
        }
        return this.handleError(error);
      })
    );
  }

  /**
   * Actualiza datos del expediente legal
   * PUT /api/ExpedienteLegal/{id}
   * Roles: Admision, Administrador
   */
  actualizar(id: number, dto: UpdateExpedienteLegalDTO): Observable<ExpedienteLegalDTO> {
    return this.http.put<ExpedienteLegalDTO>(
      `${this.apiUrl}/${id}`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  // ========================================
  // VALIDACIONES Y AUTORIZACIONES
  // ========================================

  /**
   * Valida o rechaza el expediente legal (Admisión)
   * PUT /api/ExpedienteLegal/{id}/validar-admision
   * Roles: Admision, Administrador
   */
  validarAdmision(dto: ValidarAdmisionDTO): Observable<ExpedienteLegalDTO> {
    return this.http.put<ExpedienteLegalDTO>(
      `${this.apiUrl}/${dto.expedienteLegalID}/validar-admision`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Autoriza el expediente legal (Jefe de Guardia)
   * PUT /api/ExpedienteLegal/{id}/autorizar-jefe-guardia
   * Roles: JefeGuardia, Administrador
   */
  autorizarJefeGuardia(dto: AutorizarJefeGuardiaDTO): Observable<ExpedienteLegalDTO> {
    return this.http.put<ExpedienteLegalDTO>(
      `${this.apiUrl}/${dto.expedienteLegalID}/autorizar-jefe-guardia`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }

  // ========================================
  // GESTIÓN DE DOCUMENTOS PDF
  // ========================================

  /**
 * Sube un documento al expediente legal.
 * Adaptado para el frontend: tipoDocumento, numeroDocumento, archivo.
 */
  subirDocumento(
    expedienteLegalId: number,
    tipoDocumento: string,
    numeroDocumento: string,
    archivo: File
  ): Observable<DocumentoLegalDTO> {
    const formData = new FormData();

    // 1. Registrar documento primero
    return this.http.post<DocumentoLegalDTO>(
      `${this.apiUrl}/${expedienteLegalId}/documentos`,
      { tipoDocumento },
      { headers: this.getHeaders() }
    ).pipe(
      switchMap(documento => {
        // 2. Subir archivo al documento creado
        formData.append('archivo', archivo);

        return this.http.post<DocumentoLegalDTO>(
          `${this.apiUrl}/${expedienteLegalId}/documentos/${documento.documentoLegalID}/upload`,
          formData,
          {
            headers: new HttpHeaders({
              'Authorization': `Bearer ${localStorage.getItem('sgm_token')}`
            })
          }
        );
      }),
      map(response => this.mapearFechasDocumento(response)),
      catchError(this.handleError)
    );
  }

  /**
   * Lista todos los documentos de un expediente legal
   * GET /api/ExpedienteLegal/{id}/documentos
   * Roles: Todos autenticados
   */
  listarDocumentos(expedienteLegalId: number): Observable<DocumentoLegalDTO[]> {
    return this.http.get<DocumentoLegalDTO[]>(
      `${this.apiUrl}/${expedienteLegalId}/documentos`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(d => this.mapearFechasDocumento(d))),
      catchError(this.handleError)
    );
  }

  /**
   * Descarga un documento PDF
   * GET /api/ExpedienteLegal/documentos/{documentoId}/download
   * Roles: Todos autenticados
   */
  downloadDocumento(documentoId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/documentos/${documentoId}/download`,
      {
        headers: this.getHeaders(),
        responseType: 'blob'
      }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Helper para descargar y abrir PDF en nueva pestaña
   */
  descargarYAbrirDocumento(documentoId: number, nombreArchivo: string): void {
    this.downloadDocumento(documentoId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = nombreArchivo;
        link.click();
        // Liberar URL
        setTimeout(() => window.URL.revokeObjectURL(url), 100);
      },
      error: (error) => {
        console.error('[ExpedienteLegalService] Error al descargar documento:', error);
        alert('Error al descargar el documento PDF');
      }
    });
  }

  /**
   * Abre un documento PDF en nueva pestaña (sin descargar)
   */
  abrirDocumentoEnNuevaPestaña(documentoId: number): void {
    this.downloadDocumento(documentoId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        // Liberar URL después de un tiempo
        setTimeout(() => window.URL.revokeObjectURL(url), 100);
      },
      error: (error) => {
        console.error('[ExpedienteLegalService] Error al abrir documento:', error);
        alert('Error al abrir el documento PDF');
      }
    });
  }

  /**
   * Elimina un documento del expediente legal
   * DELETE /api/ExpedienteLegal/documentos/{documentoId}
   * Roles: Admision, Administrador
   */
  eliminarDocumento(documentoId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/documentos/${documentoId}`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  // ========================================
  // LISTAS Y CONSULTAS
  // ========================================

  /**
   * Obtiene lista de expedientes pendientes de validación por Admisión
   * GET /api/ExpedienteLegal/pendientes-validacion
   * Roles: Admision, Administrador
   */
  obtenerPendientesValidacion(): Observable<ExpedienteLegalDTO[]> {
    return this.http.get<ExpedienteLegalDTO[]>(
      `${this.apiUrl}/pendientes-validacion`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(e => this.mapearFechas(e))),
      catchError(this.handleError)
    );
  }

  /**
   * Obtiene lista de expedientes pendientes de autorización por Jefe de Guardia
   * GET /api/ExpedienteLegal/pendientes-autorizacion
   * Roles: JefeGuardia, Administrador
   */
  obtenerPendientesAutorizacion(): Observable<ExpedienteLegalDTO[]> {
    return this.http.get<ExpedienteLegalDTO[]>(
      `${this.apiUrl}/pendientes-autorizacion`,
      { headers: this.getHeaders() }
    ).pipe(
      map(response => response.map(e => this.mapearFechas(e))),
      catchError(this.handleError)
    );
  }

  // ========================================
  // HELPERS PRIVADOS
  // ========================================

  /**
   * Mapea las fechas de string a Date (expediente legal)
   */
  private mapearFechas(dto: ExpedienteLegalDTO): ExpedienteLegalDTO {
    return {
      ...dto,
      fechaCreacion: new Date(dto.fechaCreacion),
      fechaUltimaActualizacion: dto.fechaUltimaActualizacion? new Date(dto.fechaUltimaActualizacion) : undefined,
      fechaValidacionAdmision: dto.fechaValidacionAdmision ? new Date(dto.fechaValidacionAdmision) : undefined,
      fechaAutorizacion: dto.fechaAutorizacion ? new Date(dto.fechaAutorizacion) : undefined,
      documentos: dto.documentos?.map(d => this.mapearFechasDocumento(d)) || []
    };
  }

  /**
   * Mapea las fechas de string a Date (documento legal)
   */
  private mapearFechasDocumento(dto: DocumentoLegalDTO): DocumentoLegalDTO {
    return {
      ...dto,
      fechaCarga: new Date(dto.fechaCarga)
    };
  }

  /**
   * Manejo centralizado de errores
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'Error desconocido';

    if (error.error instanceof ErrorEvent) {
      // Error del lado del cliente
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.error instanceof Blob) {
      // Error en descarga de PDF
      errorMessage = 'Error al descargar el documento PDF';
    } else {
      // Error del lado del servidor
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.error?.title) {
        errorMessage = error.error.title;
      } else if (error.message) {
        errorMessage = error.message;
      } else {
        errorMessage = `Error ${error.status}: ${error.statusText}`;
      }
    }

    console.error('[ExpedienteLegalService] Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }

  // ========================================
  // UTILIDADES
  // ========================================

  /**
   * Obtiene el usuario ID del token JWT
   */
  obtenerUsuarioId(): number {
    const user = localStorage.getItem('sgm_user_id');
    return user ? parseInt(user, 10) : 0;
  }

  /**
   * Valida si el usuario tiene rol de Admisión
   */
  esAdmision(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('Admision') || roles?.includes('Administrador') || false;
  }

  /**
   * Valida si el usuario tiene rol de Jefe de Guardia
   */
  esJefeGuardia(): boolean {
    const roles = localStorage.getItem('sgm_roles');
    return roles?.includes('JefeGuardia') || roles?.includes('Administrador') || false;
  }
  /**
 * Agrega una autoridad externa al expediente legal.
 * POST /api/ExpedienteLegal/{id}/autoridades
 */
  agregarAutoridad(dto: CreateAutoridadExternaDTO): Observable<AutoridadExternaDTO> {
    return this.http.post<AutoridadExternaDTO>(
      `${this.apiUrl}/${dto.expedienteLegalID}/autoridades`,
      dto,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Elimina una autoridad externa del expediente legal.
   * DELETE /api/ExpedienteLegal/autoridades/{autoridadId}
   */
  eliminarAutoridad(autoridadId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/autoridades/${autoridadId}`,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Marca el expediente legal como listo para validación de Admisión.
   * POST /api/ExpedienteLegal/{id}/marcar-listo-admision
   * Transición: EnRegistro → PendienteValidacionAdmision
   */
  marcarListoAdmision(expedienteLegalId: number, observaciones?: string): Observable<ExpedienteLegalDTO> {
    return this.http.post<ExpedienteLegalDTO>(
      `${this.apiUrl}/${expedienteLegalId}/marcar-listo-admision`,
      { observaciones },
      { headers: this.getHeaders() }
    ).pipe(
      map(response => this.mapearFechas(response)),
      catchError(this.handleError)
    );
  }
  /**
   * Valida extensión de archivo
   */
  validarExtensionArchivo(nombreArchivo: string): { valido: boolean; error?: string } {
    const extension = nombreArchivo.split('.').pop()?.toLowerCase();

    if (!extension) {
      return { valido: false, error: 'El archivo no tiene extensión' };
    }

    if (extension !== 'pdf') {
      return { valido: false, error: 'Solo se permiten archivos PDF' };
    }

    return { valido: true };
  }

  /**
   * Valida tamaño de archivo (máximo 5MB)
   */
  validarTamanoArchivo(tamanoBytes: number): { valido: boolean; error?: string } {
    const maxBytes = 5 * 1024 * 1024; // 5MB

    if (tamanoBytes === 0) {
      return { valido: false, error: 'El archivo está vacío' };
    }

    if (tamanoBytes > maxBytes) {
      return {
        valido: false,
        error: `El archivo excede el tamaño máximo permitido (5MB). Tamaño actual: ${this.formatearTamano(tamanoBytes)}`
      };
    }

    return { valido: true };
  }

  /**
   * Formatea el tamaño de archivo
   */
  private formatearTamano(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
