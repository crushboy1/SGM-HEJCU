import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import Swal from 'sweetalert2';

import { ExpedienteService } from '../../services/expediente';
import { BandejaService } from '../../services/bandeja';
import { NotificacionService } from '../../services/notificacion';
import { IconComponent } from '../../components/icon/icon.component';
import { FormularioSalida } from '../../components/formulario-salida/formulario-salida';
import { EstadisticasBandejaDTO, BandejaDTO } from '../../models/notificacion.model';

/**
 * Componente para visualizar el mapa del mortuorio en tiempo real.
 * 
 * CARACTERISTICAS:
 * - Grid visual de 8 bandejas (B-01 a B-08)
 * - Actualizacion en tiempo real via SignalR
 * - KPIs de ocupacion y alertas
 * - Codigo de colores por estado
 * - Auto-refresh cada 30s como respaldo
 * - Modal integrado para registro de salida
 * 
 * FLUJO REGISTRO SALIDA:
 * - Usuario hace click en boton "Salida" de bandeja ocupada
 * - Sistema carga expediente via ExpedienteService
 * - Abre modal FormularioSalida pasando expediente
 * - Modal registra salida y libera bandeja automaticamente
 * - Mapa se actualiza via SignalR
 * 
 * @version 2.3.0
 * @author SGM Development Team
 * 
 * CHANGELOG v2.3.0:
 * - Integrado modal FormularioSalida para registro de salida
 * - Eliminada navegacion a /registro-salida
 * - Agregado metodo abrirModalSalida()
 * - Agregado metodo onSalidaCompletada()
 */
@Component({
  selector: 'app-mapa-mortuorio',
  standalone: true,
  imports: [CommonModule, IconComponent, FormularioSalida],
  templateUrl: './mapa-mortuorio.html',
  styleUrls: ['./mapa-mortuorio.css']
})
export class MapaMortuorioComponent implements OnInit, OnDestroy {
  private bandejaService = inject(BandejaService);
  private notificacionService = inject(NotificacionService);
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  bandejas: BandejaDTO[] = [];
  estadisticas: EstadisticasBandejaDTO | null = null;
  isLoading = true;
  errorMessage = '';
  bandejaSeleccionada: BandejaDTO | null = null;

  private destroy$ = new Subject<void>();
  private refreshInterval: any;

  // Estado de alertas
  alertaOcupacionActiva = false;
  private toastAlertaMostrado = false;

  // Modo asignacion (desde Mis Tareas)
  modoAsignacion = false;
  expedienteIdParaAsignar: number | null = null;
  pacienteNombre: string = '';
  pacienteCodigo: string = '';

