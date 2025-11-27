import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import Swal from 'sweetalert2';

// Servicios del Backend
import { BandejaUniversalService, BandejaItem } from '../../../services/bandeja-universal';
import { CustodiaService } from '../../../services/custodia';
import { NotificacionService } from '../../../services/notificacion';
import { AuthService } from '../../../services/auth';

// Componentes y Utils
import { IconComponent } from '../../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../../utils/badge-styles';

@Component({
  selector: 'app-mis-tareas',
  standalone: true,
  imports: [CommonModule, IconComponent, FormsModule],
  templateUrl: './mis-tareas.html',
  styleUrl: './mis-tareas.css'
})
export class MisTareasComponent implements OnInit, OnDestroy {
  private bandejaService = inject(BandejaUniversalService);
  private custodiaService = inject(CustodiaService);
  private notificacionService = inject(NotificacionService);
  private authService = inject(AuthService);
  private router = inject(Router);

  private destroy$ = new Subject<void>();

  // Estado de Datos
  tareas: BandejaItem[] = [];
  tareasFiltradas: BandejaItem[] = [];

  isLoading = true;
  activeTab: 'pendientes' | 'custodia' = 'pendientes';
  userName = '';

  // KPIs para el Header (Contadores rápidos)
  kpiPendientes = 0;
  kpiEnCustodia = 0;

