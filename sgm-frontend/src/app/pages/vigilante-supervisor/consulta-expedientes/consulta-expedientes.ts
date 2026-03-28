import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';

import { VigilanciaExpediente } from '../../../services/vigilancia-expediente';
import { ActaRetiroService } from '../../../services/acta-retiro';
import { IconComponent } from '../../../components/icon/icon.component';
import { VisorPdfModal } from '../../../components/visor-pdf-modal/visor-pdf-modal';
import { ExpedienteVigilanciaDTO, DetalleVigilanciaDTO } from '../../../models/notificacion.model';
import { getBadgeClasses } from '../../../utils/badge-styles';

/**
 * Módulo Supervisor de Vigilancia — Consulta de Expedientes.
 * Solo lectura: semáforo de deudas, detalle y visor del acta de retiro.
 *
 * SEMÁFORO (bloqueaSangre / bloqueaEconomica):
 *   null  → verde "Sin deuda registrada" (sin registro en SGM)
 *   true  → rojo (deuda activa que bloquea retiro)
 *   false → verde (deuda resuelta / libre)
 *   Excepción: null + tipoExpediente='Externo' + económica → verde (DOA no genera deuda)
 *
 * TIEMPO: recalculado localmente cada 60s desde FechaIngresoBandeja
 * para mantener sincronía sin llamadas al servidor.
 */
@Component({
  selector: 'app-consulta-expedientes',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, VisorPdfModal],
  templateUrl: './consulta-expedientes.html',
  styleUrl: './consulta-expedientes.css'
})
export class ConsultaExpedientes implements OnInit, OnDestroy {

  private vigilanciaService = inject(VigilanciaExpediente);
  private actaRetiroService = inject(ActaRetiroService);
  private destroy$ = new Subject<void>();
  private busqueda$ = new Subject<string>();
  private timerTiempo: ReturnType<typeof setInterval> | null = null;

  // ── Datos ────────────────────────────────────────────────────────
  expedientes: ExpedienteVigilanciaDTO[] = [];
  expedientesFiltrados: ExpedienteVigilanciaDTO[] = [];
  paginatedItems: ExpedienteVigilanciaDTO[] = [];

  // ── Estado ───────────────────────────────────────────────────────
  cargando = true;
  cargandoDetalle = false;
  cargandoPdf = false;
  error: string | null = null;

  // ── Modal detalle ─────────────────────────────────────────────────
  mostrarModalDetalle = false;
  detalleSeleccionado: DetalleVigilanciaDTO | null = null;

  // ── Visor PDF ─────────────────────────────────────────────────────
  pdfBlob: Blob | null = null;
  tituloPdf = '';

  // ── Filtros ───────────────────────────────────────────────────────
  textoBusqueda = '';
  filtroEstado = '';
  filtroDeuda = ''; // '' | 'pendiente' | 'sinDeuda' | 'bypass'

  // ── Paginación ────────────────────────────────────────────────────
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  totalItems = 0;

  Math = Math;

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    this.cargarExpedientes();

    // Debounce en búsqueda
    this.busqueda$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(texto => { this.textoBusqueda = texto; this.aplicarFiltros(); });

