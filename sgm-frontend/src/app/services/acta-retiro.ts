import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * DTOs para Acta de Retiro (Soporta Familiar y AutoridadLegal)
 */

// ═══════════════════════════════════════════════════════════
// DTO PARA CREAR ACTA
// ═══════════════════════════════════════════════════════════
export interface CreateActaRetiroDTO {
  expedienteID: number;

  // ─── DOCUMENTO LEGAL (CONDICIONAL SEGÚN TIPO) ───────────
  /** N° Certificado SINADEF - OBLIGATORIO si tipoSalida = Familiar Y no hay medicoExternoNombre */
  numeroCertificadoDefuncion?: string;
  /** N° Oficio Legal - OBLIGATORIO si tipoSalida = AutoridadLegal */
  numeroOficioPolicial?: string;

  // ─── DATOS DEL FALLECIDO ────────────────────────────────
  nombreCompletoFallecido: string;
  historiaClinica: string;
  tipoDocumentoFallecido: number;
  numeroDocumentoFallecido: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: string;

  // ─── MÉDICO CERTIFICANTE (hospital) ─────────────────────
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string;

  // ─── MÉDICO EXTERNO (opcional) ───────────────────────────
  /** Aplica cuando causaViolentaODudosa = false y familia trae médico de cabecera */
  medicoExternoNombre?: string;
  medicoExternoCMP?: string;

  // ─── JEFE DE GUARDIA ─────────────────────────────────────
  jefeGuardiaNombre: string;
  jefeGuardiaCMP: string;

  // ─── TIPO DE SALIDA ──────────────────────────────────────
  tipoSalida: 'Familiar' | 'AutoridadLegal';

  // ─── FAMILIAR (OBLIGATORIO si tipoSalida = Familiar) ────
  familiarApellidoPaterno?: string;
  familiarApellidoMaterno?: string;
  familiarNombres?: string;
  familiarTipoDocumento?: number;
  familiarNumeroDocumento?: string;
  familiarParentesco?: string;
  familiarTelefono?: string;

  // ─── AUTORIDAD LEGAL (OBLIGATORIO si tipoSalida = AutoridadLegal) ───
  autoridadApellidoPaterno?: string;
  autoridadApellidoMaterno?: string;
  autoridadNombres?: string;
  tipoAutoridad?: number; // 1=Policia, 2=Fiscal, 3=MedicoLegista
  autoridadTipoDocumento?: number;
  autoridadNumeroDocumento?: string;
  /** Grado y cargo. Ej: "SO3 PNP", "Fiscal Provincial" */
  autoridadCargo?: string;
  /** Comisaría o institución. Ej: "Comisaría San Antonio" */
  autoridadInstitucion?: string;
  autoridadTelefono?: string;

  // ─── DATOS ADICIONALES ───────────────────────────────────
  datosAdicionales?: string;
  destino?: string;
  observaciones?: string;

  // ─── USUARIO ─────────────────────────────────────────────
  usuarioAdmisionID: number;
}

// ═══════════════════════════════════════════════════════════
// DTO PARA AUTORIZAR BYPASS DE DEUDA
// Solo roles JefeGuardia y Administrador.
// El usuarioAutorizaID viene del JWT en el backend.
// ═══════════════════════════════════════════════════════════
export interface AutorizarBypassDeudaRequest {
  /** ID del expediente con deuda pendiente */
  expedienteID: number;
  /**
   * Justificación obligatoria (mín. 10 chars).
   * Ej: "PNP retira cuerpo sin familiar — caso legal urgente"
   */
  justificacion: string;
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

  // ─── DOCUMENTO LEGAL ─────────────────────────────────────
  numeroCertificadoDefuncion?: string;
  numeroOficioPolicial?: string;

  // ─── DATOS DEL FALLECIDO ────────────────────────────────
  nombreCompletoFallecido: string;
  historiaClinica: string;
  tipoDocumentoFallecido: string;
  numeroDocumentoFallecido: string;
  servicioFallecimiento: string;
  fechaHoraFallecimiento: Date;

  /**
   * Edad al momento del fallecimiento.
   * Leída desde Expediente — digitaliza cuaderno VigSup.
   */
  edad: number;

