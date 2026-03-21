import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil, interval, debounceTime } from 'rxjs';
import Swal from 'sweetalert2';

import { BandejaUniversalService, BandejaItem } from '../../services/bandeja-universal';
import { IconComponent } from '../../components/icon/icon.component';
import { AuthService } from '../../services/auth';
import { NotificacionService } from '../../services/notificacion';
import { getBadgeClasses } from '../../utils/badge-styles';

interface FiltroConfig {
  id: string;
  label: string;
  placeholder: string;
  campo: keyof BandejaItem | 'multiple';
  visible: boolean;
}

interface ConfiguracionFiltros {
  filtros: FiltroConfig[];
  permiteFiltroExpediente: boolean;
  camposPorDefecto: string[];
}

/**
 * Componente universal de bandeja de entrada.
 * Filtros, tabla y acciones adaptados dinámicamente según rol.
 */
@Component({
  selector: 'app-bandeja-universal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './bandeja-universal.html',
  styleUrl: './bandeja-universal.css'
})
export class BandejaUniversalComponent implements OnInit, OnDestroy {
  // ── Inyección ────────────────────────────────────────────────────
  private bandejaService = inject(BandejaUniversalService);
  private authService = inject(AuthService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);

  private destroy$ = new Subject<void>();
  private refreshTrigger$ = new Subject<void>();

  // ── Estado ───────────────────────────────────────────────────────
  items: BandejaItem[] = [];
  filteredItems: BandejaItem[] = [];
  paginatedItems: BandejaItem[] = [];

  isLoading = true;
  errorMessage = '';

  tituloBandeja = 'Bandeja de Tareas';
  descripcionBandeja = 'Gestiona tus pendientes';
  iconoBandeja = 'inbox';
  rolUsuario = '';

  // ── Configuración de tabla ───────────────────────────────────────
  labelColumna1 = 'Código';
  mostrarColumnaHC = true;
  esRolEnfermeria = false;

  // ── Filtros ──────────────────────────────────────────────────────
  configuracionFiltros: ConfiguracionFiltros = {
    filtros: [],
    permiteFiltroExpediente: false,
    camposPorDefecto: []
  };

  filtroExpediente = '';
  filtroHC = '';
  filtroNombre = '';
  filtroDocumento = '';
  filtroEstado = '';
  searchTerm = '';
  estadosDisponibles: string[] = [];

  // ── Estadísticas ─────────────────
  totalItemsSinFiltro = 0;
  totalItemsFiltrados = 0;
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
  ordenColumna: 'codigo' | 'paciente' | 'hc' | 'documento' | 'servicio' | 'fecha' | 'estado' = 'fecha';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // --- Helpers de template ---

  /** Colspan dinámico para filas de estado (loading/error/empty) */
  get colspanTotal(): number {
    let cols = 6; // paciente, doc, info, fecha, estado, acción
    if (this.mostrarColumnaHC) cols++;
    if (this.configuracionFiltros?.permiteFiltroExpediente) cols++;
    return cols;
  }
  // ── Lifecycle ────────────────────────────────────────────────────

  ngOnInit(): void {
    this.inicializarComponente();
    this.iniciarActualizacionFecha();
    this.configurarRecargaInteligente();
    this.suscribirseANotificacionesSignalR();
    this.suscribirseAEstadoConexion();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshTrigger$.complete();
  }

  // ── Inicialización ───────────────────────────────────────────────

  private inicializarComponente(): void {
    this.rolUsuario = this.authService.getUserRole();
    this.configurarContextoSegunRol();
    this.configurarFiltrosSegunRol();
    this.cargarItems();
  }

