import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, FormArray, AbstractControl, ValidatorFn } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, switchMap } from 'rxjs';
import Swal from 'sweetalert2';

import { ExpedienteService, CreateExpedienteDTO } from '../../services/expediente';
import { IntegracionService, PacienteParaForm } from '../../services/integracion';
import { IconComponent } from '../../components/icon/icon.component';

// ===================================================================
// ENUMS (alineados con backend)
// ===================================================================

export enum TipoIngreso {
  Interno = 1,
  Externo = 2
}

export enum FuenteFinanciamiento {
  SIS = 1,
  EsSalud = 2,
  Particular = 3,
  SOAT = 4,
  PendientePago = 5,
  Otros = 6
}

export enum TipoDocumentoIdentidad {
  DNI = 1,
  Pasaporte = 2,
  CarneExtranjeria = 3,
  SinDocumento = 4,
  NN = 5
}

// ===================================================================
// VALIDADORES CUSTOM
// ===================================================================

/** Validador que bloquea caracteres no numéricos en tiempo real */
function soloNumerosValidator(): ValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return null;
    return /^[0-9]+$/.test(control.value) ? null : { soloNumeros: true };
  };
}

/** Validador de N° Documento dinámico según tipo */
function documentoValidator(tipoControl: AbstractControl | null): ValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return null;
    const tipo = Number(tipoControl?.value);
    if (tipo === TipoDocumentoIdentidad.DNI) {
      return /^[0-9]{8}$/.test(control.value) ? null : { formatoDocumento: 'El DNI debe tener exactamente 8 dígitos numéricos' };
    }
    if (tipo === TipoDocumentoIdentidad.CarneExtranjeria || tipo === TipoDocumentoIdentidad.Pasaporte) {
      return control.value.length <= 12 ? null : { formatoDocumento: 'Máximo 12 caracteres' };
    }
    return null;
  };
}

@Component({
  selector: 'app-expediente-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent],
  templateUrl: './expediente-create.html',
  styleUrl: './expediente-create.css'
})
export class ExpedienteCreateComponent implements OnInit, OnDestroy {

  // ===================================================================
  // INYECCIÓN
  // ===================================================================
  private fb = inject(FormBuilder);
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private integracionService = inject(IntegracionService);
  private destroy$ = new Subject<void>();

  // ===================================================================
  // ESTADO
  // ===================================================================
  expedienteForm!: FormGroup;
  isLoading = false;
  pacienteData: PacienteParaForm | null = null;
  modoManual = false;
  datosGalenhosDisponibles = false;

  // ===================================================================
  // FECHAS MÁXIMAS PARA VALIDACIÓN EN TEMPLATE
  // TODO: GMT-5 Perú — DateTime.Now en backend usa hora del servidor.
  // Implementar TimeZoneInfo.ConvertTimeBySystemTimeZoneId en backend
  // y ajustar appsettings.json con "TimeZone": "SA Pacific Standard Time"
  // ===================================================================
  readonly maxFechaHoy: string = new Date().toISOString().substring(0, 10);
  readonly maxFechaHoraActual: string = new Date().toISOString().substring(0, 16);

  // ===================================================================
  // CATÁLOGOS
  // ===================================================================
  tiposDocumento = [
    { id: TipoDocumentoIdentidad.DNI, label: 'DNI' },
    { id: TipoDocumentoIdentidad.Pasaporte, label: 'Pasaporte' },
    { id: TipoDocumentoIdentidad.CarneExtranjeria, label: 'Carné de Extranjería' },
    { id: TipoDocumentoIdentidad.SinDocumento, label: 'Sin Documento' },
    { id: TipoDocumentoIdentidad.NN, label: 'NN (No Identificado)' },
  ];

  tiposIngreso = [
    { id: TipoIngreso.Interno, label: 'Interno (hospitalizado)' },
    { id: TipoIngreso.Externo, label: 'Externo (DOA / Traumashock)' },
  ];

  fuentesFinanciamiento = [
    { id: FuenteFinanciamiento.SIS, label: 'SIS' },
    { id: FuenteFinanciamiento.EsSalud, label: 'EsSalud' },
    { id: FuenteFinanciamiento.Particular, label: 'Particular' },
    { id: FuenteFinanciamiento.SOAT, label: 'SOAT' },
    { id: FuenteFinanciamiento.PendientePago, label: 'Pendiente de Pago' },
    { id: FuenteFinanciamiento.Otros, label: 'Otros' },
  ];

