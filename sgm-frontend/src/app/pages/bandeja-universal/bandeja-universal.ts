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

/**
 * Configuración de filtros por rol
 */
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
 * Componente Universal de Bandeja de Entrada
 * Sistema de filtros inteligentes adaptados por rol
 * Integración con badge-styles.ts para estilos centralizados
 * 
 * @version 2.0.0
 * @changelog
 * - v2.0.0: Integración con sistema centralizado de badges
 * - v1.1.0: Sistema de filtros inteligentes por rol
 * - v1.0.0: Implementación inicial
 * 
 * Características:
 * - Filtros dinámicos según rol (Enfermería sin expediente, otros con expediente)
 * - Tabla con columnas dinámicas y anchos fijos
 * - Badges centralizados desde badge-styles.ts
 * - Ordenamiento multi-columna con indicadores visuales
 * - Paginación real con controles responsivos
 * - Búsqueda multi-campo con contador de filtros activos
 */
@Component({
  selector: 'app-bandeja-universal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './bandeja-universal.html',
  styleUrl: './bandeja-universal.css'
})
export class BandejaUniversalComponent implements OnInit, OnDestroy {
  // ===================================================================
  // DEPENDENCY INJECTION
  // ===================================================================
  private bandejaService = inject(BandejaUniversalService);
  private authService = inject(AuthService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);

  private destroy$ = new Subject<void>();

  // Subject para controlar recargas masivas (Debounce SignalR)
  private refreshTrigger$ = new Subject<void>();

  // ===================================================================
  // STATE MANAGEMENT
  // ===================================================================
  items: BandejaItem[] = [];
  filteredItems: BandejaItem[] = [];
  paginatedItems: BandejaItem[] = [];

  isLoading = true;
  errorMessage = '';

  tituloBandeja = 'Bandeja de Tareas';
  descripcionBandeja = 'Gestiona tus pendientes';
  iconoBandeja = 'inbox';
  rolUsuario = '';

  // ===================================================================
  // CONFIGURACIÓN DE TABLA DINÁMICA
  // ===================================================================
  labelColumna1 = 'Código';
  mostrarColumnaHC = true;
  esRolEnfermeria = false;

  // ===================================================================
  // SISTEMA DE FILTROS INTELIGENTES
  // ===================================================================
  configuracionFiltros: ConfiguracionFiltros = {
    filtros: [],
    permiteFiltroExpediente: false,
    camposPorDefecto: []
  };

  // Valores de los filtros (binding con el template)
  filtroExpediente = '';
  filtroHC = '';
  filtroNombre = '';
  filtroDocumento = '';
  filtroEstado = '';

  // Estados disponibles (se carga dinámicamente desde los datos)
  estadosDisponibles: string[] = [];

  // ===================================================================
  // BÚSQUEDA Y ESTADÍSTICAS
  // ===================================================================
  searchTerm = ''; // Búsqueda rápida global
  totalItems = 0;
  itemsUrgentes = 0;
  fechaActual: Date = new Date();

  // Estado de conexión SignalR
  ultimaActualizacionSignalR: Date | null = null;
  conexionSignalREstablecida = false;

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  paginaInicio = 0;
  paginaFin = 0;

  // ===================================================================
  // ORDENAMIENTO
  // ===================================================================
  ordenColumna: 'codigo' | 'paciente' | 'hc' | 'documento' | 'servicio' | 'fecha' | 'estado' = 'fecha';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // ===================================================================
  // LIFECYCLE HOOKS
  // ===================================================================
  ngOnInit(): void {
    this.inicializarComponente();
    this.iniciarActualizacionFecha();

    // Configurar SignalR
    this.configurarRecargaInteligente();
    this.suscribirseANotificacionesSignalR();
    this.suscribirseAEstadoConexion();
  }

  ngOnDestroy(): void {
    // Detener todas las suscripciones
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshTrigger$.complete();

    console.log('Bandeja Universal: Componente destruido, suscripciones cerradas');
  }

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  private inicializarComponente(): void {
    this.rolUsuario = this.authService.getUserRole();
    this.configurarContextoSegunRol();
    this.configurarFiltrosSegunRol();
    this.cargarItems();
  }