  // Modal de registro de salida
  mostrarModalSalida = false;
  expedienteParaSalida: any = null;

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnInit(): void {
    this.cargarMapa();
    this.suscribirseASignalR();

    // Detectar si venimos de "Mis Tareas" para asignar bandeja
    this.route.queryParams.subscribe(params => {
      const id = params['expedienteId'];
      if (id) {
        this.modoAsignacion = true;
        this.expedienteIdParaAsignar = +id;
        this.cargarDatosExpediente(this.expedienteIdParaAsignar);
      }
    });

    // Auto-refresh cada 30s como respaldo (por si SignalR falla)
    this.refreshInterval = setInterval(() => {
      console.log('[MapaMortuorio] Auto-refresh de bandejas (respaldo)');
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
    // 1. Actualizacion de bandeja individual (Tiempo Real)
    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejaActualizada: BandejaDTO) => {
        console.log('[SignalR] Bandeja actualizada:', bandejaActualizada.codigo);

        const index = this.bandejas.findIndex(b => b.bandejaID === bandejaActualizada.bandejaID);

        if (index !== -1) {
          this.bandejas[index] = {
            ...this.bandejas[index],
            estado: bandejaActualizada.estado,
            expedienteID: bandejaActualizada.expedienteID,
            codigoExpediente: bandejaActualizada.codigoExpediente,
            nombrePaciente: bandejaActualizada.nombrePaciente,
            usuarioAsignaNombre: bandejaActualizada.usuarioAsignaNombre,
            fechaHoraAsignacion: bandejaActualizada.fechaHoraAsignacion,
            fechaHoraLiberacion: bandejaActualizada.fechaHoraLiberacion,
            tiempoOcupada: bandejaActualizada.tiempoOcupada,
            tieneAlerta: bandejaActualizada.tieneAlerta,
            observaciones: bandejaActualizada.observaciones
          };
        }
      });

    // 2. Alerta de ocupacion critica
    this.notificacionService.onAlertaOcupacion
      .pipe(takeUntil(this.destroy$))
      .subscribe((estadisticas: EstadisticasBandejaDTO) => {
        console.log('[SignalR] Alerta ocupacion:', estadisticas.porcentajeOcupacion + '%');

        this.alertaOcupacionActiva = estadisticas.porcentajeOcupacion > 70;

        if (this.alertaOcupacionActiva && !this.toastAlertaMostrado) {
          this.mostrarAlertaOcupacion(estadisticas);
          this.toastAlertaMostrado = true;

          // Reset despues de 5 minutos
          setTimeout(() => {
            this.toastAlertaMostrado = false;
          }, 300000);
        }
      });

    // 3. Alerta de permanencia (>24h o >48h)
    this.notificacionService.onAlertaPermanencia
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejasConAlerta: BandejaDTO[]) => {
        console.log('[SignalR] Alerta permanencia:', bandejasConAlerta.length + ' bandejas');

        bandejasConAlerta.forEach(bandeja => {
          const index = this.bandejas.findIndex(b => b.bandejaID === bandeja.bandejaID);
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
   * Carga el mapa completo de bandejas desde el backend.
   */
  cargarMapa(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.bandejaService.getDashboard().subscribe({
      next: (data) => {
        this.bandejas = data;
        this.isLoading = false;
        console.log('[MapaMortuorio] Mapa cargado:', data.length + ' bandejas');
      },
      error: (error) => {
        console.error('[MapaMortuorio] Error al cargar mapa:', error);
        this.errorMessage = 'Error al cargar el mapa del mortuorio';
        this.isLoading = false;

        Swal.fire({
          icon: 'error',
          title: 'Error de Conexion',
          text: 'No se pudo cargar el mapa del mortuorio. Verifica tu conexion.',
          confirmButtonColor: '#EF4444',
          showConfirmButton: true
        });
      }
    });
  }

  /**
   * Carga datos del expediente para modo asignacion.
   */
  private cargarDatosExpediente(id: number): void {
    this.expedienteService.getById(id).subscribe({
      next: (exp) => {
        if (exp) {
          this.pacienteNombre = exp.nombreCompleto;
          this.pacienteCodigo = exp.codigoExpediente;
        }
      },
      error: () => {
        this.mostrarAlerta('error', 'No se pudo cargar la informacion del expediente a asignar');
      }
    });
  }

  // ===================================================================
  // GETTERS PARA KPIs
  // ===================================================================

  get totalDisponibles(): number {
    return this.bandejas.filter(b => b.estado.toLowerCase() === 'disponible').length;
  }

  get totalOcupadas(): number {
    return this.bandejas.filter(b => b.estado.toLowerCase() === 'ocupada').length;
  }

  get porcentajeOcupacion(): number {
    const total = this.bandejas.length;
    if (total === 0) return 0;
    return Math.round((this.totalOcupadas / total) * 100);
  }

  get bandejaConAlertas(): number {
    return this.bandejas.filter(b => b.tieneAlerta).length;
  }

  // ===================================================================
  // REGISTRO DE SALIDA (MODAL)
  // ===================================================================

  /**
   * Abre modal de registro de salida.
   * Carga expediente y valida estado antes de abrir.
   */
  registrarSalida(bandeja: BandejaDTO): void {
    console.log('[MapaMortuorio] Iniciando registro de salida para bandeja:', bandeja.codigo);

    // 1. Validar estado
    if (bandeja.estado !== 'Ocupada') {
      Swal.fire({
        icon: 'warning',
        title: 'Bandeja No Ocupada',
        text: 'Solo se puede registrar salida de bandejas ocupadas',
        confirmButtonColor: '#F59E0B',
        showConfirmButton: true
      });
      return;
    }

    // 2. Validar que tenga expediente asociado
    if (!bandeja.expedienteID) {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se encontro expediente asociado a esta bandeja',
        confirmButtonColor: '#EF4444',
        showConfirmButton: true
      });
      return;
    }

    // 3. Cargar expediente completo
    this.isLoading = true;
    this.expedienteService.getById(bandeja.expedienteID).subscribe({
      next: (expediente) => {
        this.isLoading = false;

        // 4. Validar estado del expediente
        const estadosPermitidos = ['EnBandeja', 'PendienteRetiro'];
        if (!estadosPermitidos.includes(expediente.estadoActual)) {
          Swal.fire({
            icon: 'error',
            title: 'Estado Invalido para Salida',
            html: `
              <div class="text-left text-sm">
                <p>El expediente esta en estado: <strong class="text-red-600">${expediente.estadoActual}</strong></p>
                <p class="text-gray-600 mt-2">
                  Solo se puede registrar salida si el expediente esta en:<br>
                  - <strong>En Bandeja</strong><br>
                  - <strong>Pendiente Retiro</strong>
                </p>
              </div>
            `,
            confirmButtonColor: '#EF4444',
            showConfirmButton: true
          });
          return;
        }

        // 5. Agregar codigo de bandeja al expediente (para mostrar en modal)
        expediente.codigoBandeja = bandeja.codigo;

        // 6. Abrir modal
        this.expedienteParaSalida = expediente;
        this.mostrarModalSalida = true;

        console.log('[MapaMortuorio] Modal de salida abierto para expediente:', expediente.codigoExpediente);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[MapaMortuorio] Error al cargar expediente:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Cargar Expediente',
          text: err.error?.message || 'No se pudo cargar el expediente',
          confirmButtonColor: '#EF4444',
          showConfirmButton: true
        });
      }
    });
  }

  /**
   * Maneja el evento cuando se registra salida exitosamente.
   * Cierra modal y refresca mapa.
   */
  onSalidaCompletada(response: any): void {
    console.log('[MapaMortuorio] Salida completada:', response);

    // Cerrar modal
    this.cerrarModalSalida();

    // Recargar mapa (SignalR tambien actualizara, pero esto es respaldo)
    this.cargarMapa();

    // Mostrar notificacion toast
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000
    });

    Toast.fire({
      icon: 'success',
      title: 'Salida Registrada',
      text: 'Bandeja liberada exitosamente'
    });
  }

  /**
   * Cierra el modal de registro de salida.
   */
  cerrarModalSalida(): void {
    this.mostrarModalSalida = false;
    this.expedienteParaSalida = null;
  }

  // ===================================================================
  // MODO ASIGNACION (DESDE MIS TAREAS)
  // ===================================================================

  /**
   * Maneja el click en una bandeja.
   * Si estamos en modo asignacion, valida y confirma asignacion.
   */
  seleccionarBandeja(bandeja: BandejaDTO): void {
    // Modo asignacion (prioridad)
    if (this.modoAsignacion) {
      if (bandeja.estado === 'Disponible') {
        this.confirmarAsignacion(bandeja);
      } else {
        this.mostrarAlerta('warning', 'Esta bandeja esta ocupada o en mantenimiento. Elija una verde');
      }
      return;
    }

    // Modo normal (ver detalle si esta ocupada)
    if (bandeja.estado === 'Ocupada') {
      this.verDetalleBandeja(bandeja);
    }
  }

  /**
   * Confirma asignacion de bandeja en modo asignacion.
   */
  private confirmarAsignacion(bandeja: BandejaDTO): void {
    if (!this.expedienteIdParaAsignar) return;

    Swal.fire({
      title: 'Confirmar Asignacion',
      html: `
        <div class="text-left text-sm">
          <p><strong>Paciente:</strong> ${this.pacienteNombre}</p>
          <p><strong>Expediente:</strong> ${this.pacienteCodigo}</p>
          <p class="mt-2">Se asignara a la bandeja: <span class="text-green-600 font-bold text-xl">${bandeja.codigo}</span></p>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10B981',
      confirmButtonText: 'Si, Asignar',
      cancelButtonText: 'Cancelar',
      showConfirmButton: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.ejecutarAsignacion(bandeja.bandejaID);
      }
    });
  }

  /**
   * Ejecuta la asignacion de bandeja.
   */
  private ejecutarAsignacion(bandejaId: number): void {
    this.bandejaService.asignar({
      bandejaID: bandejaId,
      expedienteID: this.expedienteIdParaAsignar!,
      observaciones: 'Asignacion desde Mapa Mortuorio'
    }).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: 'Asignado Correctamente',
          text: `${this.pacienteNombre} ahora esta en la bandeja asignada`,
          confirmButtonColor: '#10B981',
          showConfirmButton: true
        }).then(() => {
          this.router.navigate(['/mis-tareas']);
        });
      },
      error: (err) => {
        console.error('[MapaMortuorio] Error al asignar:', err);
        this.mostrarAlerta('error', 'Error al asignar la bandeja');
      }
    });
  }

  /**
   * Cancela modo asignacion y vuelve a Mis Tareas.
   */
  cancelarAsignacion(): void {
    this.router.navigate(['/mis-tareas']);
  }

  // ===================================================================
  // NAVEGACION Y ACCIONES
  // ===================================================================

  /**
   * Muestra detalle de una bandeja.
   */
  verDetalleBandeja(bandeja: BandejaDTO): void {
    this.bandejaSeleccionada = bandeja;
    console.log('[MapaMortuorio] Ver detalle bandeja:', bandeja.codigo);
  }

  /**
   * Marca una bandeja como "En Mantenimiento".
   */
  marcarMantenimiento(bandeja: BandejaDTO): void {
    if (bandeja.estado.toLowerCase() !== 'disponible') {
      Swal.fire({
        icon: 'warning',
        title: 'Accion No Permitida',
        text: 'Solo se pueden poner en mantenimiento bandejas disponibles',
        confirmButtonColor: '#F59E0B',
        showConfirmButton: true
      });
      return;
    }

    Swal.fire({
      title: `Poner ${bandeja.codigo} en mantenimiento`,
      input: 'textarea',
      inputLabel: 'Motivo del mantenimiento',
      inputPlaceholder: 'Ej: Limpieza profunda, reparacion, etc.',
      inputAttributes: {
        'aria-label': 'Motivo del mantenimiento'
      },
      showCancelButton: true,
      confirmButtonColor: '#F59E0B',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Si, marcar',
      cancelButtonText: 'Cancelar',
      showConfirmButton: true,
      inputValidator: (value) => {
        if (!value) {
          return 'Debes ingresar un motivo';
        }
        return null;
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.bandejaService.marcarMantenimiento(bandeja.bandejaID, result.value).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Mantenimiento Iniciado',
              text: `${bandeja.codigo} esta ahora en mantenimiento`,
              confirmButtonColor: '#10B981',
              showConfirmButton: true
            });
            this.cargarMapa();
          },
          error: (err: any) => {
            console.error('[MapaMortuorio] Error al marcar mantenimiento:', err);
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: err.message || 'No se pudo marcar la bandeja en mantenimiento',
              confirmButtonColor: '#EF4444',
              showConfirmButton: true
            });
          }
        });
      }
    });
  }

  /**
   * Finaliza el mantenimiento de una bandeja.
   */
  finalizarMantenimiento(bandeja: BandejaDTO): void {
    if (bandeja.estado.toLowerCase() !== 'mantenimiento') {
      Swal.fire({
        icon: 'warning',
        title: 'Accion No Permitida',
        text: 'Solo se puede finalizar mantenimiento de bandejas en ese estado',
        confirmButtonColor: '#F59E0B',
        showConfirmButton: true
      });
      return;
    }

    Swal.fire({
      title: `Finalizar mantenimiento de ${bandeja.codigo}`,
      text: 'La bandeja volvera a estar disponible',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10B981',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Si, finalizar',
      cancelButtonText: 'Cancelar',
      showConfirmButton: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.bandejaService.finalizarMantenimiento(bandeja.bandejaID).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Mantenimiento Finalizado',
              text: `${bandeja.codigo} esta ahora disponible`,
              confirmButtonColor: '#10B981',
              showConfirmButton: true
            });
            this.cargarMapa();
          },
          error: (err: any) => {
            console.error('[MapaMortuorio] Error al finalizar mantenimiento:', err);
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: err.message || 'No se pudo finalizar el mantenimiento',
              confirmButtonColor: '#EF4444',
              showConfirmButton: true
            });
          }
        });
      }
    });
  }

  /**
   * Libera manualmente una bandeja ocupada (emergencia).
   */
  liberarBandejaManual(bandeja: BandejaDTO): void {
    if (!this.bandejaService.puedeGestionarMantenimiento()) {
      Swal.fire({
        icon: 'error',
        title: 'Sin Permisos',
        text: 'No tienes permisos para liberar bandejas manualmente',
        confirmButtonColor: '#EF4444',
        showConfirmButton: true
      });
      return;
    }

    Swal.fire({
      title: `Liberar ${bandeja.codigo} manualmente`,
      html: `
        <p class="text-sm text-gray-600 mb-4">Esta es una accion de emergencia que quedara registrada en auditoria.</p>
        <p class="text-sm font-semibold mb-2">Paciente: ${bandeja.nombrePaciente || 'N/A'}</p>
        <select id="motivo-select" class="swal2-input">
          <option value="">Seleccionar motivo...</option>
          <option value="Error en sistema de registro">Error en sistema de registro</option>
          <option value="Salida sin completar tramites">Salida sin completar tramites</option>
          <option value="Correccion de asignacion incorrecta">Correccion de asignacion incorrecta</option>
          <option value="Emergencia o evacuacion">Emergencia o evacuacion</option>
          <option value="otro">Otro (especificar abajo)</option>
        </select>
        <textarea id="observaciones-textarea" class="swal2-textarea" placeholder="Observaciones detalladas (minimo 20 caracteres)" rows="3"></textarea>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Si, liberar',
      cancelButtonText: 'Cancelar',
      showConfirmButton: true,
      preConfirm: () => {
        const motivoSelect = document.getElementById('motivo-select') as HTMLSelectElement;
        const observaciones = (document.getElementById('observaciones-textarea') as HTMLTextAreaElement).value;

        let motivo = motivoSelect.value;

        if (!motivo) {
          Swal.showValidationMessage('Debes seleccionar un motivo');
          return false;
        }

        if (!observaciones || observaciones.length < 20) {
          Swal.showValidationMessage('Las observaciones deben tener al menos 20 caracteres');
          return false;
        }

        if (motivo === 'otro') {
          motivo = observaciones.split('\n')[0].substring(0, 100);
        }

        return { motivo, observaciones };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.bandejaService.liberarManualmente(
          bandeja.bandejaID,
          result.value.motivo,
          result.value.observaciones
        ).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Bandeja Liberada',
              text: `${bandeja.codigo} esta ahora disponible`,
              confirmButtonColor: '#10B981',
              showConfirmButton: true
            });
            this.cargarMapa();
          },
          error: (err: any) => {
            console.error('[MapaMortuorio] Error al liberar bandeja:', err);
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: err.message || 'No se pudo liberar la bandeja',
              confirmButtonColor: '#EF4444',
              showConfirmButton: true
            });
          }
        });
      }
    });
  }

  // ===================================================================
  // ALERTAS Y NOTIFICACIONES
  // ===================================================================

  /**
   * Muestra toast de alerta de ocupacion critica.
   */
  private mostrarAlertaOcupacion(estadisticas: EstadisticasBandejaDTO): void {
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 5000,
      timerProgressBar: true
    });

    Toast.fire({
      icon: 'warning',
      title: 'Ocupacion Critica',
      text: `Mortuorio al ${estadisticas.porcentajeOcupacion}% de capacidad`
    });
  }

  /**
   * Muestra alerta toast generica.
   */
  private mostrarAlerta(icon: 'success' | 'warning' | 'error' | 'info', title: string): void {
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000
    });
    Toast.fire({ icon, title });
  }

  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================

  /**
   * Obtiene la clase CSS segun el estado de la bandeja.
   */
  getEstadoClase(bandeja: BandejaDTO): string {
    return this.bandejaService.getEstadoColor(bandeja.estado);
  }

  /**
   * Obtiene el icono segun el estado de la bandeja.
   */
  getEstadoIcono(bandeja: BandejaDTO): string {
    return this.bandejaService.getEstadoIcon(bandeja.estado);
  }

  /**
   * Formatea el tiempo ocupada en formato legible.
   */
  formatearTiempoOcupada(tiempoString?: string): string {
    return this.bandejaService.formatearTiempoOcupada(tiempoString);
  }

  /**
   * Verifica si una bandeja esta disponible.
   */
  esDisponible(bandeja: BandejaDTO): boolean {
    return bandeja.estado.toLowerCase() === 'disponible';
  }

  /**
   * Verifica si una bandeja esta ocupada.
   */
  esOcupada(bandeja: BandejaDTO): boolean {
    return bandeja.estado.toLowerCase() === 'ocupada';
  }

  /**
   * Verifica si una bandeja esta en mantenimiento.
   */
  esMantenimiento(bandeja: BandejaDTO): boolean {
    return bandeja.estado.toLowerCase() === 'mantenimiento';
  }

  /**
   * Verifica si el usuario puede gestionar mantenimiento.
   */
  puedeGestionarMantenimiento(): boolean {
    return this.bandejaService.puedeGestionarMantenimiento();
  }
}
