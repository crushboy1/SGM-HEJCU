import {
  Component, inject, OnInit, OnDestroy,
  ViewChild, ElementRef, AfterViewInit,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Observable, takeUntil, finalize } from 'rxjs';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';
import { Chart, registerables } from 'chart.js';

import { AuthService } from '../../services/auth';
import {
  ReportesService,
  RangoFecha,
  DashboardReportesDTO,
  PermanenciaItemDTO,
  SalidaItemDTO,
  ActaReportesItemDTO,
  DeudaConsolidadaDTO,
  ExpedienteServicioItemDTO,
  EstadisticasSalida,
  ActaEstadisticasDTO,
} from '../../services/reportes';
import { IconComponent } from '../../components/icon/icon.component';
import { VisorPdfModal } from '../../components/visor-pdf-modal/visor-pdf-modal';
import { getBadgeWithIcon } from '../../utils/badge-styles';

Chart.register(...registerables);

// ===================================================================
// TIPOS
// ===================================================================

export type TabReporte =
  | 'dashboard' | 'permanencia' | 'salidas'
  | 'actas' | 'deudas' | 'servicios';

interface TabConfig {
  key: TabReporte;
  label: string;
  icon: string;
  roles: string[];
}

interface ServicioItemVM extends ExpedienteServicioItemDTO {
  badge: ReturnType<typeof getBadgeWithIcon>;
}

// ===================================================================
// COMPONENTE
// ===================================================================

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, VisorPdfModal],
  templateUrl: './reportes.html',
  styleUrl: './reportes.css',
})
export class ReportesComponent implements OnInit, OnDestroy, AfterViewInit {

  private authService = inject(AuthService);
  private reportesService = inject(ReportesService);
  private cdr = inject(ChangeDetectorRef);
  private destroy$ = new Subject<void>();

  // ─── Chart refs únicos por tab ───────────────────────────────────
  @ViewChild('chartDonaDashboard') chartDonaDashboardRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('chartBarrasDashboard') chartBarrasDashboardRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('chartBarrasPermanencia') chartBarrasPermanenciaRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('chartDonaPermanencia') chartDonaPermanenciaRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('chartDonaSalidas') chartDonaSalidasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('chartDonaDeudas') chartDonaDeudasRef!: ElementRef<HTMLCanvasElement>;

  private chartDonaDashboard?: Chart;
  private chartBarrasDashboard?: Chart;
  private chartBarrasPermanencia?: Chart;
  private chartDonaPermanencia?: Chart;
  private chartDonaSalidas?: Chart;
  private chartDonaDeudas?: Chart;

  // ─── Usuario ─────────────────────────────────────────────────────
  userName = '';
  userRole = '';

  // ─── Tabs ────────────────────────────────────────────────────────
  readonly tabs: TabConfig[] = [
    { key: 'dashboard', label: 'Resumen', icon: 'activity', roles: ['VigilanteSupervisor', 'JefeGuardia', 'Administrador'] },
    { key: 'permanencia', label: 'Permanencia', icon: 'clock', roles: ['VigilanteSupervisor', 'JefeGuardia', 'Administrador'] },
    { key: 'salidas', label: 'Salidas', icon: 'log-out', roles: ['VigilanteSupervisor', 'JefeGuardia', 'Administrador', 'Admision'] },
    { key: 'actas', label: 'Actas', icon: 'file-check', roles: ['Admision', 'JefeGuardia', 'Administrador'] },
    { key: 'deudas', label: 'Deudas', icon: 'credit-card', roles: ['JefeGuardia', 'Administrador'] },
    { key: 'servicios', label: 'Por Servicio', icon: 'building-2', roles: ['SupervisoraEnfermeria', 'EnfermeriaLicenciada', 'Administrador'] },
  ];

  tabsVisibles: TabConfig[] = [];
  tabActivo: TabReporte = 'dashboard';

