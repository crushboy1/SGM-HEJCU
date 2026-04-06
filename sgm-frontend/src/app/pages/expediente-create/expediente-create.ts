import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, Validators,
  FormGroup, FormArray, AbstractControl, ValidatorFn
} from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, switchMap } from 'rxjs';
import Swal from 'sweetalert2';

import { ExpedienteService, CreateExpedienteDTO } from '../../services/expediente';
import { IntegracionService, PacienteParaForm } from '../../services/integracion';
import { IconComponent } from '../../components/icon/icon.component';
import { SoloNumerosDirective } from '../../shared/directives/solo-numeros';

// ===================================================================
// ENUMS (alineados con backend)
// ===================================================================
export enum TipoIngreso {
  Interno = 1,
  Externo = 2
}
export enum FuenteFinanciamiento {
  SIS = 1, EsSalud = 2, Particular = 3,
  SOAT = 4, PendientePago = 5, Otros = 6
}
export enum TipoDocumentoIdentidad {
  DNI = 1, Pasaporte = 2, CarneExtranjeria = 3,
  SinDocumento = 4, NN = 5
}

// ===================================================================
// VALIDADORES CUSTOM
// ===================================================================

/**
 * Validador de N° Documento dinámico según tipo seleccionado.
 * DNI            → exactamente 8 dígitos numéricos
 * CE / Pasaporte → alfanumérico, máx 12 caracteres
 * SinDocumento / NN → sin validación de formato
 */
function documentoValidator(tipoControl: AbstractControl | null): ValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return null;
    const tipo = Number(tipoControl?.value);
    if (tipo === TipoDocumentoIdentidad.DNI) {
      return /^\d{8}$/.test(control.value)
        ? null
        : { formatoDocumento: 'El DNI debe tener exactamente 8 dígitos numéricos' };
    }
    if (
      tipo === TipoDocumentoIdentidad.CarneExtranjeria ||
      tipo === TipoDocumentoIdentidad.Pasaporte
    ) {
      return control.value.length <= 12
        ? null
        : { formatoDocumento: 'Máximo 12 caracteres' };
    }
    return null;
  };
}

/** Valida que la fecha/hora no sea futura (comparación local). */
function fechaNoFuturaValidator(): ValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return null;
    return new Date(control.value) > new Date()
      ? { fechaFutura: true }
      : null;
  };
}