  /** Configura título, descripción, ícono y columnas según rol */
  private configurarContextoSegunRol(): void {
    const defaultConfig = {
      titulo: 'Bandeja de Entrada', descripcion: 'Tareas pendientes',
      icono: 'inbox', labelColumna1: 'N° Expediente',
      mostrarHC: true, esEnfermeria: false
    };

    const configs: Record<string, typeof defaultConfig> = {
      'EnfermeriaTecnica': {
        titulo: 'Pacientes Fallecidos', descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list', labelColumna1: 'Historia Clínica',
        mostrarHC: false, esEnfermeria: true
      },
      'EnfermeriaLicenciada': {
        titulo: 'Pacientes Fallecidos', descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list', labelColumna1: 'Historia Clínica',
        mostrarHC: false, esEnfermeria: true
      },
      'SupervisoraEnfermeria': {
        titulo: 'Pacientes Fallecidos', descripcion: 'Supervisión de expedientes',
        icono: 'clipboard-list', labelColumna1: 'Historia Clínica',
        mostrarHC: false, esEnfermeria: true
      },
      'Admision': {
        titulo: 'Solicitudes de Retiro', descripcion: 'Expedientes por autorizar para retiro familiar',
        icono: 'file-check', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      },
      'BancoSangre': {
        titulo: 'Deudas de Sangre', descripcion: 'Compromisos de reposición pendientes',
        icono: 'alert-circle', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      },
      'CuentasPacientes': {
        titulo: 'Deudas Económicas', descripcion: 'Pagos pendientes por regularizar',
        icono: 'alert-triangle', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      },
      'ServicioSocial': {
        titulo: 'Casos Sociales', descripcion: 'Expedientes que requieren evaluación social',
        icono: 'info', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      },
      'VigilanteSupervisor': {
        titulo: 'Validaciones Pendientes', descripcion: 'Documentos legales por verificar',
        icono: 'shield', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      },
      'JefeGuardia': {
        titulo: 'Solicitudes de Excepción', descripcion: 'Casos especiales que requieren autorización',
        icono: 'shield', labelColumna1: 'N° Expediente',
        mostrarHC: true, esEnfermeria: false
      }
    };

    const c = configs[this.rolUsuario] ?? defaultConfig;
    this.tituloBandeja = c.titulo;
    this.descripcionBandeja = c.descripcion;
    this.iconoBandeja = c.icono;
    this.labelColumna1 = c.labelColumna1;
    this.mostrarColumnaHC = c.mostrarHC;
    this.esRolEnfermeria = c.esEnfermeria;
  }

  /**
   * Enfermería: filtros sin expediente (los pacientes aún no tienen uno).
   * Otros roles: filtros con expediente incluido.
   */
  private configurarFiltrosSegunRol(): void {
    const rolesEnfermeria = ['EnfermeriaTecnica', 'EnfermeriaLicenciada', 'SupervisoraEnfermeria'];
    const esEnfermeria = rolesEnfermeria.includes(this.rolUsuario);

    const filtrosBase: FiltroConfig[] = [
      { id: 'hc', label: 'Historia Clínica', placeholder: 'Ej: 553830', campo: 'hc', visible: true },
      { id: 'nombre', label: 'Nombre del Paciente', placeholder: 'Buscar por nombre o apellido', campo: 'nombreCompleto', visible: true },
      { id: 'documento', label: 'N° Documento', placeholder: 'DNI, CE, Pasaporte', campo: 'numeroDocumento', visible: true },
      { id: 'estado', label: 'Estado', placeholder: 'Filtrar por estado', campo: 'estadoTexto', visible: true }
    ];

    const filtroExpediente: FiltroConfig = {
      id: 'expediente', label: 'N° Expediente', placeholder: 'Ej: SGM-2025-00123',
      campo: 'codigoExpediente', visible: !esEnfermeria
    };

    this.configuracionFiltros = {
      filtros: esEnfermeria ? filtrosBase : [filtroExpediente, ...filtrosBase],
      permiteFiltroExpediente: !esEnfermeria,
      camposPorDefecto: esEnfermeria
        ? ['hc', 'nombreCompleto', 'numeroDocumento', 'estadoTexto']
        : ['codigoExpediente', 'hc', 'nombreCompleto', 'numeroDocumento', 'estadoTexto']
    };
  }

