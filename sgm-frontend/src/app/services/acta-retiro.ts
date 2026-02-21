import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * DTOs para Acta de Retiro (Soporta Familiar y AutoridadLegal)
 */

// ═══════════════════════════════════════════════════════════
// DTO PARA CREAR ACTA
// ═══════════════════════════════════════════════════════════
export interface CreateActaRetiroDTO {
  expedienteID: number;

  // ─────────────────────────────────────────────────────────
  // DOCUMENTO LEGAL (CONDICIONAL SEGÚN TIPO)
  // ─────────────────────────────────────────────────────────

  /** N° Certificado SINADEF - OBLIGATORIO si tipoSalida = Familiar */
  numeroCertificadoDefuncion?: string;

  /** N° Oficio Legal - OBLIGATORIO si tipoSalida = AutoridadLegal */
  numeroOficioLegal?: string;

  // ─────────────────────────────────────────────────────────
  // DATOS DEL FALLECIDO
  // ─────────────────────────────────────────────────────────

  nombreCompletoFallecido: string;
  historiaClinica: string;
  tipoDocumentoFallecido: number;
  numeroDocumentoFallecido: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: string;

  // ─────────────────────────────────────────────────────────
  // MÉDICO CERTIFICANTE
  // ─────────────────────────────────────────────────────────

  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string;

  // ─────────────────────────────────────────────────────────
  // JEFE DE GUARDIA
  // ─────────────────────────────────────────────────────────

  jefeGuardiaNombre: string;
  jefeGuardiaCMP: string;

  // ─────────────────────────────────────────────────────────
  // TIPO DE SALIDA
  // ─────────────────────────────────────────────────────────

  tipoSalida: 'Familiar' | 'AutoridadLegal';

  // ─────────────────────────────────────────────────────────
  // FAMILIAR (OBLIGATORIO si tipoSalida = Familiar)
  // ─────────────────────────────────────────────────────────

  familiarApellidoPaterno?: string;
  familiarApellidoMaterno?: string;
  familiarNombres?: string;
  familiarTipoDocumento?: number;
  familiarNumeroDocumento?: string;
  familiarParentesco?: string;
  familiarTelefono?: string;

  // ─────────────────────────────────────────────────────────
  // AUTORIDAD LEGAL (OBLIGATORIO si tipoSalida = AutoridadLegal)
  // ─────────────────────────────────────────────────────────

  autoridadApellidoPaterno?: string;
  autoridadApellidoMaterno?: string;
  autoridadNombres?: string;
  tipoAutoridad?: number; // 1=Policia, 2=Fiscal, 3=MedicoLegista
  autoridadTipoDocumento?: number;
  autoridadNumeroDocumento?: string;
  autoridadCargo?: string;
  autoridadInstitucion?: string;
  autoridadPlacaVehiculo?: string;
  autoridadTelefono?: string;

  // ─────────────────────────────────────────────────────────
  // DATOS ADICIONALES
  // ─────────────────────────────────────────────────────────

  datosAdicionales?: string;
  destino?: string;
  observaciones?: string;

  // ─────────────────────────────────────────────────────────
  // USUARIO
  // ─────────────────────────────────────────────────────────

  usuarioAdmisionID: number;
}

// ═══════════════════════════════════════════════════════════
// DTO PARA SUBIR PDF FIRMADO
// ═══════════════════════════════════════════════════════════
export interface UpdateActaRetiroPDFDTO {
  actaRetiroID: number;
  rutaPDFFirmado: string;
  nombreArchivoPDFFirmado: string;
  tamañoPDFFirmado: number;
  usuarioSubidaPDFID: number;
  observaciones?: string;
}

// ═══════════════════════════════════════════════════════════
// DTO DE RESPUESTA (desde backend)
// ═══════════════════════════════════════════════════════════
export interface ActaRetiroDTO {
  actaRetiroID: number;
  expedienteID: number;
  codigoExpediente: string;

  // ─────────────────────────────────────────────────────────
  // DOCUMENTO LEGAL
  // ─────────────────────────────────────────────────────────

  numeroCertificadoDefuncion?: string;
  numeroOficioLegal?: string;

  // ─────────────────────────────────────────────────────────
  // DATOS DEL FALLECIDO
  // ─────────────────────────────────────────────────────────

  nombreCompletoFallecido: string;
  historiaClinica: string;
  tipoDocumentoFallecido: string;
  numeroDocumentoFallecido: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: Date;