  ngOnInit() {
    this.userName = this.authService.getUserName();
    this.cargarDatos();
    this.suscribirseANotificaciones();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // CARGA DE DATOS (Integración con Bandeja Universal)
  // ===================================================================

  cargarDatos() {
    this.isLoading = true;

    // Reutilizamos BandejaUniversalService que ya filtra por rol 'Ambulancia'
    this.bandejaService.getItems()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items: BandejaItem[]) => {
          this.tareas = items;
          this.calcularKPIs();
          this.aplicarFiltro(this.activeTab);
          this.isLoading = false;
        },
        error: (err: any) => {
          console.error('Error cargando tareas:', err);
          this.isLoading = false;
        }
      });
  }

  private calcularKPIs() {
    // Filtramos localmente para los contadores
    this.kpiPendientes = this.tareas.filter(t =>
      t.estado === 'PendienteDeRecojo' || t.estado === 'EnPiso'
    ).length;

    this.kpiEnCustodia = this.tareas.filter(t =>
      t.estado === 'EnTrasladoMortuorio' || t.estado === 'PendienteAsignacionBandeja'
    ).length;
  }

  cambiarTab(tab: 'pendientes' | 'custodia') {
    this.activeTab = tab;
    this.aplicarFiltro(tab);
  }

  aplicarFiltro(tab: 'pendientes' | 'custodia') {
    if (tab === 'pendientes') {
      this.tareasFiltradas = this.tareas.filter(t =>
        t.estado === 'PendienteDeRecojo' || t.estado === 'EnPiso'
      );
    } else {
      this.tareasFiltradas = this.tareas.filter(t =>
        t.estado === 'EnTrasladoMortuorio' || t.estado === 'PendienteAsignacionBandeja'
      );
    }
  }

  // ===================================================================
  // SIGNALR (Tiempo Real)
  // ===================================================================

  private suscribirseANotificaciones() {
    // Escuchar nuevas solicitudes o cambios de estado
    this.notificacionService.onNotificacionGenerica
      .pipe(takeUntil(this.destroy$))
      .subscribe(notif => {
        // Si es relevante para ambulancia, recargar
        if (notif.titulo.includes('Nuevo Fallecido') || notif.titulo.includes('Traslado')) {
          this.cargarDatos();
          this.mostrarAlertaNuevaTarea(notif.mensaje);
        }
      });
  }

  private mostrarAlertaNuevaTarea(mensaje: string) {
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 5000,
      timerProgressBar: true,
      background: '#F3E8FF', // Morado suave
      color: '#6B21A8',
      iconColor: '#9333EA'
    });
    Toast.fire({
      icon: 'info',
      title: 'Nueva Solicitud',
      text: mensaje
    });
  }

  // ===================================================================
  // ACCIONES PRINCIPALES
  // ===================================================================

  /**
   * Acción: Escanear QR para aceptar custodia (Enfermería -> Ambulancia)
   */
  escanearQR() {
    Swal.fire({
      title: ' Escanear QR',
      html: `
        <div class="text-center mb-4">
          <div class="mx-auto w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mb-2">
             <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="text-gray-500"><rect width="5" height="5" x="3" y="3" rx="1"/><rect width="5" height="5" x="16" y="3" rx="1"/><rect width="5" height="5" x="3" y="16" rx="1"/><path d="M21 16h-3a2 2 0 0 0-2 2v3"/><path d="M21 21v.01"/><path d="M12 7v3a2 2 0 0 1-2 2H7"/><path d="M3 12h.01"/><path d="M12 3h.01"/><path d="M12 16v.01"/><path d="M16 12h1"/><path d="M21 12v.01"/><path d="M12 21v-1"/></svg>
          </div>
          <p class="text-sm text-gray-500">Simulación: Ingrese el código del expediente o QR del brazalete</p>
        </div>
      `,
      input: 'text',
      inputPlaceholder: 'Ej: SGM-2025-00001',
      showCancelButton: true,
      confirmButtonText: 'Simular Escaneo',
      confirmButtonColor: '#7E22CE', // Morado Ambulancia
      cancelButtonText: 'Cancelar',
      preConfirm: (codigo) => {
        if (!codigo) {
          Swal.showValidationMessage('Debe ingresar un código para continuar');
        }
        return codigo;
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.procesarTraspaso(result.value as string);
      }
    });
  }

  /**
   * Llama al servicio backend para registrar el traspaso de custodia
   */
  procesarTraspaso(codigoQR: string) {
    Swal.fire({
      title: 'Procesando...',
      text: 'Validando código y aceptando custodia',
      allowOutsideClick: false,
      didOpen: () => Swal.showLoading()
    });

    this.custodiaService.realizarTraspaso({
      codigoQR: codigoQR,
      observaciones: 'Recojo registrado desde móvil (App Técnico)'
    }).subscribe({
      next: (res: any) => {
        Swal.fire({
          title: '¡Custodia Aceptada!',
          html: `Has recibido el cuerpo de:<br><strong>${res.nombreCompleto}</strong>`,
          icon: 'success',
          timer: 2500,
          showConfirmButton: false
        });
        // Cambiar automáticamente a la pestaña de "En Custodia" para ver el item
        this.activeTab = 'custodia';
        this.cargarDatos();
      },
      error: (err: any) => {
        console.error(err);
        Swal.fire({
          title: 'Error',
          text: err.error?.message || 'No se pudo realizar el traspaso. Verifique el código o el estado del expediente.',
          icon: 'error',
          confirmButtonColor: '#7E22CE'
        });
      }
    });
  }

  /**
   * Navegar al mapa para la entrega final en mortuorio
   */
  irAMapa() {
    this.router.navigate(['/mapa-mortuorio']);
  }

  // ===================================================================
  // HELPERS VISUALES (Para estilos dinámicos en HTML)
  // ===================================================================

  getHeaderClass(estado: string): string {
    if (estado === 'EnTrasladoMortuorio') return 'bg-purple-600 text-white'; // Morado para tránsito
    return 'bg-hospital-cyan text-white'; // Azul para pendientes
  }

  getIconContainerClass(estado: string): string {
    if (estado === 'EnTrasladoMortuorio') return 'bg-purple-100 text-purple-600 border-purple-200';
    return 'bg-cyan-50 text-hospital-cyan border-cyan-200';
  }

  formatearTiempo(horas: number | undefined): string {
    if (!horas) return 'Reciente';
    if (horas < 1) return '< 1h';
    return `${Math.round(horas)}h`;
  }
}