    // Recalcular tiempos cada 60s sin llamadas al backend
    this.timerTiempo = setInterval(() => this.recalcularTiempos(), 60_000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.timerTiempo) clearInterval(this.timerTiempo);
  }

  // ===================================================================
  // CARGA
  // ===================================================================

  cargarExpedientes(): void {
    this.cargando = true;
    this.error = null;

    this.vigilanciaService.obtenerExpedientes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.expedientes = data;
          this.aplicarFiltros();
          this.cargando = false;
        },
        error: err => {
          this.cargando = false;
          this.error = 'No se pudieron cargar los expedientes';
          Swal.fire({
            icon: 'error', title: 'Error al cargar',
            text: err.message || this.error,
            confirmButtonColor: '#EF4444'
          });
        }
      });
  }

  onBusquedaChange(texto: string): void { this.busqueda$.next(texto); }

  // ===================================================================
  // TIEMPO — recálculo local cada 60s
  // ===================================================================

  /** Recalcula tiempoEnMortuorio en todos los items usando FechaIngresoBandeja. */
  private recalcularTiempos(): void {
    this.expedientes = this.expedientes.map(e => ({
      ...e,
      tiempoEnMortuorio: this.calcularTiempoLocal(e.fechaIngresoBandeja)
    }));
    // Refleja en los arrays derivados sin recargar del servidor
    this.aplicarFiltros();
  }

  /** Mismo algoritmo que FormatearTiempo() del backend. */
  calcularTiempoLocal(fecha?: Date): string | undefined {
    if (!fecha) return undefined;
    const t = (Date.now() - new Date(fecha).getTime()) / 1000;
    const h = Math.floor(t / 3600);
    const m = Math.floor((t % 3600) / 60);
    if (h < 24) return `${h}h ${m}m`;
    const d = Math.floor(h / 24);
    const hr = h % 24;
    return m > 0 ? `${d}d ${hr}h ${m}m` : `${d}d ${hr}h`;
  }

  // ===================================================================
  // FILTRADO + PAGINACIÓN
  // ===================================================================

  aplicarFiltros(): void {
    let resultado = [...this.expedientes];

    if (this.textoBusqueda.trim()) {
      const t = this.textoBusqueda.toLowerCase();
      resultado = resultado.filter(e =>
        e.hc.toLowerCase().includes(t) ||
        e.nombreCompleto.toLowerCase().includes(t) ||
        e.numeroDocumento.toLowerCase().includes(t)
      );
    }

    if (this.filtroEstado)
      resultado = resultado.filter(e => e.estadoActual === this.filtroEstado);

    // Filtro de deuda
    if (this.filtroDeuda === 'pendiente')
      resultado = resultado.filter(e =>
        (e.bloqueaSangre === true || e.bloqueaEconomica === true) && !e.bypassDeudaAutorizado);
    else if (this.filtroDeuda === 'sinDeuda')
      resultado = resultado.filter(e => e.bloqueaSangre !== true && e.bloqueaEconomica !== true);
    else if (this.filtroDeuda === 'bypass')
      resultado = resultado.filter(e => e.bypassDeudaAutorizado);

    this.expedientesFiltrados = resultado;
    this.totalItems = resultado.length;
    this.paginaActual = 1;
    this.calcularPaginacion();
  }

  limpiarFiltros(): void {
    this.textoBusqueda = '';
    this.filtroEstado = '';
    this.filtroDeuda = '';
    this.aplicarFiltros();
  }

  get filtrosActivos(): number {
    return (this.textoBusqueda ? 1 : 0) +
      (this.filtroEstado ? 1 : 0) +
      (this.filtroDeuda ? 1 : 0);
  }

  private calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.totalItems / this.itemsPorPagina) || 1;
    this.actualizarPagina();
  }

  private actualizarPagina(): void {
    const ini = (this.paginaActual - 1) * this.itemsPorPagina;
    this.paginatedItems = this.expedientesFiltrados.slice(ini, ini + this.itemsPorPagina);
  }

  paginaAnterior(): void {
    if (this.paginaActual > 1) { this.paginaActual--; this.actualizarPagina(); }
  }

  paginaSiguiente(): void {
    if (this.paginaActual < this.totalPaginas) { this.paginaActual++; this.actualizarPagina(); }
  }

  // ===================================================================
  // MODAL DETALLE
  // ===================================================================

  abrirDetalle(expediente: ExpedienteVigilanciaDTO): void {
    this.cargandoDetalle = true;
    this.mostrarModalDetalle = true;
    this.detalleSeleccionado = null;

    this.vigilanciaService.obtenerDetalle(expediente.expedienteID)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: detalle => {
          this.detalleSeleccionado = detalle;
          this.cargandoDetalle = false;
        },
        error: err => {
          this.cargandoDetalle = false;
          this.mostrarModalDetalle = false;
          Swal.fire({
            icon: 'error', title: 'Error al cargar detalle',
            text: err.message, confirmButtonColor: '#EF4444'
          });
        }
      });
  }

  cerrarDetalle(): void {
    this.mostrarModalDetalle = false;
    this.detalleSeleccionado = null;
  }

  // ===================================================================
  // VISOR ACTA PDF
  // ===================================================================

  /**
   * Descarga el PDF del acta y lo abre en el VisorPdfModal.
   * Usa actaRetiroID ya disponible en el DTO — sin llamada previa.
   */
  verActa(detalle: DetalleVigilanciaDTO): void {
    if (!detalle.actaRetiroID) return;
    this.cargandoPdf = true;
    this.actaRetiroService.generarPDF(detalle.actaRetiroID)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: blob => {
          this.pdfBlob = blob;
          this.tituloPdf = `Acta Retiro — ${detalle.codigoExpediente}`;
          this.cargandoPdf = false;
        },
        error: err => {
          this.cargandoPdf = false;
          Swal.fire({
            icon: 'error', title: 'Error al cargar acta',
            text: err.message || 'No se pudo generar el PDF.',
            confirmButtonColor: '#EF4444'
          });
        }
      });
  }

  cerrarPdf(): void { this.pdfBlob = null; }

  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================

  /**
   * Clase CSS del círculo semáforo.
   * null → verde (sin deuda registrada en SGM).
   * Excepción: si es Externo DOA siempre verde en deuda económica.
   */
  getSemaforoClase(
    valor: boolean | null,
    esExterno: boolean,
    esEconomica = false
  ): string {
    if (valor === null && esExterno && esEconomica) return 'bg-green-500';
    if (valor === true) return 'bg-red-500';
    return 'bg-green-500'; // null o false → verde
  }

  /** Tooltip del semáforo. */
  getSemaforoTooltip(
    valor: boolean | null,
    descripcion: string,
    esExterno: boolean,
    esEconomica = false
  ): string {
    if (valor === null && esExterno && esEconomica) return 'Sin deuda (caso externo)';
    if (valor === null) return 'Sin deuda registrada';
    return descripcion;
  }

  /**
   * Mensaje de estado económico en el modal.
   * Si hay bypass + estado pendiente → mensaje alternativo claro.
   */
  getMensajeEconomica(detalle: DetalleVigilanciaDTO): string {
    if (detalle.bypassDeudaAutorizado && detalle.estadoEconomica === 'Pendiente')
      return 'Deuda pendiente — retiro autorizado excepcionalmente por Jefe de Guardia';
    return detalle.mensajeEconomica;
  }

  getBadge(estado: string): string { return getBadgeClasses(estado); }

  trackById(_: number, e: ExpedienteVigilanciaDTO): number { return e.expedienteID; }
}
