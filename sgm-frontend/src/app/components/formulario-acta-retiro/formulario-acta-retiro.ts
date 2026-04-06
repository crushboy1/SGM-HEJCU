import {
  Component, Input, Output, EventEmitter,
  OnInit, OnDestroy, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup, FormControl,
  Validators, AbstractControl, ValidationErrors, AsyncValidatorFn
} from '@angular/forms';
import { Subject, Observable, of, timer } from 'rxjs';
import { takeUntil, map, catchError, switchMap } from 'rxjs/operators';
import Swal from 'sweetalert2';

import { IconComponent } from '../icon/icon.component';
import { SoloNumerosDirective } from '../../shared/directives/solo-numeros';
import { ActaRetiroService } from '../../services/acta-retiro';
import { AuthService } from '../../services/auth';
import { Expediente } from '../../services/expediente';
import { CreateActaRetiroDTO, ActaRetiroDTO } from '../../services/acta-retiro';

// ===================================================================
// INTERFAZ — valores tipados del form (evita values: any en el DTO)
// ===================================================================
interface ActaRetiroFormValues {
  tipoSalida: 'Familiar' | 'AutoridadLegal';
  numeroCertificadoDefuncion: string;
  numeroOficioPolicial: string;
  nombreJefeGuardia: string;
  cmpJefeGuardia: string;
  tieneMedicoExterno: boolean;
  medicoExternoNombre: string;
  medicoExternoCMP: string;
  nombreFamiliar: string;
  tipoDocumentoFamiliar: number;
  dniFamiliar: string;
  parentesco: string;
  telefonoFamiliar: string;
  tipoAutoridad: number;
  tipoDocumentoAutoridad: number;
  nombreAutoridad: string;
  documentoAutoridad: string;
  cargoAutoridad: string;
  institucionAutoridad: string;
  telefonoAutoridad: string;
  destinoCuerpo: string;
  observaciones: string;
}

/**
 * Formulario para Crear Acta de Retiro (Familiar o AutoridadLegal).
 *
 * Arquitectura: ReactiveFormsModule + FormGroup + AsyncValidatorFn.
 * - Validaciones asíncronas nativas (SINADEF / oficio via timer debounce)
 * - Lógica condicional via valueChanges (enable/disable por rama)
 * - form.dirty reemplaza formHaCambiado() manual
 * - SoloNumerosDirective para campos numéricos
 * - Getters para toda la lógica del template (evita overhead en change detection)
 */
