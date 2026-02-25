import { Component, EventEmitter, Input, Output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import Swal from 'sweetalert2';

import { IconComponent } from '../icon/icon.component';
import { SemaforoDeudas } from '../semaforo-deudas/semaforo-deudas';
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
 * Búsqueda HC/DNI/Nombre → seleccionar expediente → este modal → confirmar entrega → emite onSalidaRegistrada
 *
 * DECISIÓN DE DISEÑO (v3.0):
 * ─────────────────────────────────────────────────────────────────
 * El ActaRetiro ya fue firmado por 3 actores (Responsable, Admisionista,
 * Supervisor Vigilancia). Editar esos datos post-firma rompe la integridad
 * del documento físico firmado.
 *
 * Por lo tanto:
 *
 * CASO AutoridadLegal:
 *   - Todos los datos son READONLY (vienen del ActaRetiro firmado)
 *   - Solo "Observaciones" es editable
 *   - Funeraria NO aplica
 *
 * CASO Familiar:
 *   - Datos del familiar son READONLY (vienen del ActaRetiro firmado)
 *   - Sección Funeraria es EDITABLE (no existe en ActaRetiro, llega el día del retiro)
 *   - "Observaciones" es editable
 *
 * Si hay discrepancia con el papel:
 *   Vigilante documenta en Observaciones → Admisión corrige → nueva acta si aplica.
 *
 * @version 3.0.0
 * @changelog
 * - v3.0.0: Formulario inteligente por tipo de salida.
 *           Datos del ActaRetiro en modo READONLY post-firma.
 *           AutoridadLegal: solo observaciones editables.
 *           Familiar: solo funeraria + observaciones editables.
 *           Pre-llenado completo desde ActaRetiro (placa, teléfono, institución).
 *           Eliminados campos innecesarios según tipo.
 * - v2.0.0: TipoSalida pasa a solo lectura. forkJoin para carga paralela.
 * - v1.1.0: Integración semáforo de deudas.
 * - v1.0.0: Versión inicial.
 */
@Component({
  selector: 'app-formulario-salida',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, SemaforoDeudas],
  templateUrl: './formulario-salida.html',
  styleUrl: './formulario-salida.css'
})
export class FormularioSalida implements OnInit {
  private salidaService = inject(SalidaService);
  private deudaEconomicaService = inject(DeudaEconomica);
  private deudaSangreService = inject(DeudaSangre);
  private actaRetiroService = inject(ActaRetiroService);

  /* Expediente pre-cargado desde componente padre. */
  @Input() expediente: any = null;

  /* Emitido cuando la salida se confirma exitosamente. */
  @Output() onSalidaRegistrada = new EventEmitter<any>();

  /* Emitido cuando el usuario cierra el modal sin confirmar. */
  @Output() onCerrar = new EventEmitter<void>();

  isLoading = false;

  // ===================================================================
  // ESTADO DE CARGA
  // ===================================================================
  cargandoSemaforos = false;
  cargandoActa = false;

  // ===================================================================
  // SEMÁFOROS DE DEUDAS
  // ===================================================================
  semaforoEconomica?: DeudaEconomicaSemaforoDTO;
  semaforoSangre?: string;

  // ===================================================================
  // ACTA DE RETIRO (fuente de verdad — readonly post-firma)
  // ===================================================================
  actaRetiro?: ActaRetiroDTO;

  // ===================================================================
  // HELPERS VISUALES (para template)
  // ===================================================================
  getBadgeClasses = getBadgeClasses;
  getEstadoIcon = getEstadoIcon;
  getEstadoLabel = getEstadoLabel;

  // ===================================================================
  // MODELO DEL FORMULARIO
  // Solo campos capturados físicamente por el Vigilante.
  // TipoSalida, responsable y destino los resuelve el backend
  // desde ActaRetiro (relación 1-1 con ExpedienteID).
  // - Familiar:        funeraria + observaciones
  // - AutoridadLegal:  solo observaciones
  // ===================================================================
  form: RegistrarSalidaRequest = {
    expedienteID: 0,
    expedienteLegalID: undefined,

    // Funeraria — editable solo en caso Familiar
    nombreFuneraria: '',
    funerariaRUC: '',
    funerariaTelefono: '',
    conductorFuneraria: '',
    dniConductor: '',
    ayudanteFuneraria: '',
    dniAyudante: '',

    // Vehículo y destino
    placaVehiculo: '',
    destino: '',

    // Observaciones — siempre editable
    observaciones: ''
  };

