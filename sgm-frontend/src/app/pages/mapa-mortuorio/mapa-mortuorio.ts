import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
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
 * Mapa visual del mortuorio en tiempo real.
 *
 * CARACTERÍSTICAS:
 * - Grid de 8 bandejas con estados (Disponible / Ocupada / Mantenimiento)
 * - Tiempo real vía SignalR + auto-refresh 30s como respaldo
 * - KPIs con cache (calcularEstadisticas) — no recalcula en cada CD
 * - Modal mantenimiento inline (Admin / JefeGuardia / VigilanteSupervisor)
 * - Modal salida via componente FormularioSalida
 * - Modo asignación desde Mis Tareas
 * - Salida forzada solo Admin/JG/VigSup
 *
 * @version 3.0.0
 * @changelog
 * - v3.0.0: Modal mantenimiento con datos completos (motivo, fechas, responsable).
 *           Cache _statsCache — KPIs O(1). Toast reutilizable.
 *           manejarError() centralizado. queryParams con takeUntil.
 *           ESTADOS_SALIDA_PERMITIDOS readonly. trackByBandeja.
 *           puedeGestionarMantenimiento → getter puedeLiberar.
 *           refreshInterval tipado. Fix tiempo >24h (backend).
 *           Renombrado "Registro Manual" → "Registro".
 */