  // ─── Servicios del hospital (de GalenhosService) ─────────────────
  readonly serviciosHospital: string[] = [
    'Cirugía General',
    'Medicina Interna',
    'UCI - Unidad de Cuidados Intensivos',
    'UCINT - UCI Intermedia',
    'UVE1 - Unidad de Vigilancia 1',
    'UVE2 - Unidad de Vigilancia 2',
    'Emergencia',
    'Trauma Shock',
  ];

  // ─── Rangos de fecha ─────────────────────────────────────────────
  rangos: RangoFecha[] = [];
  rangoActivo = '7dias';
  fechaInicio!: Date;
  fechaFin!: Date;
  fechaInicioStr = '';
  fechaFinStr = '';
  mostrarFechasPersonalizadas = false;

  // ─── Estados de carga ────────────────────────────────────────────
  isLoading = false;
  isExportingPdf = false;
  isExportingExcel = false;

  // ─── Visor PDF ───────────────────────────────────────────────────
  pdfBlob: Blob | null = null;
  tituloPdf = '';

  // ─── Datos crudos ────────────────────────────────────────────────
  dashboard?: DashboardReportesDTO;
  permanencia: PermanenciaItemDTO[] = [];
  salidas: SalidaItemDTO[] = [];
  actas: ActaReportesItemDTO[] = [];
  deudas?: DeudaConsolidadaDTO;
  servicios: ServicioItemVM[] = [];

  estadSalidas?: EstadisticasSalida;
  estadActas?: ActaEstadisticasDTO;

  // ─── Arrays filtrados ─────────────────────────────────────────────
  permanenciaFiltrada: PermanenciaItemDTO[] = [];
  salidasFiltradas: SalidaItemDTO[] = [];
  actasFiltradas: ActaReportesItemDTO[] = [];
  serviciosFiltrados: ServicioItemVM[] = [];

  // ─── Arrays paginados ─────────────────────────────────────────────
  permanenciaPaginada: PermanenciaItemDTO[] = [];
  salidasPaginadas: SalidaItemDTO[] = [];
  actasPaginadas: ActaReportesItemDTO[] = [];
  serviciosPaginados: ServicioItemVM[] = [];

  // ─── Stats precalculadas ─────────────────────────────────────────
  totalActivos = 0;
  totalExcedidos = 0;
  totalConActa = 0;
  totalSinActa = 0;
  ocupacionEsCritica = false;
  ocupacionEsAlta = false;

  // ─── Filtros ─────────────────────────────────────────────────────
  filtroPermanencia = '';
  soloActivos = false;
  filtroSalidas = '';
  tipoSalidaFiltro = '';
  soloIncidentes = false;
  soloExcedidos = false;
  filtroActas = '';
  tipoActaFiltro = '';
  conBypass = false;
  filtroServicios = '';
  servicioFiltro = '';  // combobox — valor del select

  // ─── Paginación ──────────────────────────────────────────────────
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  paginasVisibles: number[] = [];

