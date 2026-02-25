import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../icon/icon.component';
import { ExpedienteService } from '../../services/expediente';
import {DocumentoExpedienteService,DocumentoExpedienteDTO,ResumenDocumentosDTO,TipoDocumentoExpediente,EstadoDocumentoExpediente} from '../../services/documento-expediente';
import { firstValueFrom } from 'rxjs';
import Swal from 'sweetalert2';

// ===================================================================
// INTERFACES INTERNAS
// ===================================================================

interface FilaDocumento {
  tipo: TipoDocumentoExpediente;
  label: string;
  obligatorio: boolean;
  estado: EstadoDocumentoExpediente | null; // null = no subido
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

  // ===================================================================
  // DEPENDENCY INJECTION
  // ===================================================================
  private documentoService = inject(DocumentoExpedienteService);
  private expedienteService = inject(ExpedienteService);
  // ===================================================================
  // INPUTS Y OUTPUTS
  // ===================================================================

  /** ID del expediente cuyos documentos se gestionan */
  @Input() expedienteId!: number;

  /**
   * Nombre del paciente para mostrar en el header.
   * Opcional — si no se pasa, no se muestra.
   */
  @Input() nombrePaciente?: string;

  /**
   * Si true, solo muestra el semáforo sin botones de acción.
   * Útil para vistas de consulta.
   */
  @Input() soloLectura: boolean = false;

  /**
   * Emite true cuando todos los documentos requeridos están verificados.
   * Emite false cuando el estado vuelve a incompleto (ej. rechazo).
   */
  @Output() documentacionCompleta = new EventEmitter<boolean>();

  /** Emite cada vez que cambia el resumen (subida, verificación, rechazo) */
  @Output() resumenActualizado = new EventEmitter<ResumenDocumentosDTO>();

  // ===================================================================
  // ESTADO
  // ===================================================================

  cargando = true;
  error: string | null = null;
  resumen: ResumenDocumentosDTO | null = null;

  /** Filas construidas a partir del resumen — una por tipo requerido */
  filas: FilaDocumento[] = [];

  /** Motivoss de rechazo por documentoID (input del usuario) */
  motivosRechazo: Map<number, string> = new Map();

  /** Controla qué fila tiene el panel de rechazo expandido */
  filaConRechazoAbierto: number | null = null;