  /**
   * Diagnóstico final CIE-10.
   * Leído desde Expediente — digitaliza cuaderno VigSup.
   */
  diagnosticoFinal?: string;

  /**
   * Indica si la causa es violenta o dudosa.
   * true → bloquea médico externo y SINADEF en el formulario.
   */
  causaViolentaODudosa: boolean;

  /** Tipo de expediente: "Interno" | "Externo" */
  tipoExpediente: string;

  // ─── MÉDICO CERTIFICANTE (hospital) ─────────────────────
  medicoCertificaNombre: string;
  medicoCMP: string;
  medicoRNE?: string;

  // ─── MÉDICO EXTERNO (opcional) ───────────────────────────
  /** Solo cuando causaViolentaODudosa = false */
  medicoExternoNombre?: string;
  medicoExternoCMP?: string;

  // ─── JEFE DE GUARDIA ─────────────────────────────────────
  jefeGuardiaNombre: string;
  jefeGuardiaCMP: string;

  // ─── TIPO DE SALIDA ──────────────────────────────────────
  tipoSalida: string; // "Familiar" | "AutoridadLegal"

  // ─── FAMILIAR ────────────────────────────────────────────
  familiarApellidoPaterno?: string;
  familiarApellidoMaterno?: string;
  familiarNombres?: string;
  familiarNombreCompleto?: string;
  familiarTipoDocumento?: string;
  familiarNumeroDocumento?: string;
  familiarParentesco?: string;
  familiarTelefono?: string;

  // ─── AUTORIDAD LEGAL ─────────────────────────────────────
  autoridadApellidoPaterno?: string;
  autoridadApellidoMaterno?: string;
  autoridadNombres?: string;
  autoridadNombreCompleto?: string;
  tipoAutoridad?: string; // "Policia" | "Fiscal" | "MedicoLegista"
  autoridadTipoDocumento?: string;
  autoridadNumeroDocumento?: string;
  autoridadCargo?: string;
  autoridadInstitucion?: string;
  autoridadTelefono?: string;

  // ─── BYPASS DE DEUDA ─────────────────────────────────────
  /** true si JG/Admin autorizó retiro con deudas pendientes */
  bypassDeudaAutorizado: boolean;
  bypassDeudaJustificacion?: string;
  /** Nombre del usuario que autorizó el bypass */
  bypassDeudaUsuarioNombre?: string;
  bypassDeudaFecha?: Date;

  // ─── DATOS ADICIONALES ───────────────────────────────────
  datosAdicionales?: string;
  destino?: string;
  observaciones?: string;

  // ─── FIRMAS ──────────────────────────────────────────────
  firmadoResponsable: boolean;
  fechaFirmaResponsable?: Date;
  firmadoAdmisionista: boolean;
  fechaFirmaAdmisionista?: Date;
  firmadoSupervisorVigilancia: boolean;
  fechaSupervisorVigilancia?: Date;
  /** Helper: Nombre del responsable que firmó (calculado en backend) */
  nombreResponsableFirma?: string;

  // ─── ARCHIVOS PDF ────────────────────────────────────────
  rutaPDFSinFirmar?: string;
  nombreArchivoPDFSinFirmar?: string;
  tamañoPDFSinFirmarLegible?: string;
  rutaPDFFirmado?: string;
  nombreArchivoPDFFirmado?: string;
  tamañoPDFFirmadoLegible?: string;

  // ─── ESTADO (computed desde backend) ─────────────────────
  estaCompleta: boolean;
  tieneTodasLasFirmas: boolean;
  tienePDFFirmado: boolean;

  // ─── AUDITORÍA ───────────────────────────────────────────
  usuarioAdmisionNombre: string;
  fechaRegistro: Date;
  usuarioSubidaPDFNombre?: string;
  fechaSubidaPDF?: Date;

  // ─── RELACIÓN CON SALIDA ─────────────────────────────────
  salidaMortuorioID?: number;
  fechaHoraSalida?: Date;
}

