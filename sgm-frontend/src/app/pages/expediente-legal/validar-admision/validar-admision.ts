import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import Swal from 'sweetalert2';

// Services
import { ExpedienteService, Expediente } from '../../../services/expediente';
import { DeudaEconomica } from '../../../services/deuda-economica';
import { DeudaSangre } from '../../../services/deuda-sangre';
import { ActaRetiroService, ActaRetiroDTO } from '../../../services/acta-retiro';
import { AuthService } from '../../../services/auth';
import { DocumentoExpedienteService, ResumenDocumentosDTO } from '../../../services/documento-expediente';

// Models
import { DeudaEconomicaSemaforoDTO } from '../../../models/deuda-economica.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';
import { SemaforoDeudas } from '../../../components/semaforo-deudas/semaforo-deudas';
import { FormularioActaRetiroComponent } from '../../../components/formulario-acta-retiro/formulario-acta-retiro';
import { UploadPdf } from '../../../components/upload-pdf/upload-pdf';
import { GestionDocumentos } from '../../../components/gestion-documentos/gestion-documentos';

// Utils
import { getBadgeClasses } from '../../../utils/badge-styles';

// ===================================================================
// TIPOS INTERNOS
// ===================================================================

interface SemaforoExpediente {
  economica?: DeudaEconomicaSemaforoDTO;
  /** Semáforo de sangre: "SIN DEUDA" | "PENDIENTE (X unidades)" | "LIQUIDADO" | "ANULADO POR MEDICO" */
  sangre?: string;
  cargando: boolean;
  error?: boolean;
}

@Component({
  selector: 'app-validar-admision',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IconComponent,
    SemaforoDeudas,
    FormularioActaRetiroComponent,
    UploadPdf,
    GestionDocumentos
  ],
  templateUrl: './validar-admision.html',
  styleUrl: './validar-admision.css'
})
export class ValidarAdmision implements OnInit {
  private router = inject(Router);
  private expedienteService = inject(ExpedienteService);
  private deudaEconomicaService = inject(DeudaEconomica);
  private deudaSangreService = inject(DeudaSangre);
  private actaRetiroService = inject(ActaRetiroService);
  private authService = inject(AuthService);
  private documentoService = inject(DocumentoExpedienteService);

  // ===================================================================
  // ESTADO DE DATOS
  // ===================================================================
  expedientes: Expediente[] = [];
  expedientesFiltrados: Expediente[] = [];
  paginatedItems: Expediente[] = [];
  cargando = true;
  error: string | null = null;
  procesando: number | null = null;

  /** Semáforos de deudas por expedienteID */
  semaforos: Map<number, SemaforoExpediente> = new Map();

  /** Cache de actas de retiro por expedienteID */
  actasCache: Map<number, ActaRetiroDTO | null> = new Map();

  /** Cache de resumen de documentos por expedienteID */
  resumenDocumentosCache: { [expedienteId: number]: ResumenDocumentosDTO } = {};

  // ===================================================================
  // FILTROS
  // ===================================================================
  filtroExpediente = '';
  filtroHC = '';
  filtroNombre = '';
  filtroServicio = '';
  filtroEstadoActa: 'sinActa' | 'actaCreada' | 'pdfFirmado' | 'todos' = 'sinActa';

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  totalItems = 0;

  // ===================================================================
  // MODALES
  // ===================================================================
  mostrarModalActa = false;
  mostrarModalUploadPDF = false;
  mostrarModalDetalle = false;
  mostrarModalDocumentos = false;

  expedienteSeleccionado: Expediente | null = null;
  actaCreadaTemporal: ActaRetiroDTO | null = null;
  pdfFirmadoSeleccionado: File | null = null;

  Math = Math;

