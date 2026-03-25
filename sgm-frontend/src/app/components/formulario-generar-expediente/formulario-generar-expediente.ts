import {
  Component, Input, Output, EventEmitter,
  OnInit, OnDestroy, inject, ChangeDetectorRef
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, FormArray } from '@angular/forms';
import { Subject, takeUntil, switchMap } from 'rxjs';
import Swal from 'sweetalert2';

import { ExpedienteService, CreateExpedienteDTO } from '../../services/expediente';
import { IntegracionService, PacienteParaForm } from '../../services/integracion';
import { IconComponent } from '../icon/icon.component';
import { SoloNumerosDirective } from '../../shared/directives/solo-numeros';

// ===================================================================
// ENUMS — alineados con backend
// ===================================================================
export enum TipoIngreso { Interno = 1, Externo = 2 }
export enum FuenteFinanciamiento {
  SIS = 1, EsSalud = 2, Particular = 3, SOAT = 4, PendientePago = 5, Otros = 6
}
export enum TipoDocumentoIdentidad {
  DNI = 1, Pasaporte = 2, CarneExtranjeria = 3, SinDocumento = 4, NN = 5
}

@Component({
  selector: 'app-formulario-generar-expediente',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent, DatePipe, SoloNumerosDirective],
  templateUrl: './formulario-generar-expediente.html',
  styleUrl: './formulario-generar-expediente.css'
})
export class FormularioGenerarExpediente implements OnInit, OnDestroy {

  // ── Inyección ────────────────────────────────────────────────────
  private fb = inject(FormBuilder);
  private expedienteService = inject(ExpedienteService);
  private integracionService = inject(IntegracionService);
  private cd = inject(ChangeDetectorRef);
  private destroy$ = new Subject<void>();

  // ── Inputs / Outputs ─────────────────────────────────────────────
  @Input() hc: string | null = null;
  @Output() expedienteCreado = new EventEmitter<string>();
  @Output() cancelar = new EventEmitter<void>();

  // ── Estado ───────────────────────────────────────────────────────
  form!: FormGroup;
  isLoading = false;
  cargandoDatos = false;
  modoManual = false;
  datosGalenhosDisponibles = false;

  /**
   * Bandera que evita que los listeners de valueChanges
   * sobreescriban el form durante la carga de datos de integración.
   */
  private actualizandoDesdeCarga = false;

  readonly maxFechaHoy: string = new Date().toISOString().substring(0, 10);
  readonly maxFechaHoraActual: string = new Date().toISOString().substring(0, 16);

  // ── Catálogos ────────────────────────────────────────────────────
  readonly tiposDocumento = [
    { id: TipoDocumentoIdentidad.DNI, label: 'DNI' },
    { id: TipoDocumentoIdentidad.Pasaporte, label: 'Pasaporte' },
    { id: TipoDocumentoIdentidad.CarneExtranjeria, label: 'Carné de Extranjería' },
    { id: TipoDocumentoIdentidad.SinDocumento, label: 'Sin Documento' },
    { id: TipoDocumentoIdentidad.NN, label: 'NN (No Identificado)' },
  ];

  readonly fuentesFinanciamiento = [
    { id: FuenteFinanciamiento.SIS, label: 'SIS' },
    { id: FuenteFinanciamiento.EsSalud, label: 'EsSalud' },
    { id: FuenteFinanciamiento.Particular, label: 'Particular' },
    { id: FuenteFinanciamiento.SOAT, label: 'SOAT' },
    { id: FuenteFinanciamiento.PendientePago, label: 'Pendiente de Pago' },
    { id: FuenteFinanciamiento.Otros, label: 'Otros' },
  ];

  readonly servicios = [
    'Medicina Interna', 'Cirugía General', 'Cirugía 4A', 'UCI', 'UCINT',
    'Emergencia', 'Trauma Shock', 'Traumatología', 'Neurocirugía',
    'Sala de Recuperación', 'Observaciones', 'UVE', 'Otro'
  ];

  // ── Getters ───────────────────────────────────────────────────────
  /** Acceso directo a controls — f.campo en template */
  get f() { return this.form.controls; }

  /** Raw values — para campos disabled en modo lectura */
  get rv() { return this.form?.getRawValue?.() ?? {}; }

  get medicoExternoHabilitado(): boolean {
    return Number(this.f['tipoExpediente'].value) === TipoIngreso.Externo
      && !this.f['causaViolentaODudosa'].value;
  }

  get fuenteLabel(): string {
    const id = Number(this.form?.getRawValue()?.fuenteFinanciamiento);
    return this.fuentesFinanciamiento.find(f => f.id === id)?.label ?? 'Pendiente de Pago';
  }

