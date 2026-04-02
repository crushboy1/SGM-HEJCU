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
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);
  private elementRef = inject(ElementRef);
  private destroy$ = new Subject<void>();

  // ── Sidebar ──────────────────────────────────────────────────────
  sidebarOpen = false;
  sidebarHovered = false;

  /**
   * true cuando el sidebar debe mostrarse expandido.
   * Desktop → hover. Mobile → overlay abierto.
   */
  get sidebarExpanded(): boolean {
    return this.sidebarOpen || this.sidebarHovered;
  }

  // ── Usuario ───────────────────────────────────────────────────────
  userName = '';
  userRole = '';

  // ── Notificaciones ────────────────────────────────────────────────
  notificacionesOpen = false;
  notificaciones: NotificacionDTO[] = [];
  notificacionesNoLeidas = 0;
  conexionSignalREstablecida = false;

  coloresNotificacion = COLORES_NOTIFICACION;
  iconosNotificacion = ICONOS_NOTIFICACION;

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  async ngOnInit(): Promise<void> {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();
    await this.iniciarSignalR();
    this.suscribirseANotificaciones();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.notificacionService.detenerConexion();
  }

  // ===================================================================
  // SIGNALR
  // ===================================================================

  private async iniciarSignalR(): Promise<void> {
    try {
      await this.notificacionService.iniciarConexion();
    } catch {
      Swal.fire({
        icon: 'warning',
        title: 'Conexión en Tiempo Real',
        text: 'No se pudo conectar al servidor de notificaciones. Las alertas en tiempo real podrían no funcionar.',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 5000,
        timerProgressBar: true
      });
    }
  }

  private suscribirseANotificaciones(): void {
    this.notificacionService.conexionEstablecida
      .pipe(takeUntil(this.destroy$))
      .subscribe(conectado => { this.conexionSignalREstablecida = conectado; });

    this.notificacionService.notificaciones
      .pipe(takeUntil(this.destroy$))
      .subscribe(n => { this.notificaciones = n; });

    this.notificacionService.contadorNoLeidas
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => { this.notificacionesNoLeidas = count; });

    this.notificacionService.onExpedienteListoParaRetiro
      .pipe(takeUntil(this.destroy$))
      .subscribe(notif => {
        Swal.fire({
          icon: 'success', title: 'Retiro Autorizado', text: notif.mensaje,
          toast: true, position: 'top-end',
          showConfirmButton: false, timer: 6000, timerProgressBar: true
        });
      });

    this.notificacionService.onSalidaRegistrada
      .pipe(takeUntil(this.destroy$))
      .subscribe(notif => {
        Swal.fire({
          icon: 'info', title: 'Cuerpo Retirado', text: notif.mensaje,
          toast: true, position: 'top-end',
          showConfirmButton: false, timer: 5000, timerProgressBar: true
        });
      });

    this.notificacionService.onConfirmacionAccion
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ accion, exito, mensaje }) => {
        Swal.fire({
          icon: exito ? 'success' : 'error', title: accion, text: mensaje,
          toast: true, position: 'top-end',
          showConfirmButton: false, timer: 3000, timerProgressBar: true
        });
      });
  }

  // ===================================================================
  // NOTIFICACIONES
  // ===================================================================

  toggleNotificaciones(): void {
    this.notificacionesOpen = !this.notificacionesOpen;
  }

  abrirNotificacion(notif: NotificacionDTO): void {
    this.notificacionService.marcarComoLeida(notif.id);
    this.notificacionesOpen = false;
    if (notif.urlNavegacion) this.router.navigate([notif.urlNavegacion]);
  }

  marcarTodasLeidas(): void {
    this.notificacionService.marcarTodasComoLeidas();
    Swal.fire({
      icon: 'success', title: 'Notificaciones marcadas',
      toast: true, position: 'top-end',
      showConfirmButton: false, timer: 2000
    });
  }

  limpiarTodas(): void {
    Swal.fire({
      title: '¿Limpiar notificaciones?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#EF4444', cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, limpiar', cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then(result => {
      if (result.isConfirmed) {
        this.notificacionService.limpiarTodas();
        Swal.fire({
          icon: 'success', title: 'Notificaciones eliminadas',
          toast: true, position: 'top-end',
          showConfirmButton: false, timer: 2000
        });
      }
    });
  }

  getIconoNotificacion(tipo: string): string {
    return this.iconosNotificacion[tipo as keyof typeof this.iconosNotificacion] || 'info';
  }

  getColoresNotificacion(tipo: string) {
    return this.coloresNotificacion[tipo as keyof typeof this.coloresNotificacion]
      || this.coloresNotificacion['info'];
  }

  formatearTiempo(fecha: Date): string {
    const diff = Date.now() - new Date(fecha).getTime();
    const min = Math.floor(diff / 60000);
    const horas = Math.floor(diff / 3600000);
    const dias = Math.floor(diff / 86400000);
    if (min < 1) return 'Hace un momento';
    if (min < 60) return `Hace ${min} min`;
    if (horas < 24) return `Hace ${horas} h`;
    return dias === 1 ? 'Hace 1 día' : `Hace ${dias} días`;
  }

  /** Cierra el dropdown de notificaciones al hacer clic fuera */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.notificacionesOpen = false;
    }
  }

  // ===================================================================
  // SIDEBAR
  // ===================================================================

  toggleSidebar(): void { this.sidebarOpen = !this.sidebarOpen; }
  closeMobileSidebar(): void { this.sidebarOpen = false; }

  /** Hover-expand — solo en desktop (md+) */
  onSidebarMouseEnter(): void { this.sidebarHovered = true; }
  onSidebarMouseLeave(): void { this.sidebarHovered = false; }

  // ===================================================================
  // LOGOUT
  // ===================================================================

  async confirmLogout(): Promise<void> {
    const result = await Swal.fire({
      title: '¿Cerrar sesión?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#0891B2', cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, salir', cancelButtonText: 'Cancelar',
      reverseButtons: true
    });

    if (result.isConfirmed) {
      await this.notificacionService.detenerConexion();
      this.authService.logout();
      this.router.navigate(['/login']);
    }
  }
}