  private iniciarActualizacionFecha(): void {
    interval(60000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.fechaActual = new Date();
    });
  }

  // ── SignalR ──────────────────────────────────────────────────────

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
        // Actualización optimista local
        const local = this.items.find(i => i.id === n.expedienteId || i.codigoExpediente === n.codigoExpediente);
        if (local && n.estadoNuevo) {
          local.estadoTexto = n.estadoNuevo;
          this.aplicarFiltros();
        }
        this.refreshTrigger$.next();
      });

    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe(b => {
        this.ultimaActualizacionSignalR = new Date();
        if (this.items.some(i => i.bandeja === b.codigo)) {
          this.refreshTrigger$.next();
        }
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

  // ── Carga de datos ───────────────────────────────────────────────

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
          console.error('Error al cargar bandeja:', err);
          this.errorMessage = 'No se pudieron cargar los elementos. Intenta nuevamente.';
          this.isLoading = false;
        }
      });
  }

  private extraerEstadosDisponibles(): void {
    this.estadosDisponibles = Array.from(new Set(this.items.map(i => i.estadoTexto))).sort();
  }

  recargar(): void {
    this.limpiarFiltros();
    this.cargarItems();
  }

  // ── Filtrado ─────────────────────────────────────────────────────

  aplicarFiltros(): void {
    let resultados = [...this.items];
    resultados = this.aplicarFiltrosEspecificos(resultados);
    if (this.searchTerm.trim()) resultados = this.aplicarBusquedaGlobal(resultados);
    this.ordenarResultados(resultados);
    this.filteredItems = resultados;
    this.totalItemsFiltrados = resultados.length;
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
      r = r.filter(i => i.numeroDocumento?.toLowerCase().includes(t) || i.tipoDocumento?.toLowerCase().includes(t));
    }
    if (this.filtroEstado) {
      r = r.filter(i => i.estadoTexto === this.filtroEstado);
    }
    return r;
  }

  private aplicarBusquedaGlobal(items: BandejaItem[]): BandejaItem[] {
    const t = this.searchTerm.toLowerCase();
    return items.filter(item => {
      const campos = [
        this.configuracionFiltros.permiteFiltroExpediente ? item.codigoExpediente : null,
        item.hc, item.nombreCompleto, item.numeroDocumento,
        item.tipoDocumento, item.servicio, item.bandeja, item.estadoTexto
      ].filter(Boolean) as string[];
      return campos.some(c => c.toLowerCase().includes(t));
    });
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

  // ── Ordenamiento ─────────────────────────────────────────────────

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
        case 'fecha': vA = a.fechaFallecimiento ?? a.fechaIngreso ?? new Date(0);
          vB = b.fechaFallecimiento ?? b.fechaIngreso ?? new Date(0); break;
        case 'estado': vA = a.estadoTexto.toLowerCase(); vB = b.estadoTexto.toLowerCase(); break;
        default: vA = a.nombreCompleto; vB = b.nombreCompleto;
      }
      if (vA < vB) return this.ordenDireccion === 'asc' ? -1 : 1;
      if (vA > vB) return this.ordenDireccion === 'asc' ? 1 : -1;
      return 0;
    });
  }

  // ── Paginación ───────────────────────────────────────────────────

  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.totalItemsFiltrados / this.itemsPorPagina) || 1;
    this.actualizarPaginaVisible();
  }

  actualizarPaginaVisible(): void {
    this.paginaActual = Math.min(Math.max(this.paginaActual, 1), this.totalPaginas);
    this.paginaInicio = (this.paginaActual - 1) * this.itemsPorPagina;
    this.paginaFin = Math.min(this.paginaInicio + this.itemsPorPagina, this.totalItemsFiltrados);
    this.paginatedItems = this.filteredItems.slice(this.paginaInicio, this.paginaFin);
  }

  paginaAnterior(): void { if (this.paginaActual > 1) { this.paginaActual--; this.actualizarPaginaVisible(); } }
  paginaSiguiente(): void { if (this.paginaActual < this.totalPaginas) { this.paginaActual++; this.actualizarPaginaVisible(); } }

  // ── Acciones ─────────────────────────────────────────────────────

  /**
   * Acción principal según tipoItem.
   * Navegación usa solo queryParams (sin state — no persiste en recarga).
   */
  ejecutarAccion(item: BandejaItem): void {
    switch (item.tipoItem) {
      case 'generacion_expediente':
        // Solo pasa el HC; el formulario consulta al backend para obtener el resto
        this.router.navigate(['/nuevo-expediente'], {
          queryParams: { hc: item.hc, origen: 'bandeja' }
        });
        break;

      case 'aceptar_custodia':
        // Navega al flujo de custodia de ambulancia
        this.router.navigate(['/custodia', item.expedienteID]);
        break;

      case 'deuda_sangre':
        this.mostrarModalRegularizacionSangre(item);
        break;

      case 'deuda_economica':
        Swal.fire({ icon: 'info', title: 'Gestión de Caja', text: 'Redirigiendo al sistema de pagos...', timer: 1500, showConfirmButton: false });
        break;

      case 'autorizacion_retiro':
        Swal.fire({ icon: 'info', title: 'Módulo en construcción', text: 'La autorización de retiro se implementará próximamente', confirmButtonColor: '#0891B2' });
        break;

      default:
        Swal.fire({ icon: 'info', title: 'En construcción', text: 'Esta funcionalidad estará disponible próximamente', confirmButtonColor: '#0891B2' });
    }
  }

  // ── Modales ──────────────────────────────────────────────────────

  private mostrarModalRegularizacionSangre(item: BandejaItem): void {
    Swal.fire({
      title: 'Regularizar Deuda de Sangre',
      html: `
        <div class="text-left mb-4">
          <p class="font-semibold text-gray-700">Paciente: ${item.nombreCompleto}</p>
          <p class="text-sm text-gray-600 mt-2">HC: ${item.hc}</p>
          ${item.unidadesSangre ? `<p class="text-sm text-gray-600">Pendiente: ${item.unidadesSangre} unidades</p>` : ''}
        </div>
      `,
      input: 'select',
      inputOptions: { compromiso: 'Compromiso Firmado', reposicion: 'Reposición Realizada', anulacion: 'Anulación Médica' },
      inputPlaceholder: 'Seleccione el estado',
      showCancelButton: true,
      confirmButtonText: 'Guardar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      inputValidator: v => !v ? 'Debes seleccionar una opción' : null
    }).then(r => {
      if (r.isConfirmed && r.value) this.simularAccionBackend(item.id, 'Deuda de sangre regularizada correctamente');
    });
  }

  private simularAccionBackend(id: string | number, mensaje: string): void {
    Swal.fire({ icon: 'success', title: '¡Éxito!', text: mensaje, timer: 2000, showConfirmButton: false });
    this.items = this.items.filter(i => i.id !== id);
    this.aplicarFiltros();
  }

  // ── Helpers visuales ─────────────────────────────────────────────

  getBadgeClasses(estado: string): string {
    return getBadgeClasses(estado);
  }

  /** @param horas Horas transcurridas */
  formatearTiempo(horas: number | undefined): string {
    if (!horas) return '';
    if (horas < 1) return '< 1h';
    if (horas < 24) return `${Math.round(horas)}h`;
    const d = Math.floor(horas / 24);
    const h = Math.round(horas % 24);
    return h === 0 ? `${d}d` : `${d}d ${h}h`;
  }

  private mostrarToast(tipo: 'success' | 'error' | 'warning' | 'info', titulo: string, texto: string): void {
    const colores: Record<string, { bg: string; icon: string }> = {
      success: { bg: '#ECFDF5', icon: '#10B981' },
      error: { bg: '#FEE2E2', icon: '#EF4444' },
      warning: { bg: '#FEF3C7', icon: '#F59E0B' },
      info: { bg: '#EFF6FF', icon: '#0891B2' }
    };
    const c = colores[tipo] ?? colores['info'];
    Swal.fire({ icon: tipo, title: titulo, text: texto, toast: true, position: 'top-end', showConfirmButton: false, timer: 4000, timerProgressBar: true, background: c.bg, iconColor: c.icon });
  }
}