  get pertenencias(): FormArray {
    return this.form.get('pertenencias') as FormArray;
  }

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  ngOnInit(): void {
    this.buildForm();
    this.suscribirCambiosCondicionales();

    if (this.hc) {
      this.cargandoDatos = true;
      this.integracionService.consultarPaciente(this.hc)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: data => {
            this.cargarDatosDesdeServicio(data);
            this.cargandoDatos = false;
          },
          error: err => {
            console.warn('[FormularioGenerarExpediente] Error integración:', err);
            this.cargandoDatos = false;
            this.modoManual = true;
            this.form.patchValue({ hc: this.hc });
            this.mostrarAlertaIntegracion(
              'No se pudo conectar con SIGEM/Galenhos. Ingrese los datos manualmente.'
            );
          }
        });
    } else {
      this.modoManual = true;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===================================================================
  // CONSTRUCCIÓN DEL FORM
  // Todos los campos nacen habilitados.
  // Los demográficos se deshabilitan después del patch si vienen de Galenhos.
  // ===================================================================
  private buildForm(): void {
    this.form = this.fb.group({
      // Demográficos
      hc: ['', Validators.required],
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

      // Tipo expediente
      tipoExpediente: [TipoIngreso.Interno, Validators.required],

      // Fallecimiento
      servicioFallecimiento: ['', Validators.required],
      numeroCama: [''],
      fechaHoraFallecimiento: [this.formatDateTimeLocal(new Date()), Validators.required],
      diagnosticoFinal: [''],

      // Médico certificante
      medicoCertificaNombre: ['', Validators.required],
      medicoCMP: ['', [Validators.required, Validators.pattern('^[0-9]{4,6}$')]],
      medicoRNE: ['', Validators.pattern('^[0-9]{5}$')],

      // Médico externo
      medicoExternoNombre: [''],
      medicoExternoCMP: ['', Validators.pattern('^[0-9]{4,6}$')],

      // Observaciones y pertenencias
      observaciones: ['', Validators.maxLength(1000)],
      pertenencias: this.fb.array([])
    });
  }

  // ===================================================================
  // SUSCRIPCIONES CONDICIONALES
  // ===================================================================
  private suscribirCambiosCondicionales(): void {
    this.form.get('tipoExpediente')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.actualizarMedicoExterno());

    this.form.get('causaViolentaODudosa')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.actualizarMedicoExterno());

    this.form.get('esNN')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((esNN: boolean) => this.actualizarCamposNN(esNN));
  }

  private actualizarMedicoExterno(): void {
    if (this.actualizandoDesdeCarga) return;
    const esExterno = Number(this.f['tipoExpediente'].value) === TipoIngreso.Externo;
    const esViolenta = this.f['causaViolentaODudosa'].value === true;
    if (!esExterno || esViolenta) {
      this.form.patchValue(
        { medicoExternoNombre: '', medicoExternoCMP: '' },
        { emitEvent: false }
      );
    }
  }

  private actualizarCamposNN(esNN: boolean): void {
    // Guardia: no ejecutar durante carga ni en modo readonly
    if (this.actualizandoDesdeCarga || !this.modoManual) return;

    if (esNN) {
      this.form.patchValue({
        tipoDocumento: TipoDocumentoIdentidad.NN,
        numeroDocumento: '',
        apellidoPaterno: 'NN',
        apellidoMaterno: 'NN',
        nombres: 'No Identificado',
        fechaNacimiento: '1900-01-01'
      }, { emitEvent: false });
    } else {
      this.form.patchValue({
        tipoDocumento: TipoDocumentoIdentidad.DNI,
        numeroDocumento: '',
        apellidoPaterno: '',
        apellidoMaterno: '',
        nombres: '',
        fechaNacimiento: ''
      }, { emitEvent: false });
    }
  }

  // ===================================================================
  // CARGA DESDE INTEGRACIÓN
  // ===================================================================
  private cargarDatosDesdeServicio(data: PacienteParaForm): void {
    this.datosGalenhosDisponibles = data.existeEnGalenhos;

    const fechaNac = data.fechaNacimiento?.substring(0, 10) ?? '';
    const fechaFallecimiento = data.fechaHoraFallecimiento
      ? this.formatDateTimeLocal(new Date(data.fechaHoraFallecimiento))
      : this.formatDateTimeLocal(new Date());

    // Activar bandera ANTES del patch — bloquea todos los listeners
    this.actualizandoDesdeCarga = true;

    this.form.patchValue({
      hc: data.hc,
      tipoDocumento: data.tipoDocumentoID,
      numeroDocumento: data.numeroDocumento,
      apellidoPaterno: data.apellidoPaterno,
      apellidoMaterno: data.apellidoMaterno,
      nombres: data.nombres,
      fechaNacimiento: fechaNac,
      sexo: data.sexo,
      fuenteFinanciamiento: this.mapFuente(data.fuenteFinanciamiento),
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
      this.deshabilitarDemograficos();
    } else {
      this.modoManual = true;
    }

    this.form.updateValueAndValidity();
    setTimeout(() => this.cd.detectChanges());

    if (data.advertencias?.length > 0) {
      this.mostrarAdvertenciasIntegracion(data.advertencias);
    }
  }

  // ===================================================================
  // HELPERS DE FORM
  // ===================================================================

  private deshabilitarDemograficos(): void {
    ['hc', 'tipoDocumento', 'numeroDocumento', 'apellidoPaterno',
      'apellidoMaterno', 'nombres', 'fechaNacimiento', 'sexo', 'fuenteFinanciamiento']
      .forEach(c => this.form.get(c)?.disable());
  }

  private mapFuente(valor: string): number {
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

  private formatDateTimeLocal(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  getTipoDocumentoLabel(id: number): string {
    return this.tiposDocumento.find(t => t.id === id)?.label ?? '';
  }

  getSexoLabel(valor: string): string {
    return valor === 'M' ? 'Masculino' : valor === 'F' ? 'Femenino' : valor;
  }

  /**
   * Valida si un campo es inválido y fue tocado.
   * Nombre estandarizado con el resto del sistema (isInvalidField).
   */
  isInvalidField(campo: string): boolean {
    const ctrl = this.form.get(campo);
    return !!ctrl?.invalid && (ctrl.dirty || ctrl.touched);
  }

  getMensajeError(campo: string): string {
    const errors = this.form.get(campo)?.errors;
    if (!errors) return '';
    if (errors['required']) return 'Campo obligatorio';
    if (errors['maxlength']) return `Máximo ${errors['maxlength'].requiredLength} caracteres`;
    if (errors['pattern']) {
      if (campo === 'medicoCMP' || campo === 'medicoExternoCMP')
        return 'Solo números, 4 a 6 dígitos';
      if (campo === 'medicoRNE')
        return 'Solo números, exactamente 5 dígitos';
      return 'Formato inválido';
    }
    return 'Valor inválido';
  }

  /**
   * trackBy para ngFor de catálogos con id numérico.
   * Mejora rendimiento en re-renders (este form usa .id, no .value).
   */
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

  eliminarPertenencia(i: number): void {
    this.pertenencias.removeAt(i);
  }

  // ===================================================================
  // SUBMIT — crear → generar QR → imprimir brazalete
  // ===================================================================
  onSubmit(): void {
    if (this.isLoading) return;
    this.form.markAllAsTouched();

    if (this.form.invalid) {
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
    const raw = this.form.getRawValue();

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
      switchMap(exp =>
        this.expedienteService.generarQR(exp.expedienteID).pipe(
          switchMap(() =>
            this.expedienteService.imprimirBrazalete(exp.expedienteID)
          ),
          switchMap(blob => {
            this.descargarPDF(blob, `Brazalete-${exp.codigoExpediente}`);
            return [exp];
          })
        )
      )
    ).subscribe({
      next: (exp: any) => {
        this.isLoading = false;
        Swal.fire({
          title: 'Expediente Generado',
          html: `<div class="text-left space-y-1 text-sm">
            <p>Expediente: <strong>${exp.codigoExpediente}</strong></p>
            <p>QR generado correctamente</p>
            <p>Brazalete descargado</p></div>`,
          icon: 'success',
          confirmButtonText: 'Aceptar',
          confirmButtonColor: '#0891B2',
          timer: 4000,
          timerProgressBar: true
        }).then(() => this.expedienteCreado.emit(exp.codigoExpediente));
      },
      error: err => {
        this.isLoading = false;
        Swal.fire({
          title: 'Error',
          text: err.error?.message ?? 'Error al procesar el expediente.',
          icon: 'error',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  onCancelar(): void {
    if (this.form.dirty) {
      Swal.fire({
        icon: 'warning',
        title: '¿Cancelar registro?',
        text: 'Se perderán los datos ingresados.',
        showCancelButton: true,
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'Continuar',
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280'
      }).then(r => { if (r.isConfirmed) this.cancelar.emit(); });
    } else {
      this.cancelar.emit();
    }
  }

  private descargarPDF(blob: Blob, nombre: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url; link.download = `${nombre}.pdf`; link.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 100);
  }

  private mostrarAlertaIntegracion(mensaje: string): void {
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