  // ─────────────────────────────────────────────────────────
  // MÉDICO CERTIFICANTE
  // ─────────────────────────────────────────────────────────

  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string;

  // ─────────────────────────────────────────────────────────
  // JEFE DE GUARDIA
  // ─────────────────────────────────────────────────────────

  jefeGuardiaNombre: string;
  jefeGuardiaCMP: string;

  // ─────────────────────────────────────────────────────────
  // TIPO DE SALIDA
  // ─────────────────────────────────────────────────────────

  tipoSalida: string; // "Familiar" | "AutoridadLegal"

  // ─────────────────────────────────────────────────────────
  // FAMILIAR
  // ─────────────────────────────────────────────────────────

  familiarApellidoPaterno?: string;
  familiarApellidoMaterno?: string;
  familiarNombres?: string;
  familiarNombreCompleto?: string;
  familiarTipoDocumento?: string;
  familiarNumeroDocumento?: string;
  familiarParentesco?: string;
  familiarTelefono?: string;

  // ─────────────────────────────────────────────────────────
  // AUTORIDAD LEGAL
  // ─────────────────────────────────────────────────────────

  autoridadApellidoPaterno?: string;
  autoridadApellidoMaterno?: string;
  autoridadNombres?: string;
  autoridadNombreCompleto?: string;
  tipoAutoridad?: string; // "Policia" | "Fiscal" | "MedicoLegista"
  autoridadTipoDocumento?: string;
  autoridadNumeroDocumento?: string;
  autoridadCargo?: string;
  autoridadInstitucion?: string;
  autoridadPlacaVehiculo?: string;
  autoridadTelefono?: string;

  // ─────────────────────────────────────────────────────────
  // DATOS ADICIONALES
  // ─────────────────────────────────────────────────────────

  datosAdicionales?: string;
  destino?: string;
  observaciones?: string;

  // ─────────────────────────────────────────────────────────
  // FIRMAS
  // ─────────────────────────────────────────────────────────

  firmadoResponsable: boolean;
  fechaFirmaResponsable?: Date;
  firmadoAdmisionista: boolean;
  fechaFirmaAdmisionista?: Date;
  firmadoSupervisorVigilancia: boolean;
  fechaSupervisorVigilancia?: Date;

  /** Helper: Nombre del responsable que firmó (desde backend) */
  nombreResponsableFirma?: string;

  // ─────────────────────────────────────────────────────────
  // ARCHIVOS PDF
  // ─────────────────────────────────────────────────────────

  rutaPDFSinFirmar?: string;
  nombreArchivoPDFSinFirmar?: string;
  tamañoPDFSinFirmarLegible?: string;

  rutaPDFFirmado?: string;
  nombreArchivoPDFFirmado?: string;
  tamañoPDFFirmadoLegible?: string;

  // ─────────────────────────────────────────────────────────
  // ESTADO (COMPUTED PROPERTIES DEL BACKEND)
  // ─────────────────────────────────────────────────────────

  estaCompleta: boolean;
  tieneTodasLasFirmas: boolean;
  tienePDFFirmado: boolean;

  // ─────────────────────────────────────────────────────────
  // AUDITORÍA
  // ─────────────────────────────────────────────────────────

  usuarioAdmisionNombre: string;
  fechaRegistro: Date;
  usuarioSubidaPDFNombre?: string;
  fechaSubidaPDF?: Date;

  // ─────────────────────────────────────────────────────────
  // RELACIÓN CON SALIDA
  // ─────────────────────────────────────────────────────────

  salidaMortuorioID?: number;
  fechaHoraSalida?: Date;
}

/**
 * Servicio para gestión de Actas de Retiro
 * Soporta casos de Familiar y AutoridadLegal
 * 
 * Endpoints:
 * - POST /api/ActaRetiro → Crear acta
 * - POST /api/ActaRetiro/{id}/generar-pdf → Generar PDF sin firmar
 * - GET /api/ActaRetiro/expediente/{expedienteId}/reimprimir-pdf → Reimprimir PDF
 * - POST /api/ActaRetiro/subir-pdf-firmado → Subir PDF firmado
 * - GET /api/ActaRetiro/{id} → Obtener por ID
 * - GET /api/ActaRetiro/expediente/{expedienteId} → Obtener por expediente
 * - GET /api/ActaRetiro/existe/{expedienteId} → Verificar si existe
 */