  // ===================================================================
  // CICLO DE VIDA
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

    console.log('[FormularioSalida] Inicializado para:', this.expediente.codigoExpediente);
  }

  // ===================================================================
  // CARGA INICIAL — forkJoin paralelo
  // ===================================================================

  /**
   * Carga en paralelo: ActaRetiro + semáforos de deudas.
   * Una vez completado, pre-llena el formulario desde ActaRetiro.
   */
  private cargarDatosIniciales(): void {
    this.cargandoSemaforos = true;
    this.cargandoActa = true;

    forkJoin({
      economica: this.deudaEconomicaService.obtenerSemaforo(this.expediente.expedienteID),
      sangre: this.deudaSangreService.obtenerSemaforo(this.expediente.expedienteID),
      acta: this.actaRetiroService.obtenerPorExpediente(this.expediente.expedienteID)
    }).subscribe({
      next: ({ economica, sangre, acta }) => {
        this.semaforoEconomica = economica;
        this.semaforoSangre = sangre;
        this.actaRetiro = acta;
        this.cargandoSemaforos = false;
        this.cargandoActa = false;

        // Pre-llenar form desde ActaRetiro real
        this.prellenarDesdeActa();

        console.log('[FormularioSalida] Datos cargados — TipoSalida:', acta.tipoSalida);
      },
      error: (err) => {
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

  /**
   * Pre-llena el formulario con datos del ActaRetiro.
   * Estos datos NO son editables en la UI — el ActaRetiro ya fue firmado.
   * Se envían al backend para registrar la salida con coherencia.
   */
  private prellenarDesdeActa(): void {
    this.form.destino = this.actaRetiro?.destino ?? '';
    console.log('[FormularioSalida] Pre-llenado desde ActaRetiro — TipoSalida:',
      this.actaRetiro?.tipoSalida);
  }

  // ===================================================================
  // GETTERS — TIPO DE SALIDA
  // ===================================================================

  /** TipoSalida leído del ActaRetiro. Fallback 'Familiar' mientras carga. */
  get tipoSalidaActa(): string {
    return this.actaRetiro?.tipoSalida ?? 'Familiar';
  }

  /** True si es retiro por familiar. */
  get esFamiliar(): boolean {
    return this.tipoSalidaActa === 'Familiar';
  }

  /** True si es retiro por autoridad legal. */
  get esAutoridadLegal(): boolean {
    return this.tipoSalidaActa === 'AutoridadLegal';
  }

  /** Etiqueta legible del tipo de salida. */
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
  // GETTERS — DATOS READONLY DEL ACTA (para template)
  // ===================================================================

  /**
   * Datos del responsable para mostrar en modo readonly.
   * Familiar → datos del familiar. AutoridadLegal → datos de la autoridad.
   */
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
        // No aplica para familiar
        cargo: null,
        institucion: null,
        placa: null,
        nroOficio: null
      };
    }

    return {
      nombre: acta.autoridadNombreCompleto ?? '—',
      tipoDoc: acta.autoridadTipoDocumento ?? '—',
      nroDoc: acta.autoridadNumeroDocumento ?? '—',
      cargo: acta.autoridadCargo ?? '—',
      institucion: acta.autoridadInstitucion ?? '—',
      placa: acta.autoridadPlacaVehiculo ?? '—',
      telefono: acta.autoridadTelefono ?? '—',
      nroOficio: acta.numeroOficioLegal ?? '—',
      // No aplica para autoridad
      parentesco: null
    };
  }

  // ===================================================================
  // GETTER — DEUDAS BLOQUEANTES
  // ===================================================================

  get hayDeudasBloqueantes(): boolean {
    return (this.semaforoEconomica?.tieneDeuda ?? false) ||
      (this.semaforoSangre?.includes('PENDIENTE') ?? false);
  }

  // ===================================================================
  // GETTER — FORMULARIO TIENE DATOS EDITABLES
  // ===================================================================

  /** Solo considera campos que el vigilante puede editar. */
  private formularioTieneDatos(): boolean {
    if (this.esFamiliar) {
      return !!(
        this.form.nombreFuneraria?.trim() ||
        this.form.conductorFuneraria?.trim() ||
        this.form.observaciones?.trim()
      );
    }
    // AutoridadLegal — solo observaciones
    return !!this.form.observaciones?.trim();
  }

  // ===================================================================
  // FLUJO DE CONFIRMACIÓN
  // ===================================================================

  /**
   * Punto de entrada del botón "Confirmar Entrega".
   * Verifica semáforos → valida → muestra confirmación → procesa.
   */
  confirmarSalida(): void {
    if (!this.verificarSemaforos()) return;
    if (!this.validarFormulario()) return;
    this.mostrarConfirmacion();
  }

  /**
   * Verifica semáforos. Muestra detalle si hay bloqueo.
   * @returns true si puede continuar
   */
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

  /**
   * Valida solo los campos editables según tipo de salida.
   * Los campos readonly vienen del ActaRetiro — ya validados por Admisión.
   */
  private validarFormulario(): boolean {
    const error = (texto: string) => Swal.fire({
      icon: 'warning', title: 'Datos Incompletos',
      text: texto, confirmButtonColor: '#F59E0B'
    });

    if (!this.actaRetiro)
      return error('El acta de retiro no está disponible. Espere o recargue.'), false;

    // Familiar con funeraria — conductor y placa obligatorios
    if (this.esFamiliar && this.form.nombreFuneraria?.trim()) {
      if (!this.form.conductorFuneraria?.trim())
        return error('Si registra funeraria, ingrese el nombre del conductor.'), false;
      if (!this.form.placaVehiculo?.trim())
        return error('Si registra funeraria, ingrese la placa del vehículo.'), false;
    }

    return true;
  }

  /**
   * Muestra confirmación final antes de procesar.
   */
  private mostrarConfirmacion(): void {
    const datos = this.datosResponsableReadonly;

    Swal.fire({
      title: 'Confirmar Entrega',
      html: `
        <div class="text-left space-y-3 text-sm">
          <div class="bg-blue-50 border border-blue-200 p-3 rounded-lg space-y-1">
            <p class="font-bold text-gray-800">${this.expediente.nombreCompleto}</p>
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
      cancelButtonText: 'Revisar',
      reverseButtons: true
    }).then((result) => {
      if (result.isConfirmed) this.procesarSalida();
    });
  }

  // ===================================================================
  // PROCESAMIENTO
  // ===================================================================

  private procesarSalida(): void {
    this.isLoading = true;

    this.salidaService.registrarSalida(this.form).subscribe({
      next: (response) => {
        this.isLoading = false;

        Swal.fire({
          icon: 'success',
          title: 'Entrega Confirmada',
          html: `
          <div class="text-left space-y-2 text-sm">
            <p class="text-green-600 font-semibold">
              El cuerpo ha sido retirado del mortuorio correctamente.
            </p>
            <div class="bg-green-50 border border-green-200 p-3 rounded-lg mt-2 space-y-1">
              <p>Expediente cerrado:
                <strong>${this.expediente.codigoExpediente}</strong>
              </p>
              <p>Bandeja liberada:
                <strong>${this.expediente.codigoBandeja ?? 'N/A'}</strong>
              </p>
              <p>Responsable:
                <strong>${this.datosResponsableReadonly?.nombre ?? '—'}</strong>
                ${this.esFamiliar && this.datosResponsableReadonly?.parentesco
                  ? `· <span class="text-gray-500">${this.datosResponsableReadonly.parentesco}</span>`
                  : this.esAutoridadLegal && this.datosResponsableReadonly?.cargo
                    ? `· <span class="text-gray-500">${this.datosResponsableReadonly.cargo}</span>`
                    : ''}
              </p>

            </div>
          </div>`,
          confirmButtonColor: '#16A34A',
          confirmButtonText: 'Aceptar'
        }).then(() => {
          this.onSalidaRegistrada.emit(response);
        });

        console.log('[FormularioSalida] Entrega confirmada:', response);
      },
      error: (err: any) => {
        this.isLoading = false;
        console.error('[FormularioSalida] Error al procesar:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Confirmar Entrega',
          text: err.error?.message ?? err.message
            ?? 'No se pudo procesar. Intente nuevamente.',
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
      }).then((result) => {
        if (result.isConfirmed) this.onCerrar.emit();
      });
    } else {
      this.onCerrar.emit();
    }
  }
}