  // ===================================================================
  // LIFECYCLE
  // ===================================================================
  ngOnInit(): void {
    this.cargarExpedientes();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  async cargarExpedientes(): Promise<void> {
    this.cargando = true;
    this.error = null;
    this.actasCache.clear();
    this.resumenDocumentosCache = {};
    try {
      this.expedientes = await firstValueFrom(
        this.expedienteService.getPendientesValidacionAdmision()
      );

      this.expedientesFiltrados = [...this.expedientes];

      // Carga en paralelo: actas, semáforos y resumen de documentos
      await Promise.all([
        this.cargarActasParaExpedientes(),
        this.cargarSemaforosParaExpedientes(),
        this.cargarResumenDocumentosParaExpedientes()
      ]);

      this.aplicarFiltros();

    } catch (error) {
      console.error('❌ Error al cargar expedientes:', error);
      this.error = 'No se pudieron cargar los expedientes pendientes';

      Swal.fire({
        icon: 'error',
        title: 'Error al Cargar Datos',
        text: this.error,
        confirmButtonColor: '#EF4444'
      });
    } finally {
      this.cargando = false;
    }
  }

  /**
   * Carga semáforos de deudas para todos los expedientes en paralelo.
   */
  private async cargarSemaforosParaExpedientes(): Promise<void> {
    const promesas = this.expedientes.map(async (expediente) => {
      const id = expediente.expedienteID;

      this.semaforos.set(id, { cargando: true });

      try {
        const [economica, sangre] = await Promise.all([
          firstValueFrom(this.deudaEconomicaService.obtenerSemaforo(id)),
          firstValueFrom(this.deudaSangreService.obtenerSemaforo(id))
        ]);

        this.semaforos.set(id, { economica, sangre, cargando: false });

      } catch (err) {
        console.error(`❌ Error al cargar semáforos del expediente ${id}:`, err);
        this.semaforos.set(id, { cargando: false, error: true });
      }
    });

    await Promise.all(promesas);
  }

  private async cargarActasParaExpedientes(): Promise<void> {
    const expedientesSinCache = this.expedientes.filter(
      exp => !this.actasCache.has(exp.expedienteID)
    );

    if (expedientesSinCache.length === 0) return;

    const promesas = expedientesSinCache.map(async (expediente) => {
      try {
        const existe = await firstValueFrom(
          this.actaRetiroService.existeActa(expediente.expedienteID)
        );

        if (existe) {
          const acta = await firstValueFrom(
            this.actaRetiroService.obtenerPorExpediente(expediente.expedienteID)
          );
          this.actasCache.set(expediente.expedienteID, acta);
        } else {
          this.actasCache.set(expediente.expedienteID, null);
        }
      } catch (error) {
        console.error(`❌ Error al cargar acta para expediente ${expediente.expedienteID}:`, error);
        this.actasCache.set(expediente.expedienteID, null);
      }
    });

    await Promise.all(promesas);
  }

  /**
   * Carga el resumen de documentación para todos los expedientes.
   * Determina si el botón "Crear Acta" debe estar habilitado.
   */
  private async cargarResumenDocumentosParaExpedientes(force = false): Promise<void> {
    const expedientes = force
      ? this.expedientes
      : this.expedientes.filter(exp => !this.resumenDocumentosCache[exp.expedienteID]);

    if (expedientes.length === 0) return;

    const promesas = expedientes.map(async (expediente) => {
      try {
        const resumen = await firstValueFrom(
          this.documentoService.obtenerResumen(expediente.expedienteID)
        );
        this.resumenDocumentosCache = {
          ...this.resumenDocumentosCache,
          [expediente.expedienteID]: resumen
        };
      } catch (error) {
        console.error(`❌ Error al cargar documentos del expediente ${expediente.expedienteID}:`, error);
      }
    });

    await Promise.all(promesas);
  }

  // ===================================================================
  // FILTRADO
  // ===================================================================

  aplicarFiltros(): void {
    let resultados = [...this.expedientes];

    if (this.filtroExpediente.trim()) {
      const term = this.filtroExpediente.toLowerCase();
      resultados = resultados.filter(e =>
        e.codigoExpediente.toLowerCase().includes(term)
      );
    }

    if (this.filtroHC.trim()) {
      const term = this.filtroHC.toLowerCase();
      resultados = resultados.filter(e =>
        e.hc.toLowerCase().includes(term)
      );
    }

    if (this.filtroNombre.trim()) {
      const term = this.filtroNombre.toLowerCase();
      resultados = resultados.filter(e =>
        e.nombreCompleto.toLowerCase().includes(term)
      );
    }

    if (this.filtroServicio.trim()) {
      const term = this.filtroServicio.toLowerCase();
      resultados = resultados.filter(e =>
        e.servicioFallecimiento?.toLowerCase().includes(term)
      );
    }

    if (this.filtroEstadoActa !== 'todos') {
      resultados = resultados.filter(e => {
        const tieneActa = this.expedienteTieneActa(e);
        const tienePDFFirmado = this.expedienteTienePDFFirmado(e);

        switch (this.filtroEstadoActa) {
          case 'sinActa': return !tieneActa;
          case 'actaCreada': return tieneActa && !tienePDFFirmado;
          case 'pdfFirmado': return tienePDFFirmado;
          default: return true;
        }
      });
    }

    this.expedientesFiltrados = resultados;
    this.totalItems = resultados.length;
    this.paginaActual = 1;
    this.calcularPaginacion();
  }

  limpiarFiltros(): void {
    this.filtroExpediente = '';
    this.filtroHC = '';
    this.filtroNombre = '';
    this.filtroServicio = '';
    this.filtroEstadoActa = 'sinActa';
    this.aplicarFiltros();
  }

  get filtrosActivos(): number {
    let count = 0;
    if (this.filtroExpediente) count++;
    if (this.filtroHC) count++;
    if (this.filtroNombre) count++;
    if (this.filtroServicio) count++;
    if (this.filtroEstadoActa !== 'sinActa') count++;
    return count;
  }

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================
  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.totalItems / this.itemsPorPagina) || 1;
    this.actualizarPagina();
  }

  actualizarPagina(): void {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    const fin = inicio + this.itemsPorPagina;
    this.paginatedItems = this.expedientesFiltrados.slice(inicio, fin);
  }

  paginaAnterior(): void {
    if (this.paginaActual > 1) {
      this.paginaActual--;
      this.actualizarPagina();
    }
  }

  paginaSiguiente(): void {
    if (this.paginaActual < this.totalPaginas) {
      this.paginaActual++;
      this.actualizarPagina();
    }
  }

  // ===================================================================
  // FLUJO DOCUMENTOS DIGITALIZADOS
  // ===================================================================
  /**
   * Abre el modal de gestión de documentos del expediente.
   * Disponible siempre — el admisionista puede subir docs en cualquier momento.
   */
  abrirModalDocumentos(expediente: Expediente): void {
    this.expedienteSeleccionado = expediente;
    this.mostrarModalDocumentos = true;
  }
  /**
   * Callback cuando se actualiza documentación en el modal.
   * Recarga el resumen para ese expediente y actualiza el cache.
   */
  onDocumentosActualizados(completa: boolean): void {
    if (!this.expedienteSeleccionado) return;
    const id = this.expedienteSeleccionado.expedienteID;
    // Invalidar cache del expediente afectado
    delete this.resumenDocumentosCache[id];
    // Recargar solo los que faltan (incluye este)
    this.cargarResumenDocumentosParaExpedientes();
  }
  onResumenDocumentosActualizado(resumen: ResumenDocumentosDTO): void {
    this.resumenDocumentosCache = {
      ...this.resumenDocumentosCache,
      [resumen.expedienteID]: resumen
    };
  }
  // ===================================================================
  // VALIDACIÓN DE DOCUMENTACIÓN → CREAR ACTA
  // ===================================================================
  /**
   * Verifica deudas y documentación antes de abrir el modal de acta.
   * Orden de validación:
   * 1. Deudas económicas y de sangre (bloquean si pendientes)
   * 2. Documentación completa (gate para crear acta)
   */
  async validarDocumentacion(expediente: Expediente): Promise<void> {
    const semaforo = this.semaforos.get(expediente.expedienteID);

    // ── 1. Verificar si semáforos aún cargan ──
    if (semaforo?.cargando) {
      Swal.fire({
        icon: 'info',
        title: 'Cargando...',
        text: 'Verificando estado de deudas, por favor espere.',
        timer: 2000,
        showConfirmButton: false
      });
      return;
    }

    // ── 2. Verificar deudas ──
    const tieneDeudaEconomica = semaforo?.economica?.tieneDeuda === true;
    const tieneDeudaSangre = semaforo?.sangre?.toUpperCase().includes('PENDIENTE') === true;

    if (tieneDeudaEconomica || tieneDeudaSangre) {
      this.mostrarAlertaDeudaPendiente(expediente, semaforo);
      return;
    }

    // ── 3. Verificar documentación completa ──
    const resumen = this.resumenDocumentosCache[expediente.expedienteID];

    if (!resumen) {
      Swal.fire({
        icon: 'warning',
        title: 'Documentación Requerida',
        html: `
        <p class="text-gray-600">
          Primero debe subir y verificar la documentación del familiar<br>
          antes de crear el Acta de Retiro.
        </p>
      `,
        confirmButtonText: 'Gestionar Documentos',
        confirmButtonColor: '#0891B2'
      }).then((result) => {
        if (result.isConfirmed) {
          this.abrirModalDocumentos(expediente);
        }
      });
      return;
    }

    if (!resumen.documentacionCompleta) {
      const faltantes = this.obtenerDocumentosFaltantes(resumen);

      Swal.fire({
        icon: 'warning',
        title: 'Documentación Incompleta',
        html: `
        <p class="text-gray-600 mb-3">
          No se puede crear el Acta de Retiro hasta que todos los documentos
          estén <strong>verificados</strong>.
        </p>
        ${faltantes.length > 0 ? `
          <div class="text-left bg-yellow-50 p-3 rounded-lg">
            <p class="font-semibold text-yellow-800 mb-2">Pendientes:</p>
            <ul class="list-disc list-inside text-sm text-yellow-700 space-y-1">
              ${faltantes.map(f => `<li>${f}</li>`).join('')}
            </ul>
          </div>
        ` : ''}
      `,
        showCancelButton: true,
        confirmButtonText: 'Gestionar Documentos',
        cancelButtonText: 'Cerrar',
        confirmButtonColor: '#0891B2',
        cancelButtonColor: '#6B7280'
      }).then((result) => {
        if (result.isConfirmed) {
          this.abrirModalDocumentos(expediente);
        }
      });
      return;
    }

    // ── 4. Todo OK → recargar expediente fresco y abrir modal acta ──
    this.procesando = expediente.expedienteID;
    try {
      const expedienteActualizado = await firstValueFrom(
        this.expedienteService.getById(expediente.expedienteID)
      );
      this.expedienteSeleccionado = expedienteActualizado;
      this.mostrarModalActa = true;
    } catch {
      this.expedienteSeleccionado = expediente;
      this.mostrarModalActa = true;
    } finally {
      this.procesando = null;
    }
  }
  /**
   * Obtiene lista de documentos pendientes de verificación según tipo de salida.
   */
  private obtenerDocumentosFaltantes(resumen: ResumenDocumentosDTO): string[] {
    const faltantes: string[] = [];
    const tipoSalida = resumen.tipoSalida;

    if (tipoSalida === 'AutoridadLegal') {
      if (!resumen.oficioLegal.verificado) {
        faltantes.push(resumen.oficioLegal.subido
          ? 'Oficio Legal — pendiente de verificación'
          : 'Oficio Legal — no subido');
      }
    } else {
      // Familiar o sin tipo definido aún
      if (!resumen.dniFamiliar.verificado) {
        faltantes.push(resumen.dniFamiliar.subido
          ? 'DNI del Familiar — pendiente de verificación'
          : 'DNI del Familiar — no subido');
      }
      if (!resumen.dniFallecido.verificado) {
        faltantes.push(resumen.dniFallecido.subido
          ? 'DNI del Fallecido — pendiente de verificación'
          : 'DNI del Fallecido — no subido');
      }
      if (!resumen.certificadoDefuncion.verificado) {
        faltantes.push(resumen.certificadoDefuncion.subido
          ? 'Certificado de Defunción (SINADEF) — pendiente de verificación'
          : 'Certificado de Defunción (SINADEF) — no subido');
      }
    }

    return faltantes;
  }

  private mostrarAlertaDeudaPendiente(
    expediente: Expediente,
    semaforo: SemaforoExpediente | undefined
  ): void {
    let detallesDeudas = '';

    if (semaforo?.economica?.tieneDeuda) {
      detallesDeudas += `
        <div class="flex items-center gap-3 text-red-600">
          <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <div class="text-left">
            <p class="font-semibold">Deuda Económica Pendiente</p>
            <p class="text-sm">${semaforo.economica.instruccion}</p>
          </div>
        </div>
      `;
    }

    if (semaforo?.sangre?.toUpperCase().includes('PENDIENTE')) {
      detallesDeudas += `
        <div class="flex items-center gap-3 text-red-600 mt-3">
          <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
          </svg>
          <div class="text-left">
            <p class="font-semibold">Deuda de Sangre Pendiente</p>
            <p class="text-sm">${semaforo.sangre}</p>
          </div>
        </div>
      `;
    }

    Swal.fire({
      icon: 'error',
      title: 'No se Puede Crear Acta',
      html: `
        <div class="text-left space-y-2 mb-4">
          <p class="font-semibold">${expediente.nombreCompleto}</p>
          <p class="text-sm text-gray-600">Expediente: ${expediente.codigoExpediente}</p>
        </div>
        <div class="p-4 bg-red-50 rounded-lg">
          ${detallesDeudas}
        </div>
        <p class="text-sm text-gray-600 mt-4">
          Regularice las deudas antes de crear el Acta de Retiro.
        </p>
      `,
      confirmButtonText: 'Entendido',
      confirmButtonColor: '#DC2626'
    });
  }

  // ===================================================================
  // FLUJO ACTA DE RETIRO
  // ===================================================================
  onActaCreada(acta: ActaRetiroDTO): void {
    this.actaCreadaTemporal = acta;
    this.mostrarModalActa = false;

    if (this.expedienteSeleccionado) {
      const id = this.expedienteSeleccionado.expedienteID;

      this.actasCache.set(id, acta);
      delete this.resumenDocumentosCache[id];
    }

    this.generarYDescargarPDF(acta.actaRetiroID, acta.tipoSalida);
    this.cargarExpedientes();
  }

  async generarYDescargarPDF(actaRetiroID: number, tipoSalida: string): Promise<void> {
    Swal.fire({
      title: 'Generando PDF...',
      text: 'Por favor espere',
      allowOutsideClick: false,
      didOpen: () => Swal.showLoading()
    });

    try {
      const blob = await firstValueFrom(
        this.actaRetiroService.generarPDF(actaRetiroID)
      );

      Swal.close();

      const nombreArchivo = `ActaRetiro_${this.expedienteSeleccionado?.codigoExpediente}.pdf`;
      this.actaRetiroService.descargarPDF(blob, nombreArchivo);

      const responsableTipo = tipoSalida === 'Familiar'
        ? 'Familiar responsable'
        : 'Autoridad legal (PNP/Fiscal/Legista)';

      Swal.fire({
        icon: 'success',
        title: 'Acta Creada Exitosamente',
        html: `
          <div class="text-sm space-y-3">
            <div class="bg-green-50 p-3 rounded-lg">
              <p class="font-semibold text-gray-800">PDF descargado automáticamente</p>
            </div>
            <div class="text-left">
              <p class="font-semibold text-gray-700 mb-2">Pasos a seguir:</p>
              <ol class="list-decimal list-inside space-y-1.5 text-gray-600 text-sm">
                <li>Imprima el documento</li>
                <li>Obtenga las 3 firmas requeridas:
                  <ul class="list-disc list-inside ml-5 mt-1 space-y-0.5">
                    <li>${responsableTipo}</li>
                    <li>Admisionista</li>
                    <li>Supervisor de Vigilancia</li>
                  </ul>
                </li>
                <li>Escanee el documento firmado</li>
                <li>Use el botón "Subir PDF Firmado"</li>
              </ol>
            </div>
          </div>
        `,
        confirmButtonText: 'Entendido',
        confirmButtonColor: '#0891B2',
        width: '480px',
        padding: '1.5rem'
      });

    } catch (error) {
      Swal.close();
      console.error('❌ Error al generar PDF:', error);

      Swal.fire({
        icon: 'error',
        title: 'Error al Generar PDF',
        text: 'No se pudo generar el PDF. Intente nuevamente.',
        confirmButtonColor: '#EF4444'
      });
    }
  }
  async abrirModalSubirPDF(expediente: Expediente): Promise<void> {
    this.expedienteSeleccionado = expediente;
    Swal.fire({
      title: 'Cargando datos...',
      allowOutsideClick: false,
      didOpen: () => Swal.showLoading()
    });
    try {
      const acta = await firstValueFrom(
        this.actaRetiroService.obtenerPorExpediente(expediente.expedienteID)
      );
      this.actasCache.set(expediente.expedienteID, acta);
      this.actaCreadaTemporal = acta;
      this.mostrarModalUploadPDF = true;
    } catch (error) {
      console.error('❌ Error al cargar acta:', error);
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se pudo cargar el acta. Intente nuevamente.',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      Swal.close();
    }
  }
  reimprimirActa(expediente: Expediente): void {
    Swal.fire({
      icon: 'question',
      title: '¿Reimprimir Acta?',
      html: `
        <div class="text-sm text-left">
          <p class="text-gray-600 mb-1">Expediente: ${expediente.codigoExpediente}</p>
          <p class="text-gray-600">Se descargará una copia del PDF del acta de retiro</p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, reimprimir',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280',
      width: '400px',
      padding: '1.5rem'
    }).then((result) => {
      if (!result.isConfirmed) return;

      Swal.fire({
        title: 'Generando PDF...',
        text: 'Por favor espere',
        allowOutsideClick: false,
        didOpen: () => Swal.showLoading()
      });

      this.actaRetiroService.reimprimirPDFPorExpediente(expediente.expedienteID).subscribe({
        next: (blob) => {
          Swal.close();

          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `ActaRetiro_${expediente.codigoExpediente}_reimpresion.pdf`;
          link.click();
          setTimeout(() => window.URL.revokeObjectURL(url), 100);

          Swal.fire({
            icon: 'success',
            title: 'PDF Reimpreso',
            text: 'El acta se descargó correctamente',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000
          });
        },
        error: (err) => {
          Swal.close();
          console.error('❌ Error al reimprimir acta:', err);

          const mensajeError = err.status === 404
            ? 'No se encontró el acta de retiro para este expediente.'
            : err.status === 500
              ? 'Error interno del servidor al generar el PDF.'
              : 'No se pudo reimprimir el acta.';

          Swal.fire({
            icon: 'error',
            title: 'Error al Reimprimir',
            text: mensajeError,
            confirmButtonColor: '#EF4444'
          });
        }
      });
    });
  }
  onPDFFirmadoSeleccionado(file: File): void {
    this.pdfFirmadoSeleccionado = file;
  }

  async subirPDFYValidar(): Promise<void> {
    if (!this.pdfFirmadoSeleccionado || !this.actaCreadaTemporal) return;

    const usuarioId = this.authService.getUserId();

    Swal.fire({
      title: 'Procesando...',
      text: 'Subiendo PDF del acta firmada',
      allowOutsideClick: false,
      didOpen: () => Swal.showLoading()
    });

    try {
      const timestamp = Date.now();
      const dto = {
        actaRetiroID: this.actaCreadaTemporal.actaRetiroID,
        rutaPDFFirmado: `actas-firmadas/ActaRetiro_${this.expedienteSeleccionado?.codigoExpediente}_firmado_${timestamp}.pdf`,
        nombreArchivoPDFFirmado: this.pdfFirmadoSeleccionado.name,
        tamañoPDFFirmado: this.pdfFirmadoSeleccionado.size,
        usuarioSubidaPDFID: usuarioId
      };

      await firstValueFrom(this.actaRetiroService.subirPDFFirmado(dto));

      Swal.close();

      Swal.fire({
        icon: 'success',
        title: 'PDF Cargado Correctamente',
        text: `El acta firmada del expediente ${this.expedienteSeleccionado!.codigoExpediente} fue registrada.`,
        confirmButtonColor: '#0891B2'
      });

      if (this.expedienteSeleccionado) {
        const id = this.expedienteSeleccionado.expedienteID;
        this.actasCache.delete(id);
        delete this.resumenDocumentosCache[id];
      }

      this.limpiarEstadoModal();
      await this.cargarExpedientes();

    } catch (err: any) {
      Swal.close();
      console.error('❌ Error al subir PDF firmado:', err);

      Swal.fire({
        icon: 'error',
        title: 'Error al Subir PDF',
        text: err.error?.mensaje || err.error?.title || 'No se pudo subir el PDF firmado.',
        confirmButtonColor: '#EF4444'
      });
    }
  }

  limpiarEstadoModal(): void {
    // Si había expediente seleccionado con modal docs abierto, recargar su resumen
    if (this.mostrarModalDocumentos && this.expedienteSeleccionado) {
      this.onDocumentosActualizados(false);
    }
    this.mostrarModalActa = false;
    this.mostrarModalUploadPDF = false;
    this.mostrarModalDetalle = false;
    this.mostrarModalDocumentos = false;
    this.expedienteSeleccionado = null;
    this.actaCreadaTemporal = null;
    this.pdfFirmadoSeleccionado = null;
  }

  cancelarFlujoActa(): void {
    Swal.fire({
      icon: 'warning',
      title: '¿Cancelar Proceso?',
      text: 'Se perderá el progreso del acta',
      showCancelButton: true,
      confirmButtonText: 'Sí, cancelar',
      cancelButtonText: 'Continuar',
      confirmButtonColor: '#EF4444'
    }).then((result) => {
      if (result.isConfirmed) {
        this.limpiarEstadoModal();
      }
    });
  }

  verDetalle(expedienteId: number): void {
    this.router.navigate(['/administrativo/expedientes', expedienteId]);
  }

  // ===================================================================
  // HELPERS PÚBLICOS
  // ===================================================================
  getSemaforo(expedienteId: number): SemaforoExpediente | undefined {
    return this.semaforos.get(expedienteId);
  }

  tieneDeudas(expedienteId: number): boolean {
    const semaforo = this.semaforos.get(expedienteId);
    if (!semaforo || semaforo.cargando) return false;

    const tieneDeudaEconomica = semaforo.economica?.tieneDeuda === true;
    const tieneDeudaSangre = semaforo.sangre?.toUpperCase().includes('PENDIENTE') === true;

    return tieneDeudaEconomica || tieneDeudaSangre;
  }

  expedienteTieneActa(expediente: Expediente): boolean {
    return this.actasCache.has(expediente.expedienteID)
      ? this.actasCache.get(expediente.expedienteID) !== null
      : false;
  }

  expedienteTienePDFFirmado(expediente: Expediente): boolean {
    const acta = this.actasCache.get(expediente.expedienteID);
    return acta ? !!acta.rutaPDFFirmado : false;
  }
  /**
   * Verifica si el expediente tiene documentación completa.
   * Controla si el botón "Crear Acta" está habilitado.
   */
  expedienteTieneDocumentacionCompleta(expediente: Expediente): boolean {
    const resumen = this.resumenDocumentosCache[expediente.expedienteID];
    return resumen?.documentacionCompleta === true;
  }

  /**
   * Retorna el conteo de documentos verificados del expediente.
   * Útil para mostrar progreso en el template.
   */
  getConteoDocumentos(expediente: Expediente): { verificados: number; total: number } {
    const resumen = this.resumenDocumentosCache[expediente.expedienteID];
    if (!resumen) return { verificados: 0, total: 0 };
    const verificados = resumen.documentos.filter(d =>
      d.estado === 'Verificado' || Number(d.estado) === 2
    ).length;
    if (!resumen.tipoSalida) return { verificados: 0, total: 0 };
    const total = resumen.tipoSalida === 'AutoridadLegal' ? 1 : 3;
    return { verificados, total };
  }

  getEstadoBadge(estado: string): string {
    return getBadgeClasses(estado);
  }
}
