import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import Swal from 'sweetalert2';

import { BandejaService, Bandeja } from '../../services/bandeja';
import { NotificacionService } from '../../services/notificacion';
import { IconComponent } from '../../components/icon/icon.component';
import { EstadisticasBandejaDTO, BandejaDTO } from '../../models/notificacion.model';

/**
 * Componente para visualizar el mapa del mortuorio en tiempo real.
 * 
 * Características:
 * - Grid visual de 8 bandejas (B-01 a B-08)
 * - Actualización en tiempo real vía SignalR
 * - KPIs de ocupación y alertas
 * - Código de colores por estado
 * - Auto-refresh cada 30s como respaldo
 * 
 * @author SGM Team
 * @version 2.1.0
 */
@Component({
  selector: 'app-mapa-mortuorio',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './mapa-mortuorio.html',
  styleUrls: ['./mapa-mortuorio.css']
})
export class MapaMortuorioComponent implements OnInit, OnDestroy {
  private bandejaService = inject(BandejaService);
  private notificacionService = inject(NotificacionService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  // ===================================================================
  // DATOS
  // ===================================================================
  bandejas: Bandeja[] = [];
  isLoading = true;
  errorMessage = '';
  bandejaSeleccionada: Bandeja | null = null;

  // ===================================================================
  // ESTADO DE ALERTAS
  // ===================================================================
  alertaOcupacionActiva = false;
  private toastAlertaMostrado = false;
  private refreshInterval: any;

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnInit(): void {
    this.cargarMapa();
    this.suscribirseASignalR();

    // Auto-refresh cada 30s como respaldo (por si SignalR falla)
    this.refreshInterval = setInterval(() => {
      console.log('🔄 Auto-refresh de bandejas (respaldo)');
      this.cargarMapa();
    }, 30000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  // ===================================================================
  // SIGNALR - SUSCRIPCIONES
  // ===================================================================

  /**
   * Suscribe el componente a los eventos SignalR relevantes.
   */
  private suscribirseASignalR(): void {
    // 1. Actualización de bandeja individual (Tiempo Real)
    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejaActualizada: BandejaDTO) => {
        console.log('🔔 SignalR: Bandeja actualizada', bandejaActualizada);

        // Actualizar la bandeja en el array local sin recargar todo
        const index = this.bandejas.findIndex(b => b.bandejaID === bandejaActualizada.bandejaID);

        if (index !== -1) {
          // Mapeo explícito solo de campos relevantes (evita sobrescribir incorrectamente)
          this.bandejas[index] = {
            ...this.bandejas[index],
            estado: bandejaActualizada.estado,
            expedienteID: bandejaActualizada.expedienteID,
            codigoExpediente: bandejaActualizada.codigoExpediente,
            nombrePaciente: bandejaActualizada.nombrePaciente,
            usuarioAsignaNombre: bandejaActualizada.usuarioAsignaNombre,
            fechaHoraAsignacion: bandejaActualizada.fechaHoraAsignacion
              ? new Date(bandejaActualizada.fechaHoraAsignacion).toISOString()
              : undefined,
            tiempoOcupada: bandejaActualizada.tiempoOcupada,
            tieneAlerta: bandejaActualizada.tieneAlerta,
            observaciones: bandejaActualizada.observaciones
          };

          console.log('✅ Bandeja actualizada localmente:', this.bandejas[index]);

          // Recalcular alerta localmente
          this.verificarAlertaLocal();
        } else {
          console.warn('⚠️ Bandeja no encontrada en array local, recargando mapa...');
          this.cargarMapa();
        }
      });

    // 2. Alerta de Ocupación > 70%
    this.notificacionService.onAlertaOcupacion
      .pipe(takeUntil(this.destroy$))
      .subscribe((stats: EstadisticasBandejaDTO) => {
        console.log('🚨 SignalR: Alerta de ocupación', stats);

        this.alertaOcupacionActiva = stats.porcentajeOcupacion > 70;

        // Mostrar toast si no se ha mostrado recientemente
        if (this.alertaOcupacionActiva && !this.toastAlertaMostrado) {
          this.mostrarToastAlertaOcupacion(stats.porcentajeOcupacion);
        }
      });

    // 3. Alerta de Permanencia > 24h (opcional)
    this.notificacionService.onAlertaPermanencia
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejas) => {
        console.log('⏱️ SignalR: Alerta de permanencia', bandejas);

        // Marcar bandejas con alerta en el array local
        bandejas.forEach(bandejaAlerta => {
          const index = this.bandejas.findIndex(b => b.bandejaID === bandejaAlerta.bandejaID);
          if (index !== -1) {
            this.bandejas[index].tieneAlerta = true;
          }
        });
      });
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Carga el estado actual de todas las bandejas desde el backend.
   */
  cargarMapa(): void {
    // Solo mostrar loader en carga inicial
    if (this.bandejas.length === 0) {
      this.isLoading = true;
    }

    this.errorMessage = '';

    this.bandejaService.getDashboard().subscribe({
      next: (data) => {
        this.bandejas = data;
        this.isLoading = false;
        this.verificarAlertaLocal(); // Chequeo inicial de alertas

        console.log('✅ Mapa de mortuorio cargado:', data);
      },
      error: (err) => {
        console.error('❌ Error cargando mapa:', err);
        this.errorMessage = 'No se pudo cargar el estado del mortuorio';
        this.isLoading = false;

        Swal.fire({
          icon: 'error',
          title: 'Error de Conexión',
          text: 'No se pudo cargar el mapa del mortuorio. Intente nuevamente.',
          confirmButtonColor: '#0891B2'
        });
      }
    });
  }