@Injectable({
  providedIn: 'root'
})
export class ActaRetiroService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api/ActaRetiro';

  // ═══════════════════════════════════════════════════════════
  // CREAR
  // ═══════════════════════════════════════════════════════════

  /**
   * Crea una nueva acta de retiro (Familiar o AutoridadLegal)
   */
  crear(dto: CreateActaRetiroDTO): Observable<ActaRetiroDTO> {
    return this.http.post<ActaRetiroDTO>(this.apiUrl, dto);
  }

  // ═══════════════════════════════════════════════════════════
  // GENERAR Y REIMPRIMIR PDF
  // ═══════════════════════════════════════════════════════════

  /**
   * Genera el PDF sin firmar del acta
   * Retorna un Blob para descarga directa
   */
  generarPDF(actaRetiroID: number): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/${actaRetiroID}/generar-pdf`,
      {},
      { responseType: 'blob' }
    );
  }

  /**
   * Reimprime el PDF sin firmar del acta de retiro
   * Útil si se dañó la primera impresión o se necesita una copia adicional
   */
  reimprimirPDFPorExpediente(expedienteID: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/expediente/${expedienteID}/reimprimir-pdf`,
      { responseType: 'blob' }
    );
  }

  // ═══════════════════════════════════════════════════════════
  // SUBIR PDF FIRMADO
  // ═══════════════════════════════════════════════════════════

  /**
   * Sube el PDF firmado escaneado
   */
  subirPDFFirmado(dto: UpdateActaRetiroPDFDTO): Observable<ActaRetiroDTO> {
    return this.http.post<ActaRetiroDTO>(
      `${this.apiUrl}/subir-pdf-firmado`,
      dto
    );
  }

  // ═══════════════════════════════════════════════════════════
  // CONSULTAS
  // ═══════════════════════════════════════════════════════════

  /**
   * Obtiene un acta por ID
   */
  obtenerPorId(actaRetiroID: number): Observable<ActaRetiroDTO> {
    return this.http.get<ActaRetiroDTO>(`${this.apiUrl}/${actaRetiroID}`);
  }

  /**
   * Obtiene el acta asociada a un expediente
   */
  obtenerPorExpediente(expedienteID: number): Observable<ActaRetiroDTO> {
    return this.http.get<ActaRetiroDTO>(
      `${this.apiUrl}/expediente/${expedienteID}`
    );
  }

  /**
   * Verifica si existe un acta para un expediente
   */
  existeActa(expedienteID: number): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/existe/${expedienteID}`);
  }
  /**
 * Verifica si existe un acta con el certificado SINADEF especificado
 */
  verificarCertificadoSINADEF(numeroCertificado: string): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/existe-certificado/${encodeURIComponent(numeroCertificado)}`
    );
  }

  /**
   * Verifica si existe un acta con el número de oficio legal especificado
   */
  verificarOficioLegal(numeroOficio: string): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/existe-oficio/${encodeURIComponent(numeroOficio)}`
    );
  }
  // ═══════════════════════════════════════════════════════════
  // HELPERS
  // ═══════════════════════════════════════════════════════════

  /**
   * Helper: Descarga un PDF (Blob) como archivo
   */
  descargarPDF(blob: Blob, nombreArchivo: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Helper: Convierte un File a Base64 (para subir PDFs)
   */
  fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const base64 = (reader.result as string).split(',')[1];
        resolve(base64);
      };
      reader.onerror = () => reject(new Error('Error al leer el archivo'));
      reader.readAsDataURL(file);
    });
  }

  /**
   * Helper: Obtiene tamaño de archivo legible (KB, MB)
   */
  obtenerTamañoLegible(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Helper: Obtiene descripción del tipo de autoridad
   */
  obtenerDescripcionTipoAutoridad(tipo?: string): string {
    if (!tipo) return 'No especificado';

    const descripciones: Record<string, string> = {
      'Policia': 'Policía Nacional del Perú (PNP)',
      'Fiscal': 'Ministerio Público - Fiscalía',
      'MedicoLegista': 'Médico Legista'
    };

    return descripciones[tipo] || tipo;
  }

  /**
   * Helper: Genera el nombre completo desde apellidos y nombres
   */
  generarNombreCompleto(
    apellidoPaterno: string,
    apellidoMaterno: string | undefined,
    nombres: string
  ): string {
    const apellidos = [apellidoPaterno, apellidoMaterno].filter(Boolean).join(' ');
    return `${apellidos}, ${nombres}`.trim();
  }
}
