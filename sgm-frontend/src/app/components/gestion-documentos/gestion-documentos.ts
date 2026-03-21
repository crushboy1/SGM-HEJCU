import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../icon/icon.component';
import { ExpedienteService } from '../../services/expediente';
import {
  DocumentoExpedienteService,
  DocumentoExpedienteDTO,
  ResumenDocumentosDTO,
  TipoDocumentoExpediente,
  EstadoDocumentoExpediente
} from '../../services/documento-expediente';
import { firstValueFrom } from 'rxjs';
import Swal from 'sweetalert2';

// ===================================================================
// INTERFACES INTERNAS
// ===================================================================

interface FilaDocumento {
  tipo: TipoDocumentoExpediente;
  label: string;
  obligatorio: boolean;
  estado: EstadoDocumentoExpediente | null;
  documentoID?: number;
  nombreArchivo?: string;
  tamanioLegible?: string;
  observaciones?: string;
  subiendo: boolean;
  verificando: boolean;
}

@Component({
  selector: 'app-gestion-documentos',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './gestion-documentos.html',
  styleUrl: './gestion-documentos.css'
})
export class GestionDocumentos implements OnInit {

  private documentoService = inject(DocumentoExpedienteService);
  private expedienteService = inject(ExpedienteService);

  // ===================================================================
  // INPUTS Y OUTPUTS
  // ===================================================================

  @Input() expedienteId!: number;
  @Input() nombrePaciente?: string;
  @Input() soloLectura: boolean = false;

  /**
   * Si true: causa de muerte violenta o dudosa.
   * Fuerza el tipo de salida a AutoridadLegal sin excepción.
   * La card de Familiar queda bloqueada y gris en el selector.
   * Si tipoSalida es null al iniciar, se auto-selecciona AutoridadLegal.
   */
  @Input() causaViolentaODudosa: boolean = false;

  @Output() documentacionCompleta = new EventEmitter<boolean>();
  @Output() resumenActualizado = new EventEmitter<ResumenDocumentosDTO>();

  // ===================================================================
  // ESTADO
  // ===================================================================

  cargando = true;
  error: string | null = null;
  resumen: ResumenDocumentosDTO | null = null;
  filas: FilaDocumento[] = [];
  motivosRechazo: Map<number, string> = new Map();
  filaConRechazoAbierto: number | null = null;
  guardandoTipo = false;

  readonly EstadoDoc = EstadoDocumentoExpediente;
  readonly TipoDoc = TipoDocumentoExpediente;

  get mostrarSelectorTipo(): boolean {
    return !this.resumen?.tipoSalida && !this.soloLectura;
  }

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  async ngOnInit(): Promise<void> {
    if (!this.expedienteId) {
      console.error('[GestionDocumentos] expedienteId es requerido');
      return;
    }

    await this.cargarResumen();

    // Auto-seleccionar AutoridadLegal si causa violenta y aún sin tipo definido
    if (this.causaViolentaODudosa && !this.resumen?.tipoSalida && !this.soloLectura) {
      await this.seleccionarTipoSalidaSilencioso('AutoridadLegal');
    }
  }

  // ===================================================================
  // SELECCIÓN DE TIPO DE SALIDA
  // ===================================================================

