import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, firstValueFrom } from 'rxjs';

import Swal from 'sweetalert2';

// Services
import { ExpedienteService, Expediente } from '../../../services/expediente';
import { DeudaEconomica } from '../../../services/deuda-economica';
import { DeudaSangre } from '../../../services/deuda-sangre';
import { ActaRetiroService, ActaRetiroDTO } from '../../../services/acta-retiro';
import { AuthService } from '../../../services/auth';

// Models
import { DeudaEconomicaSemaforoDTO } from '../../../models/deuda-economica.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';
import { SemaforoDeudas } from '../../../components/semaforo-deudas/semaforo-deudas';
import { FormularioActaRetiroComponent } from '../../../components/formulario-acta-retiro/formulario-acta-retiro';
import { UploadPdf } from '../../../components/upload-pdf/upload-pdf';

// Utils
import { getBadgeClasses } from '../../../utils/badge-styles';

@Component({
  selector: 'app-validar-admision',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IconComponent,
    SemaforoDeudas,
    FormularioActaRetiroComponent,
    UploadPdf
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

  // ===================================================================
  // ESTADO DE DATOS
  // ===================================================================
  expedientes: Expediente[] = [];
  expedientesFiltrados: Expediente[] = [];
  paginatedItems: Expediente[] = [];
  cargando = true;
  error: string | null = null;
  procesando: number | null = null;

  semaforos: Map<number, {
    economica?: DeudaEconomicaSemaforoDTO;
    sangre?: string;
    cargando: boolean;
  }> = new Map();

  actasCache: Map<number, ActaRetiroDTO | null> = new Map();

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
  // MODALES ACTA DE RETIRO
  // ===================================================================
  mostrarModalActa = false;
  mostrarModalUploadPDF = false;
  mostrarModalDetalle = false;
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
  async cargarExpedientes() {
    this.cargando = true;

    try {
      this.expedientes = await firstValueFrom(
        this.expedienteService.getPendientesValidacionAdmision()
      );

      this.expedientesFiltrados = [...this.expedientes];
      await this.cargarActasParaExpedientes();
      this.aplicarFiltros();

    } catch (error) {
      console.error('❌ Error al cargar expedientes:', error);

      Swal.fire({
        icon: 'error',
        title: 'Error al Cargar Datos',
        text: 'No se pudieron cargar los expedientes pendientes',
        confirmButtonColor: '#EF4444'
      });
    } finally {
      this.cargando = false;
    }
  }

  private cargarSemaforos(expedienteId: number): void {
    this.semaforos.set(expedienteId, { cargando: true });

    forkJoin({
      economica: this.deudaEconomicaService.obtenerSemaforo(expedienteId),
      sangre: this.deudaSangreService.obtenerSemaforo(expedienteId)
    }).subscribe({
      next: (semaforos) => {
        this.semaforos.set(expedienteId, {
          economica: semaforos.economica,
          sangre: semaforos.sangre,
          cargando: false
        });
      },
      error: (err) => {
        console.error(`❌ Error al cargar semáforos del expediente ${expedienteId}:`, err);
        this.semaforos.set(expedienteId, { cargando: false });
      }
    });
  }

  async cargarActasParaExpedientes() {
    const expedientesSinActaEnCache = this.expedientesFiltrados.filter(
      exp => !this.actasCache.has(exp.expedienteID)
    );

    if (expedientesSinActaEnCache.length === 0) return;

    const promesas = expedientesSinActaEnCache.map(async (expediente) => {
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
  // VALIDACIÓN DE DOCUMENTACIÓN
  // ===================================================================
  validarDocumentacion(expediente: Expediente): void {
    const semaforo = this.semaforos.get(expediente.expedienteID);

    const tieneDeudaEconomica = semaforo?.economica?.tieneDeuda || false;
    const tieneDeudaSangre = semaforo?.sangre === 'PENDIENTE' ||
      (semaforo?.sangre?.includes('PENDIENTE') || false);

    if (tieneDeudaEconomica || tieneDeudaSangre) {
      this.mostrarAlertaDeudaPendiente(expediente, semaforo);
      return;
    }

    this.expedienteSeleccionado = expediente;
    this.mostrarModalActa = true;
  }

  private mostrarAlertaDeudaPendiente(
    expediente: Expediente,
    semaforo: any
  ): void {
    let detallesDeudas = '';

    if (semaforo?.economica?.tieneDeuda) {
      detallesDeudas += `
        <div class="flex items-center gap-3 text-red-600">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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

    if (semaforo?.sangre?.includes('PENDIENTE')) {
      detallesDeudas += `
        <div class="flex items-center gap-3 text-red-600 mt-3">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
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
      title: 'No se Puede Validar',
      html: `
        <div class="text-left space-y-2 mb-4">
          <p class="font-semibold">${expediente.nombreCompleto}</p>
          <p class="text-sm text-gray-600">Expediente: ${expediente.codigoExpediente}</p>
        </div>
        <div class="p-4 bg-red-50 rounded-lg">
          ${detallesDeudas}
        </div>
        <p class="text-sm text-gray-600 mt-4">
          El expediente debe regularizar sus deudas antes de validar documentación.
        </p>
      `,
      confirmButtonText: 'Entendido',
      confirmButtonColor: '#DC2626'
    });
  }

  // ===================================================================
  // FLUJO ACTA DE RETIRO
  // ===================================================================
  onActaCreada(acta: ActaRetiroDTO) {
    this.actaCreadaTemporal = acta;
    this.mostrarModalActa = false;

    if (this.expedienteSeleccionado) {
      this.actasCache.set(this.expedienteSeleccionado.expedienteID, acta);
    }

    this.generarYDescargarPDF(acta.actaRetiroID, acta.tipoSalida);
    this.cargarExpedientes();
  }

  /**
   * Genera y descarga el PDF con instrucciones inteligentes según tipo de salida
   */
  async generarYDescargarPDF(actaRetiroID: number, tipoSalida: string) {
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

      // ═══════════════════════════════════════════════════════════
      // INSTRUCCIONES INTELIGENTES SEGÚN TIPO DE SALIDA
      // ═══════════════════════════════════════════════════════════
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

  async abrirModalSubirPDF(expediente: Expediente) {
    this.expedienteSeleccionado = expediente;

    let acta = this.actasCache.get(expediente.expedienteID);

    if (!acta) {
      try {
        Swal.fire({
          title: 'Cargando datos...',
          allowOutsideClick: false,
          didOpen: () => Swal.showLoading()
        });

        acta = await firstValueFrom(
          this.actaRetiroService.obtenerPorExpediente(expediente.expedienteID)
        );

        Swal.close();
        this.actasCache.set(expediente.expedienteID, acta);

      } catch (error) {
        Swal.close();
        console.error('❌ Error al cargar acta:', error);

        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo cargar el acta. Intente nuevamente.',
          confirmButtonColor: '#EF4444'
        });
        return;
      }
    }

    this.actaCreadaTemporal = acta;
    this.mostrarModalUploadPDF = true;
  }

  reimprimirActa(expediente: Expediente): void {
    console.log('🖨️ Solicitando reimpresión de acta para:', expediente.expedienteID);

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

          let mensajeError = 'No se pudo reimprimir el acta.';

          if (err.status === 404) {
            mensajeError = 'No se encontró el acta de retiro para este expediente.';
          } else if (err.status === 500) {
            mensajeError = 'Error interno del servidor al generar el PDF.';
          }

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

  expedienteTieneActa(expediente: Expediente): boolean {
    if (this.actasCache.has(expediente.expedienteID)) {
      return this.actasCache.get(expediente.expedienteID) !== null;
    }
    return false;
  }

  expedienteTienePDFFirmado(expediente: Expediente): boolean {
    const acta = this.actasCache.get(expediente.expedienteID);
    return acta ? !!acta.rutaPDFFirmado : false;
  }

  onPDFFirmadoSeleccionado(file: File): void {
    this.pdfFirmadoSeleccionado = file;
  }

  async subirPDFYValidar(): Promise<void> {
    if (!this.pdfFirmadoSeleccionado || !this.actaCreadaTemporal) return;

    const usuarioId = this.authService.getUserId();

    Swal.fire({
      title: 'Procesando...',
      text: 'Subiendo PDF y validando documentación',
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

      await firstValueFrom(
        this.expedienteService.validarDocumentacion(
          this.expedienteSeleccionado!.expedienteID
        )
      );

      Swal.close();

      Swal.fire({
        icon: 'success',
        title: 'Documentación Validada',
        text: `El expediente ${this.expedienteSeleccionado!.codigoExpediente} está listo para retiro`,
        confirmButtonColor: '#0891B2'
      });

      this.limpiarEstadoModal();
      this.cargarExpedientes();

    } catch (err: any) {
      Swal.close();
      console.error('❌ Error al validar:', err);

      let mensajeError = err.error?.title || 'No se pudo completar la validación';

      if (err.error?.errors) {
        const errores = Object.entries(err.error.errors)
          .map(([campo, mensajes]: [string, any]) => `${campo}: ${mensajes.join(', ')}`)
          .join('\n');
        mensajeError += `\n\n${errores}`;
      }

      Swal.fire({
        icon: 'error',
        title: 'Error al Validar',
        text: mensajeError,
        confirmButtonColor: '#EF4444'
      });
    }
  }

  limpiarEstadoModal(): void {
    this.mostrarModalActa = false;
    this.mostrarModalUploadPDF = false;
    this.mostrarModalDetalle = false;
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
  // HELPERS
  // ===================================================================
  getSemaforo(expedienteId: number) {
    return this.semaforos.get(expedienteId);
  }

  tieneDeudas(expedienteId: number): boolean {
    const semaforo = this.semaforos.get(expedienteId);
    if (!semaforo || semaforo.cargando) return false;

    const tieneDeudaEconomica = semaforo.economica?.tieneDeuda || false;
    const tieneDeudaSangre = semaforo.sangre?.includes('PENDIENTE') || false;

    return tieneDeudaEconomica || tieneDeudaSangre;
  }

  getEstadoBadge(estado: string): string {
    return getBadgeClasses(estado);
  }
}