  Math = Math;

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();
    this.rangos = this.reportesService.getRangosFecha();
    this.tabsVisibles = this.tabs.filter(t => t.roles.includes(this.userRole));
    this.tabActivo = this.tabsVisibles[0]?.key ?? 'dashboard';
    this.aplicarRango('7dias');
    this.cargarDatosTab();
  }

  ngAfterViewInit(): void { }

  ngOnDestroy(): void {
    this.destruirCharts();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // HELPER CENTRALIZADO DE REQUESTS
  // ===================================================================

  private handleRequest<T>(
    obs$: Observable<T>,
    next: (data: T) => void,
    errorMsg = 'No se pudo cargar la información.'
  ): void {
    this.isLoading = true;
    obs$.pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isLoading = false; this.cdr.markForCheck(); })
    ).subscribe({ next, error: () => this.mostrarError(errorMsg) });
  }

  // ===================================================================
  // TABS
  // ===================================================================

  cambiarTab(tab: TabReporte): void {
    if (this.tabActivo === tab) return;
    this.tabActivo = tab;
    this.paginaActual = 1;
    this.destruirCharts();
    this.cargarDatosTab();
  }

  // ===================================================================
  // RANGOS DE FECHA
  // ===================================================================

  aplicarRango(key: string): void {
    this.rangoActivo = key;
    this.mostrarFechasPersonalizadas = key === 'personalizado';
    const rango = this.rangos.find(r => r.key === key);
    if (!rango || key === 'personalizado') return;
    this.fechaInicio = rango.fechaInicio;
    this.fechaFin = rango.fechaFin;
    this.fechaInicioStr = this.toInputDate(rango.fechaInicio);
    this.fechaFinStr = this.toInputDate(rango.fechaFin);
    this.cargarDatosTab();
  }

  aplicarFechasPersonalizadas(): void {
    if (!this.fechaInicioStr || !this.fechaFinStr) return;
    this.fechaInicio = new Date(this.fechaInicioStr + 'T00:00:00');
    this.fechaFin = new Date(this.fechaFinStr + 'T23:59:59');
    if (this.fechaInicio > this.fechaFin) {
      this.mostrarError('La fecha inicio no puede ser mayor a la fecha fin.');
      return;
    }
    this.cargarDatosTab();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  cargarDatosTab(): void {
    this.paginaActual = 1;
    switch (this.tabActivo) {
      case 'dashboard': this.cargarDashboard(); break;
      case 'permanencia': this.cargarPermanencia(); break;
      case 'salidas': this.cargarSalidas(); break;
      case 'actas': this.cargarActas(); break;
      case 'deudas': this.cargarDeudas(); break;
      case 'servicios': this.cargarServicios(); break;
    }
  }

  private cargarDashboard(): void {
    this.handleRequest(
      this.reportesService.getDashboard(this.fechaInicio, this.fechaFin),
      data => {
        this.dashboard = data;
        this.ocupacionEsCritica = (data.bandeja?.porcentajeOcupacion ?? 0) >= 90;
        this.ocupacionEsAlta = (data.bandeja?.porcentajeOcupacion ?? 0) >= 70;
        this.cdr.detectChanges();
        setTimeout(() => this.inicializarChartsDashboard());
      },
      'No se pudo cargar el resumen.'
    );
  }

  private cargarPermanencia(): void {
    this.handleRequest(
      this.reportesService.getPermanencia(this.fechaInicio, this.fechaFin, this.soloActivos),
      data => {
        this.permanencia = data;
        this.calcularStatsPermanencia();
        this.aplicarFiltrosPermanencia();
        this.cdr.detectChanges();
        setTimeout(() => this.inicializarChartsPermanencia());
      },
      'No se pudo cargar la permanencia.'
    );
  }

  private cargarSalidas(): void {
    this.handleRequest(
      this.reportesService.getSalidas(
        this.fechaInicio, this.fechaFin,
        this.tipoSalidaFiltro || undefined,
        this.soloIncidentes, this.soloExcedidos
      ),
      resp => {
        this.salidas = resp.salidas;
        this.estadSalidas = resp.estadisticas;
        this.aplicarFiltrosSalidas();
        this.cdr.detectChanges();
        setTimeout(() => this.inicializarChartSalidas());
      },
      'No se pudo cargar las salidas.'
    );
  }

  private cargarActas(): void {
    this.handleRequest(
      this.reportesService.getActas(
        this.fechaInicio, this.fechaFin,
        this.tipoActaFiltro || undefined,
        this.conBypass
      ),
      resp => {
        this.actas = resp.actas;
        this.estadActas = resp.estadisticas;
        this.aplicarFiltrosActas();
      },
      'No se pudo cargar las actas.'
    );
  }

  private cargarDeudas(): void {
    this.handleRequest(
      this.reportesService.getDeudas(this.fechaInicio, this.fechaFin),
      data => {
        this.deudas = data;
        this.cdr.detectChanges();
        setTimeout(() => this.inicializarChartDeudas());
      },
      'No se pudo cargar las deudas.'
    );
  }

  private cargarServicios(): void {
    this.handleRequest(
      this.reportesService.getExpedientesPorServicio(
        this.fechaInicio, this.fechaFin,
        this.servicioFiltro || undefined
      ),
      data => {
        this.servicios = data.map(e => ({ ...e, badge: getBadgeWithIcon(e.estadoActual) }));
        this.calcularStatsServicios();
        this.aplicarFiltrosServicios();
      },
      'No se pudo cargar los expedientes.'
    );
  }

  // ===================================================================
  // FILTROS
  // ===================================================================

  aplicarFiltrosPermanencia(): void {
    let data = [...this.permanencia];
    if (this.soloActivos) data = data.filter(p => p.estaActivo);
    if (this.filtroPermanencia.trim()) {
      const q = this.filtroPermanencia.toLowerCase();
      data = data.filter(p =>
        p.nombreCompleto.toLowerCase().includes(q) ||
        p.hc.toLowerCase().includes(q) ||
        p.codigoExpediente.toLowerCase().includes(q) ||
        p.codigoBandeja.toLowerCase().includes(q)
      );
    }
    this.permanenciaFiltrada = data;
    this.actualizarPaginacion();
  }

  aplicarFiltrosSalidas(): void {
    let data = [...this.salidas];
    if (this.filtroSalidas.trim()) {
      const q = this.filtroSalidas.toLowerCase();
      data = data.filter(s =>
        s.nombrePaciente.toLowerCase().includes(q) ||
        s.codigoExpediente.toLowerCase().includes(q) ||
        (s.responsableNombre?.toLowerCase().includes(q) ?? false)
      );
    }
    this.salidasFiltradas = data;
    this.actualizarPaginacion();
  }

  aplicarFiltrosActas(): void {
    let data = [...this.actas];
    if (this.filtroActas.trim()) {
      const q = this.filtroActas.toLowerCase();
      data = data.filter(a =>
        a.nombreCompleto.toLowerCase().includes(q) ||
        a.codigoExpediente.toLowerCase().includes(q) ||
        a.hc.toLowerCase().includes(q)
      );
    }
    if (this.conBypass) data = data.filter(a => a.tieneBypass);
    this.actasFiltradas = data;
    this.actualizarPaginacion();
  }

  aplicarFiltrosServicios(): void {
    let data = [...this.servicios];
    if (this.filtroServicios.trim()) {
      const q = this.filtroServicios.toLowerCase();
      data = data.filter(e =>
        e.nombreCompleto.toLowerCase().includes(q) ||
        e.hc.toLowerCase().includes(q) ||
        e.codigoExpediente.toLowerCase().includes(q)
      );
    }
    this.serviciosFiltrados = data;
    this.actualizarPaginacion();
  }

  // ===================================================================
  // STATS PRECALCULADAS
  // ===================================================================

  private calcularStatsPermanencia(): void {
    this.totalActivos = this.permanencia.filter(p => p.estaActivo).length;
    this.totalExcedidos = this.permanencia.filter(p => p.excedioLimite).length;
  }

  private calcularStatsServicios(): void {
    this.totalConActa = this.servicios.filter(e => e.tieneActa).length;
    this.totalSinActa = this.servicios.filter(e => !e.tieneActa).length;
  }

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================

  private actualizarPaginacion(): void {
    const datos = this.obtenerDatosTabActual();
    this.totalPaginas = Math.max(1, Math.ceil(datos.length / this.itemsPorPagina));
    if (this.paginaActual > this.totalPaginas) this.paginaActual = 1;
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    const fin = inicio + this.itemsPorPagina;
    switch (this.tabActivo) {
      case 'permanencia': this.permanenciaPaginada = (datos as PermanenciaItemDTO[]).slice(inicio, fin); break;
      case 'salidas': this.salidasPaginadas = (datos as SalidaItemDTO[]).slice(inicio, fin); break;
      case 'actas': this.actasPaginadas = (datos as ActaReportesItemDTO[]).slice(inicio, fin); break;
      case 'servicios': this.serviciosPaginados = (datos as ServicioItemVM[]).slice(inicio, fin); break;
    }
    this.calcularPaginasVisibles();
  }

  private obtenerDatosTabActual(): unknown[] {
    switch (this.tabActivo) {
      case 'permanencia': return this.permanenciaFiltrada;
      case 'salidas': return this.salidasFiltradas;
      case 'actas': return this.actasFiltradas;
      case 'servicios': return this.serviciosFiltrados;
      default: return [];
    }
  }

  private calcularPaginasVisibles(): void {
    const max = 5;
    let inicio = Math.max(1, this.paginaActual - 2);
    const fin = Math.min(this.totalPaginas, inicio + max - 1);
    if (fin - inicio < max - 1) inicio = Math.max(1, fin - max + 1);
    this.paginasVisibles = [];
    for (let i = inicio; i <= fin; i++) this.paginasVisibles.push(i);
  }

  irAPagina(p: number): void {
    if (p >= 1 && p <= this.totalPaginas) {
      this.paginaActual = p;
      this.actualizarPaginacion();
    }
  }

  // ===================================================================
  // EVENTOS DE FILTRO
  // ===================================================================

  onFiltroPermanenciaChange(): void { this.aplicarFiltrosPermanencia(); }
  onFiltroSalidasChange(): void { this.aplicarFiltrosSalidas(); }
  onFiltroActasChange(): void { this.aplicarFiltrosActas(); }
  onFiltroServiciosChange(): void { this.aplicarFiltrosServicios(); }
  onFiltroBackendSalidas(): void { this.cargarSalidas(); }
  onFiltroBackendActas(): void { this.cargarActas(); }

  /** Combobox servicios — dispara backend al seleccionar (no en cada tecla) */
  onServicioChange(): void { this.cargarServicios(); }

  onToggleSoloActivos(): void {
    this.soloActivos = !this.soloActivos;
    this.cargarPermanencia();
  }

  /** Limpiar todos los filtros del tab activo y recargar */
  limpiarFiltrosTab(): void {
    switch (this.tabActivo) {
      case 'permanencia':
        this.filtroPermanencia = '';
        this.soloActivos = false;
        this.cargarPermanencia();
        break;
      case 'salidas':
        this.filtroSalidas = '';
        this.tipoSalidaFiltro = '';
        this.soloIncidentes = false;
        this.soloExcedidos = false;
        this.cargarSalidas();
        break;
      case 'actas':
        this.filtroActas = '';
        this.tipoActaFiltro = '';
        this.conBypass = false;
        this.cargarActas();
        break;
      case 'servicios':
        this.filtroServicios = '';
        this.servicioFiltro = '';
        this.cargarServicios();
        break;
    }
  }

  // ===================================================================
  // CHARTS
  // ===================================================================

  private inicializarChartsDashboard(): void {
    if (!this.dashboard) return;
    this.destruirCharts();
    const donaEl = this.chartDonaDashboardRef?.nativeElement;
    if (donaEl) {
      const b = this.dashboard.bandeja;
      this.chartDonaDashboard = new Chart(donaEl, {
        type: 'doughnut',
        data: {
          labels: ['Disponibles', 'Ocupadas', 'Mantenimiento'],
          datasets: [{
            data: [b.disponibles, b.ocupadas, b.enMantenimiento],
            backgroundColor: ['#22c55e', '#0891b2', '#f97316'], borderWidth: 2, borderColor: '#fff'
          }]
        },
        options: {
          responsive: true, cutout: '70%',
          plugins: { legend: { position: 'bottom', labels: { font: { size: 11 } } } }
        }
      });
    }
    const barrasEl = this.chartBarrasDashboardRef?.nativeElement;
    if (barrasEl) {
      const s = this.dashboard.salidas;
      const v = this.dashboard.verificaciones;
      this.chartBarrasDashboard = new Chart(barrasEl, {
        type: 'bar',
        data: {
          labels: ['Familiar', 'Autoridad Legal', 'Verificadas', 'Rechazadas'],
          datasets: [{
            label: 'Cantidad',
            data: [s.salidasFamiliar, s.salidasAutoridadLegal, v.aprobadas, v.rechazadas],
            backgroundColor: ['#0891b2', '#8b5cf6', '#22c55e', '#ef4444'], borderRadius: 6
          }]
        },
        options: {
          responsive: true, plugins: { legend: { display: false } },
          scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
      });
    }
  }

  private inicializarChartsPermanencia(): void {
    if (!this.permanencia.length) return;
    this.destruirCharts();

    // Barras horizontal — top 10
    const barrasEl = this.chartBarrasPermanenciaRef?.nativeElement;
    if (barrasEl) {
      const top10 = [...this.permanencia]
        .sort((a, b) => b.tiempoMinutos - a.tiempoMinutos)
        .slice(0, 10);
      this.chartBarrasPermanencia = new Chart(barrasEl, {
        type: 'bar',
        data: {
          labels: top10.map(p => `${p.codigoBandeja} — ${p.nombreCompleto.split(',')[0]}`),
          datasets: [{
            label: 'Horas',
            data: top10.map(p => +(p.tiempoMinutos / 60).toFixed(1)),
            backgroundColor: top10.map(p =>
              p.excedioLimite ? '#ef4444' : p.tiempoMinutos > 24 * 60 ? '#f97316' : '#0891b2'),
            borderRadius: 4
          }]
        },
        options: {
          indexAxis: 'y', responsive: true,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: ctx => `${ctx.parsed.x}h` } }
          },
          scales: { x: { beginAtZero: true, title: { display: true, text: 'Horas' } } }
        }
      });
    }

    // Dona — activos vs retirados
    const donaEl = this.chartDonaPermanenciaRef?.nativeElement;
    if (donaEl) {
      this.chartDonaPermanencia = new Chart(donaEl, {
        type: 'doughnut',
        data: {
          labels: ['En mortuorio', 'Retirados'],
          datasets: [{
            data: [this.totalActivos, this.permanencia.length - this.totalActivos],
            backgroundColor: ['#0891b2', '#22c55e'], borderWidth: 2, borderColor: '#fff'
          }]
        },
        options: {
          responsive: true, cutout: '65%',
          plugins: { legend: { position: 'bottom', labels: { font: { size: 11 } } } }
        }
      });
    }
  }

  private inicializarChartSalidas(): void {
    if (!this.estadSalidas) return;
    this.destruirCharts();
    const el = this.chartDonaSalidasRef?.nativeElement;
    if (!el) return;
    this.chartDonaSalidas = new Chart(el, {
      type: 'doughnut',
      data: {
        labels: ['Familiar', 'Autoridad Legal'],
        datasets: [{
          data: [this.estadSalidas.salidasFamiliar, this.estadSalidas.salidasAutoridadLegal],
          backgroundColor: ['#0891b2', '#8b5cf6'], borderWidth: 2, borderColor: '#fff'
        }]
      },
      options: {
        responsive: true, cutout: '65%',
        plugins: { legend: { position: 'bottom', labels: { font: { size: 11 } } } }
      }
    });
  }

  private inicializarChartDeudas(): void {
    if (!this.deudas) return;
    this.destruirCharts();
    const el = this.chartDonaDeudasRef?.nativeElement;
    if (!el) return;
    const d = this.deudas;
    this.chartDonaDeudas = new Chart(el, {
      type: 'doughnut',
      data: {
        labels: ['Pendientes', 'Liquidadas', 'Exoneradas', 'Sin deuda'],
        datasets: [{
          data: [d.economicasPendientes, d.economicasLiquidadas, d.economicasExoneradas, d.economicasSinDeuda],
          backgroundColor: ['#ef4444', '#22c55e', '#8b5cf6', '#9ca3af'], borderWidth: 2, borderColor: '#fff'
        }]
      },
      options: {
        responsive: true, cutout: '65%',
        plugins: { legend: { position: 'bottom', labels: { font: { size: 11 } } } }
      }
    });
  }

  private destruirCharts(): void {
    this.chartDonaDashboard?.destroy();
    this.chartBarrasDashboard?.destroy();
    this.chartBarrasPermanencia?.destroy();
    this.chartDonaPermanencia?.destroy();
    this.chartDonaSalidas?.destroy();
    this.chartDonaDeudas?.destroy();
    this.chartDonaDashboard = this.chartBarrasDashboard =
      this.chartBarrasPermanencia = this.chartDonaPermanencia =
      this.chartDonaSalidas = this.chartDonaDeudas = undefined;
  }

  // ===================================================================
  // EXPORTACIÓN — visor PDF en lugar de descarga automática
  // ===================================================================

  exportar(tipo: 'pdf' | 'excel'): void {
    tipo === 'pdf' ? this.abrirVisorPdf() : this.exportarExcel();
  }

  private abrirVisorPdf(): void {
    const dto = { fechaInicio: this.fechaInicio, fechaFin: this.fechaFin };
    const obs$ = this.resolverExportarPdf(dto);
    if (!obs$) return;
    this.isExportingPdf = true;
    obs$.pipe(finalize(() => this.isExportingPdf = false))
      .subscribe({
        next: blob => {
          this.pdfBlob = blob;
          this.tituloPdf = `Reporte ${this.labelPeriodo} — ${this.tabActivo}`;
        },
        error: () => this.mostrarError('No se pudo generar el PDF.')
      });
  }

  private resolverExportarPdf(dto: { fechaInicio: Date; fechaFin: Date }) {
    switch (this.tabActivo) {
      case 'permanencia': return this.reportesService.exportarPermanenciaPdf({ ...dto, soloActivos: this.soloActivos });
      case 'salidas': return this.reportesService.exportarSalidasPdf(dto);
      case 'actas': return this.reportesService.exportarActasPdf(dto);
      case 'deudas': return this.reportesService.exportarDeudasPdf(dto);
      default: return null;
    }
  }

  cerrarPdf(): void { this.pdfBlob = null; }

  private exportarExcel(): void {
    this.isExportingExcel = true;
    try {
      const { datos, nombre } = this.prepararDatosExcel();
      if (!datos.length) { this.mostrarError('No hay datos para exportar.'); return; }
      const ws = XLSX.utils.json_to_sheet(datos);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Reporte');
      XLSX.writeFile(wb, nombre);
      this.mostrarExito('Excel generado correctamente.');
    } catch { this.mostrarError('No se pudo generar el Excel.'); }
    finally { this.isExportingExcel = false; }
  }

  private prepararDatosExcel(): { datos: Record<string, unknown>[]; nombre: string } {
    const fmt = (d: Date | string) => d ? new Date(d).toLocaleDateString('es-PE') : '—';
    const nombre = this.reportesService.generarNombreArchivo(
      this.tabActivo, this.fechaInicio, this.fechaFin, 'xlsx');
    switch (this.tabActivo) {
      case 'permanencia':
        return {
          nombre, datos: this.permanenciaFiltrada.map(p => ({
            'Bandeja': p.codigoBandeja, 'Código': p.codigoExpediente,
            'Paciente': p.nombreCompleto, 'HC': p.hc, 'Servicio': p.servicio,
            'Diagnóstico': p.diagnosticoFinal ?? '—',
            'Ingreso': fmt(p.fechaHoraIngreso),
            'Salida': p.fechaHoraSalida ? fmt(p.fechaHoraSalida) : 'En mortuorio',
            'Tiempo': p.tiempoLegible,
            'Responsable Retiro': p.responsableRetiro ?? '—',
            'Destino': p.destino ?? '—',
            'Obs/Médico': p.observacionesMedico ?? '—',
            'Estado': p.estaActivo ? 'En mortuorio' : 'Retirado',
            'Excedió 48h': p.excedioLimite ? 'Sí' : 'No',
          }))
        };
      case 'salidas':
        return {
          nombre, datos: this.salidasFiltradas.map(s => ({
            'Código': s.codigoExpediente, 'Paciente': s.nombrePaciente,
            'Fecha Salida': fmt(s.fechaHoraSalida), 'Tipo': s.tipoSalida,
            'Responsable': s.responsableNombre ?? '—',
            'Funeraria': s.nombreFuneraria ?? '—',
            'Permanencia': s.tiempoPermanenciaLegible ?? '—',
            'Incidente': s.incidenteRegistrado ? 'Sí' : 'No',
          }))
        };
      case 'actas':
        return {
          nombre, datos: this.actasFiltradas.map(a => ({
            'Código': a.codigoExpediente, 'Paciente': a.nombreCompleto, 'HC': a.hc,
            'Fecha': fmt(a.fechaRegistro), 'Tipo Salida': a.tipoSalida,
            'Estado Acta': a.estadoActa, 'Bypass': a.tieneBypass ? 'Sí' : 'No',
            'Médico Externo': a.tieneMedicoExterno ? 'Sí' : 'No',
            'PDF Firmado': a.tienePDFFirmado ? 'Sí' : 'No',
            'Responsable': a.responsableNombre ?? '—',
          }))
        };
      case 'servicios':
        return {
          nombre, datos: this.serviciosFiltrados.map(e => ({
            'Código': e.codigoExpediente, 'Paciente': e.nombreCompleto, 'HC': e.hc,
            'Servicio': e.servicio, 'Estado': e.estadoActual,
            'Bandeja': e.codigoBandeja ?? '—', 'Tiempo': e.tiempoEnMortuorio ?? '—',
            'Acta': e.tieneActa ? 'Sí' : 'No', 'Creado por': e.usuarioCreadorNombre,
          }))
        };
      default: return { datos: [], nombre };
    }
  }

  // ===================================================================
  // TRACKBY específicos por tipo
  // ===================================================================

  trackByTab(_: number, tab: TabConfig): string { return tab.key; }
  trackByRango(_: number, r: RangoFecha): string { return r.key; }
  trackByPagina(_: number, p: number): number { return p; }
  trackByPermanencia(_: number, p: PermanenciaItemDTO): number { return p.historialID; }
  trackBySalida(_: number, s: SalidaItemDTO): number { return s.salidaID; }
  trackByActa(_: number, a: ActaReportesItemDTO): number { return a.actaRetiroID; }
  trackByServicio(_: number, e: ServicioItemVM): number { return e.expedienteID; }
  trackByServicioStr(_: number, s: string): string { return s; }

  // ===================================================================
  // HELPERS DE TEMPLATE
  // ===================================================================

  get puedeExportarPdf(): boolean { return ['permanencia', 'salidas', 'actas', 'deudas'].includes(this.tabActivo); }
  get puedeExportarExcel(): boolean { return ['permanencia', 'salidas', 'actas', 'servicios'].includes(this.tabActivo); }

  get labelPeriodo(): string {
    return this.rangos.find(r => r.key === this.rangoActivo)?.label ?? 'Período';
  }

  get hayFiltrosActivos(): boolean {
    switch (this.tabActivo) {
      case 'permanencia': return !!this.filtroPermanencia || this.soloActivos;
      case 'salidas': return !!this.filtroSalidas || !!this.tipoSalidaFiltro || this.soloIncidentes || this.soloExcedidos;
      case 'actas': return !!this.filtroActas || !!this.tipoActaFiltro || this.conBypass;
      case 'servicios': return !!this.filtroServicios || !!this.servicioFiltro;
      default: return false;
    }
  }

  private toInputDate(d: Date): string { return d.toISOString().split('T')[0]; }

  private mostrarError(msg: string): void {
    Swal.fire({
      icon: 'error', title: 'Error', text: msg,
      toast: true, position: 'top-end', showConfirmButton: false,
      timer: 3500, timerProgressBar: true
    });
  }

  private mostrarExito(msg: string): void {
    Swal.fire({
      icon: 'success', title: msg,
      toast: true, position: 'top-end', showConfirmButton: false,
      timer: 2500, timerProgressBar: true
    });
  }
}
