import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// ═══════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════

export enum TipoDocumentoExpediente {
  DNI_Familiar = 1,
  DNI_Fallecido = 2,
  CertificadoDefuncion = 3,
  OficioLegal = 4,
  ActaLevantamiento = 5,
  Otro = 6
}

export enum EstadoDocumentoExpediente {
  PendienteVerificacion = 1,
  Verificado = 2,
  Rechazado = 3
}

// ═══════════════════════════════════════════════════════════
// INTERFACES - RESPUESTA
// ═══════════════════════════════════════════════════════════

/**
 * Documento digitalizado del expediente (respuesta del backend)
 */
export interface DocumentoExpedienteDTO {
  documentoExpedienteID: number;
  expedienteID: number;

  // Clasificación
  tipoDocumento: TipoDocumentoExpediente;
  tipoDocumentoDescripcion: string;
  estado: EstadoDocumentoExpediente | string;
  estadoDescripcion: string;

  // Archivo
  nombreArchivo: string;
  extensionArchivo: string;
  tamanioLegible: string;

  // Auditoría - Subida
  usuarioSubioNombre: string;
  fechaHoraSubida: string;

  // Auditoría - Verificación
  usuarioVerificoNombre?: string;
  fechaHoraVerificacion?: string;

  // Observaciones
  observaciones?: string;
}

/**
 * Estado individual de un tipo de documento requerido.
 * Usado en el semáforo visual del frontend.
 */
export interface EstadoDocumentoItem {
  subido: boolean;
  verificado: boolean;
  rechazado: boolean;
  documentoID?: number;
  nombreArchivo?: string;
  observaciones?: string;
}

/**
 * Resumen del estado de documentación del expediente.
 * Determina si el botón "Crear Acta" está habilitado.
 */
export interface ResumenDocumentosDTO {
  expedienteID: number;

  /** true → habilita botón "Crear Acta" en frontend */
  documentacionCompleta: boolean;

  /** TipoSalida del ActaRetiro si existe, null si aún no se creó */
  tipoSalida?: 'Familiar' | 'AutoridadLegal' | null;

  // Documentos caso Familiar
  dniFamiliar: EstadoDocumentoItem;
  dniFallecido: EstadoDocumentoItem;
  certificadoDefuncion: EstadoDocumentoItem;

  // Documentos caso AutoridadLegal
  oficioLegal: EstadoDocumentoItem;

  // Lista completa
  documentos: DocumentoExpedienteDTO[];
}

// ═══════════════════════════════════════════════════════════
// INTERFACES - REQUESTS
// ═══════════════════════════════════════════════════════════

export interface RechazarDocumentoDTO {
  documentoExpedienteID: number;
  motivo: string;
  usuarioVerificoID: number;
}

// ═══════════════════════════════════════════════════════════
// SERVICIO
// ═══════════════════════════════════════════════════════════

/**
 * Servicio para gestión de documentos digitalizados del expediente.
 * Reemplaza el proceso manual de "3 juegos de copias físicas".
 *
 * Endpoints:
 * - GET  /api/DocumentosExpediente/expediente/{id}          → lista documentos
 * - GET  /api/DocumentosExpediente/expediente/{id}/resumen  → semáforo
 * - GET  /api/DocumentosExpediente/{id}                     → detalle
 * - POST /api/DocumentosExpediente/subir                    → upload archivo
 * - POST /api/DocumentosExpediente/{id}/verificar           → marcar verificado
 * - POST /api/DocumentosExpediente/{id}/rechazar            → marcar rechazado
 * - GET  /api/DocumentosExpediente/{id}/descargar           → descarga
 * - DELETE /api/DocumentosExpediente/{id}                   → eliminar
 */