  /**
   * Configura tabla dinámica según rol
   * Define título, descripción, ícono y configuración de columnas
   */
  private configurarContextoSegunRol(): void {
    const config = {
      titulo: 'Bandeja de Entrada',
      descripcion: 'Tareas pendientes',
      icono: 'inbox',
      labelColumna1: 'N° Expediente',
      mostrarHC: true,
      esEnfermeria: false
    };

    const configs: Record<string, typeof config> = {
      'EnfermeriaTecnica': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list' as const,
        labelColumna1: 'Historia Clínica',
        mostrarHC: false,
        esEnfermeria: true
      },
      'EnfermeriaLicenciada': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Pendientes de generar expediente mortuorio',
        icono: 'clipboard-list' as const,
        labelColumna1: 'Historia Clínica',
        mostrarHC: false,
        esEnfermeria: true
      },
      'SupervisoraEnfermeria': {
        titulo: 'Pacientes Fallecidos',
        descripcion: 'Supervisión de expedientes',
        icono: 'clipboard-list' as const,
        labelColumna1: 'Historia Clínica',
        mostrarHC: false,
        esEnfermeria: true
      },
      'Admision': {
        titulo: 'Solicitudes de Retiro',
        descripcion: 'Expedientes por autorizar para retiro familiar',
        icono: 'file-check' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      },
      'BancoSangre': {
        titulo: 'Deudas de Sangre',
        descripcion: 'Compromisos de reposición pendientes',
        icono: 'alert-circle' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      },
      'CuentasPacientes': {
        titulo: 'Deudas Económicas',
        descripcion: 'Pagos pendientes por regularizar',
        icono: 'alert-triangle' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      },
      'ServicioSocial': {
        titulo: 'Casos Sociales',
        descripcion: 'Expedientes que requieren evaluación social',
        icono: 'info' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      },
      'VigilanteSupervisor': {
        titulo: 'Validaciones Pendientes',
        descripcion: 'Documentos legales por verificar',
        icono: 'shield' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      },
      'JefeGuardia': {
        titulo: 'Solicitudes de Excepción',
        descripcion: 'Casos especiales que requieren autorización',
        icono: 'shield' as const,
        labelColumna1: 'N° Expediente',
        mostrarHC: true,
        esEnfermeria: false
      }
    };

    const selectedConfig = configs[this.rolUsuario] || config;

    this.tituloBandeja = selectedConfig.titulo;
    this.descripcionBandeja = selectedConfig.descripcion;
    this.iconoBandeja = selectedConfig.icono;
    this.labelColumna1 = selectedConfig.labelColumna1;
    this.mostrarColumnaHC = selectedConfig.mostrarHC;
    this.esRolEnfermeria = selectedConfig.esEnfermeria;
  }

  /**
   * Configura filtros inteligentes según rol
   * 
   * Enfermería: HC + Nombre + Documento + Estado (SIN Expediente)
   * - Los pacientes recién fallecidos no tienen expediente aún
   * - Enfermería es quien los genera
   * 
   * Otros roles: Expediente + HC + Nombre + Documento + Estado
   * - Trabajan con expedientes ya existentes
   */
  private configurarFiltrosSegunRol(): void {
    const rolesEnfermeria = ['EnfermeriaTecnica', 'EnfermeriaLicenciada', 'SupervisoraEnfermeria'];
    const esEnfermeria = rolesEnfermeria.includes(this.rolUsuario);

    // Filtros base para TODOS los roles
    const filtrosBase: FiltroConfig[] = [
      {
        id: 'hc',
        label: 'Historia Clínica',
        placeholder: 'Ej: HC-2024-001234',
        campo: 'hc',
        visible: true
      },
      {
        id: 'nombre',
        label: 'Nombre del Paciente',
        placeholder: 'Buscar por nombre o apellido',
        campo: 'nombreCompleto',
        visible: true
      },
      {
        id: 'documento',
        label: 'N° Documento',
        placeholder: 'DNI, CE, Pasaporte',
        campo: 'numeroDocumento',
        visible: true
      },
      {
        id: 'estado',
        label: 'Estado',
        placeholder: 'Filtrar por estado',
        campo: 'estadoTexto',
        visible: true
      }
    ];

    // Filtro de Expediente (SOLO para roles NO-enfermería)
    const filtroExpediente: FiltroConfig = {
      id: 'expediente',
      label: 'N° Expediente',
      placeholder: 'Ej: SGM-2024-00123',
      campo: 'codigoExpediente',
      visible: !esEnfermeria
    };

    // Configuración final
    this.configuracionFiltros = {
      filtros: esEnfermeria ? filtrosBase : [filtroExpediente, ...filtrosBase],
      permiteFiltroExpediente: !esEnfermeria,
      camposPorDefecto: esEnfermeria
        ? ['hc', 'nombreCompleto', 'numeroDocumento', 'estadoTexto']
        : ['codigoExpediente', 'hc', 'nombreCompleto', 'numeroDocumento', 'estadoTexto']
    };

    console.log('🔍 Filtros configurados para rol:', this.rolUsuario, this.configuracionFiltros);
  }

  /**
   * Inicia actualización automática de fecha cada minuto
   * Esto hace que el timestamp "Última actualización" se refresque automáticamente
   */
  private iniciarActualizacionFecha(): void {
    interval(60000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.fechaActual = new Date();
      });
  }

  // ===================================================================
  // SIGNALR - NOTIFICACIONES EN TIEMPO REAL
  // ===================================================================

  /**
   * Configura el debounce para evitar spam de peticiones al backend
   * si llegan muchas actualizaciones de SignalR seguidas.
   * 
   * Estrategia: Espera 500ms de "silencio" tras la última notificación
   * antes de recargar datos del backend.
   */
  private configurarRecargaInteligente(): void {
    this.refreshTrigger$
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(500) // Espera 500ms tras el último evento
      )
      .subscribe(() => {
        console.log('🔄 Bandeja Universal: Recarga inteligente ejecutada');
        this.cargarItems();
      });
  }

  /**
   * Suscribe al estado de la conexión SignalR para actualizar el indicador visual.
   * Muestra un punto verde cuando está conectado, amarillo cuando está reconectando.
   */
  private suscribirseAEstadoConexion(): void {
    this.notificacionService.conexionEstablecida
      .pipe(takeUntil(this.destroy$))
      .subscribe(conectado => {
        this.conexionSignalREstablecida = conectado;
        if (conectado) {
          console.log(' Bandeja Universal: Conexión SignalR activa');
          this.ultimaActualizacionSignalR = new Date();
        } else {
          console.warn(' Bandeja Universal: Conexión SignalR perdida o reconectando...');
        }
      });
  }

  /**
   * Suscribe el componente a las notificaciones relevantes de SignalR.
   * 
   * Eventos escuchados:
   * 1. NuevoExpediente: Cuando se crea un nuevo expediente
   * 2. ExpedienteActualizado: Cuando cambia el estado de un expediente
   * 3. BandejaActualizada: Cuando se asigna/libera una bandeja
   * 
   * Estrategia: Actualización optimista (actualiza local) + debounce (recarga backend)
   */
  private suscribirseANotificacionesSignalR(): void {
    // 1. Nuevo Expediente Creado
    this.notificacionService.onNuevoExpediente
      .pipe(takeUntil(this.destroy$))
      .subscribe((notificacion) => {
        console.log('📥 Bandeja Universal: Nuevo expediente recibido', notificacion);

        this.ultimaActualizacionSignalR = new Date();

        // Mostrar toast visual
        this.mostrarToastNuevoExpediente(notificacion.titulo);

        // Trigger de recarga inteligente (con debounce)
        this.refreshTrigger$.next();
      });

    // 2. Expediente Actualizado (cambio de estado)
    this.notificacionService.onExpedienteActualizado
      .pipe(takeUntil(this.destroy$))
      .subscribe((notificacion) => {
        console.log('🔄 Bandeja Universal: Expediente actualizado', notificacion);

        this.ultimaActualizacionSignalR = new Date();

        // OPTIMIZACIÓN: Actualización optimista (buscar y actualizar local)
        const expedienteLocal = this.items.find(
          item => item.id === notificacion.expedienteId || item.codigoExpediente === notificacion.codigoExpediente
        );

        if (expedienteLocal && notificacion.estadoNuevo) {
          expedienteLocal.estadoTexto = notificacion.estadoNuevo;
          console.log('✨ Actualización optimista aplicada:', notificacion.codigoExpediente);

          // Re-aplicar filtros sin hacer HTTP request
          this.aplicarFiltros();
        }

        // Trigger de recarga completa (con debounce) para sincronizar con backend
        this.refreshTrigger$.next();
      });

    // 3. Bandeja Actualizada (asignación/liberación)
    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandeja) => {
        console.log('🗄️ Bandeja Universal: Bandeja actualizada', bandeja);

        this.ultimaActualizacionSignalR = new Date();

        // Si la bandeja afecta algún item de la lista, recargar
        const afectaLista = this.items.some(item => item.bandeja === bandeja.codigo);

        if (afectaLista) {
          this.refreshTrigger$.next();
        }
      });

    // 4. Notificación Genérica (catch-all para notificaciones dirigidas)
    this.notificacionService.onNotificacionGenerica
      .pipe(takeUntil(this.destroy$))
      .subscribe((notificacion) => {
        console.log('📢 Bandeja Universal: Notificación genérica recibida', notificacion);

        this.ultimaActualizacionSignalR = new Date();

        // Si la notificación menciona términos relevantes, recargar
        const esRelevante =
          notificacion.titulo.toLowerCase().includes('expediente') ||
          notificacion.titulo.toLowerCase().includes('bandeja') ||
          notificacion.mensaje.toLowerCase().includes(this.rolUsuario.toLowerCase());

        if (esRelevante) {
          this.mostrarToastGenerico(notificacion.titulo, notificacion.mensaje, notificacion.tipo);
          this.refreshTrigger$.next();
        }
      });
  }

  /**
   * Muestra un toast visual cuando se crea un nuevo expediente
   */
  private mostrarToastNuevoExpediente(titulo: string): void {
    Swal.fire({
      icon: 'info',
      title: 'Nuevo Expediente',
      text: titulo,
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 4000,
      timerProgressBar: true,
      background: '#EFF6FF',
      iconColor: '#0891B2'
    });
  }

  /**
   * Muestra un toast genérico basado en el tipo de notificación
   */
  private mostrarToastGenerico(titulo: string, mensaje: string, tipo: string): void {
    let icon: 'success' | 'error' | 'warning' | 'info' = 'info';
    let background = '#F3F4F6';
    let iconColor = '#6B7280';

    // Mapeo de tipos a íconos y colores
    switch (tipo.toLowerCase()) {
      case 'success':
        icon = 'success';
        background = '#ECFDF5';
        iconColor = '#10B981';
        break;
      case 'error':
        icon = 'error';
        background = '#FEE2E2';
        iconColor = '#EF4444';
        break;
      case 'warning':
        icon = 'warning';
        background = '#FEF3C7';
        iconColor = '#F59E0B';
        break;
      case 'info':
      default:
        icon = 'info';
        background = '#EFF6FF';
        iconColor = '#0891B2';
    }

    Swal.fire({
      icon,
      title: titulo,
      text: mensaje,
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 4000,
      timerProgressBar: true,
      background,
      iconColor
    });
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Carga items desde el servicio de bandeja
   * Extrae estados disponibles para el dropdown de filtros
   */
  cargarItems(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.bandejaService.getItems()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.items = data;
          this.extraerEstadosDisponibles();
          this.calcularEstadisticas();
          this.aplicarFiltros();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('❌ Error al cargar bandeja:', err);
          this.errorMessage = 'No se pudieron cargar los elementos. Intenta nuevamente.';
          this.isLoading = false;
        }
      });
  }

  /**
   * Extrae estados únicos para el filtro dropdown
   * Se ejecuta cada vez que se cargan los items
   */
  private extraerEstadosDisponibles(): void {
    const estadosSet = new Set(this.items.map(item => item.estadoTexto));
    this.estadosDisponibles = Array.from(estadosSet).sort();
  }

  /**
   * Recarga todos los items y limpia los filtros
   */
  recargar(): void {
    this.limpiarFiltros();
    this.cargarItems();
  }

  // ===================================================================
  // SISTEMA DE FILTRADO INTELIGENTE
  // ===================================================================

  /**
   * Aplica filtros específicos + búsqueda global
   * Pipeline: Filtros específicos → Búsqueda global → Ordenamiento → Paginación
   */
  aplicarFiltros(): void {
    let resultados = [...this.items];

    // 1. Filtros específicos (tienen prioridad)
    resultados = this.aplicarFiltrosEspecificos(resultados);

    // 2. Búsqueda global (si hay término)
    if (this.searchTerm.trim()) {
      resultados = this.aplicarBusquedaGlobal(resultados);
    }

    // 3. Ordenar
    this.ordenarResultados(resultados);

    // 4. Actualizar estado
    this.filteredItems = resultados;
    this.totalItems = resultados.length;

    // 5. Resetear paginación
    this.paginaActual = 1;
    this.calcularPaginacion();
  }

  /**
   * Aplica filtros específicos por campo
   * Cada filtro se aplica de forma independiente (AND lógico)
   */
  private aplicarFiltrosEspecificos(items: BandejaItem[]): BandejaItem[] {
    let resultados = items;

    // Filtro por Expediente (solo si está permitido por el rol)
    if (this.configuracionFiltros.permiteFiltroExpediente && this.filtroExpediente.trim()) {
      const term = this.filtroExpediente.toLowerCase().trim();
      resultados = resultados.filter(item =>
        item.codigoExpediente?.toLowerCase().includes(term)
      );
    }

    // Filtro por HC
    if (this.filtroHC.trim()) {
      const term = this.filtroHC.toLowerCase().trim();
      resultados = resultados.filter(item =>
        item.hc?.toLowerCase().includes(term)
      );
    }

    // Filtro por Nombre
    if (this.filtroNombre.trim()) {
      const term = this.filtroNombre.toLowerCase().trim();
      resultados = resultados.filter(item =>
        item.nombreCompleto?.toLowerCase().includes(term)
      );
    }

    // Filtro por Documento
    if (this.filtroDocumento.trim()) {
      const term = this.filtroDocumento.toLowerCase().trim();
      resultados = resultados.filter(item =>
        item.numeroDocumento?.toLowerCase().includes(term) ||
        item.tipoDocumento?.toLowerCase().includes(term)
      );
    }

    // Filtro por Estado (exacto, no parcial)
    if (this.filtroEstado) {
      resultados = resultados.filter(item =>
        item.estadoTexto === this.filtroEstado
      );
    }

    return resultados;
  }

  /**
   * Búsqueda global que busca en múltiples campos
   * Se aplica después de los filtros específicos
   */
  private aplicarBusquedaGlobal(items: BandejaItem[]): BandejaItem[] {
    const term = this.searchTerm.toLowerCase().trim();

    return items.filter(item => {
      // Construir campos de búsqueda según configuración del rol
      const camposBusqueda: string[] = [];

      if (this.configuracionFiltros.permiteFiltroExpediente && item.codigoExpediente) {
        camposBusqueda.push(item.codigoExpediente);
      }
      if (item.hc) camposBusqueda.push(item.hc);
      if (item.nombreCompleto) camposBusqueda.push(item.nombreCompleto);
      if (item.numeroDocumento) camposBusqueda.push(item.numeroDocumento);
      if (item.tipoDocumento) camposBusqueda.push(item.tipoDocumento);
      if (item.servicio) camposBusqueda.push(item.servicio);
      if (item.bandeja) camposBusqueda.push(item.bandeja);
      if (item.estadoTexto) camposBusqueda.push(item.estadoTexto);

      return camposBusqueda.some(campo =>
        campo.toLowerCase().includes(term)
      );
    });
  }

  /**
   * Limpia todos los filtros activos
   * Resetea campos de filtro y búsqueda, luego reaplica filtros
   */
  limpiarFiltros(): void {
    this.filtroExpediente = '';
    this.filtroHC = '';
    this.filtroNombre = '';
    this.filtroDocumento = '';
    this.filtroEstado = '';
    this.searchTerm = '';
    this.aplicarFiltros();
  }

  /**
   * Cuenta cuántos filtros están activos
   * Se usa para mostrar badge en el header de filtros
   */
  get filtrosActivos(): number {
    let count = 0;
    if (this.filtroExpediente) count++;
    if (this.filtroHC) count++;
    if (this.filtroNombre) count++;
    if (this.filtroDocumento) count++;
    if (this.filtroEstado) count++;
    if (this.searchTerm) count++;
    return count;
  }

  // ===================================================================
  // ORDENAMIENTO
  // ===================================================================

  /**
   * Ordena por columna específica
   * Toggle de dirección si se hace clic en la misma columna
   */
  ordenarPor(columna: typeof this.ordenColumna): void {
    if (this.ordenColumna === columna) {
      this.ordenDireccion = this.ordenDireccion === 'asc' ? 'desc' : 'asc';
    } else {
      this.ordenColumna = columna;
      this.ordenDireccion = 'asc';
    }

    this.aplicarFiltros();
  }

  /**
   * Ordena la lista según la columna y dirección actuales
   * Soporta ordenamiento por string, number y Date
   */
  private ordenarResultados(lista: BandejaItem[]): void {
    lista.sort((a, b) => {
      let valA: any;
      let valB: any;

      switch (this.ordenColumna) {
        case 'codigo':
          valA = a.id;
          valB = b.id;
          break;
        case 'paciente':
          valA = a.nombreCompleto.toLowerCase();
          valB = b.nombreCompleto.toLowerCase();
          break;
        case 'hc':
          valA = a.hc || '';
          valB = b.hc || '';
          break;
        case 'documento':
          valA = a.numeroDocumento || '';
          valB = b.numeroDocumento || '';
          break;
        case 'servicio':
          valA = a.servicio || '';
          valB = b.servicio || '';
          break;
        case 'fecha':
          valA = a.fechaFallecimiento || a.fechaIngreso || new Date(0);
          valB = b.fechaFallecimiento || b.fechaIngreso || new Date(0);
          break;
        case 'estado':
          valA = a.estadoTexto.toLowerCase();
          valB = b.estadoTexto.toLowerCase();
          break;
        default:
          valA = a.nombreCompleto;
          valB = b.nombreCompleto;
      }

      if (valA < valB) return this.ordenDireccion === 'asc' ? -1 : 1;
      if (valA > valB) return this.ordenDireccion === 'asc' ? 1 : -1;
      return 0;
    });
  }

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================

  /**
   * Calcula total de páginas basado en items filtrados
   */
  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.totalItems / this.itemsPorPagina) || 1;
    this.actualizarPaginaVisible();
  }

  /**
   * Actualiza el slice de items visible en la página actual
   * Incluye validación de límites
   */
  actualizarPaginaVisible(): void {
    if (this.paginaActual > this.totalPaginas) this.paginaActual = this.totalPaginas;
    if (this.paginaActual < 1) this.paginaActual = 1;

    this.paginaInicio = (this.paginaActual - 1) * this.itemsPorPagina;
    this.paginaFin = Math.min(this.paginaInicio + this.itemsPorPagina, this.totalItems);

    this.paginatedItems = this.filteredItems.slice(this.paginaInicio, this.paginaFin);
  }

  /**
   * Navega a la página anterior
   */
  paginaAnterior(): void {
    if (this.paginaActual > 1) {
      this.paginaActual--;
      this.actualizarPaginaVisible();
    }
  }

  /**
   * Navega a la página siguiente
   */
  paginaSiguiente(): void {
    if (this.paginaActual < this.totalPaginas) {
      this.paginaActual++;
      this.actualizarPaginaVisible();
    }
  }

  // ===================================================================
  // ACCIONES Y NAVEGACIÓN
  // ===================================================================

  /**
   * Ejecuta la acción principal del item según su tipo
   * Enruta a diferentes módulos o muestra modales según corresponda
   */
  ejecutarAccion(item: BandejaItem): void {
    console.log('🎯 Ejecutando acción:', item.tipoItem, 'para ID:', item.id);

    switch (item.tipoItem) {
      case 'generacion_expediente':
        this.router.navigate(['/nuevo-expediente'], {
          queryParams: {
            hc: item.hc || item.id,
            origen: 'bandeja',
            nombre: item.nombreCompleto,
            doc: item.numeroDocumento
          },
          state: {
            pacientePreseleccionado: item,
            modo: 'desde_bandeja'
          }
        });
        break;

      case 'deuda_sangre':
        this.mostrarModalRegularizacionSangre(item);
        break;

      case 'deuda_economica':
        Swal.fire({
          icon: 'info',
          title: 'Gestión de Caja',
          text: 'Redirigiendo al sistema de pagos...',
          timer: 1500,
          showConfirmButton: false
        });
        break;

      case 'autorizacion_retiro':
        Swal.fire({
          icon: 'info',
          title: 'Módulo en construcción',
          text: 'La autorización de retiro se implementará próximamente',
          confirmButtonColor: '#0891B2'
        });
        break;

      default:
        Swal.fire({
          icon: 'info',
          title: 'En construcción',
          text: 'Esta funcionalidad estará disponible próximamente',
          confirmButtonColor: '#0891B2'
        });
    }
  }

  // ===================================================================
  // MODALES
  // ===================================================================

  /**
   * Muestra modal para regularizar deuda de sangre
   * Permite marcar como: Compromiso firmado, Reposición realizada o Anulación médica
   */
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
      inputOptions: {
        'compromiso': 'Compromiso Firmado',
        'reposicion': 'Reposición Realizada',
        'anulacion': 'Anulación Médica'
      },
      inputPlaceholder: 'Seleccione el estado',
      showCancelButton: true,
      confirmButtonText: 'Guardar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      inputValidator: (value) => {
        if (!value) {
          return 'Debes seleccionar una opción';
        }
        return null;
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.simularAccionBackend(item.id, 'Deuda de sangre regularizada correctamente');
      }
    });
  }
  /**
   * Simula acción de backend exitosa
   * Remueve el item de la lista local y reaplica filtros
   */
  private simularAccionBackend(id: string | number, mensaje: string): void {
    Swal.fire({
      icon: 'success',
      title: '¡Éxito!',
      text: mensaje,
      timer: 2000,
      showConfirmButton: false
    });

    this.items = this.items.filter(i => i.id !== id);
    this.aplicarFiltros();
  }
  // ===================================================================
  // ESTADÍSTICAS
  // ===================================================================
  /**
   * Calcula estadísticas básicas de los items
   * Total de items e items marcados como urgentes
   */
  private calcularEstadisticas(): void {
    this.totalItems = this.items.length;
    this.itemsUrgentes = this.items.filter(item => item.esUrgente).length;
  }
  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================
  /**
   * Retorna clases CSS para badge de estado
   * Usa el sistema centralizado de badge-styles.ts
   * 
   * @param estado - Estado del expediente o item
   * @returns String con clases de Tailwind CSS
   */
  getBadgeClasses(estado: string): string {
    return getBadgeClasses(estado);
  }

  /**
   * Formatea tiempo transcurrido en texto legible
   * 
   * @param horas - Horas transcurridas
   * @returns String formateado (ej: "2d 5h", "< 1h", "23h")
   */
  formatearTiempo(horas: number | undefined): string {
    if (!horas) return '';

    if (horas < 1) {
      return '< 1h';
    }
    if (horas < 24) {
      return `${Math.round(horas)}h`;
    }

    const dias = Math.floor(horas / 24);
    const horasRestantes = Math.round(horas % 24);

    if (horasRestantes === 0) {
      return `${dias}d`;
    }

    return `${dias}d ${horasRestantes}h`;
  }
}