  /**
   * Selección manual con confirmación — para uso del admisionista.
   * Cuando causaViolentaODudosa = true, solo AutoridadLegal está habilitada.
   */
  async seleccionarTipoSalida(tipo: 'Familiar' | 'AutoridadLegal'): Promise<void> {
    // Guardia: no permitir Familiar si causa violenta
    if (tipo === 'Familiar' && this.causaViolentaODudosa) return;

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: '¿Confirmar tipo de salida?',
      html: `
        <div class="text-sm text-left">
          <p class="text-gray-700 mb-3">Tipo seleccionado:</p>
          <p class="font-semibold text-lg ${tipo === 'Familiar' ? 'text-blue-700' : 'text-orange-700'}">
            ${tipo === 'Familiar' ? 'Familiar' : 'Autoridad Legal (PNP / Fiscalía)'}
          </p>
          <p class="text-xs text-gray-500 mt-3">
            ${tipo === 'Familiar'
          ? 'Requerirá: DNI Familiar + DNI Fallecido + Certificado Defunción'
          : 'Requerirá: Oficio Legal (PNP / Fiscal / Legista)'}
          </p>
          <p class="text-xs text-orange-600 mt-2 font-medium">
            Este dato no podrá modificarse una vez creada el Acta de Retiro.
          </p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Confirmar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    this.guardandoTipo = true;
    try {
      await firstValueFrom(
        this.expedienteService.establecerTipoSalidaPreliminar(this.expedienteId, tipo)
      );
      await this.cargarResumen(true);
    } catch (err: any) {
      console.error('[GestionDocumentos] Error al establecer tipo de salida:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: err.error?.mensaje || 'No se pudo guardar el tipo de salida.',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      this.guardandoTipo = false;
    }
  }

  /**
   * Auto-selección silenciosa sin confirmación del usuario.
   * Usada cuando causaViolentaODudosa = true al iniciar el componente.
   */
  private async seleccionarTipoSalidaSilencioso(tipo: 'Familiar' | 'AutoridadLegal'): Promise<void> {
    this.guardandoTipo = true;
    try {
      await firstValueFrom(
        this.expedienteService.establecerTipoSalidaPreliminar(this.expedienteId, tipo)
      );
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error en auto-selección tipo salida:', err);
    } finally {
      this.guardandoTipo = false;
    }
  }

  async volverASelectorTipo(): Promise<void> {
    // Bloquear retroceso si causa violenta — no tiene sentido volver al selector
    if (this.causaViolentaODudosa) return;

    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: 'Cambiar tipo de salida',
      text: 'Se limpiará el tipo seleccionado. Deberá elegirlo nuevamente.',
      showCancelButton: true,
      confirmButtonText: 'Continuar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    this.guardandoTipo = true;
    try {
      await firstValueFrom(
        this.expedienteService.limpiarTipoSalidaPreliminar(this.expedienteId)
      );
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error al limpiar tipo de salida:', err);
    } finally {
      this.guardandoTipo = false;
    }
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  async cargarResumen(silencioso = false): Promise<void> {
    if (!silencioso) this.cargando = true;
    this.error = null;
    try {
      this.resumen = await firstValueFrom(
        this.documentoService.obtenerResumen(this.expedienteId)
      );
      this.construirFilas();
      this.resumenActualizado.emit(this.resumen);
      this.documentacionCompleta.emit(this.resumen.documentacionCompleta);
    } catch (err) {
      console.error('[GestionDocumentos] Error al cargar resumen:', err);
      this.error = 'No se pudo cargar el estado de documentación';
    } finally {
      this.cargando = false;
    }
  }

  private construirFilas(): void {
    if (!this.resumen) return;

    const tipoSalida = this.resumen.tipoSalida;

    if (tipoSalida === 'AutoridadLegal') {
      this.filas = [
        this.construirFila(
          TipoDocumentoExpediente.OficioLegal,
          'Oficio Legal (PNP / Fiscalía)',
          true,
          this.resumen.oficioLegal
        )
      ];
    } else {
      this.filas = [
        this.construirFila(
          TipoDocumentoExpediente.DNI_Familiar,
          'DNI del Familiar',
          true,
          this.resumen.dniFamiliar
        ),
        this.construirFila(
          TipoDocumentoExpediente.DNI_Fallecido,
          'DNI del Fallecido',
          true,
          this.resumen.dniFallecido
        ),
        this.construirFila(
          TipoDocumentoExpediente.CertificadoDefuncion,
          'Certificado de Defunción (SINADEF)',
          true,
          this.resumen.certificadoDefuncion
        )
      ];
    }

    const adicionales = this.resumen.documentos.filter(
      d => d.tipoDocumento === TipoDocumentoExpediente.Otro
    );
    adicionales.forEach(doc => {
      this.filas.push({
        tipo: TipoDocumentoExpediente.Otro,
        label: 'Documento Adicional',
        obligatorio: false,
        estado: doc.estado as EstadoDocumentoExpediente | null,
        documentoID: doc.documentoExpedienteID,
        nombreArchivo: doc.nombreArchivo,
        tamanioLegible: doc.tamanioLegible,
        observaciones: doc.observaciones,
        subiendo: false,
        verificando: false
      });
    });
  }

  private construirFila(
    tipo: TipoDocumentoExpediente,
    label: string,
    obligatorio: boolean,
    item: {
      subido: boolean;
      verificado: boolean;
      rechazado: boolean;
      documentoID?: number;
      nombreArchivo?: string;
      observaciones?: string;
    }
  ): FilaDocumento {
    let estado: EstadoDocumentoExpediente | null = null;
    if (item.verificado) estado = EstadoDocumentoExpediente.Verificado;
    else if (item.rechazado) estado = EstadoDocumentoExpediente.Rechazado;
    else if (item.subido) estado = EstadoDocumentoExpediente.PendienteVerificacion;

    const docCompleto = this.resumen?.documentos.find(
      d => d.documentoExpedienteID === item.documentoID
    );

    return {
      tipo, label, obligatorio, estado,
      documentoID: item.documentoID,
      nombreArchivo: item.nombreArchivo,
      tamanioLegible: docCompleto?.tamanioLegible,
      observaciones: item.observaciones,
      subiendo: false,
      verificando: false
    };
  }

  // ===================================================================
  // SUBIDA DE DOCUMENTO
  // ===================================================================

  seleccionarArchivo(fila: FilaDocumento, inputRef: HTMLInputElement): void {
    inputRef.value = '';
    inputRef.click();
  }

  async onArchivoSeleccionado(event: Event, fila: FilaDocumento): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (!archivo) return;

    if (!this.documentoService.validarFormato(archivo)) {
      Swal.fire({
        icon: 'error',
        title: 'Formato no permitido',
        text: 'Solo se aceptan archivos PDF, JPG, JPEG o PNG',
        confirmButtonColor: '#EF4444'
      });
      return;
    }

    if (!this.documentoService.validarTamanio(archivo)) {
      Swal.fire({
        icon: 'error',
        title: 'Archivo muy grande',
        text: `El archivo excede el límite de 5 MB (${this.documentoService.formatearTamanio(archivo.size)})`,
        confirmButtonColor: '#EF4444'
      });
      return;
    }

    fila.subiendo = true;
    try {
      const dto = await firstValueFrom(
        this.documentoService.subirDocumento(this.expedienteId, fila.tipo, archivo)
      );
      fila.estado = EstadoDocumentoExpediente.PendienteVerificacion;
      fila.documentoID = dto.documentoExpedienteID;
      fila.nombreArchivo = dto.nombreArchivo;
      fila.tamanioLegible = dto.tamanioLegible;

      Swal.fire({
        icon: 'success',
        title: 'Documento subido',
        text: `${fila.label} subido. Pendiente de verificación.`,
        toast: true, position: 'top-end',
        showConfirmButton: false, timer: 3000
      });
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error al subir:', err);
      Swal.fire({
        icon: 'error', title: 'Error al subir',
        text: 'No se pudo subir el documento. Intente nuevamente.',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      fila.subiendo = false;
    }
  }

  // ===================================================================
  // VERIFICACIÓN
  // ===================================================================

  async verificarDocumento(fila: FilaDocumento): Promise<void> {
    if (!fila.documentoID) return;

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: 'Verificar documento',
      html: `
        <div class="text-sm text-left">
          <p class="text-gray-700 mb-2">¿Confirma que verificó el documento original físico?</p>
          <p class="font-semibold text-gray-800">${fila.label}</p>
          <p class="text-xs text-gray-500 mt-1">${fila.nombreArchivo ?? ''}</p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, verificado',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#16A34A',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    fila.verificando = true;
    try {
      await firstValueFrom(this.documentoService.verificar(fila.documentoID));
      fila.estado = EstadoDocumentoExpediente.Verificado;
      Swal.fire({
        icon: 'success', title: 'Documento verificado',
        toast: true, position: 'top-end',
        showConfirmButton: false, timer: 2500
      });
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error al verificar:', err);
      Swal.fire({
        icon: 'error', title: 'Error al verificar',
        text: 'No se pudo verificar el documento.',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      fila.verificando = false;
    }
  }

  // ===================================================================
  // RECHAZO
  // ===================================================================

  togglePanelRechazo(documentoID: number): void {
    this.filaConRechazoAbierto =
      this.filaConRechazoAbierto === documentoID ? null : documentoID;
    if (!this.motivosRechazo.has(documentoID)) {
      this.motivosRechazo.set(documentoID, '');
    }
  }

  getMotivoRechazo(documentoID: number): string {
    return this.motivosRechazo.get(documentoID) ?? '';
  }

  setMotivoRechazo(documentoID: number, motivo: string): void {
    this.motivosRechazo.set(documentoID, motivo);
  }

  async rechazarDocumento(fila: FilaDocumento): Promise<void> {
    if (!fila.documentoID) return;
    const motivo = this.getMotivoRechazo(fila.documentoID).trim();
    if (!motivo) {
      Swal.fire({
        icon: 'warning', title: 'Motivo requerido',
        text: 'Debe ingresar el motivo del rechazo.',
        confirmButtonColor: '#EF4444'
      });
      return;
    }

    fila.verificando = true;
    try {
      await firstValueFrom(this.documentoService.rechazar(fila.documentoID, motivo));
      fila.estado = EstadoDocumentoExpediente.Rechazado;
      fila.observaciones = motivo;
      this.filaConRechazoAbierto = null;
      this.motivosRechazo.delete(fila.documentoID);
      Swal.fire({
        icon: 'warning', title: 'Documento rechazado',
        text: 'El familiar deberá presentar el documento nuevamente.',
        toast: true, position: 'top-end',
        showConfirmButton: false, timer: 3000
      });
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error al rechazar:', err);
      Swal.fire({
        icon: 'error', title: 'Error al rechazar',
        text: 'No se pudo rechazar el documento.',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      fila.verificando = false;
    }
  }

  // ===================================================================
  // DESCARGA
  // ===================================================================

  async descargarDocumento(fila: FilaDocumento): Promise<void> {
    if (!fila.documentoID || !fila.nombreArchivo) return;
    try {
      const blob = await firstValueFrom(this.documentoService.descargar(fila.documentoID));
      this.documentoService.descargarArchivo(blob, fila.nombreArchivo);
    } catch (err) {
      console.error('[GestionDocumentos] Error al descargar:', err);
      Swal.fire({
        icon: 'error', title: 'Error al descargar',
        text: 'No se pudo descargar el archivo.',
        confirmButtonColor: '#EF4444'
      });
    }
  }

  // ===================================================================
  // ELIMINACIÓN
  // ===================================================================

  async eliminarDocumento(fila: FilaDocumento): Promise<void> {
    if (!fila.documentoID) return;

    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: '¿Eliminar documento?',
      html: `<p class="text-sm text-gray-600">
        Se eliminará <strong>${fila.nombreArchivo}</strong>.<br>Deberás subirlo nuevamente.
      </p>`,
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    try {
      await firstValueFrom(this.documentoService.eliminar(fila.documentoID));
      fila.estado = null;
      fila.documentoID = undefined;
      fila.nombreArchivo = undefined;
      fila.tamanioLegible = undefined;
      fila.observaciones = undefined;
      Swal.fire({
        icon: 'success', title: 'Documento eliminado',
        toast: true, position: 'top-end',
        showConfirmButton: false, timer: 2500
      });
      await this.cargarResumen(true);
    } catch (err) {
      console.error('[GestionDocumentos] Error al eliminar:', err);
      Swal.fire({
        icon: 'error', title: 'Error al eliminar',
        text: 'No se pudo eliminar el documento.',
        confirmButtonColor: '#EF4444'
      });
    }
  }

  // ===================================================================
  // HELPERS PARA TEMPLATE
  // ===================================================================

  getIconoEstado(estado: EstadoDocumentoExpediente | null): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado: return 'circle-check';
      case EstadoDocumentoExpediente.Rechazado: return 'circle-x';
      case EstadoDocumentoExpediente.PendienteVerificacion: return 'clock';
      default: return 'file-plus';
    }
  }

  getColorEstado(estado: EstadoDocumentoExpediente | null): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado: return 'text-green-600';
      case EstadoDocumentoExpediente.Rechazado: return 'text-red-600';
      case EstadoDocumentoExpediente.PendienteVerificacion: return 'text-yellow-600';
      default: return 'text-gray-400';
    }
  }