@Component({
  selector: 'app-mapa-mortuorio',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent, FormularioSalida],
  templateUrl: './mapa-mortuorio.html',
  styleUrls: ['./mapa-mortuorio.css']
})
export class MapaMortuorioComponent implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private bandejaService = inject(BandejaService);
  private notificacionService = inject(NotificacionService);
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private destroy$ = new Subject<void>();

  // ── Estado general ───────────────────────────────────────────────
  bandejas: BandejaDTO[] = [];
  isLoading = true;
  errorMessage = '';
  bandejaSeleccionada: BandejaDTO | null = null;

  // ── Auto-refresh tipado ──────────────────────────────────
  private refreshInterval: ReturnType<typeof setInterval> | null = null;

  // ── Alertas ───────────────────────────────────────────────────────
  alertaOcupacionActiva = false;
  private toastAlertaMostrado = false;

  // ── Cache de estadísticas ────────────────────────────────────
  private _statsCache = { disponibles: 0, ocupadas: 0, alertas: 0, mantenimiento: 0 };

  // ── Toast reutilizable ───────────────────────────────────────
  private readonly toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true
  });

  // ── Estados permitidos para salida ──────────────────────────
  private readonly ESTADOS_SALIDA_PERMITIDOS = ['PendienteRetiro'];

  // ── Modo asignación ───────────────────────────────────────────────
  modoAsignacion = false;
  expedienteIdParaAsignar: number | null = null;
  pacienteNombre = '';
  pacienteCodigo = '';

  // ── Modal salida (FormularioSalida) ───────────────────────────────
  mostrarModalSalida = false;
  expedienteParaSalida: any = null;

  // ── Modal mantenimiento ──────────────────────────────────
  mostrarModalMantenimiento = false;
  bandejaParaMantenimiento: BandejaDTO | null = null;
  formMantenimiento!: FormGroup;
  isLoadingMantenimiento = false;

  // Catálogo motivos)
  readonly motivosMantenimiento = [
    { value: 'Limpieza', label: 'Limpieza / Desinfección' },
    { value: 'Reparacion', label: 'Reparación' },
    { value: 'InspeccionSanitaria', label: 'Inspección Sanitaria' },
    { value: 'FallaTecnica', label: 'Falla Técnica' },
    { value: 'Otro', label: 'Otro' },
  ];

  // ===================================================================
  // GETTERS — KPIs desde cache
  // ===================================================================

  get totalDisponibles(): number { return this._statsCache.disponibles; }
  get totalOcupadas(): number { return this._statsCache.ocupadas; }
  get totalMantenimiento(): number { return this._statsCache.mantenimiento; }
  get bandejaConAlertas(): number { return this._statsCache.alertas; }

  get porcentajeOcupacion(): number {
    const total = this.bandejas.length;
    return total === 0 ? 0 : Math.round((this._statsCache.ocupadas / total) * 100);
  }
  get estadoOcupacion(): 'critico' | 'atencion' | 'normal' {
    if (this.porcentajeOcupacion >= 70) return 'critico';
    if (this.porcentajeOcupacion >= 50) return 'atencion';
    return 'normal';
  }
  /** Admin / JefeGuardia / VigilanteSupervisor */
  get puedeLiberar(): boolean {
    return this.bandejaService.puedeGestionarMantenimiento();
  }


  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    this.buildFormMantenimiento();
    this.cargarMapa();
    this.suscribirseASignalR();
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const id = params['expedienteId'];
        if (id) {
          this.modoAsignacion = true;
          this.expedienteIdParaAsignar = +id;
          this.cargarDatosExpediente(this.expedienteIdParaAsignar);
        }
      });

    // Auto-refresh 30s como respaldo si SignalR falla
    this.refreshInterval = setInterval(() => {
      this.cargarMapa();
    }, 30000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  // ===================================================================
  // FORM MANTENIMIENTO
  // ===================================================================

  private buildFormMantenimiento(): void {
    this.formMantenimiento = this.fb.group({
      motivo: ['', Validators.required],
      detalle: [''],
      fechaInicio: [this.formatDateTimeLocal(new Date()), Validators.required],
      fechaEstimadaFin: [''],
      responsableExterno: ['']
    });
  }

  // ===================================================================
  // CACHE DE ESTADÍSTICAS
  // Llamado después de cargar o actualizar bandejas.
  // ===================================================================

  private calcularEstadisticas(): void {
    let disponibles = 0, ocupadas = 0, alertas = 0, mantenimiento =0;
    for (const b of this.bandejas) {
      const estado = b.estado?.toLowerCase() ?? '';
      if (estado === 'disponible') disponibles++;
      if (estado === 'ocupada') ocupadas++;
      if (estado === 'mantenimiento') mantenimiento++;
      if (b.tieneAlerta) alertas++;
    }
    this._statsCache = { disponibles, ocupadas, alertas, mantenimiento };
  }

  // ===================================================================
  // HELPERS DE ESTADO
  // ===================================================================

  private normalizarEstado(estado: string): string {
    return estado?.toLowerCase() ?? '';
  }

  esDisponible(b: BandejaDTO): boolean {
    return this.normalizarEstado(b.estado) === 'disponible';
  }
  esOcupada(b: BandejaDTO): boolean {
    return this.normalizarEstado(b.estado) === 'ocupada';
  }
  esMantenimiento(b: BandejaDTO): boolean {
    return this.normalizarEstado(b.estado) === 'mantenimiento';
  }

  getEstadoIcono(b: BandejaDTO): string {
    return this.bandejaService.getEstadoIcon(b.estado);
  }

  /** trackBy para *ngFor del grid — evita re-render completo en updates SignalR (C11) */
  trackByBandeja(_: number, b: BandejaDTO): number {
    return b.bandejaID;
  }

  // ===================================================================
  // SIGNALR
  // ===================================================================

  private suscribirseASignalR(): void {
    // 1. Actualización de bandeja individual
    this.notificacionService.onActualizacionBandeja
      .pipe(takeUntil(this.destroy$))
      .subscribe((actualizada: BandejaDTO) => {
        const index = this.bandejas.findIndex(b => b.bandejaID === actualizada.bandejaID);
        if (index !== -1) {
          // Spread operator — actualiza sin recrear el array (C9)
          this.bandejas[index] = { ...this.bandejas[index], ...actualizada };
          this.calcularEstadisticas();
        }
      });

    // 2. Alerta de ocupación crítica
    this.notificacionService.onAlertaOcupacion
      .pipe(takeUntil(this.destroy$))
      .subscribe((stats: EstadisticasBandejaDTO) => {
        this.alertaOcupacionActiva = stats.porcentajeOcupacion > 70;
        if (this.alertaOcupacionActiva && !this.toastAlertaMostrado) {
          this.toast.fire({
            icon: 'warning',
            title: 'Ocupación Crítica',
            text: `Mortuorio al ${stats.porcentajeOcupacion.toFixed(0)}% de capacidad`
          });
          this.toastAlertaMostrado = true;
          setTimeout(() => { this.toastAlertaMostrado = false; }, 300000);
        }
      });

    // 3. Alerta de permanencia >24h / >48h
    this.notificacionService.onAlertaPermanencia
      .pipe(takeUntil(this.destroy$))
      .subscribe((bandejasConAlerta: BandejaDTO[]) => {
        bandejasConAlerta.forEach(b => {
          const index = this.bandejas.findIndex(x => x.bandejaID === b.bandejaID);
          if (index !== -1) {
            this.bandejas[index] = { ...this.bandejas[index], tieneAlerta: true };
          }
        });
        this.calcularEstadisticas();
      });
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  cargarMapa(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.bandejaService.getDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.bandejas = data;
          this.calcularEstadisticas();
          this.isLoading = false;
        },
        error: err => {
          this.isLoading = false;
          this.manejarError('Error al cargar el mapa del mortuorio', err);
          this.errorMessage = 'Error al cargar el mapa del mortuorio';
        }
      });
  }

  private cargarDatosExpediente(id: number): void {
    this.expedienteService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: exp => {
          if (exp) {
            this.pacienteNombre = exp.nombreCompleto;
            this.pacienteCodigo = exp.codigoExpediente;
          }
        },
        error: () => {
          this.toast.fire({ icon: 'error', title: 'No se pudo cargar el expediente' });
        }
      });
  }

  // ===================================================================
  // REGISTRO DE SALIDA (MODAL FormularioSalida)
  // ===================================================================

  registrarSalida(bandeja: BandejaDTO): void {
    // Guard clauses (C7)
    if (!this.esOcupada(bandeja)) {
      this.toast.fire({ icon: 'warning', title: 'Solo se puede registrar salida de bandejas ocupadas' });
      return;
    }
    if (!bandeja.expedienteID) {
      this.toast.fire({ icon: 'error', title: 'No se encontró expediente asociado a esta bandeja' });
      return;
    }

    this.isLoading = true;
    this.expedienteService.getById(bandeja.expedienteID)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: expediente => {
          this.isLoading = false;

          if (!this.ESTADOS_SALIDA_PERMITIDOS.includes(expediente.estadoActual)) {
            Swal.fire({
              icon: 'error',
              title: 'Estado Inválido para Salida',
              html: `<div class="text-left text-sm">
                <p>Estado actual: <strong class="text-red-600">${expediente.estadoActual}</strong></p>
                <p class="text-gray-500 mt-2">Requerido: Pendiente Retiro</p>
              </div>`,
              confirmButtonColor: '#EF4444'
            });
            return;
          }

          expediente.codigoBandeja = bandeja.codigo;
          this.expedienteParaSalida = expediente;
          this.mostrarModalSalida = true;
        },
        error: err => {
          this.isLoading = false;
          this.manejarError('Error al cargar expediente', err);
        }
      });
  }

  onSalidaCompletada(response: any): void {
    this.cerrarModalSalida();
    this.cargarMapa();
    this.toast.fire({ icon: 'success', title: 'Salida Registrada', text: 'Bandeja liberada exitosamente' });
  }

  cerrarModalSalida(): void {
    this.mostrarModalSalida = false;
    this.expedienteParaSalida = null;
  }

  // ===================================================================
  // MODAL MANTENIMIENTO
  // ===================================================================

  abrirModalMantenimiento(bandeja: BandejaDTO): void {
    if (!this.esDisponible(bandeja)) {
      this.toast.fire({
        icon: 'warning',
        title: 'Solo se puede poner en mantenimiento bandejas disponibles'
      });
      return;
    }

    this.bandejaParaMantenimiento = bandeja;
    this.formMantenimiento.reset({
      motivo: '',
      detalle: '',
      fechaInicio: this.formatDateTimeLocal(new Date()),
      fechaEstimadaFin: '',
      responsableExterno: ''
    });
    this.mostrarModalMantenimiento = true;
  }

  cerrarModalMantenimiento(): void {
    this.mostrarModalMantenimiento = false;
    this.bandejaParaMantenimiento = null;
    this.isLoadingMantenimiento = false;
  }

  confirmarMantenimiento(): void {
    if (this.isLoadingMantenimiento) return;
    this.formMantenimiento.markAllAsTouched();
    if (this.formMantenimiento.invalid || !this.bandejaParaMantenimiento) return;

    this.isLoadingMantenimiento = true;
    const raw = this.formMantenimiento.value;

    const dto = {
      motivo: raw.motivo,
      detalle: raw.detalle || null,
      fechaInicio: raw.fechaInicio
        ? new Date(raw.fechaInicio).toISOString() : null,
      fechaEstimadaFin: raw.fechaEstimadaFin
        ? new Date(raw.fechaEstimadaFin).toISOString() : null,
      responsableExterno: raw.responsableExterno || null
    };

    this.bandejaService.marcarMantenimiento(
      this.bandejaParaMantenimiento.bandejaID, dto
    ).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isLoadingMantenimiento = false;
          this.cerrarModalMantenimiento();
          this.cargarMapa();
          this.toast.fire({
            icon: 'success',
            title: `${this.bandejaParaMantenimiento?.codigo ?? 'Bandeja'} en mantenimiento`
          });
        },
        error: err => {
          this.isLoadingMantenimiento = false;
          this.manejarError('Error al iniciar mantenimiento', err);
        }
      });
  }

  // ===================================================================
  // FINALIZAR MANTENIMIENTO
  // ===================================================================

  finalizarMantenimiento(bandeja: BandejaDTO): void {
    if (!this.esMantenimiento(bandeja)) {
      this.toast.fire({ icon: 'warning', title: 'Solo se puede finalizar bandejas en mantenimiento' });
      return;
    }

    Swal.fire({
      title: `Finalizar mantenimiento de ${bandeja.codigo}`,
      text: 'La bandeja volverá a estar disponible',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10B981',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, finalizar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then(result => {
      if (!result.isConfirmed) return;

      this.bandejaService.finalizarMantenimiento(bandeja.bandejaID)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.cargarMapa();
            this.toast.fire({ icon: 'success', title: `${bandeja.codigo} disponible` });
          },
          error: err => this.manejarError('Error al finalizar mantenimiento', err)
        });
    });
  }


  /*@TODO V2 — IMPLEMENTAR CUANDO SE TENGAN CASUISTICAS CONCRETAS DE FALLOS EN SALIDA O ALGUN PROBLEMA CON BANDEJA.
  // ===================================================================
  // SALIDA FORZADA ADMIN (solo Admin/JG/VigSup)

  // ===================================================================

  liberarBandejaManual(bandeja: BandejaDTO): void {
    Swal.fire({
      title: `Liberar ${bandeja.codigo} manualmente`,
      html: `
        <p class="text-sm text-gray-600 mb-4">
          Acción de emergencia — quedará registrada en auditoría.
        </p>
        <p class="text-sm font-semibold text-gray-700 mb-3">
          Paciente: ${bandeja.nombrePaciente || 'N/A'}
        </p>
        <select id="motivo-select" class="swal2-input text-sm">
          <option value="">Seleccionar motivo...</option>
          <option value="Error en sistema de registro">Error en sistema de registro</option>
          <option value="Salida sin completar trámites">Salida sin completar trámites</option>
          <option value="Corrección de asignación incorrecta">Corrección de asignación incorrecta</option>
          <option value="Emergencia o evacuación">Emergencia o evacuación</option>
          <option value="Otro">Otro (especificar en observaciones)</option>
        </select>
        <textarea id="obs-textarea" class="swal2-textarea text-sm"
                  placeholder="Observaciones detalladas (mínimo 20 caracteres)" rows="3"></textarea>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, liberar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true,
      preConfirm: () => {
        const motivo = (document.getElementById('motivo-select') as HTMLSelectElement).value;
        const obs = (document.getElementById('obs-textarea') as HTMLTextAreaElement).value;
        if (!motivo) { Swal.showValidationMessage('Selecciona un motivo'); return false; }
        if (!obs || obs.trim().length < 20) {
          Swal.showValidationMessage('Las observaciones deben tener al menos 20 caracteres');
          return false;
        }
        return { motivo, observaciones: obs };
      }
    }).then(result => {
      if (!result.isConfirmed || !result.value) return;

      this.bandejaService.liberarManualmente(
        bandeja.bandejaID,
        result.value.motivo,
        result.value.observaciones
      ).pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.cargarMapa();
            this.toast.fire({ icon: 'success', title: `${bandeja.codigo} liberada` });
          },
          error: err => this.manejarError('Error al liberar bandeja', err)
        });
    });
  }
  */

  // ===================================================================
  // MODO ASIGNACIÓN
  // ===================================================================

  seleccionarBandeja(bandeja: BandejaDTO): void {
    if (this.modoAsignacion) {
      if (this.esDisponible(bandeja)) {
        this.confirmarAsignacion(bandeja);
      } else {
        this.toast.fire({ icon: 'warning', title: 'Elija una bandeja verde (disponible)' });
      }
      return;
    }
    if (this.esOcupada(bandeja)) this.verDetalleBandeja(bandeja);
  }

  private confirmarAsignacion(bandeja: BandejaDTO): void {
    if (!this.expedienteIdParaAsignar) return;

    Swal.fire({
      title: 'Confirmar Asignación',
      html: `<div class="text-left text-sm">
        <p><strong>Paciente:</strong> ${this.pacienteNombre}</p>
        <p><strong>Expediente:</strong> ${this.pacienteCodigo}</p>
        <p class="mt-2">Bandeja: <span class="text-green-600 font-bold text-xl">${bandeja.codigo}</span></p>
      </div>`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10B981',
      confirmButtonText: 'Sí, asignar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then(result => {
      if (result.isConfirmed) this.ejecutarAsignacion(bandeja.bandejaID);
    });
  }

  private ejecutarAsignacion(bandejaId: number): void {
    this.bandejaService.asignar({
      bandejaID: bandejaId,
      expedienteID: this.expedienteIdParaAsignar!,
      observaciones: 'Asignación desde Mapa Mortuorio'
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          Swal.fire({
            icon: 'success',
            title: 'Asignado Correctamente',
            text: `${this.pacienteNombre} asignado a la bandeja`,
            confirmButtonColor: '#10B981'
          }).then(() => this.router.navigate(['/mis-tareas']));
        },
        error: err => this.manejarError('Error al asignar la bandeja', err)
      });
  }

  cancelarAsignacion(): void {
    this.router.navigate(['/mis-tareas']);
  }

  // ===================================================================
  // VER DETALLE (placeholder — A2)
  // ===================================================================

  verDetalleBandeja(bandeja: BandejaDTO): void {
    this.bandejaSeleccionada = bandeja;
    // TODO: implementar modal Ver Detalle (A2)
  }

  onVerDetalle(bandeja: BandejaDTO, event: Event): void {
    event.stopPropagation();
    this.verDetalleBandeja(bandeja);
  }

  // ===================================================================
  // HELPER — manejarError central
  // ===================================================================

  private manejarError(titulo: string, error: any): void {
    console.error(`[MapaMortuorio] ${titulo}:`, error);
    Swal.fire({
      icon: 'error',
      title: titulo,
      text: error?.error?.message || error?.message || 'Ocurrió un error inesperado',
      confirmButtonColor: '#EF4444'
    });
  }

  // ===================================================================
  // HELPER — formateo hora local Lima GMT-5
  // ===================================================================

  private formatDateTimeLocal(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
