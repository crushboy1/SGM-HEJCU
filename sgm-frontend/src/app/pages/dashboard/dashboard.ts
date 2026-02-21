import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil, debounceTime } from 'rxjs';
import Swal from 'sweetalert2';

import { ExpedienteService, Expediente } from '../../services/expediente';
import { DashboardService, DashboardKPIs } from '../../services/dashboard';
import { NotificacionService } from '../../services/notificacion';
import { IconComponent } from '../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../utils/badge-styles';
import { AuthService } from '../../services/auth';
import { EstadisticasBandejaDTO } from '../../models/notificacion.model';

/**
 * Dashboard v2.0 - Panel de Control SGM
 * 
 * CHANGELOG v2.0:
 * - ✅ Filtros y paginación para tabla de expedientes
 * - ✅ Ordenamiento por columnas
 * - ✅ UIState optimizado para evitar recálculos
 * - ✅ SignalR con indicador de conexión en vivo
 * - ✅ Preparado para métricas específicas por rol (futuro)
 * - ✅ Debounce inteligente para actualizaciones masivas
 */

// Interfaz para el estado visual (evita recálculos en el template)
interface UIState {
  ocupacionColor: string;
  ocupacionBadgeClass: string;
  progressBarClass: string;
  mantenimientoCardClass: string;
  mantenimientoIconColor: string;
  mantenimientoTextColor: string;
}

// Interfaz para filtros de tabla
interface FiltrosTabla {
  busqueda: string;
  estado: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private expedienteService = inject(ExpedienteService);
  private dashboardService = inject(DashboardService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);

  // Subject para cleanup de suscripciones
  private destroy$ = new Subject<void>();

  // Subject para controlar recargas masivas (Debounce)
  private refreshTrigger$ = new Subject<void>();

  // ===================================================================
  // DATOS DEL DASHBOARD
  // ===================================================================
  kpis: DashboardKPIs | null = null;
  expedientes: Expediente[] = [];
  expedientesRecientes: Expediente[] = [];

  // ⭐ NUEVO: Filtros y paginación para tabla
  filtros: FiltrosTabla = {
    busqueda: '',
    estado: ''
  };
  expedientesFiltrados: Expediente[] = [];
  expedientesPaginados: Expediente[] = [];

  // Ordenamiento
  ordenColumna: 'hc' | 'nombre' | 'fecha' | '' = '';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // Paginación
  paginaActual = 1;
  itemsPorPagina = 5;
  totalPaginas = 1;

  // Estados de carga
  isLoadingKPIs = true;
  isLoadingExpedientes = true;
  errorKPIs: string | null = null;
  errorExpedientes: string | null = null;

  // Usuario
  userName: string = '';
  userRole: string = '';

  // Estado de alertas y conexión en tiempo real
  alertaOcupacionActiva = false;
  ultimaActualizacionSignalR: Date | null = null;
  conexionSignalREstablecida = false;

  // Estado visual pre-calculado (mejora rendimiento de renderizado)
  uiState: UIState = {
    ocupacionColor: 'text-gray-500',
    ocupacionBadgeClass: 'bg-gray-100 text-gray-600',
    progressBarClass: 'bg-green-500',
    mantenimientoCardClass: 'bg-gray-50 border-gray-200',
    mantenimientoIconColor: 'text-gray-600 bg-gray-100',
    mantenimientoTextColor: 'text-gray-600'
  };

  Math = Math;

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();

    // Cargar datos iniciales (con deudas si aplica)
    this.cargarKPIs();
    this.cargarExpedientes();

