import { Component, inject, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import Swal from 'sweetalert2';
import { AuthService } from '../../services/auth';
import { IconComponent } from '../../components/icon/icon.component';
import { NotificacionService } from '../../services/notificacion';
import {
  NotificacionDTO,
  COLORES_NOTIFICACION,
  ICONOS_NOTIFICACION
} from '../../models/notificacion.model';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    IconComponent
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);
  private elementRef = inject(ElementRef);

  // Subject para cleanup de suscripciones
  private destroy$ = new Subject<void>();

  // ===================================================================
  // SIDEBAR
  // ===================================================================
  sidebarOpen = false;
  sidebarCollapsed = false;

  // ===================================================================
  // USUARIO
  // ===================================================================
  userName = '';
  userRole = '';

  // ===================================================================
  // NOTIFICACIONES SIGNALR
  // ===================================================================
  notificacionesOpen = false;
  notificaciones: NotificacionDTO[] = [];
  notificacionesNoLeidas = 0;
  conexionSignalREstablecida = false;

  // Helpers visuales (para template)
  coloresNotificacion = COLORES_NOTIFICACION;
  iconosNotificacion = ICONOS_NOTIFICACION;

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  async ngOnInit(): Promise<void> {
    // Datos de usuario
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();

    // Estado del sidebar (persistido en localStorage)
    const savedCollapsed = localStorage.getItem('sidebar_collapsed');
    if (savedCollapsed) {
      this.sidebarCollapsed = savedCollapsed === 'true';
    }

    // INICIAR CONEXIÓN SIGNALR
    await this.iniciarSignalR();

    // SUSCRIBIRSE A NOTIFICACIONES
    this.suscribirseANotificaciones();
  }

  /**
   * Inicia la conexión con el Hub de SignalR.
   * Se ejecuta automáticamente al cargar el layout.
   */
  private async iniciarSignalR(): Promise<void> {
    try {
      await this.notificacionService.iniciarConexion();
      console.log('MainLayout: Conexión SignalR iniciada exitosamente');
    } catch (error) {
      console.error('MainLayout: Error al iniciar SignalR:', error);

      // Mostrar alerta al usuario (opcional)
      Swal.fire({
        icon: 'warning',
        title: 'Conexión en Tiempo Real',
        text: 'No se pudo establecer conexión con el servidor de notificaciones. Las alertas en tiempo real podrían no funcionar.',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 5000,
        timerProgressBar: true
      });
    }
  }

  /**
   * Suscribe el componente a los observables del servicio de notificaciones.
   */
  private suscribirseANotificaciones(): void {
    // Estado de conexión SignalR
    this.notificacionService.conexionEstablecida
      .pipe(takeUntil(this.destroy$))
      .subscribe(conectado => {
        this.conexionSignalREstablecida = conectado;

        if (conectado) {
          console.log('MainLayout: SignalR conectado ✅');
        } else {
          console.warn('MainLayout: SignalR desconectado ⚠️');
        }
      });

    // Lista de notificaciones
    this.notificacionService.notificaciones
      .pipe(takeUntil(this.destroy$))
      .subscribe(notificaciones => {
        this.notificaciones = notificaciones;
        console.log('MainLayout: Notificaciones actualizadas:', notificaciones.length);
      });

    // Contador de no leídas
    this.notificacionService.contadorNoLeidas
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => {
        this.notificacionesNoLeidas = count;
        console.log('MainLayout: Notificaciones no leídas:', count);
      });

    //  Retiro autorizado (Vigilancia recibe alerta)
    this.notificacionService.onExpedienteListoParaRetiro
      .pipe(takeUntil(this.destroy$))
      .subscribe((notif) => {
        Swal.fire({
          icon: 'success',
          title: 'Retiro Autorizado',
          text: notif.mensaje,
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 6000,
          timerProgressBar: true
        });
      });

    // Salida registrada (Admisión y JefeGuardia reciben confirmación)
    this.notificacionService.onSalidaRegistrada
      .pipe(takeUntil(this.destroy$))
      .subscribe((notif) => {
        Swal.fire({
          icon: 'info',
          title: 'Cuerpo Retirado',
          text: notif.mensaje,
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 5000,
          timerProgressBar: true
        });
      });

    // Confirmaciones de acción (feedback instantáneo)
    this.notificacionService.onConfirmacionAccion
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ accion, exito, mensaje }) => {
        Swal.fire({
          icon: exito ? 'success' : 'error',
          title: accion,
          text: mensaje,
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 3000,
          timerProgressBar: true
        });
      });
  }

  // ===================================================================
  // GESTIÓN DE NOTIFICACIONES
  // ===================================================================

  /**
   * Abre/cierra el dropdown de notificaciones.
   */
  toggleNotificaciones(): void {
    this.notificacionesOpen = !this.notificacionesOpen;
  }

  /**
   * Abre una notificación específica.
   * Marca como leída y navega a la URL si existe.
   */
  abrirNotificacion(notif: NotificacionDTO): void {
    // Marcar como leída
    this.notificacionService.marcarComoLeida(notif.id);

    // Cerrar dropdown
    this.notificacionesOpen = false;

    // Navegar si tiene URL
    if (notif.urlNavegacion) {
      this.router.navigate([notif.urlNavegacion]);
    }
  }

  /**
   * Marca todas las notificaciones como leídas.
   */
  marcarTodasLeidas(): void {
    this.notificacionService.marcarTodasComoLeidas();

    Swal.fire({
      icon: 'success',
      title: 'Notificaciones marcadas',
      text: 'Todas las notificaciones han sido marcadas como leídas',
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 2000
    });
  }

  /**
   * Limpia todas las notificaciones.
   */
  limpiarTodas(): void {
    Swal.fire({
      title: '¿Limpiar notificaciones?',
      text: "Se eliminarán todas las notificaciones del historial",
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, limpiar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.notificacionService.limpiarTodas();

        Swal.fire({
          icon: 'success',
          title: 'Notificaciones eliminadas',
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 2000
        });
      }
    });
  }

  /**
   * Obtiene el ícono de una notificación según su tipo.
   */
  getIconoNotificacion(tipo: string): string {
    return this.iconosNotificacion[tipo as keyof typeof this.iconosNotificacion] || 'info';
  }

  /**
   * Obtiene las clases de color de una notificación según su tipo.
   */
  getColoresNotificacion(tipo: string): { bg: string; border: string; text: string; icon: string } {
    return this.coloresNotificacion[tipo as keyof typeof this.coloresNotificacion] || this.coloresNotificacion['info'];
  }

  /**
   * Formatea el tiempo transcurrido desde una fecha.
   * Ejemplo: "Hace 5 minutos", "Hace 2 horas", "Hace 1 día"
   */
  formatearTiempo(fecha: Date): string {
    const ahora = new Date();
    const diff = ahora.getTime() - fecha.getTime();
    const minutos = Math.floor(diff / 60000);
    const horas = Math.floor(diff / 3600000);
    const dias = Math.floor(diff / 86400000);

    if (minutos < 1) return 'Hace un momento';
    if (minutos < 60) return `Hace ${minutos} min`;
    if (horas < 24) return `Hace ${horas} h`;
    if (dias === 1) return 'Hace 1 día';
    return `Hace ${dias} días`;
  }

  /**
   * Cierra el dropdown al hacer clic fuera.
   */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;

    // Verificar si el click fue dentro del dropdown de notificaciones
    const clickedInside = this.elementRef.nativeElement.contains(target);
    const clickedOnNotificationButton = target.closest('.notification-button');

    if (!clickedInside && !clickedOnNotificationButton) {
      this.notificacionesOpen = false;
    }
  }

  // ===================================================================
  // SIDEBAR
  // ===================================================================

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  toggleCollapse(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
    localStorage.setItem('sidebar_collapsed', String(this.sidebarCollapsed));
  }

  closeMobileSidebar(): void {
    this.sidebarOpen = false;
  }

  // ===================================================================
  // LOGOUT
  // ===================================================================

  async confirmLogout(): Promise<void> {
    const result = await Swal.fire({
      title: '¿Cerrar sesión?',
      text: "¿Estás seguro de que deseas salir?",
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, salir',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    });

    if (result.isConfirmed) {
      //DETENER CONEXIÓN SIGNALR ANTES DE LOGOUT
      await this.notificacionService.detenerConexion();

      // Logout normal
      this.authService.logout();
      this.router.navigate(['/login']);
    }
  }

  // ===================================================================
  // CLEANUP
  // ===================================================================

  ngOnDestroy(): void {
    // Detener todas las suscripciones
    this.destroy$.next();
    this.destroy$.complete();

    // Detener SignalR (por si acaso, aunque logout ya lo hace)
    this.notificacionService.detenerConexion();

    console.log('MainLayout: Componente destruido, conexiones cerradas');
  }
}