@Component({
  selector: 'app-formulario-acta-retiro',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent, SoloNumerosDirective],
  templateUrl: './formulario-acta-retiro.html',
  styleUrl: './formulario-acta-retiro.css'
})
export class FormularioActaRetiroComponent implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private fb = inject(FormBuilder);
  private actaRetiroService = inject(ActaRetiroService);
  private authService = inject(AuthService);
  private destroy$ = new Subject<void>();

  // ── Inputs / Outputs ─────────────────────────────────────────────
  @Input() expediente!: Expediente;
  @Output() actaCreada = new EventEmitter<ActaRetiroDTO>();
  @Output() onCancelar = new EventEmitter<void>();

  // ── Estado ───────────────────────────────────────────────────────
  form!: FormGroup;
  isSubmitting = false;

  // Campos readonly del expediente — fuera del FormGroup
  edadPaciente: number = 0;
  diagnosticoFinal: string = '';

  // ── Catálogos ────────────────────────────────────────────────────
  readonly tiposDocumento = [
    { value: 1, label: 'DNI' },
    { value: 2, label: 'Carnet de Extranjería' },
    { value: 3, label: 'Pasaporte' },
    { value: 4, label: 'Registro NN' }
  ];

  readonly tiposDocumentoFamiliar = this.tiposDocumento.filter(t => t.value !== 4);

  readonly tiposAutoridad = [
    { value: 1, label: 'Policía Nacional del Perú (PNP)' },
    { value: 2, label: 'Ministerio Público - Fiscalía' },
    { value: 3, label: 'Médico Legista' }
  ];

  private readonly placeholdersDocumento: Record<number, string> = {
    1: '12345678', 2: '001234567', 3: 'ABC123456'
  };

  /** Mapa tipo documento → número — constante de clase, no se recrea en cada llamada */
  private readonly mapaTipoDocumento: Record<string, number> = {
    DNI: 1, CE: 2, Pasaporte: 3, RUC: 4, NN: 5
  };

  // ===================================================================
  // GETTERS — toda la lógica del template aquí
  // ===================================================================

  /** Acceso directo a controls — f.campo en template */
  get f() { return this.form.controls; }

  /**
   * Acceso cacheado a tipoSalida — evita form.get('tipoSalida') repetido
   * en setupConditionalLogic y getters relacionados.
   */
  private get tipoSalidaCtrl(): FormControl {
    return this.form.get('tipoSalida') as FormControl;
  }

  /** form.get().value es O(1) — evita el overhead de getRawValue() que clona el form */
  get esFamiliar(): boolean {
    return this.tipoSalidaCtrl?.value === 'Familiar';
  }
  get esAutoridad(): boolean {
    return this.tipoSalidaCtrl?.value === 'AutoridadLegal';
  }

  /** Getter semántico de tipoSalida para ngSwitch en template */
  get tipoSalidaActual(): string {
    return this.tipoSalidaCtrl?.value ?? 'Familiar';
  }

  get tieneExterno(): boolean {
    return !!this.form.get('tieneMedicoExterno')?.value;
  }
  get tipoSalidaBloqueado(): boolean {
    return !!this.expediente?.tipoSalidaPreliminar;
  }
  get medicoExternoHabilitado(): boolean {
    return this.esFamiliar && !this.expediente?.causaViolentaODudosa;
  }
  get puedeCrear(): boolean {
    return !this.form.invalid && !this.form.pending && !this.isSubmitting;
  }
  get tituloFormulario(): string {
    return this.esFamiliar
      ? 'Acta de Retiro — Familiar'
      : 'Acta de Retiro — Autoridad Legal';
  }

  // ── Getters de placeholder ────────────────────────────────────────

  get placeholderDocFamiliar(): string {
    const tipo = this.form.get('tipoDocumentoFamiliar')?.value as number;
    return this.placeholdersDocumento[tipo] ?? 'Número de documento';
  }
  get placeholderDocAutoridad(): string {
    const tipo = this.form.get('tipoDocumentoAutoridad')?.value as number;
    return this.placeholdersDocumento[tipo] ?? 'Número de documento';
  }

  // ===================================================================
  // HELPERS UI
  // ===================================================================

  isInvalidField(campo: string): boolean {
    const ctrl = this.form.get(campo);
    // !!ctrl?.invalid evita doble evaluación vs !!ctrl && ctrl.invalid
    return !!ctrl?.invalid && (ctrl.dirty || ctrl.touched);
  }

  getErrorMessage(campo: string): string {
    const ctrl = this.form.get(campo);
    if (!ctrl?.errors) return '';
    const { errors } = ctrl;
    if (errors['required']) return 'Este campo es obligatorio';
    if (errors['duplicado']) return 'Este número ya está registrado en otra acta';
    if (errors['pattern']) {
      const mensajes: Record<string, string> = {
        cmpJefeGuardia: 'El CMP debe tener entre 4 y 6 dígitos',
        medicoExternoCMP: 'El CMP debe tener entre 4 y 6 dígitos',
        dniFamiliar: 'El DNI debe tener exactamente 8 dígitos',
        documentoAutoridad: 'El DNI debe tener exactamente 8 dígitos',
        telefonoFamiliar: 'El teléfono debe tener exactamente 9 dígitos',
        telefonoAutoridad: 'El teléfono debe tener exactamente 9 dígitos',
      };
      return mensajes[campo] ?? 'Formato inválido';
    }
    return 'Dato inválido';
  }

  trackByValue(_: number, item: { value: number; label: string }): number {
    return item.value;
  }

  /**
   * Clases CSS del input con estado de error.
   * Array + filter(Boolean) + join — escalable y evita strings vacíos.
   */
  inputClass(campo: string, mono = false): string {
    return [
      'form-input',
      mono ? 'font-mono' : '',
      this.isInvalidField(campo) ? 'form-input-error' : ''
    ].filter(Boolean).join(' ');
  }

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    if (!this.expediente) {
      console.error('[FormularioActaRetiro] No se recibió expediente');
      return;
    }
    this.initForm();
    this.setupConditionalLogic();
    this.preLlenarDatos();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // INICIALIZACIÓN DEL FORM
  // ===================================================================

  private initForm(): void {
    this.form = this.fb.group({
      tipoSalida: ['Familiar', Validators.required],

      // SINADEF / Oficio — validador asíncrono nativo
      numeroCertificadoDefuncion: [
        '', [Validators.required], [this.sinadefAsyncValidator()]
      ],
      numeroOficioPolicial: [
        { value: '', disabled: true },
        [Validators.required],
        [this.oficioAsyncValidator()]
      ],

      // Jefe de Guardia
      nombreJefeGuardia: ['', Validators.required],
      cmpJefeGuardia: ['', [Validators.required, Validators.pattern(/^\d{4,6}$/)]],

      // Médico externo — acordeón
      tieneMedicoExterno: [false],
      medicoExternoNombre: [{ value: '', disabled: true }],
      medicoExternoCMP: [{ value: '', disabled: true }],

      // Familiar
      nombreFamiliar: ['', Validators.required],
      tipoDocumentoFamiliar: [1, Validators.required],
      dniFamiliar: ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
      parentesco: ['', Validators.required],
      telefonoFamiliar: ['', [Validators.pattern(/^\d{9}$/)]],

      // Autoridad Legal — inicia deshabilitado
      tipoAutoridad: [{ value: 1, disabled: true }, Validators.required],
      tipoDocumentoAutoridad: [{ value: 1, disabled: true }, Validators.required],
      nombreAutoridad: [{ value: '', disabled: true }, Validators.required],
      documentoAutoridad: [{ value: '', disabled: true },
      [Validators.required, Validators.pattern(/^\d{8}$/)]],
      cargoAutoridad: [{ value: '', disabled: true }, Validators.required],
      institucionAutoridad: [{ value: '', disabled: true }, Validators.required],
      telefonoAutoridad: [{ value: '', disabled: true }, [Validators.pattern(/^\d{9}$/)]],

      // Opcionales
      destinoCuerpo: [''],
      observaciones: ['']
    });
  }

  private preLlenarDatos(): void {
    const e = this.expediente;
    this.edadPaciente = e.edad ?? 0;
    this.diagnosticoFinal = e.diagnosticoFinal || '';

    if (e.tipoSalidaPreliminar) {
      this.tipoSalidaCtrl?.setValue(
        e.tipoSalidaPreliminar, { emitEvent: false }
      );
      this.tipoSalidaCtrl?.disable();
      this.aplicarLogicaTipoSalida(
        e.tipoSalidaPreliminar as 'Familiar' | 'AutoridadLegal'
      );
    }
  }

  // ===================================================================
  // LÓGICA CONDICIONAL REACTIVA
  // ===================================================================

  private setupConditionalLogic(): void {
    // tipoSalidaCtrl getter evita form.get('tipoSalida') repetido
    this.tipoSalidaCtrl?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((tipo: 'Familiar' | 'AutoridadLegal') =>
        this.aplicarLogicaTipoSalida(tipo)
      );

    this.form.get('tieneMedicoExterno')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((tiene: boolean) => {
        const nombreCtrl = this.form.get('medicoExternoNombre');
        const cmpCtrl = this.form.get('medicoExternoCMP');
        if (tiene) {
          nombreCtrl?.enable();
          cmpCtrl?.enable();
          nombreCtrl?.setValidators([Validators.required]);
          cmpCtrl?.setValidators([
            Validators.required, Validators.pattern(/^\d{4,6}$/)
          ]);
        } else {
          nombreCtrl?.disable(); nombreCtrl?.reset('');
          cmpCtrl?.disable(); cmpCtrl?.reset('');
          nombreCtrl?.clearValidators();
          cmpCtrl?.clearValidators();
        }
        nombreCtrl?.updateValueAndValidity();
        cmpCtrl?.updateValueAndValidity();
      });

    this.setupDocumentValidation('tipoDocumentoFamiliar', 'dniFamiliar');
    this.setupDocumentValidation('tipoDocumentoAutoridad', 'documentoAutoridad');
  }

  private aplicarLogicaTipoSalida(tipo: 'Familiar' | 'AutoridadLegal'): void {
    if (tipo === 'Familiar') {
      this.enableGroup([
        'numeroCertificadoDefuncion', 'tieneMedicoExterno',
        'nombreFamiliar', 'tipoDocumentoFamiliar', 'dniFamiliar',
        'parentesco', 'telefonoFamiliar'
      ]);
      this.disableGroup([
        'numeroOficioPolicial',
        'tipoAutoridad', 'tipoDocumentoAutoridad', 'nombreAutoridad',
        'documentoAutoridad', 'cargoAutoridad', 'institucionAutoridad',
        'telefonoAutoridad'
      ]);
      this.form.get('tieneMedicoExterno')?.setValue(false, { emitEvent: true });
    } else {
      this.disableGroup([
        'numeroCertificadoDefuncion', 'tieneMedicoExterno',
        'medicoExternoNombre', 'medicoExternoCMP',
        'nombreFamiliar', 'tipoDocumentoFamiliar', 'dniFamiliar',
        'parentesco', 'telefonoFamiliar'
      ]);
      this.enableGroup([
        'numeroOficioPolicial',
        'tipoAutoridad', 'tipoDocumentoAutoridad', 'nombreAutoridad',
        'documentoAutoridad', 'cargoAutoridad', 'institucionAutoridad',
        'telefonoAutoridad'
      ]);
    }
  }
  // 1. INICIAN EN TRUE (Porque DNI es la opción por defecto al cargar)
  esDniFamiliar = true;
  esDniAutoridad = true;

  private setupDocumentValidation(
    tipoCtrlName: string,
    docCtrlName: string
  ): void {
    this.form.get(tipoCtrlName)?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((tipo: any) => {

        const tipoNum = Number(tipo);

        if (tipoCtrlName === 'tipoDocumentoFamiliar') {
          this.esDniFamiliar = tipoNum === 1;
        } else {
          this.esDniAutoridad = tipoNum === 1;
        }

        const docCtrl = this.form.get(docCtrlName);
        if (!docCtrl) return;

        const validators = [Validators.required];

        if (tipoNum === 1) {
          validators.push(
            Validators.pattern(/^\d{8}$/),
            Validators.minLength(8),
            Validators.maxLength(8)
          );
        }

        docCtrl.setValidators(validators);
        docCtrl.setValue('');
        docCtrl.markAsPristine();
        docCtrl.markAsUntouched();
        docCtrl.updateValueAndValidity();
      });
  }

  private enableGroup(controls: string[]): void {
    controls.forEach(c => this.form.get(c)?.enable());
  }

  /**
   * Deshabilita y resetea controles.
   * emitEvent: false evita disparar loops en valueChanges.
   * Separamos disable() y reset() para máxima compatibilidad Angular.
   */
  private disableGroup(controls: string[]): void {
    controls.forEach(c => {
      const ctrl = this.form.get(c);
      if (!ctrl) return;
      ctrl.disable({ emitEvent: false });
      ctrl.reset(null, { emitEvent: false });
    });
  }

  // ===================================================================
  // VALIDADORES ASÍNCRONOS
  // ===================================================================

  private sinadefAsyncValidator(): AsyncValidatorFn {
    return (ctrl: AbstractControl): Observable<ValidationErrors | null> => {
      // Extrae y limpia el valor una sola vez
      const value = (ctrl.value || '').trim();
      if (value.length < 10) return of(null);
      return timer(500).pipe(
        switchMap(() =>
          this.actaRetiroService.verificarCertificadoSINADEF(value)
        ),
        map(existe => (existe ? { duplicado: true } : null)),
        catchError(() => of(null))
      );
    };
  }

  private oficioAsyncValidator(): AsyncValidatorFn {
    return (ctrl: AbstractControl): Observable<ValidationErrors | null> => {
      const value = (ctrl.value || '').trim();
      if (value.length < 5) return of(null);
      return timer(500).pipe(
        switchMap(() =>
          this.actaRetiroService.verificarOficioLegal(value)
        ),
        map(existe => (existe ? { duplicado: true } : null)),
        catchError(() => of(null))
      );
    };
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  async crear(): Promise<void> {
    // Guard doble: TS + HTML ([disabled]="!puedeCrear")
    if (this.form.invalid || this.form.pending || this.isSubmitting) {
      // Fuerza visibilidad de todos los errores y re-evalúa validadores
      this.form.markAllAsTouched();
      this.form.updateValueAndValidity();
      if (this.form.pending) {
        Swal.fire({
          icon: 'info',
          title: 'Verificando documentos',
          text: 'Espere mientras se validan los números ingresados...',
          confirmButtonColor: '#0891B2'
        });
      }
      return;
    }

    const values = this.form.getRawValue() as ActaRetiroFormValues;

    const confirmacion = await Swal.fire({
      icon: 'question',
      title: 'Confirmar Creación de Acta',
      html: this.generarHTMLConfirmacion(values),
      showCancelButton: true,
      confirmButtonText: 'Sí, crear acta',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;
    this.ejecutarCreacion(values);
  }

  private generarHTMLConfirmacion(values: ActaRetiroFormValues): string {
    const esFamiliar = values.tipoSalida === 'Familiar';
    const quienRetira = esFamiliar ? values.nombreFamiliar : values.nombreAutoridad;

    let html = `
      <div class="text-left text-sm space-y-2">
        <p class="font-semibold text-gray-800">
          Paciente: <span class="font-normal">${this.expediente.nombreCompleto}</span>
        </p>
    `;

    if (this.edadPaciente > 0)
      html += `<p class="text-gray-500 text-xs">Edad: ${this.edadPaciente} años</p>`;

    html += `
        <p class="font-semibold text-gray-800 mt-2">
          Retira: <span class="font-normal">${quienRetira}</span>
        </p>
    `;

    if (esFamiliar && values.dniFamiliar)
      html += `
        <p class="text-gray-500 text-xs">
          DNI: ${values.dniFamiliar} — ${values.parentesco}
        </p>
      `;
    else if (!esFamiliar && values.cargoAutoridad)
      html += `
        <p class="text-gray-500 text-xs">
          ${values.cargoAutoridad} — ${values.institucionAutoridad}
        </p>
      `;

    if (esFamiliar && values.medicoExternoNombre?.trim())
      html += `
        <p class="font-semibold text-gray-800 mt-2">Médico externo:</p>
        <p class="text-gray-600">${values.medicoExternoNombre}</p>
        <p class="text-gray-500 text-xs">CMP: ${values.medicoExternoCMP}</p>
      `;

    html += `
        <p class="font-semibold text-gray-800 mt-2">
          Jefe de Guardia:
          <span class="font-normal">${values.nombreJefeGuardia}</span>
        </p>
        <p class="text-gray-500 text-xs">CMP: ${values.cmpJefeGuardia}</p>
      </div>
    `;
    return html;
  }

  private ejecutarCreacion(values: ActaRetiroFormValues): void {
    this.isSubmitting = true;

    const { apellidoPaterno: famAP, apellidoMaterno: famAM, nombres: famN } =
      this.splitNombre(values.nombreFamiliar ?? '');
    const { apellidoPaterno: autAP, apellidoMaterno: autAM, nombres: autN } =
      this.splitNombre(values.nombreAutoridad ?? '');

    const dto: CreateActaRetiroDTO = {
      expedienteID: this.expediente.expedienteID,
      tipoSalida: values.tipoSalida,

      numeroCertificadoDefuncion: this.esFamiliar
        ? values.numeroCertificadoDefuncion || undefined : undefined,
      numeroOficioPolicial: this.esAutoridad
        ? values.numeroOficioPolicial || undefined : undefined,

      nombreCompletoFallecido: this.expediente.nombreCompleto,
      historiaClinica: this.expediente.hc,
      tipoDocumentoFallecido: this.mapearTipoDocumento(
        this.expediente.tipoDocumento || 'DNI'
      ),
      numeroDocumentoFallecido: this.expediente.numeroDocumento || '',
      servicioFallecimiento: this.expediente.servicioFallecimiento || '',
      fechaHoraFallecimiento: this.expediente.fechaHoraFallecimiento,
      medicoCertificaNombre: this.expediente.medicoCertificaNombre || '',
      medicoCMP: this.expediente.medicoCMP || '',
      medicoRNE: this.expediente.medicoRNE || undefined,

      medicoExternoNombre: this.esFamiliar && this.tieneExterno
        ? values.medicoExternoNombre || undefined : undefined,
      medicoExternoCMP: this.esFamiliar && this.tieneExterno
        ? values.medicoExternoCMP || undefined : undefined,

      jefeGuardiaNombre: values.nombreJefeGuardia,
      jefeGuardiaCMP: values.cmpJefeGuardia,

      familiarApellidoPaterno: this.esFamiliar ? famAP : undefined,
      familiarApellidoMaterno: this.esFamiliar ? famAM : undefined,
      familiarNombres: this.esFamiliar ? famN : undefined,
      familiarTipoDocumento: this.esFamiliar ? values.tipoDocumentoFamiliar : undefined,
      familiarNumeroDocumento: this.esFamiliar ? values.dniFamiliar : undefined,
      familiarParentesco: this.esFamiliar ? values.parentesco : undefined,
      familiarTelefono: this.esFamiliar
        ? values.telefonoFamiliar || undefined : undefined,

      autoridadApellidoPaterno: this.esAutoridad ? autAP : undefined,
      autoridadApellidoMaterno: this.esAutoridad ? autAM : undefined,
      autoridadNombres: this.esAutoridad ? autN : undefined,
      tipoAutoridad: this.esAutoridad ? values.tipoAutoridad : undefined,
      autoridadTipoDocumento: this.esAutoridad ? values.tipoDocumentoAutoridad : undefined,
      autoridadNumeroDocumento: this.esAutoridad ? values.documentoAutoridad : undefined,
      autoridadCargo: this.esAutoridad ? values.cargoAutoridad : undefined,
      autoridadInstitucion: this.esAutoridad ? values.institucionAutoridad : undefined,
      autoridadTelefono: this.esAutoridad
        ? values.telefonoAutoridad || undefined : undefined,

      destino: values.destinoCuerpo || undefined,
      observaciones: values.observaciones || undefined,
      usuarioAdmisionID: this.authService.getUserId()
    };

    this.actaRetiroService.crear(dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: actaCreada => {
          this.isSubmitting = false;
          Swal.fire({
            icon: 'success',
            title: 'Acta Creada Exitosamente',
            text: 'El acta de retiro ha sido registrada correctamente',
            confirmButtonColor: '#0891B2'
          });
          this.actaCreada.emit(actaCreada);
        },
        error: err => {
          this.isSubmitting = false;
          console.error('[ActaRetiro] Error al crear:', err);
          Swal.fire({
            icon: 'error',
            title: 'Error al Crear Acta',
            text: err.error?.mensaje || 'No se pudo crear el acta. Intente nuevamente.',
            confirmButtonColor: '#EF4444'
          });
        }
      });
  }

  async cancelarFormulario(): Promise<void> {
    if (this.form.dirty) {
      const r = await Swal.fire({
        icon: 'warning',
        title: '¿Cancelar Creación?',
        text: 'Los datos ingresados se perderán',
        showCancelButton: true,
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'Seguir editando',
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280'
      });
      if (!r.isConfirmed) return;
    }
    this.onCancelar.emit();
  }

  // ===================================================================
  // HELPERS PRIVADOS
  // ===================================================================

  /**
   * Parsea "APELLIDO PATERNO APELLIDO MATERNO, Nombres".
   * /\s+/ maneja dobles espacios. nombresRaw?.trim() || '' cubre edge case "GARCIA, ".
   */
  private splitNombre(nombre: string): {
    apellidoPaterno: string;
    apellidoMaterno: string;
    nombres: string;
  } {
    if (!nombre?.trim())
      return { apellidoPaterno: '', apellidoMaterno: '', nombres: '' };

    const [apellidosRaw = '', nombresRaw] = nombre.split(',');
    const apellidos = apellidosRaw.trim().split(/\s+/).filter(Boolean);

    return {
      apellidoPaterno: apellidos[0] ?? '',
      apellidoMaterno: apellidos[1] ?? '',
      nombres: nombresRaw?.trim() || ''   // edge case: "GARCIA, "
    };
  }

  private mapearTipoDocumento(tipo: string): number {
    return this.mapaTipoDocumento[tipo] ?? 1;
  }
}