  /**
   * Recarga manualmente el mapa (botón Actualizar).
   */
  recargar(): void {
    this.isLoading = true;
    this.cargarMapa();
  }

  /**
   * Verifica localmente si la ocupación supera el 70% y activa la alerta.
   */
  private verificarAlertaLocal(): void {
    if (this.bandejas.length === 0) return;

    const ocupadas = this.bandejas.filter(b => b.estado === 'Ocupada').length;
    const porcentaje = (ocupadas / this.bandejas.length) * 100;

    this.alertaOcupacionActiva = porcentaje > 70;
  }

  /**
   * Muestra un toast de SweetAlert2 cuando la ocupación es crítica.
   */
  private mostrarToastAlertaOcupacion(porcentaje: number): void {
    this.toastAlertaMostrado = true;

    Swal.fire({
      toast: true,
      position: 'top-end',
      icon: 'warning',
      title: `Ocupación: ${porcentaje}%`,
      text: 'Mortuorio casi lleno',
      showConfirmButton: false,
      timer: 4000,
      timerProgressBar: true,
      background: '#FEF3C7',
      iconColor: '#F59E0B'
    });

    // Reset del flag después de 1 minuto (evita spam)
    setTimeout(() => {
      this.toastAlertaMostrado = false;
    }, 60000);
  }

  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================

  /**
   * Retorna las clases CSS de Tailwind según el estado de la bandeja.
   */
  getBandejaClasses(b: Bandeja): string {
    const base = 'relative overflow-hidden group rounded-xl shadow-md transition-all hover:shadow-lg cursor-pointer border-l-4';

    switch (b.estado) {
      case 'Disponible':
        return `${base} border-green-500 bg-white hover:bg-green-50/30`;

      case 'Ocupada':
        const alerta = b.tieneAlerta
          ? 'ring-2 ring-red-500 ring-offset-2 animate-pulse'
          : '';
        return `${base} border-red-500 bg-white hover:bg-red-50/30 ${alerta}`;

      case 'Mantenimiento':
        return `${base} border-yellow-500 bg-yellow-50 hover:bg-yellow-100`;

      default:
        return `${base} border-gray-300 bg-gray-50`;
    }
  }

  /**
   * Retorna el ícono apropiado según el estado de la bandeja.
   */
  getBandejaIcon(b: Bandeja): string {
    switch (b.estado) {
      case 'Disponible': return 'circle-check';
      case 'Ocupada': return 'archive';
      case 'Mantenimiento': return 'settings';
      default: return 'info';
    }
  }

  /**
   * Retorna el color del ícono según el estado de la bandeja.
   */
  getBandejaIconColor(b: Bandeja): string {
    switch (b.estado) {
      case 'Disponible': return 'text-green-500';
      case 'Ocupada': return 'text-red-500';
      case 'Mantenimiento': return 'text-yellow-600';
      default: return 'text-gray-400';
    }
  }

  // ===================================================================
  // ESTADÍSTICAS (Getters para KPIs)
  // ===================================================================

  get totalDisponibles(): number {
    return this.bandejas.filter(b => b.estado === 'Disponible').length;
  }

  get totalOcupadas(): number {
    return this.bandejas.filter(b => b.estado === 'Ocupada').length;
  }

  get totalMantenimiento(): number {
    return this.bandejas.filter(b => b.estado === 'Mantenimiento').length;
  }

  get porcentajeOcupacion(): number {
    return this.bandejas.length
      ? Math.round((this.totalOcupadas / this.bandejas.length) * 100)
      : 0;
  }