@Component({
  selector: 'app-expediente-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent, SoloNumerosDirective],
  templateUrl: './expediente-create.html',
  styleUrl: './expediente-create.css'
})
export class ExpedienteCreateComponent implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private fb = inject(FormBuilder);
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private integracionService = inject(IntegracionService);
  private destroy$ = new Subject<void>();

  // ── Estado ───────────────────────────────────────────────────────
  expedienteForm!: FormGroup;
  isLoading = false;
  pacienteData: PacienteParaForm | null = null;
  modoManual = false;
  datosGalenhosDisponibles = false;

  /**
   * Bandera que bloquea los listeners de valueChanges durante el
   * patchValue inicial de Galenhos, evitando que limpien los datos
   * recién cargados.
   */
  private actualizandoDesdeCarga = false;

  // Fechas máximas para validación en template — hora local Lima GMT-5
  readonly maxFechaHoy: string = new Date().toISOString().substring(0, 10);
  readonly maxFechaHoraActual: string = this.formatDateTimeLocal(new Date());

  // ── Catálogos ─────────────────────────────────────────────────────
  readonly tiposDocumento = [
    { id: TipoDocumentoIdentidad.DNI, label: 'DNI' },
    { id: TipoDocumentoIdentidad.Pasaporte, label: 'Pasaporte' },
    { id: TipoDocumentoIdentidad.CarneExtranjeria, label: 'Carné de Extranjería' },
    { id: TipoDocumentoIdentidad.SinDocumento, label: 'Sin Documento' },
    { id: TipoDocumentoIdentidad.NN, label: 'NN (No Identificado)' },
  ];

  readonly tiposIngreso = [
    { id: TipoIngreso.Interno, label: 'Interno (hospitalizado)' },
    { id: TipoIngreso.Externo, label: 'Externo (DOA / Traumashock)' },
  ];

  readonly fuentesFinanciamiento = [
    { id: FuenteFinanciamiento.SIS, label: 'SIS' },
    { id: FuenteFinanciamiento.EsSalud, label: 'EsSalud' },
    { id: FuenteFinanciamiento.Particular, label: 'Particular' },
    { id: FuenteFinanciamiento.SOAT, label: 'SOAT' },
    { id: FuenteFinanciamiento.PendientePago, label: 'Pendiente de Pago' },
    { id: FuenteFinanciamiento.Otros, label: 'Otros' },
  ];

  readonly servicios: string[] = [
    'Medicina Interna', 'Cirugía General', 'Cirugía 4A', 'UCI', 'UCINT',
    'Emergencia', 'Trauma Shock', 'Traumatología', 'Neurocirugía',
    'Sala de Recuperación', 'Observaciones', 'UVE', 'Otro'
  ];

  // ===================================================================
  // GETTERS
  // ===================================================================

  /** Acceso directo a controls del form */
  get f() { return this.expedienteForm.controls; }

  /** Raw values — incluye campos disabled */
  get rv() { return this.expedienteForm?.getRawValue?.() ?? {}; }

  get medicoExternoHabilitado(): boolean {
    return Number(this.f['tipoExpediente'].value) === TipoIngreso.Externo
      && !this.f['causaViolentaODudosa'].value;
  }

  get fuenteLabel(): string {
    const id = Number(this.rv.fuenteFinanciamiento);
    return this.fuentesFinanciamiento.find(f => f.id === id)?.label ?? 'Pendiente de Pago';
  }

  get pertenencias(): FormArray {
    return this.expedienteForm.get('pertenencias') as FormArray;
  }
  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    this.buildForm();
    this.suscribirCambiosCondicionales();
    this.actualizarValidadoresDocumento();
    const hc = this.route.snapshot.queryParamMap.get('hc');

    if (hc) {
      this.modoManual = false;
      this.isLoading = true;

      this.integracionService.consultarPaciente(hc)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: data => {
            this.cargarDatosDesdeServicio(data);
            this.isLoading = false;
          },
          error: err => {
            console.warn('[ExpedienteCreate] Error integración:', err);
            this.isLoading = false;
            this.modoManual = true;
            this.deshabilitarDemograficos(false); // habilitar en modo manual
            this.expedienteForm.patchValue({ hc });
            this.mostrarAlertaInfo(
              'No se pudo conectar con SIGEM/Galenhos. Ingrese los datos manualmente.'
            );
          }
        });
    } else {
      this.modoManual = true;
      // En modo manual los campos ya nacen habilitados — no hay que hacer nada
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // CONSTRUCCIÓN DEL FORM
  // Patrón SGM: todos los campos nacen habilitados.
  // Los demográficos se deshabilitan DESPUÉS del patchValue si vienen
  // de Galenhos — nunca se crean con { value, disabled }.
  // ===================================================================
  private buildForm(): void {
    this.expedienteForm = this.fb.group({
      // Demográficos — nacen habilitados
      hc: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(12)]],
      tipoDocumento: [TipoDocumentoIdentidad.DNI, Validators.required],
      numeroDocumento: ['', Validators.required],
      apellidoPaterno: ['', Validators.required],
      apellidoMaterno: ['', Validators.required],
      nombres: ['', Validators.required],
      fechaNacimiento: ['', Validators.required],
      sexo: ['', Validators.required],
      fuenteFinanciamiento: [FuenteFinanciamiento.PendientePago, Validators.required],

      // Flags
      esNN: [false],
      causaViolentaODudosa: [false],

      // Tipo de expediente
      tipoExpediente: [TipoIngreso.Interno, Validators.required],

      // Datos del fallecimiento — siempre editables, nacen habilitados
      servicioFallecimiento: ['', Validators.required],
      numeroCama: ['', Validators.maxLength(4)],
      fechaHoraFallecimiento: [
        this.formatDateTimeLocal(new Date()),
        [Validators.required, fechaNoFuturaValidator()]
      ],
      diagnosticoFinal: [''],

      // Médico certificante — siempre editable
      medicoCertificaNombre: ['', Validators.required],
      medicoCMP: ['', [Validators.required, Validators.pattern('^[0-9]{4,6}$')]],
      medicoRNE: ['', Validators.pattern('^[0-9]{5}$')],

      // Médico externo — nace habilitado, se deshabilita condicionalmente
      medicoExternoNombre: [''],
      medicoExternoCMP: ['', Validators.pattern('^[0-9]{4,6}$')],

      // Extras
      observaciones: ['', Validators.maxLength(1000)],
      pertenencias: this.fb.array([])
    });
  }

  // ===================================================================
  // SUSCRIPCIONES CONDICIONALES
  // ===================================================================
  private suscribirCambiosCondicionales(): void {
    // Actualizar médico externo cuando cambia tipo expediente o causa violenta
    const actualizarMedicoExterno = () => {
      if (this.actualizandoDesdeCarga) return;
      const esExterno = Number(this.f['tipoExpediente'].value) === TipoIngreso.Externo;
      const esViolenta = this.f['causaViolentaODudosa'].value === true;
      const habilitar = esExterno && !esViolenta;
      const nombreCtrl = this.expedienteForm.get('medicoExternoNombre');
      const cmpCtrl = this.expedienteForm.get('medicoExternoCMP');

      if (habilitar) {
        nombreCtrl?.enable();
        cmpCtrl?.enable();
      } else {
        nombreCtrl?.disable(); nombreCtrl?.reset('');
        cmpCtrl?.disable(); cmpCtrl?.reset('');
      }
    };

    this.expedienteForm.get('tipoExpediente')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => actualizarMedicoExterno());

    this.expedienteForm.get('causaViolentaODudosa')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => actualizarMedicoExterno());

    // Validadores dinámicos de numeroDocumento según tipo seleccionado
    this.expedienteForm.get('tipoDocumento')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (this.actualizandoDesdeCarga) return;
        this.actualizarValidadoresDocumento();
      });

    this.expedienteForm.get('esNN')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((esNN: boolean) => {
        if (this.actualizandoDesdeCarga) return;
        this.actualizarCamposNN(esNN);
      });
  }

  /**
   * Actualiza los validadores de numeroDocumento según el tipo seleccionado.
   * DNI            → solo números, exactamente 8 dígitos
   * CE / Pasaporte → alfanumérico, máx 12 caracteres
   * SinDocumento/NN → sin validación de formato
   */
  esDni = true;
  private actualizarValidadoresDocumento(): void {
    const tipo = Number(this.expedienteForm.get('tipoDocumento')?.value);
    const ctrl = this.expedienteForm.get('numeroDocumento');
    if (!ctrl) return;

    // Actualizar variable reactiva — 3 bindings en template
    this.esDni = tipo === TipoDocumentoIdentidad.DNI;

    ctrl.clearValidators();
    ctrl.setValue('', { emitEvent: false });
    ctrl.markAsPristine();
    ctrl.markAsUntouched();

    if (tipo === TipoDocumentoIdentidad.DNI) {
      ctrl.enable();
      ctrl.setValidators([Validators.required, Validators.pattern('^[0-9]{8}$')]);
    } else if (
      tipo === TipoDocumentoIdentidad.CarneExtranjeria ||
      tipo === TipoDocumentoIdentidad.Pasaporte
    ) {
      ctrl.enable();
      ctrl.setValidators([Validators.required, Validators.maxLength(12)]);
    } else {
      ctrl.disable({ emitEvent: false });
      ctrl.reset(null, { emitEvent: false });
    }

    ctrl.updateValueAndValidity();
  }

  private actualizarCamposNN(esNN: boolean): void {
    const camposAfectados = [
      'tipoDocumento', 'numeroDocumento',
      'apellidoPaterno', 'apellidoMaterno', 'nombres', 'fechaNacimiento'
    ];

    if (esNN) {
      camposAfectados.forEach(c => {
        this.expedienteForm.get(c)?.disable({ emitEvent: false });
        this.expedienteForm.get(c)?.clearValidators();
        this.expedienteForm.get(c)?.updateValueAndValidity();
      });
      this.expedienteForm.patchValue({
        tipoDocumento: TipoDocumentoIdentidad.NN,
        numeroDocumento: '',
        apellidoPaterno: 'NN',
        apellidoMaterno: 'NN',
        nombres: 'No Identificado',
        fechaNacimiento: '1900-01-01'
      }, { emitEvent: false });
    } else {
      this.expedienteForm.patchValue({
        tipoDocumento: TipoDocumentoIdentidad.DNI,
        numeroDocumento: '',
        apellidoPaterno: '',
        apellidoMaterno: '',
        nombres: '',
        fechaNacimiento: ''
      }, { emitEvent: false });
      this.esDni = true

      if (this.modoManual) {
        camposAfectados.forEach(c => {
          this.expedienteForm.get(c)?.enable();
          this.expedienteForm.get(c)?.setValidators(Validators.required);
          this.expedienteForm.get(c)?.updateValueAndValidity();
        });
        this.expedienteForm.get('hc')?.setValidators([
          Validators.required,
          Validators.minLength(4),
          Validators.maxLength(12),
        ]);
        this.expedienteForm.get('hc')?.updateValueAndValidity();
        this.actualizarValidadoresDocumento();
      }
    }
  }

  // ===================================================================
  // CARGA DESDE INTEGRACIÓN
  // ===================================================================
  private cargarDatosDesdeServicio(data: PacienteParaForm): void {
    this.pacienteData = data;
    this.datosGalenhosDisponibles = data.existeEnGalenhos;

    const fechaNac = data.fechaNacimiento?.substring(0, 10) ?? '';
    const fechaFallecimiento = data.fechaHoraFallecimiento
      ? this.formatDateTimeLocal(new Date(data.fechaHoraFallecimiento))
      : this.formatDateTimeLocal(new Date());

    // Activar bandera ANTES del patch — bloquea todos los listeners
    this.actualizandoDesdeCarga = true;

    this.expedienteForm.patchValue({
      hc: data.hc,
      tipoDocumento: data.tipoDocumentoID,
      numeroDocumento: data.numeroDocumento,
      apellidoPaterno: data.apellidoPaterno,
      apellidoMaterno: data.apellidoMaterno,
      nombres: data.nombres,
      fechaNacimiento: fechaNac,
      sexo: data.sexo,
      fuenteFinanciamiento: this.mapFuenteFinanciamiento(data.fuenteFinanciamiento),
      esNN: data.esNN,
      causaViolentaODudosa: data.causaViolentaODudosa,
      servicioFallecimiento: data.servicioFallecimiento ?? '',
      numeroCama: data.numeroCama ?? '',
      fechaHoraFallecimiento: fechaFallecimiento,
      diagnosticoFinal: data.diagnosticoFinal ?? '',
      medicoCertificaNombre: data.medicoCertificaNombre ?? '',
      medicoCMP: data.medicoCMP ?? '',
      medicoRNE: data.medicoRNE ?? '',
    }, { emitEvent: false });

    // Desactivar bandera DESPUÉS del patch
    this.actualizandoDesdeCarga = false;

    if (data.existeEnGalenhos) {
      this.deshabilitarDemograficos(true);
      if (!data.existeEnSigem) {
        // Galenhos OK pero SIGEM no → campos de fallecimiento editables (nacen habilitados)
      }
    } else {
      // Galenhos no encontró al paciente → modo manual completo
      this.modoManual = true;
    }

    this.expedienteForm.updateValueAndValidity();

    if (data.advertencias?.length > 0) {
      this.mostrarAdvertenciasIntegracion(data.advertencias);
    }
  }

  // ===================================================================
  // HELPERS DE FORM
  // ===================================================================

  /**
   * Habilita o deshabilita los campos demográficos.
   * deshabilitar=true → readonly (datos de Galenhos)
   * deshabilitar=false → editables (modo manual)
   */
  private deshabilitarDemograficos(deshabilitar: boolean): void {
    const campos = [
      'hc', 'tipoDocumento', 'numeroDocumento',
      'apellidoPaterno', 'apellidoMaterno', 'nombres',
      'fechaNacimiento', 'sexo', 'fuenteFinanciamiento'
    ];
    campos.forEach(c => {
      if (deshabilitar) {
        this.expedienteForm.get(c)?.disable();
      } else {
        this.expedienteForm.get(c)?.enable();
      }
    });
    if (!deshabilitar) {
      this.actualizarValidadoresDocumento();
    }
  }

  private mapFuenteFinanciamiento(valor: string): number {
    const mapa: Record<string, number> = {
      'SIS': FuenteFinanciamiento.SIS,
      'EsSalud': FuenteFinanciamiento.EsSalud,
      'Particular': FuenteFinanciamiento.Particular,
      'SOAT': FuenteFinanciamiento.SOAT,
      'PendientePago': FuenteFinanciamiento.PendientePago,
      'Otros': FuenteFinanciamiento.Otros,
    };
    return mapa[valor] ?? FuenteFinanciamiento.PendientePago;
  }

  /**
   * Formatea un Date a 'YYYY-MM-DDTHH:mm' usando hora local.
   * NO usa toISOString() (devuelve UTC → bug en GMT-5 Lima).
   */
  private formatDateTimeLocal(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  getTipoDocumentoNombre(id: number): string {
    return this.tiposDocumento.find(t => t.id === id)?.label ?? 'Desconocido';
  }

  getSexoLabel(valor: string): string {
    return valor === 'M' ? 'Masculino' : valor === 'F' ? 'Femenino' : valor;
  }

  /** Valida si un campo es inválido y fue tocado. */
  isInvalidField(campo: string): boolean {
    const ctrl = this.expedienteForm.get(campo);
    return !!ctrl?.invalid && (ctrl.dirty || ctrl.touched);
  }

  getMensajeError(campo: string): string {
    const ctrl = this.expedienteForm.get(campo);
    if (!ctrl?.errors) return '';
    const { errors } = ctrl;

    if (errors['required']) return 'Campo obligatorio';
    if (errors['minlength']) return `Mínimo ${errors['minlength'].requiredLength} caracteres`;
    if (errors['maxlength']) return `Máximo ${errors['maxlength'].requiredLength} caracteres`;
    if (errors['formatoDocumento']) return errors['formatoDocumento'];
    if (errors['fechaFutura']) return 'La fecha de fallecimiento no puede ser futura';
    if (errors['pattern']) {
      const mensajes: Record<string, string> = {
        medicoCMP: 'Solo números, entre 4 y 6 dígitos',
        medicoRNE: 'Solo números, exactamente 5 dígitos',
        medicoExternoCMP: 'Solo números, entre 4 y 6 dígitos',
        numeroDocumento: 'El DNI debe tener exactamente 8 dígitos',
      };
      return mensajes[campo] ?? 'Formato inválido';
    }
    return 'Valor inválido';
  }

  /** trackBy para ngFor de catálogos con id numérico */
  trackByValue(_: number, item: { id: number }): number {
    return item.id;
  }

  trackByIndex(index: number): number {
    return index;
  }

  // ===================================================================
  // PERTENENCIAS (FormArray)
  // ===================================================================

  agregarPertenencia(): void {
    this.pertenencias.push(this.fb.group({
      descripcion: ['', [Validators.required, Validators.maxLength(500)]],
      observaciones: ['', Validators.maxLength(500)]
    }));
  }

  eliminarPertenencia(index: number): void {
    this.pertenencias.removeAt(index);
  }

  // ===================================================================
  // SUBMIT
  // ===================================================================

  onSubmit(): void {
    if (this.isLoading) return;
    this.expedienteForm.markAllAsTouched();

    if (this.expedienteForm.invalid) {
      Swal.fire({
        title: 'Formulario incompleto',
        text: 'Revise los campos obligatorios marcados en rojo.',
        icon: 'warning',
        confirmButtonColor: '#EF4444',
        confirmButtonText: 'Aceptar'
      });
      return;
    }

    this.isLoading = true;
    const raw = this.expedienteForm.getRawValue();

    const dto: CreateExpedienteDTO = {
      hc: raw.hc,
      tipoDocumento: Number(raw.tipoDocumento),
      numeroDocumento: raw.numeroDocumento,
      apellidoPaterno: raw.apellidoPaterno,
      apellidoMaterno: raw.apellidoMaterno,
      nombres: raw.nombres,
      fechaNacimiento: raw.fechaNacimiento,
      sexo: raw.sexo,
      fuenteFinanciamiento: Number(raw.fuenteFinanciamiento),
      tipoExpediente: Number(raw.tipoExpediente),
      esNN: raw.esNN ?? false,
      causaViolentaODudosa: raw.causaViolentaODudosa ?? false,
      servicioFallecimiento: raw.servicioFallecimiento,
      numeroCama: raw.numeroCama || null,
      fechaHoraFallecimiento: raw.fechaHoraFallecimiento,
      diagnosticoFinal: raw.diagnosticoFinal || null,
      medicoCertificaNombre: raw.medicoCertificaNombre,
      medicoCMP: raw.medicoCMP,
      medicoRNE: raw.medicoRNE || null,
      medicoExternoNombre: raw.medicoExternoNombre || null,
      medicoExternoCMP: raw.medicoExternoCMP || null,
      observaciones: raw.observaciones || null,
      pertenencias: raw.pertenencias ?? []
    };

    this.expedienteService.create(dto).pipe(
      takeUntil(this.destroy$),
      switchMap(expediente =>
        this.expedienteService.generarQR(expediente.expedienteID).pipe(
          switchMap(() =>
            this.expedienteService.imprimirBrazalete(expediente.expedienteID)
          ),
          switchMap(blob => {
            this.descargarPDF(blob, `Brazalete-${expediente.codigoExpediente}`);
            return [expediente];
          })
        )
      )
    ).subscribe({
      next: (expediente: any) => {
        this.isLoading = false;
        Swal.fire({
          title: 'Expediente generado',
          html: `
            <div class="text-left space-y-1 text-sm">
              <p>Expediente: <strong>${expediente.codigoExpediente}</strong></p>
              <p>QR generado correctamente</p>
              <p>Brazalete descargado</p>
            </div>`,
          icon: 'success',
          confirmButtonText: 'Ir al Dashboard',
          confirmButtonColor: '#0891B2',
          timer: 5000,
          timerProgressBar: true
        }).then(() => this.router.navigate(['/dashboard']));
      },
      error: err => {
        this.isLoading = false;
        Swal.fire({
          title: 'Error',
          text: err.error?.message ?? 'Error al procesar el expediente.',
          icon: 'error',
          confirmButtonColor: '#EF4444',
          confirmButtonText: 'Cerrar'
        });
      }
    });
  }

  // ===================================================================
  // CANCELAR — con protección form.dirty
  // ===================================================================

  cancelar(): void {
    if (this.expedienteForm.dirty) {
      Swal.fire({
        icon: 'warning',
        title: '¿Cancelar registro?',
        text: 'Se perderán los datos ingresados.',
        showCancelButton: true,
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'Continuar',
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280'
      }).then(r => {
        if (r.isConfirmed) this.navegarAtras();
      });
    } else {
      this.navegarAtras();
    }
  }

  private navegarAtras(): void {
    const origen = this.route.snapshot.queryParamMap.get('origen');
    this.router.navigate([origen === 'bandeja' ? '/bandeja-entrada' : '/dashboard']);
  }

  // ===================================================================
  // HELPERS PRIVADOS
  // ===================================================================

  private descargarPDF(blob: Blob, nombre: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url; link.download = `${nombre}.pdf`; link.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 100);
  }

  private mostrarAlertaInfo(mensaje: string): void {
    Swal.fire({
      title: 'Información', text: mensaje,
      icon: 'info', confirmButtonColor: '#0891B2'
    });
  }

  private mostrarAdvertenciasIntegracion(advertencias: string[]): void {
    Swal.fire({
      title: 'Datos incompletos de SIGEM',
      html: `<ul class="text-left text-sm space-y-1">
        ${advertencias.map(a => `<li>• ${a}</li>`).join('')}</ul>`,
      icon: 'warning',
      confirmButtonColor: '#F59E0B',
      confirmButtonText: 'Entendido'
    });
  }
}