  // Exponer enums al template
  readonly EstadoDoc = EstadoDocumentoExpediente;
  readonly TipoDoc = TipoDocumentoExpediente;
  /** Controla si se muestra el selector de tipo de salida */
  get mostrarSelectorTipo(): boolean {
    return !this.resumen?.tipoSalida && !this.soloLectura;
  }
  guardandoTipo = false;
  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    if (!this.expedienteId) {
      console.error('❌ GestionDocumentos: expedienteId es requerido');
      return;
    }
    this.cargarResumen();
  }

  /**
 * Guarda el tipo de salida preliminar en el backend.
 * Una vez seleccionado, construye las filas correspondientes.
 * Bloqueado si ya existe Acta de Retiro.
 */
  async seleccionarTipoSalida(tipo: 'Familiar' | 'AutoridadLegal'): Promise<void> {
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
          : 'Requerirá: Oficio Legal (PNP / Fiscal / Legista)'
        }
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
        this.expedienteService.establecerTipoSalidaPreliminar(
          this.expedienteId,
          tipo
        )
      );

      // Recargar resumen - ahora tendrá tipoSalida definido
      await this.cargarResumen();

    } catch (err: any) {
      console.error('❌ Error al establecer tipo de salida:', err);
      const mensaje = err.error?.mensaje || 'No se pudo guardar el tipo de salida.';
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: mensaje,
        confirmButtonColor: '#EF4444'
      });
    } finally {
      this.guardandoTipo = false;
    }
  }
  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  async cargarResumen(): Promise<void> {
    this.cargando = true;
    this.error = null;

    try {
      this.resumen = await firstValueFrom(
        this.documentoService.obtenerResumen(this.expedienteId)
      );
      this.construirFilas();
      this.resumenActualizado.emit(this.resumen);
      this.documentacionCompleta.emit(this.resumen.documentacionCompleta);

    } catch (err) {
      console.error('❌ Error al cargar resumen de documentos:', err);
      this.error = 'No se pudo cargar el estado de documentación';
    } finally {
      this.cargando = false;
    }
  }

  /**
   * Construye las filas de la tabla a partir del resumen del backend.
   * Las filas mostradas dependen del tipoSalida.
   */
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
      // Familiar o sin tipo definido — mostrar los 3 requeridos
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

    // Documentos adicionales (Otro) si existen
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
    item: { subido: boolean; verificado: boolean; rechazado: boolean; documentoID?: number; nombreArchivo?: string; observaciones?: string }
  ): FilaDocumento {
    // Determinar estado desde los bools del EstadoDocumentoItem
    let estado: EstadoDocumentoExpediente | null = null;
    if (item.verificado) estado = EstadoDocumentoExpediente.Verificado;
    else if (item.rechazado) estado = EstadoDocumentoExpediente.Rechazado;
    else if (item.subido) estado = EstadoDocumentoExpediente.PendienteVerificacion;

    // Buscar tamaño en la lista completa si está disponible
    const docCompleto = this.resumen?.documentos.find(
      d => d.documentoExpedienteID === item.documentoID
    );

    return {
      tipo,
      label,
      obligatorio,
      estado,
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

  /**
   * Abre el selector de archivo nativo para una fila específica.
   * El input[type=file] es invisible — se activa programáticamente.
   */
  seleccionarArchivo(fila: FilaDocumento, inputRef: HTMLInputElement): void {
    inputRef.value = ''; // Reset para permitir reselección del mismo archivo
    inputRef.click();
  }

  async onArchivoSeleccionado(
    event: Event,
    fila: FilaDocumento
  ): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (!archivo) return;

    // Validaciones cliente
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
        this.documentoService.subirDocumento(
          this.expedienteId,
          fila.tipo,
          archivo
        )
      );

      // Actualizar fila con el nuevo documento
      fila.estado = EstadoDocumentoExpediente.PendienteVerificacion;
      fila.documentoID = dto.documentoExpedienteID;
      fila.nombreArchivo = dto.nombreArchivo;
      fila.tamanioLegible = dto.tamanioLegible;

      Swal.fire({
        icon: 'success',
        title: 'Documento subido',
        text: `${fila.label} subido correctamente. Pendiente de verificación.`,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
      });

      // Recargar resumen para sincronizar documentacionCompleta
      await this.cargarResumen();

    } catch (err) {
      console.error('❌ Error al subir documento:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error al subir',
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
          <p class="text-gray-700 mb-2">
            ¿Confirma que verificó el documento original físico?
          </p>
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
      await firstValueFrom(
        this.documentoService.verificar(fila.documentoID)
      );

      fila.estado = EstadoDocumentoExpediente.Verificado;

      Swal.fire({
        icon: 'success',
        title: 'Documento verificado',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 2500
      });

      await this.cargarResumen();

    } catch (err) {
      console.error('❌ Error al verificar documento:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error al verificar',
        text: 'No se pudo verificar el documento. Intente nuevamente.',
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
        icon: 'warning',
        title: 'Motivo requerido',
        text: 'Debe ingresar el motivo del rechazo para continuar',
        confirmButtonColor: '#EF4444'
      });
      return;
    }

    fila.verificando = true;

    try {
      await firstValueFrom(
        this.documentoService.rechazar(fila.documentoID, motivo)
      );

      fila.estado = EstadoDocumentoExpediente.Rechazado;
      fila.observaciones = motivo;
      this.filaConRechazoAbierto = null;
      this.motivosRechazo.delete(fila.documentoID);

      Swal.fire({
        icon: 'warning',
        title: 'Documento rechazado',
        text: 'El familiar deberá presentar el documento nuevamente',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
      });

      await this.cargarResumen();

    } catch (err) {
      console.error('❌ Error al rechazar documento:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error al rechazar',
        text: 'No se pudo rechazar el documento. Intente nuevamente.',
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
      const blob = await firstValueFrom(
        this.documentoService.descargar(fila.documentoID)
      );
      this.documentoService.descargarArchivo(blob, fila.nombreArchivo);

    } catch (err) {
      console.error('❌ Error al descargar documento:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error al descargar',
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
      html: `
        <p class="text-sm text-gray-600">
          Se eliminará <strong>${fila.nombreArchivo}</strong>.
          <br>Deberás subirlo nuevamente.
        </p>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    try {
      await firstValueFrom(
        this.documentoService.eliminar(fila.documentoID)
      );

      // Resetear fila
      fila.estado = null;
      fila.documentoID = undefined;
      fila.nombreArchivo = undefined;
      fila.tamanioLegible = undefined;
      fila.observaciones = undefined;

      Swal.fire({
        icon: 'success',
        title: 'Documento eliminado',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 2500
      });

      await this.cargarResumen();

    } catch (err) {
      console.error('❌ Error al eliminar documento:', err);
      Swal.fire({
        icon: 'error',
        title: 'Error al eliminar',
        text: 'No se pudo eliminar el documento.',
        confirmButtonColor: '#EF4444'
      });
    }
  }
  // ===================================================================
  // Funcion para volver al selector previo
  // ===================================================================
  async volverASelectorTipo(): Promise<void> {
    const confirmacion = await Swal.fire({
      icon: 'warning',
      title: 'Cambiar tipo de salida',
      text: 'Se limpiara el tipo seleccionado. Debera elegirlo nuevamente.',
      showCancelButton: true,
      confirmButtonText: 'Continuar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    this.guardandoTipo = true;
    try {
      // Enviar null al backend para limpiar el tipo preliminar
      await firstValueFrom(
        this.expedienteService.limpiarTipoSalidaPreliminar(this.expedienteId)
      );
      await this.cargarResumen();
    } catch (err) {
      console.error('❌ Error al limpiar tipo de salida:', err);
    } finally {
      this.guardandoTipo = false;
    }
  }
  // ===================================================================
  // HELPERS PARA TEMPLATE
  // ===================================================================

  getIconoEstado(estado: EstadoDocumentoExpediente | null): string {
    switch (estado) {
      case EstadoDocumentoExpediente.Verificado: return 'check-circle';
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

  /** true si el botón verificar debe mostrarse */
  puedeVerificar(fila: FilaDocumento): boolean {
    return !this.soloLectura &&
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion &&
      !!fila.documentoID;
  }

  /** true si el botón rechazar debe mostrarse */
  puedeRechazar(fila: FilaDocumento): boolean {
    return !this.soloLectura &&
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion &&
      !!fila.documentoID;
  }

  /** true si el botón subir/resubir debe mostrarse */
  puedeSubir(fila: FilaDocumento): boolean {
    return !this.soloLectura && (
      fila.estado === null ||
      fila.estado === EstadoDocumentoExpediente.Rechazado
    );
  }

  /** true si el botón eliminar debe mostrarse */
  puedeEliminar(fila: FilaDocumento): boolean {
    return !this.soloLectura && (
      fila.estado === EstadoDocumentoExpediente.PendienteVerificacion ||
      fila.estado === EstadoDocumentoExpediente.Rechazado
    ) && !!fila.documentoID;
  }

  get conteoVerificados(): number {
    return this.filas.filter(
      f => f.estado === EstadoDocumentoExpediente.Verificado
    ).length;
  }

  get totalObligatorios(): number {
    return this.filas.filter(f => f.obligatorio).length;
  }
  /** true si no hay ningún documento subido aún — permite volver a elegir tipo */
  get puedeVolverASelectorTipo(): boolean {
    return !this.soloLectura &&
      !!this.resumen?.tipoSalida &&
      this.filas.every(f => f.estado === null);
  }
}