  servicios: string[] = [
    'Medicina Interna', 'Cirugía General', 'Cirugía 4A', 'UCI', 'UCINT',
    'Emergencia', 'Trauma Shock', 'Traumatología', 'Neurocirugía',
    'Sala de Recuperación', 'Observaciones', 'UVE', 'Otro'
  ];

  // ===================================================================
  // CONSTRUCTOR
  // ===================================================================
  constructor() {
    this.buildForm();
  }

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================
  ngOnInit(): void {
    this.suscribirCambiosCondicionales();

    const hc = this.route.snapshot.queryParamMap.get('hc');
    const origen = this.route.snapshot.queryParamMap.get('origen');

    if (hc) {
      this.modoManual = false;
      this.isLoading = true;

      this.integracionService.consultarPaciente(hc)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (data) => {
            this.cargarDatosDesdeServicio(data);
            this.isLoading = false;
          },
          error: (err) => {
            console.warn('Error consultando integración:', err);
            this.isLoading = false;
            this.modoManual = true;
            this.habilitarCamposDemograficos();
            this.expedienteForm.patchValue({ hc });
            this.mostrarAlertaInfo('No se pudo conectar con SIGEM/Galenhos. Ingrese los datos manualmente.');
          }
        });
    } else {
      this.modoManual = true;
      this.habilitarCamposDemograficos();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // CONSTRUCCIÓN DEL FORM
  // ===================================================================
  private buildForm(): void {
    this.expedienteForm = this.fb.group({
      // Datos demográficos
      hc: [{ value: '', disabled: true }, [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(12),
        soloNumerosValidator()
      ]],
      tipoDocumento: [{ value: TipoDocumentoIdentidad.DNI, disabled: true }, Validators.required],
      numeroDocumento: [{ value: '', disabled: true }, Validators.required],
      apellidoPaterno: [{ value: '', disabled: true }, Validators.required],
      apellidoMaterno: [{ value: '', disabled: true }, Validators.required],
      nombres: [{ value: '', disabled: true }, Validators.required],
      fechaNacimiento: [{ value: '', disabled: true }, Validators.required],
      sexo: [{ value: '', disabled: true }, Validators.required],
      fuenteFinanciamiento: [{ value: FuenteFinanciamiento.PendientePago, disabled: true }, Validators.required],

      // Flags
      esNN: [false],
      causaViolentaODudosa: [false],

      // Tipo de expediente
      tipoExpediente: [TipoIngreso.Interno, Validators.required],

      // Datos del fallecimiento
      servicioFallecimiento: ['', Validators.required],
      numeroCama: ['', [
        soloNumerosValidator(),
        Validators.maxLength(4)
      ]],
      fechaHoraFallecimiento: [this.formatDateTimeLocal(new Date()), [
        Validators.required,
        this.fechaNoFuturaValidator()
      ]],
      diagnosticoFinal: [''],

      // Médico certificante
      medicoCertificaNombre: ['', Validators.required],
      medicoCMP: ['', [
        Validators.required,
        Validators.pattern('^[0-9]{4,6}$')
      ]],
      medicoRNE: ['', [
        Validators.pattern('^[0-9]{5}$')
      ]],

      // Médico externo
      medicoExternoNombre: [{ value: '', disabled: true }],
      medicoExternoCMP: [{ value: '', disabled: true }, [
        Validators.pattern('^[0-9]{4,6}$')
      ]],

      // Observaciones
      observaciones: ['', Validators.maxLength(1000)],

      // Pertenencias
      pertenencias: this.fb.array([])
    });
  }

  // ===================================================================
  // SUSCRIPCIONES CONDICIONALES
  // ===================================================================
  private suscribirCambiosCondicionales(): void {
    const actualizarMedicoExterno = () => {
      const esExterno = Number(this.expedienteForm.get('tipoExpediente')?.value) === TipoIngreso.Externo;
      const esViolenta = this.expedienteForm.get('causaViolentaODudosa')?.value === true;
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

    // Cuando cambia tipoDocumento → actualizar validadores de numeroDocumento
    this.expedienteForm.get('tipoDocumento')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.actualizarValidadoresDocumento());

    this.expedienteForm.get('esNN')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((esNN: boolean) => this.actualizarCamposNN(esNN));
  }

  /**
   * Actualiza los validadores de numeroDocumento según el tipo seleccionado.
   * DNI           → solo números, exactamente 8 dígitos
   * CE / Pasaporte → alfanumérico, máx 12 chars
   * SinDocumento/NN → sin validación
   */
  private actualizarValidadoresDocumento(): void {
    const tipo = Number(this.expedienteForm.get('tipoDocumento')?.value);
    const ctrl = this.expedienteForm.get('numeroDocumento');
    if (!ctrl) return;

    ctrl.clearValidators();

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
      // SinDocumento / NN — bloquear y limpiar
      ctrl.disable();
      ctrl.reset('');
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
        this.expedienteForm.get(c)?.disable();
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
      });
    } else {
      this.expedienteForm.patchValue({
        tipoDocumento: TipoDocumentoIdentidad.DNI,
        numeroDocumento: '',
        apellidoPaterno: '',
        apellidoMaterno: '',
        nombres: '',
        fechaNacimiento: ''
      });
      if (this.modoManual) {
        camposAfectados.forEach(c => {
          this.expedienteForm.get(c)?.enable();
          this.expedienteForm.get(c)?.setValidators(Validators.required);
          this.expedienteForm.get(c)?.updateValueAndValidity();
        });
        // Restaurar validadores específicos
        this.expedienteForm.get('hc')?.setValidators([
          Validators.required,
          Validators.minLength(4),
          Validators.maxLength(12),
          soloNumerosValidator()
        ]);
        this.expedienteForm.get('hc')?.updateValueAndValidity();
        this.actualizarValidadoresDocumento();
      }
    }
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================
  private cargarDatosDesdeServicio(data: PacienteParaForm): void {
    this.pacienteData = data;
    this.datosGalenhosDisponibles = data.existeEnGalenhos;

    const fechaNac = data.fechaNacimiento ? data.fechaNacimiento.substring(0, 10) : '';
    const fechaFallecimiento = data.fechaHoraFallecimiento
      ? this.formatDateTimeLocal(new Date(data.fechaHoraFallecimiento))
      : this.formatDateTimeLocal(new Date());

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
    });

    if (data.existeEnGalenhos) {
      if (!data.existeEnSigem) this.habilitarCamposFallecimiento();
    } else {
      this.modoManual = true;
      this.habilitarCamposDemograficos();
      this.habilitarCamposFallecimiento();
    }

    if (data.advertencias?.length > 0) {
      this.mostrarAdvertenciasIntegracion(data.advertencias);
    }
  }

  // ===================================================================
  // HELPERS DE FORM
  // ===================================================================
  private habilitarCamposDemograficos(): void {
    const campos = [
      'hc', 'tipoDocumento', 'numeroDocumento',
      'apellidoPaterno', 'apellidoMaterno', 'nombres',
      'fechaNacimiento', 'sexo', 'fuenteFinanciamiento'
    ];
    campos.forEach(c => this.expedienteForm.get(c)?.enable());
    // Activar validadores específicos al habilitar
    this.actualizarValidadoresDocumento();
  }

  private habilitarCamposFallecimiento(): void {
    const campos = [
      'servicioFallecimiento', 'numeroCama', 'fechaHoraFallecimiento',
      'diagnosticoFinal', 'medicoCertificaNombre', 'medicoCMP', 'medicoRNE'
    ];
    campos.forEach(c => this.expedienteForm.get(c)?.enable());
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

  private formatDateTimeLocal(date: Date): string {
    return date.toISOString().substring(0, 16);
  }

  getInputClasses(editable: boolean): string {
    return editable
      ? 'w-full p-2.5 border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-hospital-cyan focus:border-transparent outline-none transition-all'
      : 'w-full p-2.5 border-none rounded-lg bg-gray-100 text-gray-800 font-semibold cursor-not-allowed';
  }

  getTipoDocumentoNombre(id: number): string {
    return this.tiposDocumento.find(t => t.id === id)?.label ?? 'Desconocido';
  }

  get fuenteLabel(): string {
    const id = Number(this.expedienteForm.get('fuenteFinanciamiento')?.value);
    return this.fuentesFinanciamiento.find(f => f.id === id)?.label ?? 'Pendiente de Pago';
  }

  get medicoExternoHabilitado(): boolean {
    return Number(this.expedienteForm.get('tipoExpediente')?.value) === TipoIngreso.Externo
      && !this.expedienteForm.get('causaViolentaODudosa')?.value;
  }

  /** True si un campo fue tocado y es inválido */
  esInvalido(campo: string): boolean {
    const ctrl = this.expedienteForm.get(campo);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  /**
   * Retorna el mensaje de error específico para cada campo y tipo de error.
   * Usado en el template para mensajes descriptivos.
   */
  getMensajeError(campo: string): string {
    const ctrl = this.expedienteForm.get(campo);
    if (!ctrl?.errors) return '';

    if (ctrl.errors['required']) return 'Campo obligatorio';
    if (ctrl.errors['soloNumeros']) return 'Solo se permiten números';
    if (ctrl.errors['minlength']) return `Mínimo ${ctrl.errors['minlength'].requiredLength} caracteres`;
    if (ctrl.errors['maxlength']) return `Máximo ${ctrl.errors['maxlength'].requiredLength} caracteres`;
    if (ctrl.errors['pattern']) {
      // Mensajes específicos por campo
      switch (campo) {
        case 'hc': return 'Solo números (4–12 dígitos)';
        case 'numeroCama': return 'Solo números';
        case 'medicoCMP': return 'Solo números, entre 4 y 6 dígitos';
        case 'medicoRNE': return 'Solo números, exactamente 5 dígitos';
        case 'medicoExternoCMP': return 'Solo números, entre 4 y 6 dígitos';

        default: return 'Formato inválido';
      }
    }
    if (ctrl.errors['formatoDocumento']) return ctrl.errors['formatoDocumento'];
    if (ctrl.errors['fechaFutura']) return 'La fecha de fallecimiento no puede ser futura';
    return 'Valor inválido';
  }
  soloNumerosKeydown(event: KeyboardEvent): boolean {
    return /[0-9]/.test(event.key) ||
      ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight'].includes(event.key);
  }
  /** Valida que la fecha/hora no sea futura. */
  private fechaNoFuturaValidator(): ValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value) return null;
      const fechaIngresada = new Date(control.value);
      const ahora = new Date();
      return fechaIngresada > ahora ? { fechaFutura: true } : null;
    };
  }
  // ===================================================================
  // PERTENENCIAS (FormArray)
  // ===================================================================
  get pertenencias(): FormArray {
    return this.expedienteForm.get('pertenencias') as FormArray;
  }

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
      switchMap(expediente => {
        return this.expedienteService.generarQR(expediente.expedienteID).pipe(
          switchMap(() =>
            this.expedienteService.imprimirBrazalete(expediente.expedienteID)
          ),
          switchMap(blob => {
            this.descargarPDF(blob, `Brazalete-${expediente.codigoExpediente}`);
            return [expediente];
          })
        );
      })
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
            </div>
          `,
          icon: 'success',
          confirmButtonText: 'Ir al Dashboard',
          confirmButtonColor: '#0891B2',
          timer: 5000,
          timerProgressBar: true
        }).then(() => this.router.navigate(['/dashboard']));
      },
      error: (err) => {
        this.isLoading = false;
        const msg = err.error?.message ?? 'Error al procesar el expediente.';
        Swal.fire({
          title: 'Error',
          text: msg,
          icon: 'error',
          confirmButtonColor: '#EF4444',
          confirmButtonText: 'Cerrar'
        });
      }
    });
  }

  // ===================================================================
  // HELPERS PRIVADOS
  // ===================================================================
  private descargarPDF(blob: Blob, nombreArchivo: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${nombreArchivo}.pdf`;
    link.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 100);
  }

  private mostrarAlertaInfo(mensaje: string): void {
    Swal.fire({ title: 'Información', text: mensaje, icon: 'info', confirmButtonColor: '#0891B2' });
  }

  private mostrarAdvertenciasIntegracion(advertencias: string[]): void {
    Swal.fire({
      title: 'Datos incompletos de SIGEM',
      html: `<ul class="text-left text-sm space-y-1">${advertencias.map(a => `<li>• ${a}</li>`).join('')}</ul>`,
      icon: 'warning',
      confirmButtonColor: '#F59E0B',
      confirmButtonText: 'Entendido'
    });
  }

  cancelar(): void {
    const origen = this.route.snapshot.queryParamMap.get('origen');
    this.router.navigate([origen === 'bandeja' ? '/bandeja-entrada' : '/dashboard']);
  }
}