    this.configurarRecargaInteligente();
    this.suscribirseAAlertasSignalR();
    this.suscribirseAEstadoConexion();
  }

  // ===================================================================
  // SIGNALR - ALERTAS EN TIEMPO REAL
  // ===================================================================

  /**
   * Suscribe al estado de la conexión para actualizar el indicador visual (Punto Verde/Rojo)
   */
  private suscribirseAEstadoConexion(): void {
    this.notificacionService.conexionEstablecida
      .pipe(takeUntil(this.destroy$))
      .subscribe(conectado => {
        this.conexionSignalREstablecida = conectado;
        if (conectado) {
          console.log('Dashboard: Conexión SignalR activa');
          this.ultimaActualizacionSignalR = new Date();
        } else {
          console.warn('Dashboard: Conexión SignalR perdida o reconectando...');
        }
      });
  }

  /**
   * Configura el debounce para evitar spam de peticiones al backend
   * si llegan muchas actualizaciones de SignalR seguidas.
   */
  private configurarRecargaInteligente(): void {
    this.refreshTrigger$
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(500) // Espera 500ms tras el último evento
      )
      .subscribe(() => {
        console.log(' Dashboard: Ejecutando recarga inteligente de datos...');
        this.cargarKPIs();
      });
  }

  /**
   * Suscribe el componente a las alertas de ocupación de SignalR.
   * Actualiza las estadísticas del mortuorio en tiempo real.
   */
  private suscribirseAAlertasSignalR(): void {
    // Alerta de ocupación (>70%)
    this.notificacionService.onAlertaOcupacion
      .pipe(takeUntil(this.destroy$))
      .subscribe((estadisticas: EstadisticasBandejaDTO) => {
        console.log(' Dashboard: Alerta de ocupación recibida', estadisticas);

        // Actualizar KPIs con datos en tiempo real (Actualización Optimista)
        if (this.kpis) {
          this.kpis.bandejas = {
            total: estadisticas.total,
            disponibles: estadisticas.disponibles,
            ocupadas: estadisticas.ocupadas,
            enMantenimiento: estadisticas.enMantenimiento,
            porcentajeOcupacion: estadisticas.porcentajeOcupacion,
            conAlerta24h: estadisticas.conAlerta24h,
            conAlerta48h: estadisticas.conAlerta48h
          };

          this.alertaOcupacionActiva = estadisticas.porcentajeOcupacion > 70;
          this.ultimaActualizacionSignalR = new Date();

          // Recalcular estilos visuales inmediatamente
          this.calcularEstadoVisual();
        }

        // Mostrar toast visual
        this.mostrarToastAlertaOcupacion(estadisticas);
      });

    // Alerta de permanencia (>24h)
    this.notificacionService.onAlertaPermanencia
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejas) => {
        console.log(' Dashboard: Alerta de permanencia recibida', bandejas);

        // Actualizar contador de alertas
        if (this.kpis) {
          this.kpis.bandejas.conAlerta24h = bandejas.filter(b => b.tieneAlerta).length;
          this.ultimaActualizacionSignalR = new Date();
        }

        // Mostrar notificación
        Swal.fire({
          icon: 'warning',
          title: 'Alerta de Permanencia',
          text: `${bandejas.length} cuerpo(s) llevan más de 24 horas en el mortuorio`,
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 5000,
          timerProgressBar: true
        });
      });

    // Actualización individual de bandeja (para refrescar ocupación)
    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandeja) => {
        console.log('🔄 Dashboard: Bandeja actualizada', bandeja);
        this.ultimaActualizacionSignalR = new Date();

        //Usar el trigger inteligente en lugar de llamar directamente
        this.refreshTrigger$.next();
      });
  }

  /**
   * Muestra un toast visual cuando hay alerta de ocupación.
   */
  private mostrarToastAlertaOcupacion(estadisticas: EstadisticasBandejaDTO): void {
    const porcentaje = estadisticas.porcentajeOcupacion;

    let icon: 'warning' | 'error' = 'warning';
    let color = '#F59E0B';

    if (porcentaje >= 90) {
      icon = 'error';
      color = '#EF4444';
    }

    Swal.fire({
      icon: icon,
      title: 'Capacidad del Mortuorio',
      html: `
        <p class="text-sm text-gray-600 mb-2">Ocupación actual: <strong>${porcentaje.toFixed(1)}%</strong></p>
        <p class="text-xs text-gray-500">${estadisticas.ocupadas}/${estadisticas.total} bandejas ocupadas</p>
      `,
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 5000,
      timerProgressBar: true,
      background: porcentaje >= 90 ? '#FEE2E2' : '#FEF3C7',
      iconColor: color
    });
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Cargar KPIs del dashboard (bandejas, solicitudes, salidas)
   * 
   * TODO FUTURO: Implementar endpoint por rol
   * - EnfermeriaTecnica: Solo ver sus expedientes y traspasos pendientes
   * - VigilanciaMortuorio: Solo bandejas y alertas de permanencia
   * - Admision: Solo solicitudes de corrección y salidas
   * 
   * Ejemplo:
   * if (this.userRole === 'EnfermeriaTecnica') {
   *   this.dashboardService.getKPIsEnfermeria().subscribe(...);
   * } else if (this.userRole === 'VigilanciaMortuorio') {
   *   this.dashboardService.getKPIsVigilancia().subscribe(...);
   * }
   */
  private cargarKPIs(): void {
    this.isLoadingKPIs = true;
    this.errorKPIs = null;

    const incluirDeudas = this.esRolDeudas();

    this.dashboardService.getDashboardKPIs(incluirDeudas).subscribe({
      next: (kpis) => {
        this.kpis = kpis;
        this.isLoadingKPIs = false;
        this.calcularEstadoVisual();
      },
      error: (err) => {
        console.error('Error al cargar KPIs:', err);
        this.errorKPIs = 'No se pudieron cargar las estadísticas';
        this.isLoadingKPIs = false;
      }
    });
  }

  /**
   * Cargar expedientes recientes (últimos 10)
   */
  cargarExpedientes(): void {
    this.isLoadingExpedientes = true;
    this.errorExpedientes = null;

    this.expedienteService.getAll().subscribe({
      next: (data) => {
        this.expedientes = data;
        this.expedientesRecientes = data.slice(0, 10);

        // ⭐ NUEVO: Inicializar filtros y paginación
        this.aplicarFiltros();

        this.isLoadingExpedientes = false;
        console.log('✅ Expedientes cargados:', data.length);
      },
      error: (err) => {
        console.error('❌ Error cargando expedientes:', err);
        this.errorExpedientes = 'No se pudieron cargar los expedientes';
        this.isLoadingExpedientes = false;
      }
    });
  }

  /**
   * Recargar todos los datos del dashboard
   */
  recargarDatos(): void {
    this.cargarKPIs();
    this.cargarExpedientes();

    Swal.fire({
      icon: 'success',
      title: 'Dashboard actualizado',
      toast: true,
      position: 'top-end',
      showConfirmButton: true,
      
    });
  }

  // ===================================================================
  // FILTROS Y PAGINACIÓN
  // ===================================================================

  /**
   * Aplica filtros de búsqueda y estado a la tabla
   */
  aplicarFiltros(): void {
    let resultados = [...this.expedientesRecientes];

    // Filtro por búsqueda (HC, nombre completo, documento)
    if (this.filtros.busqueda.trim()) {
      const busqueda = this.filtros.busqueda.toLowerCase();
      resultados = resultados.filter(e => {
        return e.hc.toLowerCase().includes(busqueda) ||
          e.nombreCompleto.toLowerCase().includes(busqueda) ||
          (e.numeroDocumento && e.numeroDocumento.toLowerCase().includes(busqueda));
      });
    }

    // Filtro por estado
    if (this.filtros.estado) {
      resultados = resultados.filter(e => e.estadoActual === this.filtros.estado);
    }

    this.expedientesFiltrados = resultados;
    this.calcularPaginacion();
  }

  /**
   * Limpia todos los filtros
   */
  limpiarFiltros(): void {
    this.filtros = {
      busqueda: '',
      estado: ''
    };
    this.aplicarFiltros();
  }

  /**
   * Ordena la tabla por columna
   */
  ordenarPor(columna: 'hc' | 'nombre' | 'fecha'): void {
    if (this.ordenColumna === columna) {
      this.ordenDireccion = this.ordenDireccion === 'asc' ? 'desc' : 'asc';
    } else {
      this.ordenColumna = columna;
      this.ordenDireccion = 'asc';
    }

    this.expedientesFiltrados.sort((a, b) => {
      let valorA: any;
      let valorB: any;

      switch (columna) {
        case 'hc':
          valorA = a.hc;
          valorB = b.hc;
          break;
        case 'nombre':
          valorA = a.nombreCompleto.toLowerCase();
          valorB = b.nombreCompleto.toLowerCase();
          break;
        case 'fecha':
          valorA = new Date(a.fechaHoraFallecimiento).getTime();
          valorB = new Date(b.fechaHoraFallecimiento).getTime();
          break;
      }

      if (valorA < valorB) return this.ordenDireccion === 'asc' ? -1 : 1;
      if (valorA > valorB) return this.ordenDireccion === 'asc' ? 1 : -1;
      return 0;
    });

    this.calcularPaginacion();
  }

  /**
   * Calcula la paginación
   */
  private calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.expedientesFiltrados.length / this.itemsPorPagina);

    if (this.paginaActual > this.totalPaginas && this.totalPaginas > 0) {
      this.paginaActual = this.totalPaginas;
    }

    this.actualizarPagina();
  }

  /**
   * Actualiza los items visibles de la página actual
   */
  private actualizarPagina(): void {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    const fin = inicio + this.itemsPorPagina;
    this.expedientesPaginados = this.expedientesFiltrados.slice(inicio, fin);
  }

  /**
   * Cambia a una página específica
   */
  irAPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
      this.actualizarPagina();
    }
  }

  /**
   * Genera array de números de página para mostrar
   */
  get paginasVisibles(): number[] {
    const paginas: number[] = [];
    const maxPaginas = 5;
    let inicio = Math.max(1, this.paginaActual - 2);
    let fin = Math.min(this.totalPaginas, inicio + maxPaginas - 1);

    if (fin - inicio < maxPaginas - 1) {
      inicio = Math.max(1, fin - maxPaginas + 1);
    }

    for (let i = inicio; i <= fin; i++) {
      paginas.push(i);
    }

    return paginas;
  }
 
  // ===================================================================
  // OPTIMIZACIÓN DE VISUALIZACIÓN (Helpers)
  // ===================================================================

  /**
   * Calcula todos los estilos visuales basados en KPIs y los guarda en uiState.
   * Esto reemplaza a los múltiples getters que se ejecutaban en cada ciclo.
   */
  private calcularEstadoVisual(): void {
    if (!this.kpis) return;

    const porcentaje = this.kpis.bandejas.porcentajeOcupacion;
    const enMantenimiento = this.kpis.bandejas.enMantenimiento;

    // 1. Color Ocupación (Texto)
    if (porcentaje >= 90) this.uiState.ocupacionColor = 'text-red-600';
    else if (porcentaje >= 70) this.uiState.ocupacionColor = 'text-yellow-600';
    else this.uiState.ocupacionColor = 'text-green-600';

    // 2. Badge Ocupación
    if (porcentaje >= 90) this.uiState.ocupacionBadgeClass = 'bg-red-100 text-red-800 border-red-300';
    else if (porcentaje >= 70) this.uiState.ocupacionBadgeClass = 'bg-yellow-100 text-yellow-800 border-yellow-300';
    else this.uiState.ocupacionBadgeClass = 'bg-green-100 text-green-800 border-green-300';

    // 3. Progress Bar
    if (porcentaje >= 90) this.uiState.progressBarClass = 'bg-red-500';
    else if (porcentaje >= 70) this.uiState.progressBarClass = 'bg-yellow-500';
    else this.uiState.progressBarClass = 'bg-green-500';

    // 4. Mantenimiento Cards & Icons
    if (enMantenimiento > 0) {
      this.uiState.mantenimientoCardClass = 'bg-orange-50 border-orange-200';
      this.uiState.mantenimientoIconColor = 'text-orange-600 bg-orange-100';
      this.uiState.mantenimientoTextColor = 'text-orange-600';
    } else {
      this.uiState.mantenimientoCardClass = 'bg-gray-50 border-gray-200';
      this.uiState.mantenimientoIconColor = 'text-gray-600 bg-gray-100';
      this.uiState.mantenimientoTextColor = 'text-gray-600';
    }
  }

  /**
   * Obtiene la información del badge para un estado
   */
  getBadgeInfo(estado: string) {
    return getBadgeWithIcon(estado);
  }

  // Getters simples para booleanos
  get tieneAlertasCriticas(): boolean {
    return (this.kpis?.bandejas.conAlerta48h || 0) > 0;
  }

  get tieneAlertas24h(): boolean {
    return (this.kpis?.bandejas.conAlerta24h || 0) > 0;
  }

  get tieneSolicitudesAlerta(): boolean {
    return (this.kpis?.solicitudes.conAlerta || 0) > 0;
  }

  // ===================================================================
  // NAVEGACIÓN
  // ===================================================================

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  /**
 * Verifica si el usuario tiene rol que debe ver estadísticas de deudas
 */
  private esRolDeudas(): boolean {
    const rolesConDeudas = [
      'BancoSangre',
      'ServicioSocial',
      'CuentasPacientes',
      'VigilanteSupervisor',
      'Administrador'
    ];
    return rolesConDeudas.includes(this.userRole);
  }

  verExpediente(expedienteId: number | undefined): void {
    if (!expedienteId) {
      console.warn(' ID de expediente inválido');
      return;
    }
    this.router.navigate(['/expediente', expedienteId]);
  }

  /**
   * Reimprimir brazalete de un expediente
   */
  reimprimir(expedienteId: number): void {
    console.log('🖨️ Solicitando reimpresión para:', expedienteId);

    this.expedienteService.reimprimirBrazalete(expedienteId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `brazalete-SGM-${expedienteId}-reimpresion.pdf`;
        link.click();

        setTimeout(() => window.URL.revokeObjectURL(url), 100);

        Swal.fire({
          icon: 'success',
          title: 'Brazalete reimpreso',
          text: 'El brazalete se ha descargado correctamente',
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 3000
        });
      },
      error: (err) => {
        console.error('❌ Error al reimprimir:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo reimprimir el brazalete',
          confirmButtonColor: '#0891B2'
        });
      }
    });
  }

  // ===================================================================
  // CLEANUP
  // ===================================================================

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshTrigger$.complete();

    console.log('🧹 Dashboard: Componente destruido, suscripciones cerradas');
  }
}
