import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import Swal from 'sweetalert2';

import { ExpedienteService, CreateExpedienteDTO } from '../../services/expediente';
import { IntegracionService, PacienteParaForm } from '../../services/integracion';
import { IconComponent } from '../../components/icon/icon.component';

@Component({
  selector: 'app-expediente-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent],
  templateUrl: './expediente-create.html',
  styleUrl: './expediente-create.css'
})
export class ExpedienteCreateComponent implements OnInit {

  // ===================================================================
  // INYECCIÓN DE DEPENDENCIAS
  // ===================================================================
  private fb = inject(FormBuilder);
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private integracionService = inject(IntegracionService);

  // ===================================================================
  // PROPIEDADES DEL COMPONENTE
  // ===================================================================
  expedienteForm: FormGroup;
  isLoading = false;
  pacienteData: PacienteParaForm | null = null;
  modoManual = false;

  servicios: string[] = [
    'Medicina Interna',
    'Cirugía General',
    'UCI',
    'UCINT',
    'Emergencia',
    'Trauma Shock'
  ];

  // ===================================================================
  // CONSTRUCTOR
  // ===================================================================
  constructor() {
    this.expedienteForm = this.fb.group({
      // Datos demográficos
      hc: [{ value: '', disabled: true }, Validators.required],
      tipoDocumento: [{ value: 1, disabled: true }, Validators.required],
      numeroDocumento: [{ value: '', disabled: true }, Validators.required],
      apellidoPaterno: [{ value: '', disabled: true }, Validators.required],
      apellidoMaterno: [{ value: '', disabled: true }, Validators.required],
      nombres: [{ value: '', disabled: true }, Validators.required],
      fechaNacimiento: [{ value: '', disabled: true }, Validators.required],
      sexo: [{ value: '', disabled: true }, Validators.required],
      tipoSeguro: [{ value: '', disabled: true }, Validators.required],

      // Tipo de expediente
      tipoExpediente: ['Interno', Validators.required],

      // Datos del fallecimiento
      servicioFallecimiento: ['', Validators.required],
      numeroCama: [''],
      fechaHoraFallecimiento: [this.formatDateTimeLocal(new Date()), Validators.required],
      diagnosticoFinal: ['', Validators.required],
      medicoCertificaNombre: ['', Validators.required],
      medicoCMP: ['', Validators.required],
      medicoRNE: [''],
      numeroCertificadoSINADEF: [''],

      // Pertenencias
      pertenencias: this.fb.array([])
    });
  }

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit() {
    // 1. Intentar recuperar datos del Router State (Desde Bandeja)
    const stateData = history.state?.pacientePreseleccionado;

    // 2. Obtener parámetros de URL
    const hcUrl = this.route.snapshot.queryParamMap.get('hc');
    const nombreUrl = this.route.snapshot.queryParamMap.get('nombre');

    if (stateData) {
      // CASO A: Venimos de la bandeja con el objeto completo
      console.log('📋 Pre-llenando desde State (Bandeja Universal)', stateData);
      this.cargarDatosDesdeState(stateData);
      this.modoManual = false;

    } else if (hcUrl) {
      // CASO B: Tenemos HC en la URL
      console.log('🔍 Consultando servicio de integración para HC:', hcUrl);
      this.modoManual = false;
      this.isLoading = true;

      this.integracionService.consultarParaForm(hcUrl).subscribe({
        next: (data) => {
          this.cargarDatosDesdeServicio(data);
          this.isLoading = false;
        },
        error: (err) => {
          console.warn('⚠️ Error consultando integración:', err);
          this.isLoading = false;

          // Fallback si falla el servicio
          if (nombreUrl) {
            this.mostrarAlertaInfo('No se pudo conectar con SIGEM. Se han cargado los datos básicos disponibles.');
            this.expedienteForm.patchValue({ hc: hcUrl });
            this.modoManual = true;
            this.habilitarCamposDemograficos();
          } else {
            this.mostrarAlertaInfo('No se pudo cargar la información. Por favor ingrese los datos manualmente.');
            this.modoManual = true;
            this.habilitarCamposDemograficos();
          }
        }
      });
    } else {
      // CASO C: Registro 100% Manual
      this.modoManual = true;
      this.habilitarCamposDemograficos();
    }
  }

