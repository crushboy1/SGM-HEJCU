import {
  Component, EventEmitter, Input, Output,
  OnInit, OnDestroy, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import Swal from 'sweetalert2';

import { IconComponent } from '../icon/icon.component';
import { SemaforoDeudas } from '../semaforo-deudas/semaforo-deudas';
import { SoloNumerosDirective } from '../../shared/directives/solo-numeros';
import { SalidaService, RegistrarSalidaRequest } from '../../services/salida';
import { ActaRetiroService, ActaRetiroDTO } from '../../services/acta-retiro';
import { DeudaEconomica } from '../../services/deuda-economica';
import { DeudaSangre } from '../../services/deuda-sangre';
import { DeudaEconomicaSemaforoDTO } from '../../models/deuda-economica.model';
import { getBadgeClasses, getEstadoIcon, getEstadoLabel } from '../../utils/badge-styles';

/**
 * Modal reutilizable para registro de salida del mortuorio.
 *
 * FLUJO 1 — Desde Mapa Mortuorio (90% de casos):
 * Bandeja ocupada → click "Salida" → este modal → confirmar entrega → emite onSalidaRegistrada
 *
 * FLUJO 2 — Desde Búsqueda Manual (10% excepcional):
 * Búsqueda HC/DNI/Nombre → seleccionar expediente → este modal → confirmar entrega
 *
 * DECISIÓN DE DISEÑO:
 * - ActaRetiro ya fue firmado por 3 actores — sus datos son READONLY
 * - CASO AutoridadLegal: todos los datos readonly + placa y observaciones editables
 * - CASO Familiar: datos familiar readonly + funeraria/placa/observaciones editables
 * - Bypass de deuda autorizado por JG/Admin permite continuar sin bloqueo de semáforo
 *
 * @version 3.2.0
 * @changelog
 * - v3.2.0: destroy$ + OnDestroy + takeUntil — fix memory leak en forkJoin y subscribe.
 *           isLoading guard en confirmarSalida() — evita doble submit.
 *           SoloNumerosDirective agregado a imports[].
 * - v3.1.0: actaRetiroID agregado al form. catchError en forkJoin.
 *           hayDeudasBloqueantes considera bypassDeudaAutorizado.
 * - v3.0.0: Formulario inteligente por tipo de salida.
 */
@Component({
  selector: 'app-formulario-salida',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, SemaforoDeudas, SoloNumerosDirective],
  templateUrl: './formulario-salida.html',
  styleUrl: './formulario-salida.css'
})
export class FormularioSalida implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private salidaService = inject(SalidaService);
  private deudaEconomicaService = inject(DeudaEconomica);
  private deudaSangreService = inject(DeudaSangre);
  private actaRetiroService = inject(ActaRetiroService);

  private destroy$ = new Subject<void>();

  // ── Inputs / Outputs ─────────────────────────────────────────────
  @Input() expediente: any = null;
  @Output() onSalidaRegistrada = new EventEmitter<any>();
  @Output() onCerrar = new EventEmitter<void>();

  // ── Estado ───────────────────────────────────────────────────────
  isLoading = false;
  cargandoSemaforos = false;
  cargandoActa = false;

  // ── Semáforos de deudas ──────────────────────────────────────────
  semaforoEconomica?: DeudaEconomicaSemaforoDTO;
  semaforoSangre?: string;

  // ── Acta de Retiro (fuente de verdad — readonly post-firma) ──────
  actaRetiro?: ActaRetiroDTO | null;

  // ── Helpers visuales ─────────────────────────────────────────────
  getBadgeClasses = getBadgeClasses;
  getEstadoIcon = getEstadoIcon;
  getEstadoLabel = getEstadoLabel;

  // ── Modelo del formulario ─────────────────────────────────────────
  // Campos capturados físicamente por el Vigilante Mortuorio.
  // - Familiar:       funeraria + placa + observaciones
  // - AutoridadLegal: placa patrullero + observaciones
  form: RegistrarSalidaRequest = {
    expedienteID: 0,
    actaRetiroID: 0,
    expedienteLegalID: undefined,
    nombreFuneraria: '',
    funerariaRUC: '',
    funerariaTelefono: '',
    conductorFuneraria: '',
    dniConductor: '',
    ayudanteFuneraria: '',
    dniAyudante: '',
    placaVehiculo: '',
    destino: '',
    observaciones: ''
  };

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    if (!this.expediente) {
      console.error('[FormularioSalida] Modal abierto sin expediente');
      Swal.fire({
        icon: 'error',
        title: 'Error de Configuración',
        text: 'No se proporcionó expediente al modal',
        confirmButtonColor: '#EF4444'
      }).then(() => this.cerrarModal());
      return;
    }

    this.form.expedienteID = this.expediente.expedienteID;
    this.cargarDatosIniciales();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // CARGA INICIAL — forkJoin paralelo con catchError en acta
  // ===================================================================

  private cargarDatosIniciales(): void {
    this.cargandoSemaforos = true;
    this.cargandoActa = true;

    forkJoin({
      economica: this.deudaEconomicaService.obtenerSemaforo(
        this.expediente.expedienteID
      ),
      sangre: this.deudaSangreService.obtenerSemaforo(
        this.expediente.expedienteID
      ),
      // catchError: si no existe acta, retorna null sin romper el forkJoin
      acta: this.actaRetiroService
        .obtenerPorExpediente(this.expediente.expedienteID)
        .pipe(catchError(() => of(null)))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ economica, sangre, acta }) => {
          this.semaforoEconomica = economica;
          this.semaforoSangre = sangre;
          this.actaRetiro = acta;
          this.cargandoSemaforos = false;
          this.cargandoActa = false;
          this.prellenarDesdeActa();
        },
        error: err => {
          console.error('[FormularioSalida] Error al cargar datos iniciales:', err);
          this.cargandoSemaforos = false;
          this.cargandoActa = false;
          Swal.fire({
            icon: 'warning',
            title: 'Advertencia',
            text: 'No se pudieron cargar los datos del acta o deudas. Contacte al administrador.',
            confirmButtonColor: '#F59E0B'
          });
        }
      });
  }

  // ===================================================================
  // PRE-LLENADO DESDE ACTA DE RETIRO
  // ===================================================================

  private prellenarDesdeActa(): void {
    if (!this.actaRetiro) return;
    this.form.actaRetiroID = this.actaRetiro.actaRetiroID;
    this.form.destino = this.actaRetiro.destino ?? '';
  }

  // ===================================================================
  // GETTERS — TIPO DE SALIDA
  // ===================================================================

  get tipoSalidaActa(): string {
    return this.actaRetiro?.tipoSalida ?? 'Familiar';
  }
  get esFamiliar(): boolean {
    return this.tipoSalidaActa === 'Familiar';
  }
  get esAutoridadLegal(): boolean {
    return this.tipoSalidaActa === 'AutoridadLegal';
  }
  get tipoSalidaLabel(): string {
    const etiquetas: Record<string, string> = {
      'Familiar': 'Entrega a Familiar',
      'AutoridadLegal': 'Autoridad Legal (Fiscalía / PNP)',
      'TrasladoHospital': 'Traslado a otro Hospital',
      'Otro': 'Otro'
    };
    return etiquetas[this.tipoSalidaActa] ?? this.tipoSalidaActa;
  }

  // ===================================================================
  // GETTERS — DATOS READONLY DEL ACTA
  // ===================================================================

  get datosResponsableReadonly() {
    const acta = this.actaRetiro;
    if (!acta) return null;

    if (this.esFamiliar) {
      return {
        nombre: acta.familiarNombreCompleto ?? '—',
        tipoDoc: acta.familiarTipoDocumento ?? '—',
        nroDoc: acta.familiarNumeroDocumento ?? '—',
        parentesco: acta.familiarParentesco ?? '—',
        telefono: acta.familiarTelefono ?? '—',
        cargo: null,
        institucion: null,
        nroOficio: null
      };
    }

    return {
      nombre: acta.autoridadNombreCompleto ?? '—',
      tipoDoc: acta.autoridadTipoDocumento ?? '—',
      nroDoc: acta.autoridadNumeroDocumento ?? '—',
      cargo: acta.autoridadCargo ?? '—',
      institucion: acta.autoridadInstitucion ?? '—',
      telefono: acta.autoridadTelefono ?? '—',
      nroOficio: acta.numeroOficioPolicial ?? '—',
      parentesco: null
    };
  }

  // ===================================================================
  // GETTER — DEUDAS BLOQUEANTES
  // Considera bypass autorizado: si JG/Admin autorizó, no bloquea.
  // ===================================================================

  get hayDeudasBloqueantes(): boolean {
    if (this.actaRetiro?.bypassDeudaAutorizado) return false;
    return (this.semaforoEconomica?.tieneDeuda ?? false) ||
      (this.semaforoSangre?.includes('PENDIENTE') ?? false);
  }

  // ===================================================================
  // GETTERS — AUTORIZACIÓN
  // ===================================================================

  get actaEstaCompleta(): boolean {
    return this.actaRetiro?.estaCompleta ?? false;
  }
  get actaTienePDFFirmado(): boolean {
    return this.actaRetiro?.tienePDFFirmado ?? false;
  }

  // ===================================================================
  // HELPER — FORMULARIO TIENE DATOS EDITABLES
  // ===================================================================

  private formularioTieneDatos(): boolean {
    if (this.esFamiliar) {
      return !!(
        this.form.nombreFuneraria?.trim() ||
        this.form.conductorFuneraria?.trim() ||
        this.form.placaVehiculo?.trim() ||
        this.form.observaciones?.trim()
      );
    }
    return !!(
      this.form.placaVehiculo?.trim() ||
      this.form.observaciones?.trim()
    );
  }

  // ===================================================================
  // FLUJO DE CONFIRMACIÓN
  // ===================================================================

  confirmarSalida(): void {
    // Guard: evita doble submit mientras procesa
    if (this.isLoading) return;
    if (!this.verificarSemaforos()) return;
    if (!this.validarFormulario()) return;
    this.mostrarConfirmacion();
  }

  private verificarSemaforos(): boolean {
    if (!this.hayDeudasBloqueantes) return true;

    const filas: string[] = [];

    if (this.semaforoEconomica?.tieneDeuda) {
      filas.push(`
        <div class="flex items-start gap-3 p-3 bg-red-50 border border-red-200 rounded-lg">
          <div class="flex-shrink-0 text-red-500 mt-0.5">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" x2="12" y1="8" y2="12"/>
              <line x1="12" x2="12.01" y1="16" y2="16"/>
            </svg>
          </div>
          <div class="text-left">
            <p class="font-bold text-red-700 text-sm">Deuda Económica Pendiente</p>
            <p class="text-red-600 text-xs mt-0.5">
              ${this.semaforoEconomica.instruccion ?? ''}
            </p>
          </div>
        </div>`);
    }

    if (this.semaforoSangre?.includes('PENDIENTE')) {
      filas.push(`
        <div class="flex items-start gap-3 p-3 bg-red-50 border border-red-200 rounded-lg">
          <div class="flex-shrink-0 text-red-500 mt-0.5">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
            </svg>
          </div>
          <div class="text-left">
            <p class="font-bold text-red-700 text-sm">Deuda de Sangre Pendiente</p>
            <p class="text-red-600 text-xs mt-0.5">${this.semaforoSangre}</p>
          </div>
        </div>`);
    }

    Swal.fire({
      icon: 'error',
      title: 'No se Puede Confirmar Entrega',
      html: `
        <div class="space-y-3 text-left mb-3">
          <div class="pb-3 border-b border-gray-100">
            <p class="font-bold text-gray-800 text-sm">
              ${this.expediente.nombreCompleto}
            </p>
            <p class="text-gray-500 text-xs">${this.expediente.codigoExpediente}</p>
          </div>
          <div class="space-y-2">${filas.join('')}</div>
          <p class="text-xs text-gray-500 pt-2">
            Regularice las deudas antes de autorizar la salida.
          </p>
        </div>`,
      confirmButtonText: 'Entendido',
      confirmButtonColor: '#DC2626'
    });

    return false;
  }

  private validarFormulario(): boolean {
    const error = (texto: string) => {
      Swal.fire({
        icon: 'warning',
        title: 'Datos Incompletos',
        text: texto,
        confirmButtonColor: '#F59E0B'
      });
      return false;
    };

    if (!this.actaRetiro)
      return error(
        'El acta de retiro no está disponible. Espere o recargue.'
      );

    if (!this.form.actaRetiroID || this.form.actaRetiroID === 0)
      return error(
        'No se pudo obtener el ID del acta. Cierre y vuelva a abrir el formulario.'
      );

    if (this.esFamiliar && this.form.nombreFuneraria?.trim()) {
      if (!this.form.conductorFuneraria?.trim())
        return error('Si registra funeraria, ingrese el nombre del conductor.');
      if (!this.form.placaVehiculo?.trim())
        return error('Si registra funeraria, ingrese la placa del vehículo.');
    }

    return true;
  }

  private mostrarConfirmacion(): void {
    const datos = this.datosResponsableReadonly;

    Swal.fire({
      title: 'Confirmar Entrega',
      html: `
        <div class="text-left space-y-3 text-sm">
          <div class="bg-blue-50 border border-blue-200 p-3 rounded-lg space-y-1">
            <p class="font-bold text-gray-800">
              ${this.expediente.nombreCompleto}
            </p>
            <p class="text-gray-500 text-xs">
              ${this.expediente.codigoExpediente}
              · Bandeja: ${this.expediente.codigoBandeja ?? 'N/A'}
            </p>
          </div>
          <div class="space-y-1 py-1">
            <p>
              <span class="text-gray-500">Tipo salida:</span>
              <strong>${this.tipoSalidaLabel}</strong>
            </p>
            <p>
              <span class="text-gray-500">Responsable:</span>
              <strong>${datos?.nombre ?? '—'}</strong>
            </p>
            <p>
              <span class="text-gray-500">Documento:</span>
              <strong>${datos?.tipoDoc ?? ''} ${datos?.nroDoc ?? ''}</strong>
            </p>
            ${this.esFamiliar && datos?.parentesco
          ? `<p><span class="text-gray-500">Parentesco:</span>
                 <strong>${datos.parentesco}</strong></p>` : ''}
            ${this.esAutoridadLegal && datos?.institucion
          ? `<p><span class="text-gray-500">Institución:</span>
                 <strong>${datos.institucion}</strong></p>` : ''}
            ${this.form.placaVehiculo?.trim()
          ? `<p><span class="text-gray-500">Placa:</span>
                 <strong class="uppercase">${this.form.placaVehiculo}</strong></p>` : ''}
            ${this.form.nombreFuneraria?.trim()
          ? `<p><span class="text-gray-500">Funeraria:</span>
                 <strong>${this.form.nombreFuneraria}</strong></p>` : ''}
          </div>
          <div class="bg-red-50 border border-red-200 p-3 rounded-lg">
            <p class="text-red-700 font-bold text-xs uppercase tracking-wide">
              Acción irreversible
            </p>
            <p class="text-red-600 text-xs mt-1">
              La bandeja quedará libre y el expediente se cerrará con estado Retirado.
            </p>
          </div>
        </div>`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#16A34A',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, Confirmar Entrega',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then(result => {
      if (result.isConfirmed) this.procesarSalida();
    });
  }

  // ===================================================================
  // PROCESAMIENTO
  // ===================================================================

  private procesarSalida(): void {
    this.isLoading = true;

    this.salidaService.registrarSalida(this.form)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.isLoading = false;
          Swal.fire({
            icon: 'success',
            title: 'Entrega Confirmada',
            html: `
              <div class="text-left space-y-2 text-sm">
                <p class="text-green-600 font-semibold">
                  El cuerpo ha sido retirado del mortuorio correctamente.
                </p>
                <div class="bg-green-50 border border-green-200 p-3 rounded-lg
                            mt-2 space-y-1">
                  <p>Expediente cerrado:
                    <strong>${this.expediente.codigoExpediente}</strong>
                  </p>
                  <p>Bandeja liberada:
                    <strong>${this.expediente.codigoBandeja ?? 'N/A'}</strong>
                  </p>
                  <p>Responsable:
                    <strong>${this.datosResponsableReadonly?.nombre ?? '—'}</strong>
                    ${this.esFamiliar && this.datosResponsableReadonly?.parentesco
                ? `· <span class="text-gray-500">
                           ${this.datosResponsableReadonly.parentesco}
                         </span>`
                : this.esAutoridadLegal && this.datosResponsableReadonly?.cargo
                  ? `· <span class="text-gray-500">
                             ${this.datosResponsableReadonly.cargo}
                           </span>`
                  : ''}
                  </p>
                </div>
              </div>`,
            confirmButtonColor: '#16A34A',
            confirmButtonText: 'Aceptar'
          }).then(() => this.onSalidaRegistrada.emit(response));
        },
        error: (err: any) => {
          this.isLoading = false;
          console.error('[FormularioSalida] Error al procesar:', err);
          Swal.fire({
            icon: 'error',
            title: 'Error al Confirmar Entrega',
            text: err.error?.mensaje ?? err.error?.message
              ?? err.message ?? 'No se pudo procesar. Intente nuevamente.',
            confirmButtonColor: '#EF4444'
          });
        }
      });
  }

  // ===================================================================
  // CIERRE DEL MODAL
  // ===================================================================

  cerrarModal(): void {
    if (this.formularioTieneDatos()) {
      Swal.fire({
        title: 'Cerrar Formulario',
        text: 'Se perderán los datos ingresados.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280',
        confirmButtonText: 'Sí, Cerrar',
        cancelButtonText: 'Continuar',
        reverseButtons: true
      }).then(result => {
        if (result.isConfirmed) this.onCerrar.emit();
      });
    } else {
      this.onCerrar.emit();
    }
  }
}