@Injectable({
  providedIn: 'root'
})
export class DocumentoExpedienteService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/DocumentosExpediente';

  // ═══════════════════════════════════════════════════════════
  // CONSULTAS
  // ═══════════════════════════════════════════════════════════

  /**
   * Obtiene todos los documentos de un expediente
   */
  obtenerPorExpediente(expedienteId: number): Observable<DocumentoExpedienteDTO[]> {
    return this.http.get<DocumentoExpedienteDTO[]>(
      `${this.apiUrl}/expediente/${expedienteId}`
    );
  }

  /**
   * Obtiene el resumen de documentación del expediente.
   * Incluye semáforo visual por tipo de documento y DocumentacionCompleta.
   */
  obtenerResumen(expedienteId: number): Observable<ResumenDocumentosDTO> {
    return this.http.get<ResumenDocumentosDTO>(
      `${this.apiUrl}/expediente/${expedienteId}/resumen`
    );
  }

  /**
   * Obtiene un documento por su ID
   */
  obtenerPorId(documentoId: number): Observable<DocumentoExpedienteDTO> {
    return this.http.get<DocumentoExpedienteDTO>(`${this.apiUrl}/${documentoId}`);
  }

  // ═══════════════════════════════════════════════════════════
  // SUBIDA
  // ═══════════════════════════════════════════════════════════

  /**
   * Sube un documento digitalizado al expediente.
   * Usa multipart/form-data — no JSON.
   * Formatos: .pdf, .jpg, .jpeg, .png — Máximo 5MB.
   */
  subirDocumento(
    expedienteId: number,
    tipoDocumento: TipoDocumentoExpediente,
    archivo: File,
    observaciones?: string
  ): Observable<DocumentoExpedienteDTO> {
    const formData = new FormData();
    formData.append('expedienteId', expedienteId.toString());
    formData.append('tipoDocumento', tipoDocumento.toString());
    formData.append('archivo', archivo, archivo.name);
    if (observaciones) {
      formData.append('observaciones', observaciones);
    }

    return this.http.post<DocumentoExpedienteDTO>(
      `${this.apiUrl}/subir`,
      formData
    );
  }

  // ═══════════════════════════════════════════════════════════
  // VERIFICACIÓN / RECHAZO
  // ═══════════════════════════════════════════════════════════

  /**
   * Marca un documento como verificado contra el original físico.
   * Actualiza DocumentacionCompleta del expediente automáticamente.
   */
  verificar(documentoId: number, observaciones?: string): Observable<DocumentoExpedienteDTO> {
    return this.http.post<DocumentoExpedienteDTO>(
      `${this.apiUrl}/${documentoId}/verificar`,
      observaciones ?? null
    );
  }

  /**
   * Rechaza un documento indicando el motivo.
   * El familiar deberá presentar nuevamente el documento.
   */
  rechazar(documentoId: number, motivo: string): Observable<DocumentoExpedienteDTO> {
    const dto: Partial<RechazarDocumentoDTO> = { motivo };
    return this.http.post<DocumentoExpedienteDTO>(
      `${this.apiUrl}/${documentoId}/rechazar`,
      dto
    );
  }

  // ═══════════════════════════════════════════════════════════
  // DESCARGA / ELIMINACIÓN
  // ═══════════════════════════════════════════════════════════

  /**
   * Descarga el archivo de un documento como Blob
   */
  descargar(documentoId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${documentoId}/descargar`,
      { responseType: 'blob' }
    );
  }

  /**
   * Elimina un documento.
   * Solo permitido si Estado == PendienteVerificacion o Rechazado.
   */
  eliminar(documentoId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${documentoId}`);
  }

  // ═══════════════════════════════════════════════════════════
  // HELPERS
  // ═══════════════════════════════════════════════════════════

  /**
   * Descarga un Blob como archivo en el navegador
   */
  descargarArchivo(blob: Blob, nombreArchivo: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Valida formato del archivo antes de subir
   * Formatos permitidos: .pdf, .jpg, .jpeg, .png
   */
  validarFormato(archivo: File): boolean {
    const extensionesPermitidas = ['.pdf', '.jpg', '.jpeg', '.png'];
    const extension = '.' + archivo.name.split('.').pop()?.toLowerCase();
    return extensionesPermitidas.includes(extension);
  }

  /**
   * Valida tamaño del archivo (máximo 5MB)
   */
  validarTamanio(archivo: File): boolean {
    const maxBytes = 5 * 1024 * 1024;
    return archivo.size <= maxBytes;
  }

  /**
   * Formatea bytes a texto legible (KB, MB)
   */
  formatearTamanio(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Obtiene el color del badge según el estado del documento
   */
  obtenerColorEstado(estado: EstadoDocumentoExpediente): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado:
        return 'bg-green-100 text-green-800 border-green-300';
      case EstadoDocumentoExpediente.Rechazado:
        return 'bg-red-100 text-red-800 border-red-300';
      case EstadoDocumentoExpediente.PendienteVerificacion:
      default:
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
    }
  }

  /**
   * Obtiene el icono según el estado del documento
   */
  obtenerIconoEstado(estado: EstadoDocumentoExpediente): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado:
        return 'check-circle';
      case EstadoDocumentoExpediente.Rechazado:
        return 'x-circle';
      case EstadoDocumentoExpediente.PendienteVerificacion:
      default:
        return 'clock';
    }
  }

  /**
   * Obtiene la descripción del tipo de documento
   */
  obtenerDescripcionTipo(tipo: TipoDocumentoExpediente): string {
    switch (tipo) {
      case TipoDocumentoExpediente.DNI_Familiar:
        return 'DNI del Familiar';
      case TipoDocumentoExpediente.DNI_Fallecido:
        return 'DNI del Fallecido';
      case TipoDocumentoExpediente.CertificadoDefuncion:
        return 'Certificado de Defunción (SINADEF)';
      case TipoDocumentoExpediente.OficioLegal:
        return 'Oficio Legal (PNP/Fiscal)';
      case TipoDocumentoExpediente.ActaLevantamiento:
        return 'Acta de Levantamiento';
      case TipoDocumentoExpediente.Otro:
        return 'Documento Adicional';
      default:
        return 'Documento';
    }
  }
}