  // ===================================================================
  // MÉTODOS DE CARGA DE DATOS
  // ===================================================================

  private cargarDatosDesdeServicio(data: PacienteParaForm) {
    this.pacienteData = data;
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
      tipoSeguro: data.fuenteFinanciamiento,
      servicioFallecimiento: data.servicioFallecimiento || '',
      numeroCama: data.numeroCama || '',
      fechaHoraFallecimiento: fechaFallecimiento,
      diagnosticoFinal: data.diagnosticoFinal || '',
      medicoCertificaNombre: data.medicoCertificaNombre || '',
      medicoCMP: data.medicoCMP || '',
      medicoRNE: data.medicoRNE || ''
    });
  }

  private cargarDatosDesdeState(item: any) {
    this.expedienteForm.patchValue({
      hc: item.hc || item.id,
      numeroDocumento: item.numeroDocumento,
      apellidoPaterno: item.apellidoPaterno || '',
      apellidoMaterno: item.apellidoMaterno || '',
      nombres: item.nombres || '',
      servicioFallecimiento: item.servicio || '',
      fechaHoraFallecimiento: item.fechaFallecimiento
        ? this.formatDateTimeLocal(new Date(item.fechaFallecimiento))
        : this.formatDateTimeLocal(new Date())
    });

    // Habilitar edición para completar lo que falte
    this.habilitarCamposDemograficos();
  }

  // ===================================================================
  // HELPERS
  // ===================================================================

  private habilitarCamposDemograficos() {
    const camposDemograficos = [
      'hc', 'tipoDocumento', 'numeroDocumento', 'apellidoPaterno', 'apellidoMaterno',
      'nombres', 'fechaNacimiento', 'sexo', 'tipoSeguro'
    ];
    camposDemograficos.forEach(campo => this.expedienteForm.get(campo)?.enable());
  }

  /**
   * ⭐ NUEVO: Helper para clases dinámicas de inputs
   * Evita repetir las mismas clases 9 veces en el HTML
   */
  getInputClasses(editable: boolean): string {
    return editable
      ? 'w-full p-2.5 border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-hospital-cyan focus:border-transparent outline-none transition-all'
      : 'w-full p-2.5 border-none rounded-lg bg-gray-100 text-gray-800 font-semibold cursor-not-allowed';
  }

  getTipoDocumentoNombre(id: number): string {
    switch (id) {
      case 1: return 'DNI';
      case 2: return 'Pasaporte';
      case 3: return 'Carnet de Extranjería';
      case 4: return 'Sin Documento';
      case 5: return 'NN';
      default: return 'Desconocido';
    }
  }

  private formatDateTimeLocal(date: Date): string {
    return date.toISOString().substring(0, 16);
  }

  // ===================================================================
  // PERTENENCIAS (FormArray)
  // ===================================================================

  get pertenencias(): FormArray {
    return this.expedienteForm.get('pertenencias') as FormArray;
  }

  agregarPertenencia() {
    const pertenenciaGroup = this.fb.group({
      descripcion: ['', Validators.required],
      observaciones: ['']
    });
    this.pertenencias.push(pertenenciaGroup);
  }

  eliminarPertenencia(index: number) {
    this.pertenencias.removeAt(index);
  }

  // ===================================================================
  // SUBMIT
  // ===================================================================

  onSubmit() {
    this.expedienteForm.markAllAsTouched();

    if (this.expedienteForm.invalid) {
      Swal.fire({
        title: 'Formulario Inválido',
        text: 'Por favor, revise los campos obligatorios marcados en rojo.',
        icon: 'warning',
        confirmButtonColor: '#EF4444',
        confirmButtonText: 'Aceptar'
      });
      return;
    }

    this.isLoading = true;

    const formValue = this.expedienteForm.getRawValue();

    const dto: CreateExpedienteDTO = {
      hc: formValue.hc,
      tipoDocumento: Number(formValue.tipoDocumento),
      numeroDocumento: formValue.numeroDocumento,
      apellidoPaterno: formValue.apellidoPaterno,
      apellidoMaterno: formValue.apellidoMaterno,
      nombres: formValue.nombres,
      fechaNacimiento: formValue.fechaNacimiento,
      sexo: formValue.sexo,
      tipoSeguro: formValue.tipoSeguro,
      tipoExpediente: formValue.tipoExpediente,
      servicioFallecimiento: formValue.servicioFallecimiento,
      numeroCama: formValue.numeroCama || null,
      fechaHoraFallecimiento: formValue.fechaHoraFallecimiento,
      diagnosticoFinal: formValue.diagnosticoFinal,
      medicoCertificaNombre: formValue.medicoCertificaNombre,
      medicoCMP: formValue.medicoCMP,
      medicoRNE: formValue.medicoRNE || null,
      numeroCertificadoSINADEF: formValue.numeroCertificadoSINADEF || null,
      pertenencias: formValue.pertenencias || []
    };

    // 1. CREAR EXPEDIENTE
    this.expedienteService.create(dto).subscribe({
      next: (nuevoExpediente) => {
        console.log('✅ 1. Expediente creado. ID:', nuevoExpediente.expedienteID);

        // 2. GENERAR QR
        this.expedienteService.generarQR(nuevoExpediente.expedienteID).subscribe({
          next: (qrGenerado) => {
            console.log('✅ 2. QR Generado correctamente');

            // 3. IMPRIMIR BRAZALETE
            this.expedienteService.imprimirBrazalete(nuevoExpediente.expedienteID).subscribe({
              next: (blob) => {
                this.descargarPDF(blob, `Brazalete-${nuevoExpediente.codigoExpediente}`);

                this.isLoading = false;
                Swal.fire({
                  title: '¡Expediente Generado!',
                  html: `
                    <div class="text-left space-y-2">
                      <p>✅ Expediente: <strong>${nuevoExpediente.codigoExpediente}</strong></p>
                      <p>✅ QR generado correctamente</p>
                      <p>✅ Brazalete descargado</p>
                    </div>
                  `,
                  icon: 'success',
                  confirmButtonText: 'Ir al Dashboard',
                  confirmButtonColor: '#0891B2',
                  timer: 5000,
                  timerProgressBar: true
                }).then(() => {
                  this.router.navigate(['/dashboard']);
                });
              },
              error: (errImp) => {
                this.isLoading = false;
                console.error('❌ Error impresión:', errImp);
                Swal.fire({
                  title: 'Atención',
                  text: 'Expediente y QR creados, pero falló la descarga del brazalete.',
                  icon: 'warning',
                  confirmButtonColor: '#F59E0B'
                }).then(() => this.router.navigate(['/dashboard']));
              }
            });
          },
          error: (errQR) => {
            this.isLoading = false;
            console.error('❌ Error generando QR:', errQR);
            Swal.fire({
              title: 'Error Parcial',
              text: 'El expediente se creó pero no se pudo generar el QR.',
              icon: 'warning',
              confirmButtonColor: '#F59E0B'
            }).then(() => this.router.navigate(['/dashboard']));
          }
        });
      },
      error: (err) => {
        this.isLoading = false;
        const msg = err.error?.message || 'Error al crear el expediente. Verifique los datos.';
        console.error('❌ Error en POST /api/Expedientes:', err);

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

  private descargarPDF(blob: Blob, nombreArchivo: string) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${nombreArchivo}.pdf`;
    link.click();
    setTimeout(() => window.URL.revokeObjectURL(url), 100);
  }

  private mostrarAlertaInfo(mensaje: string) {
    Swal.fire({
      title: 'Información',
      text: mensaje,
      icon: 'info',
      confirmButtonColor: '#0891B2'
    });
  }

  cancelar() {
    if (this.modoManual) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/bandeja-entrada']);
    }
  }
}