  getBgEstado(estado: EstadoDocumentoExpediente | null): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado: return 'bg-green-50 border-green-200';
      case EstadoDocumentoExpediente.Rechazado: return 'bg-red-50 border-red-200';
      case EstadoDocumentoExpediente.PendienteVerificacion: return 'bg-yellow-50 border-yellow-200';
      default: return 'bg-gray-50 border-gray-200';
    }
  }

  getLabelEstado(estado: EstadoDocumentoExpediente | null): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado: return 'Verificado';
      case EstadoDocumentoExpediente.Rechazado: return 'Rechazado';
      case EstadoDocumentoExpediente.PendienteVerificacion: return 'Pend. verificación';
      default: return 'No subido';
    }
  }

  puedeVerificar(fila: FilaDocumento): boolean {
    return !this.soloLectura &&
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion &&
      !!fila.documentoID;
  }

  puedeRechazar(fila: FilaDocumento): boolean {
    return !this.soloLectura &&
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion &&
      !!fila.documentoID;
  }

  puedeSubir(fila: FilaDocumento): boolean {
    return !this.soloLectura && (
      fila.estado === null ||
      fila.estado === EstadoDocumentoExpediente.Rechazado
    );
  }

  puedeEliminar(fila: FilaDocumento): boolean {
    return !this.soloLectura && (
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion ||
      fila.estado === EstadoDocumentoExpediente.Rechazado
    ) && !!fila.documentoID;
  }

  get conteoVerificados(): number {
    return this.filas.filter(f => f.estado === EstadoDocumentoExpediente.Verificado).length;
  }

  get totalObligatorios(): number {
    return this.filas.filter(f => f.obligatorio).length;
  }

  get puedeVolverASelectorTipo(): boolean {
    return !this.soloLectura &&
      !this.causaViolentaODudosa &&  // No puede volver si causa violenta
      !!this.resumen?.tipoSalida &&
      this.filas.every(f => f.estado === null);
  }
}