  get bandejaConAlertas(): number {
    return this.bandejas.filter(b => b.tieneAlerta).length;
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  /**
   * Selecciona una bandeja y ejecuta la acción apropiada según su estado.
   */
  seleccionarBandeja(b: Bandeja): void {
    this.bandejaSeleccionada = b;

    if (b.estado === 'Disponible') {
      // Si está disponible, ir a asignar
      this.asignarBandeja(b.bandejaID);
    } else if (b.estado === 'Ocupada' && b.expedienteID) {
      // Si está ocupada, mostrar opciones
      this.mostrarOpcionesBandejaOcupada(b);
    } else if (b.estado === 'Mantenimiento') {
      // Si está en mantenimiento, permitir reactivar
      this.mostrarOpcionesMantenimiento(b);
    }
  }

  /**
   * Muestra opciones para una bandeja ocupada.
   */
  private mostrarOpcionesBandejaOcupada(b: Bandeja): void {
    Swal.fire({
      title: `Bandeja ${b.codigo}`,
      html: `
        <div class="text-left space-y-2">
          <p><strong>Paciente:</strong> ${b.nombrePaciente || 'N/A'}</p>
          <p><strong>Expediente:</strong> ${b.codigoExpediente || 'N/A'}</p>
          <p><strong>Tiempo:</strong> ${b.tiempoOcupada || 'N/A'}</p>
          ${b.tieneAlerta ? '<p class="text-red-600 font-semibold">⚠️ Excede tiempo límite</p>' : ''}
        </div>
      `,
      showDenyButton: true,
      showCancelButton: true,
      confirmButtonText: 'Ver Expediente',
      denyButtonText: 'Liberar Bandeja',
      cancelButtonText: 'Cerrar',
      confirmButtonColor: '#0891B2',
      denyButtonColor: '#DC3545'
    }).then((result) => {
      if (result.isConfirmed && b.expedienteID) {
        this.verExpediente(b.expedienteID);
      } else if (result.isDenied) {
        this.liberarBandeja(b);
      }
    });
  }

  /**
   * Muestra opciones para una bandeja en mantenimiento.
   */
  private mostrarOpcionesMantenimiento(b: Bandeja): void {
    Swal.fire({
      title: `${b.codigo} - Mantenimiento`,
      text: b.observaciones || 'Sin observaciones',
      icon: 'info',
      showCancelButton: true,
      confirmButtonText: 'Reactivar Bandeja',
      cancelButtonText: 'Cerrar',
      confirmButtonColor: '#10B981'
    }).then((result) => {
      if (result.isConfirmed) {
        this.reactivarBandeja(b.bandejaID);
      }
    });
  }

  /**
   * Navega a la vista de detalle del expediente.
   */
  verExpediente(expedienteId: number | undefined): void {
    if (!expedienteId) return; // Validación de seguridad
    this.router.navigate(['/expediente', expedienteId]);
    console.log('Ver expediente:', expedienteId);

    Swal.fire({
      icon: 'info',
      title: 'Función en Desarrollo',
      text: 'La vista de detalle del expediente estará disponible próximamente.',
      confirmButtonColor: '#0891B2'
    });
  }

  /**
   * Navega a la vista de asignación de bandeja.
   */
  asignarBandeja(bandejaId: number): void {
    this.router.navigate(['/asignar-bandeja', bandejaId]);
  }

  /**
   * Libera una bandeja ocupada (registra salida del mortuorio).
   */
  liberarBandeja(b: Bandeja): void {
    Swal.fire({
      title: `¿Liberar ${b.codigo}?`,
      html: `
        <p>Esta acción registrará la salida del cuerpo del mortuorio.</p>
        <p class="text-sm text-gray-500 mt-2">Paciente: ${b.nombrePaciente || 'N/A'}</p>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#DC3545',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, liberar bandeja',
      cancelButtonText: 'Cancelar'
    }).then(result => {
      if (result.isConfirmed) {
        this.bandejaService.liberar({ bandejaID: b.bandejaID }).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Bandeja Liberada',
              text: `${b.codigo} está ahora disponible`,
              timer: 2000,
              showConfirmButton: false
            });
            this.cargarMapa();
          },
          error: (err) => {
            console.error('Error al liberar bandeja:', err);
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: err.error?.message || 'No se pudo liberar la bandeja',
              confirmButtonColor: '#EF4444'
            });
          }
        });
      }
    });
  }

  /**
   * Reactiva una bandeja que estaba en mantenimiento.
   */
  reactivarBandeja(bandejaId: number): void {
    this.bandejaService.reactivar(bandejaId).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: 'Bandeja Reactivada',
          text: 'La bandeja está ahora disponible',
          timer: 2000,
          showConfirmButton: false
        });
        this.cargarMapa();
      },
      error: (err) => {
        console.error('Error al reactivar bandeja:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo reactivar la bandeja',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }
}
