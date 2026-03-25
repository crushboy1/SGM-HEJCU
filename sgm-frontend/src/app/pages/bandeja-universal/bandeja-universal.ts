import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, interval, debounceTime } from 'rxjs';
import Swal from 'sweetalert2';

import { BandejaUniversalService, BandejaItem } from '../../services/bandeja-universal';
import { IconComponent } from '../../components/icon/icon.component';
import { AuthService } from '../../services/auth';
import { NotificacionService } from '../../services/notificacion';
import { FormularioGenerarExpediente } from '../../components/formulario-generar-expediente/formulario-generar-expediente';
import { getBadgeClasses } from '../../utils/badge-styles';

interface ConfiguracionFiltros {
  permiteFiltroExpediente: boolean;
}

@Component({
  selector: 'app-bandeja-universal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, FormularioGenerarExpediente],
  templateUrl: './bandeja-universal.html',
  styleUrl: './bandeja-universal.css'
})
export class BandejaUniversalComponent implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private bandejaService = inject(BandejaUniversalService);
  private authService = inject(AuthService);
  private notificacionService = inject(NotificacionService);

  private destroy$ = new Subject<void>();
  private refreshTrigger$ = new Subject<void>();

  // ── Estado general ───────────────────────────────────────────────
  items: BandejaItem[] = [];
  filteredItems: BandejaItem[] = [];
  paginatedItems: BandejaItem[] = [];

  isLoading = true;
  errorMessage = '';

  tituloBandeja = 'Bandeja de Tareas';
  descripcionBandeja = 'Gestiona tus pendientes';
  iconoBandeja = 'inbox';
  rolUsuario = '';

  // ── Modal: Generar Expediente ────────────────────────────────────
  // El componente hijo recibe el HC y hace la llamada HTTP internamente.
  // Mismo patrón que expediente-create con queryParam ?hc=xxx.
  mostrarModalExpediente = false;
  hcSeleccionado: string | null = null;

  // ── Filtros ──────────────────────────────────────────────────────
  configuracionFiltros: ConfiguracionFiltros = {
    permiteFiltroExpediente: false
  };

  filtroExpediente = '';
  filtroHC = '';
  filtroNombre = '';
  filtroDocumento = '';
  filtroEstado = '';
  searchTerm = '';
  estadosDisponibles: string[] = [];

  // ── KPIs ─────────────────────────────────────────────────────────
  totalItemsSinFiltro = 0;
  itemsUrgentes = 0;

  fechaActual: Date = new Date();
  ultimaActualizacionSignalR: Date | null = null;
  conexionSignalREstablecida = false;

  // ── Paginación ───────────────────────────────────────────────────
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  paginaInicio = 0;
  paginaFin = 0;

  // ── Ordenamiento ─────────────────────────────────────────────────
  ordenColumna: 'codigo' | 'paciente' | 'hc' | 'documento' | 'servicio' | 'fecha' | 'estado'
    = 'fecha';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    this.rolUsuario = this.authService.getUserRole();
    this.configurarContextoSegunRol();
    this.cargarItems();
    this.iniciarActualizacionFecha();
    this.configurarRecargaInteligente();
    this.suscribirseAEstadoConexion();
    this.suscribirseANotificacionesSignalR();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshTrigger$.complete();
  }

  // ===================================================================
  // CONFIGURACIÓN POR ROL
  // ===================================================================

  private configurarContextoSegunRol(): void {
    const configs: Record<string, {
      titulo: string; descripcion: string; icono: string;
      filtroExpediente: boolean;
    }> = {
      'EnfermeriaTecnica': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list', filtroExpediente: false
      },
      'EnfermeriaLicenciada': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list', filtroExpediente: false
      },
      'SupervisoraEnfermeria': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Supervisión de expedientes mortuorios',
        icono: 'clipboard-list', filtroExpediente: false
      },
      'Ambulancia': {
        titulo: 'Pendientes de Recojo',
        descripcion: 'Expedientes listos para traslado al mortuorio',
        icono: 'truck', filtroExpediente: true
      },
      'VigilanteSupervisor': {
        titulo: 'Control de Ingreso',
        descripcion: 'Familias en puerta — verificación de deudas',
        icono: 'shield', filtroExpediente: true
      },
      'JefeGuardia': {
        titulo: 'Solicitudes de Excepción',
        descripcion: 'Casos especiales que requieren autorización',
        icono: 'shield', filtroExpediente: true
      },
      'Administrador': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Vista administrador — bandeja de enfermería',
        icono: 'clipboard-list', filtroExpediente: false
      },
    };

    const c = configs[this.rolUsuario] ?? {
      titulo: 'Bandeja de Entrada', descripcion: 'Tareas pendientes',
      icono: 'inbox', filtroExpediente: false
    };

    this.tituloBandeja = c.titulo;
    this.descripcionBandeja = c.descripcion;
    this.iconoBandeja = c.icono;
    this.configuracionFiltros.permiteFiltroExpediente = c.filtroExpediente;
  }

  // ===================================================================
  // SIGNALR
  // ===================================================================

  private iniciarActualizacionFecha(): void {
    interval(60000).pipe(takeUntil(this.destroy$))
      .subscribe(() => this.fechaActual = new Date());
  }

  private configurarRecargaInteligente(): void {
    this.refreshTrigger$
      .pipe(takeUntil(this.destroy$), debounceTime(500))
      .subscribe(() => this.cargarItems());
  }

  private suscribirseAEstadoConexion(): void {
    this.notificacionService.conexionEstablecida
      .pipe(takeUntil(this.destroy$))
      .subscribe(conectado => {
        this.conexionSignalREstablecida = conectado;
        if (conectado) this.ultimaActualizacionSignalR = new Date();
      });
  }

  private suscribirseANotificacionesSignalR(): void {
    this.notificacionService.onNuevoExpediente
      .pipe(takeUntil(this.destroy$))
      .subscribe(n => {
        this.ultimaActualizacionSignalR = new Date();
        this.mostrarToast('info', 'Nuevo Expediente', n.titulo);
        this.refreshTrigger$.next();
      });

    this.notificacionService.onExpedienteActualizado
      .pipe(takeUntil(this.destroy$))
      .subscribe(n => {
        this.ultimaActualizacionSignalR = new Date();
        const local = this.items.find(
          i => i.id === n.expedienteId || i.codigoExpediente === n.codigoExpediente
        );
        if (local && n.estadoNuevo) {
          local.estadoTexto = n.estadoNuevo;
          this.aplicarFiltros();
        }
        this.refreshTrigger$.next();
      });

    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.ultimaActualizacionSignalR = new Date();
        this.refreshTrigger$.next();
      });

    this.notificacionService.onNotificacionGenerica
      .pipe(takeUntil(this.destroy$))
      .subscribe(n => {
        this.ultimaActualizacionSignalR = new Date();
        const esRelevante =
          n.titulo.toLowerCase().includes('expediente') ||
          n.titulo.toLowerCase().includes('bandeja') ||
          n.mensaje.toLowerCase().includes(this.rolUsuario.toLowerCase());
        if (esRelevante) {
          this.mostrarToast(n.tipo as any, n.titulo, n.mensaje);
          this.refreshTrigger$.next();
        }
      });
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  cargarItems(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.bandejaService.getItems()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.items = data;
          this.totalItemsSinFiltro = data.length;
          this.itemsUrgentes = data.filter(i => i.esUrgente).length;
          this.extraerEstadosDisponibles();
          this.aplicarFiltros();
          this.isLoading = false;
        },
        error: err => {
          console.error('[BandejaUniversal] Error al cargar:', err);
          this.errorMessage = 'No se pudieron cargar los elementos. Intenta nuevamente.';
          this.isLoading = false;
        }
      });
  }

  private extraerEstadosDisponibles(): void {
    this.estadosDisponibles = Array.from(
      new Set(this.items.map(i => i.estadoTexto))
    ).sort();
  }

  recargar(): void {
    this.limpiarFiltros();
    this.cargarItems();
  }

  // ===================================================================
  // FILTRADO
  // ===================================================================

  aplicarFiltros(): void {
    let r = [...this.items];
    r = this.aplicarFiltrosEspecificos(r);
    if (this.searchTerm.trim()) r = this.aplicarBusquedaGlobal(r);
    this.ordenarResultados(r);
    this.filteredItems = r;
    this.paginaActual = 1;
    this.calcularPaginacion();
  }

  private aplicarFiltrosEspecificos(items: BandejaItem[]): BandejaItem[] {
    let r = items;
    if (this.configuracionFiltros.permiteFiltroExpediente && this.filtroExpediente.trim()) {
      const t = this.filtroExpediente.toLowerCase();
      r = r.filter(i => i.codigoExpediente?.toLowerCase().includes(t));
    }
    if (this.filtroHC.trim()) {
      const t = this.filtroHC.toLowerCase();
      r = r.filter(i => i.hc?.toLowerCase().includes(t));
    }
    if (this.filtroNombre.trim()) {
      const t = this.filtroNombre.toLowerCase();
      r = r.filter(i => i.nombreCompleto?.toLowerCase().includes(t));
    }
    if (this.filtroDocumento.trim()) {
      const t = this.filtroDocumento.toLowerCase();
      r = r.filter(i =>
        i.numeroDocumento?.toLowerCase().includes(t) ||
        i.tipoDocumento?.toLowerCase().includes(t)
      );
    }
    if (this.filtroEstado) {
      r = r.filter(i => i.estadoTexto === this.filtroEstado);
    }
    return r;
  }

  private aplicarBusquedaGlobal(items: BandejaItem[]): BandejaItem[] {
    const t = this.searchTerm.toLowerCase();
    return items.filter(item =>
      [item.codigoExpediente, item.hc, item.nombreCompleto,
      item.numeroDocumento, item.tipoDocumento, item.servicio,
      item.bandeja, item.estadoTexto]
        .filter(Boolean)
        .some(c => c!.toLowerCase().includes(t))
    );
  }

  limpiarFiltros(): void {
    this.filtroExpediente = '';
    this.filtroHC = '';
    this.filtroNombre = '';
    this.filtroDocumento = '';
    this.filtroEstado = '';
    this.searchTerm = '';
    this.aplicarFiltros();
  }

  get filtrosActivos(): number {
    return [this.filtroExpediente, this.filtroHC, this.filtroNombre,
    this.filtroDocumento, this.filtroEstado, this.searchTerm]
      .filter(v => v).length;
  }

  // ===================================================================
  // ORDENAMIENTO
  // ===================================================================

  ordenarPor(columna: typeof this.ordenColumna): void {
    this.ordenDireccion = this.ordenColumna === columna
      ? (this.ordenDireccion === 'asc' ? 'desc' : 'asc')
      : 'asc';
    this.ordenColumna = columna;
    this.aplicarFiltros();
  }

  private ordenarResultados(lista: BandejaItem[]): void {
    lista.sort((a, b) => {
      let vA: any, vB: any;
      switch (this.ordenColumna) {
        case 'codigo': vA = a.id; vB = b.id; break;
        case 'paciente': vA = a.nombreCompleto.toLowerCase(); vB = b.nombreCompleto.toLowerCase(); break;
        case 'hc': vA = a.hc ?? ''; vB = b.hc ?? ''; break;
        case 'documento': vA = a.numeroDocumento ?? ''; vB = b.numeroDocumento ?? ''; break;
        case 'servicio': vA = a.servicio ?? ''; vB = b.servicio ?? ''; break;
        case 'fecha':
          vA = a.fechaFallecimiento ?? a.fechaIngreso ?? new Date(0);
          vB = b.fechaFallecimiento ?? b.fechaIngreso ?? new Date(0);
          break;
        case 'estado': vA = a.estadoTexto.toLowerCase(); vB = b.estadoTexto.toLowerCase(); break;
        default: vA = a.nombreCompleto; vB = b.nombreCompleto;
      }
      if (vA < vB) return this.ordenDireccion === 'asc' ? -1 : 1;
      if (vA > vB) return this.ordenDireccion === 'asc' ? 1 : -1;
      return 0;
    });
  }

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================

  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.filteredItems.length / this.itemsPorPagina) || 1;
    this.actualizarPaginaVisible();
  }

  actualizarPaginaVisible(): void {
    this.paginaActual = Math.min(Math.max(this.paginaActual, 1), this.totalPaginas);
    this.paginaInicio = (this.paginaActual - 1) * this.itemsPorPagina;
    this.paginaFin = Math.min(this.paginaInicio + this.itemsPorPagina, this.filteredItems.length);
    this.paginatedItems = this.filteredItems.slice(this.paginaInicio, this.paginaFin);
  }

  paginaAnterior(): void {
    if (this.paginaActual > 1) { this.paginaActual--; this.actualizarPaginaVisible(); }
  }

  paginaSiguiente(): void {
    if (this.paginaActual < this.totalPaginas) { this.paginaActual++; this.actualizarPaginaVisible(); }
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  ejecutarAccion(item: BandejaItem): void {
    switch (item.tipoItem) {

      case 'generacion_expediente':
        this.abrirModalGenerarExpediente(item);
        break;

      case 'aceptar_custodia':
        // TODO: implementar cuando se defina el flujo sin hardware móvil.
        // Opciones: (a) tabla de selección en módulo Mis Tareas de Ambulancia,
        // (b) confirmación desde PC mortuorio al llegar.
        // Ruta /custodia/:id pendiente de crear.
        this.mostrarToast('info', 'Módulo Pendiente',
          'La aceptación de custodia se gestiona desde Mis Tareas.');
        break;

      case 'autorizacion_retiro':
        // TODO: confirmar ruta exacta de validar-admision en el router
        // this.router.navigate(['/administrativo/validar-admision']);
        this.mostrarToast('info', 'Autorización de Retiro',
          'Gestione la autorización desde el módulo Validar Admisión.');
        break;

      default:
        Swal.fire({
          icon: 'info',
          title: 'En Construcción',
          text: 'Esta funcionalidad estará disponible próximamente.',
          confirmButtonColor: '#0891B2'
        });
    }
  }

  // ===================================================================
  // MODAL: GENERAR EXPEDIENTE
  // El componente hijo recibe el HC y hace la llamada HTTP internamente.
  // Mismo patrón que expediente-create con queryParam ?hc=xxx.
  // ===================================================================

  /**
   * Abre el modal pasando solo el HC.
   * El componente FormularioGenerarExpediente consulta la integración
   * en su propio ngOnInit — evita el problema de @Input timing con *ngIf.
   */
  abrirModalGenerarExpediente(item: BandejaItem): void {
    this.hcSeleccionado = item.hc ?? String(item.id);
    this.mostrarModalExpediente = true;
  }

  onExpedienteCreado(codigoExpediente: string): void {
    this.mostrarModalExpediente = false;
    this.hcSeleccionado = null;
    this.mostrarToast('success', 'Expediente Generado',
      `${codigoExpediente} registrado correctamente`);
    this.cargarItems();
  }

  cerrarModalExpediente(): void {
    this.mostrarModalExpediente = false;
    this.hcSeleccionado = null;
  }

  // ===================================================================
  // HELPERS
  // ===================================================================

  trackByItemId(_index: number, item: BandejaItem): string | number {
    return item.id;
  }

  getBadgeClasses(estado: string): string {
    return getBadgeClasses(estado);
  }

  /**
   * Formatea horas transcurridas.
   * >24h muestra "Xd Yh" en lugar de "433h" — consistente con Mapa Mortuorio.
   */
  formatearTiempo(horas: number | undefined): string {
    if (!horas) return '';
    if (horas < 1) return '< 1h';
    if (horas < 24) return `${Math.round(horas)}h`;
    const d = Math.floor(horas / 24);
    const h = Math.round(horas % 24);
    return h === 0 ? `${d}d` : `${d}d ${h}h`;
  }

  private mostrarToast(
    tipo: 'success' | 'error' | 'warning' | 'info',
    titulo: string,
    texto: string
  ): void {
    const colores: Record<string, { bg: string; icon: string }> = {
      success: { bg: '#ECFDF5', icon: '#10B981' },
      error: { bg: '#FEE2E2', icon: '#EF4444' },
      warning: { bg: '#FEF3C7', icon: '#F59E0B' },
      info: { bg: '#EFF6FF', icon: '#0891B2' }
    };
    const c = colores[tipo] ?? colores['info'];
    Swal.fire({
      icon: tipo, title: titulo, text: texto,
      toast: true, position: 'top-end',
      showConfirmButton: false, timer: 4000, timerProgressBar: true,
      background: c.bg, iconColor: c.icon
    });
  }
}
