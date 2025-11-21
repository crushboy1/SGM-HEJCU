import { Component, inject, OnInit, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import Swal from 'sweetalert2'; // Importar SweetAlert2
import { AuthService } from '../../services/auth';
import { IconComponent } from '../../components/icon/icon.component';

// Interfaz local para notificaciones
interface Notificacion {
  id: number;
  tipo: 'alerta' | 'info' | 'exito' | 'advertencia';
  titulo: string;
  mensaje: string;
  fecha: string;
  leida: boolean;
  expedienteId?: number;
}

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
export class MainLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private elementRef = inject(ElementRef); // Para detectar clicks fuera

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
  // NOTIFICACIONES
  // ===================================================================
  notificacionesOpen = false;
  notificaciones: Notificacion[] = [];
  notificacionesNoLeidas = 0;

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();

    // Recuperar estado del sidebar
    const savedCollapsed = localStorage.getItem('sidebar_collapsed');
    if (savedCollapsed) {
      this.sidebarCollapsed = savedCollapsed === 'true';
    }

    this.cargarNotificaciones();

    // Simulación de polling
    // (En producción usaríamos SignalR o un servicio compartido)
    setInterval(() => {
      // this.cargarNotificaciones(); // Descomentar para simular updates
    }, 30000);
  }

  // ===================================================================
  // NOTIFICACIONES
  // ===================================================================
  cargarNotificaciones(): void {
    this.notificaciones = [
      {
        id: 1,
        tipo: 'alerta',
        titulo: 'Bandeja B-03 > 48 horas',
        mensaje: 'El expediente SGM-2025-00015 lleva más de 48 horas.',
        fecha: new Date().toISOString(),
        leida: false,
        expedienteId: 15
      },
      {
        id: 2,
        tipo: 'info',
        titulo: 'Nuevo expediente',
        mensaje: 'Se ha generado el expediente SGM-2025-00020.',
        fecha: new Date(Date.now() - 3600000).toISOString(),
        leida: false
      },
      {
        id: 3,
        tipo: 'advertencia',
        titulo: 'Corrección solicitada',
        mensaje: 'Solicitud de corrección para SGM-2025-00018',
        fecha: new Date(Date.now() - 7200000).toISOString(),
        leida: true
      }
    ];

    this.actualizarContador();
  }

  actualizarContador() {
    this.notificacionesNoLeidas = this.notificaciones.filter(n => !n.leida).length;
  }

  toggleNotificaciones(): void {
    this.notificacionesOpen = !this.notificacionesOpen;
  }

  abrirNotificacion(notif: Notificacion): void {
    notif.leida = true;
    this.actualizarContador();
    this.notificacionesOpen = false;

    if (notif.expedienteId) {
      // Navegar al detalle si existe ID (cuando creemos esa ruta)
      // this.router.navigate(['/expediente', notif.expedienteId]);
    }
  }

  marcarTodasLeidas(): void {
    this.notificaciones.forEach(n => n.leida = true);
    this.actualizarContador();
  }

  getNotificacionIcon(tipo: string): { icon: string; color: string } {
    const icons: Record<string, { icon: string; color: string }> = {
      'alerta': { icon: 'alert-triangle', color: 'text-red-500 bg-red-50' },
      'info': { icon: 'info', color: 'text-blue-500 bg-blue-50' },
      'exito': { icon: 'circle-check', color: 'text-green-500 bg-green-50' },
      'advertencia': { icon: 'alert-circle', color: 'text-yellow-500 bg-yellow-50' }
    };
    return icons[tipo] || icons['info'];
  }

  // Cerrar dropdown al hacer clic fuera
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
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
  confirmLogout(): void {
    Swal.fire({
      title: '¿Cerrar sesión?',
      text: "¿Estás seguro de que deseas salir?",
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#0891B2', // hospital-blue
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, salir',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.authService.logout();
        this.router.navigate(['/login']);

        // Toast opcional
        const Toast = Swal.mixin({
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 3000
        });
        Toast.fire({
          icon: 'success',
          title: 'Sesión cerrada correctamente'
        });
      }
    });
  }
}