/**
 * Servicio para gestión de Actas de Retiro
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

  /** Crea una nueva acta de retiro (Familiar o AutoridadLegal) */
  crear(dto: CreateActaRetiroDTO): Observable<ActaRetiroDTO> {
    return this.http.post<ActaRetiroDTO>(this.apiUrl, dto);
  }

  // ═══════════════════════════════════════════════════════════
  // BYPASS DE DEUDA
  // Solo JefeGuardia y Administrador pueden llamar este endpoint.
  // El backend valida el rol desde el JWT.
  // ═══════════════════════════════════════════════════════════

  /**
   * Autoriza excepcionalmente el retiro con deudas pendientes.
   * Solo aplica para TipoSalidaPreliminar = AutoridadLegal.
   * Cubre AMBAS deudas (económica y sangre).
   */
  autorizarBypassDeuda(dto: AutorizarBypassDeudaRequest): Observable<{ mensaje: string; expedienteID: number; autorizadoPor: string; fecha: Date }> {
    return this.http.post<{ mensaje: string; expedienteID: number; autorizadoPor: string; fecha: Date }>(
      `${this.apiUrl}/autorizar-bypass-deuda`,
      dto
    );
  }

  // ═══════════════════════════════════════════════════════════
  // GENERAR Y REIMPRIMIR PDF
  // ═══════════════════════════════════════════════════════════

  /** Genera el PDF sin firmar del acta. Retorna Blob para descarga. */
  generarPDF(actaRetiroID: number): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/${actaRetiroID}/generar-pdf`,
      {},
      { responseType: 'blob' }
    );
  }

  /** Reimprime el PDF sin firmar del acta de retiro */
  reimprimirPDFPorExpediente(expedienteID: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/expediente/${expedienteID}/reimprimir-pdf`,
      { responseType: 'blob' }
    );
  }

  // ═══════════════════════════════════════════════════════════
  // SUBIR PDF FIRMADO
  // ═══════════════════════════════════════════════════════════

  /** Sube el PDF firmado escaneado */
  subirPDFFirmado(dto: UpdateActaRetiroPDFDTO): Observable<ActaRetiroDTO> {
    return this.http.post<ActaRetiroDTO>(`${this.apiUrl}/subir-pdf-firmado`, dto);
  }

  // ═══════════════════════════════════════════════════════════
  // CONSULTAS
  // ═══════════════════════════════════════════════════════════

  obtenerPorId(actaRetiroID: number): Observable<ActaRetiroDTO> {
    return this.http.get<ActaRetiroDTO>(`${this.apiUrl}/${actaRetiroID}`);
  }

  obtenerPorExpediente(expedienteID: number): Observable<ActaRetiroDTO> {
    return this.http.get<ActaRetiroDTO>(`${this.apiUrl}/expediente/${expedienteID}`);
  }

  existeActa(expedienteID: number): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/existe/${expedienteID}`);
  }

  verificarCertificadoSINADEF(numeroCertificado: string): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/existe-certificado/${encodeURIComponent(numeroCertificado)}`
    );
  }

  verificarOficioLegal(numeroOficio: string): Observable<boolean> {
    return this.http.get<boolean>(
      `${this.apiUrl}/existe-oficio/${encodeURIComponent(numeroOficio)}`
    );
  }

  // ═══════════════════════════════════════════════════════════
  // HELPERS
  // ═══════════════════════════════════════════════════════════

  /** Descarga un PDF (Blob) como archivo */
  descargarPDF(blob: Blob, nombreArchivo: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /** Convierte un File a Base64 (para subir PDFs) */
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

  obtenerTamañoLegible(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  obtenerDescripcionTipoAutoridad(tipo?: string): string {
    if (!tipo) return 'No especificado';
    const descripciones: Record<string, string> = {
      'Policia': 'Policía Nacional del Perú (PNP)',
      'Fiscal': 'Ministerio Público - Fiscalía',
      'MedicoLegista': 'Médico Legista'
    };
    return descripciones[tipo] || tipo;
  }

  generarNombreCompleto(
    apellidoPaterno: string,
    apellidoMaterno: string | undefined,
    nombres: string
  ): string {
    const apellidos = [apellidoPaterno, apellidoMaterno].filter(Boolean).join(' ');
    return `${apellidos}, ${nombres}`.trim();
  }
}
