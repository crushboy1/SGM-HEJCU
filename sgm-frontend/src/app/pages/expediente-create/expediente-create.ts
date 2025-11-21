import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ExpedienteService, CreateExpedienteDTO } from '../../services/expediente';
import { IntegracionService, PacienteParaForm } from '../../services/integracion';
import { IconComponent } from '../../components/icon/icon.component';
import Swal from 'sweetalert2';

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
  errorMessage = '';
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
      // Datos demográficos (disabled por defecto, se habilitan en modo manual)
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

      // Datos del fallecimiento (editables)
      servicioFallecimiento: ['', Validators.required],
      numeroCama: [''],
      fechaHoraFallecimiento: [this.formatDateTimeLocal(new Date()), Validators.required],
      diagnosticoFinal: ['', Validators.required],
      medicoCertificaNombre: ['', Validators.required],
      medicoCMP: ['', Validators.required],
      medicoRNE: [''],
      numeroCertificadoSINADEF: [''],

      // Pertenencias (FormArray)
      pertenencias: this.fb.array([])
    });
  }

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit() {
    const hc = this.route.snapshot.paramMap.get('hc');

    if (hc) {
      // Pre-llenar desde Integración
      this.modoManual = false;
      this.isLoading = true;

      this.integracionService.consultarParaForm(hc).subscribe({
        next: (data) => {
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

          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = 'No se pudo cargar la información del paciente.';
          this.isLoading = false;
          console.error('Error en consultarParaForm:', err);
        }
      });
    } else {
      // MODO MANUAL: Habilitar campos demográficos
      this.modoManual = true;
      this.errorMessage = "Modo Manual: Por favor, ingrese todos los datos del paciente.";
      this.habilitarCamposDemograficos();
    }
  }

  // ===================================================================
  // HELPERS
  // ===================================================================

  /**
   * Habilita los campos demográficos para modo manual
   */
  private habilitarCamposDemograficos() {
    const camposDemograficos = [
      'hc', 'tipoDocumento', 'numeroDocumento', 'apellidoPaterno', 'apellidoMaterno',
      'nombres', 'fechaNacimiento', 'sexo', 'tipoSeguro'
    ];
    camposDemograficos.forEach(campo => this.expedienteForm.get(campo)?.enable());
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
      this.errorMessage = 'Formulario inválido. Revise los campos obligatorios.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.expedienteForm.getRawValue();

    // Construir DTO (Igual que antes)
    const dto: CreateExpedienteDTO = {
      hc: formValue.hc,
      tipoDocumento: formValue.tipoDocumento,
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

        console.log('1. Expediente creado. ID:', nuevoExpediente.expedienteID);

        // 2. GENERAR QR (Esto cambia el estado y crea la imagen)
        this.expedienteService.generarQR(nuevoExpediente.expedienteID).subscribe({
          next: (qrGenerado) => {
            console.log('2. QR Generado correctamente');

            // 3. IMPRIMIR BRAZALETE (Descargar PDF)
            this.expedienteService.imprimirBrazalete(nuevoExpediente.expedienteID).subscribe({
              next: (blob) => {
                // Descargar archivo
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = `Brazalete-${nuevoExpediente.codigoExpediente}.pdf`;
                link.click();
                window.URL.revokeObjectURL(url);

                // ÉXITO TOTAL
                this.isLoading = false;
                Swal.fire({
                  title: '¡Expediente Generado!',
                  text: `Se ha creado el expediente ${nuevoExpediente.codigoExpediente} y se ha descargado el brazalete.`,
                  icon: 'success',
                  confirmButtonText: 'Ir al Dashboard',
                  confirmButtonColor: '#0891B2'
                }).then(() => {
                  this.router.navigate(['/dashboard']);
                });
              },
              error: (errImp) => {
                // Falló la impresión, pero el expediente y QR existen
                this.isLoading = false;
                console.error('Error impresión:', errImp);
                Swal.fire('Atención', 'Expediente y QR creados, pero falló la descarga del PDF. Intente reimprimir desde el Dashboard.', 'warning')
                  .then(() => this.router.navigate(['/dashboard']));
              }
            });
          },
          error: (errQR) => {
            // Falló generar QR (Expediente existe pero en estado EnPiso)
            this.isLoading = false;
            console.error('Error generando QR:', errQR);
            Swal.fire('Error Parcial', 'El expediente se creó pero no se pudo generar el QR. Contacte a soporte.', 'warning')
              .then(() => this.router.navigate(['/dashboard']));
          }
        });
      },
      error: (err) => {
        // Falló crear expediente
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Error al conectar con la API';
        console.error('Error en POST /api/Expedientes:', err);
      }
    });
  }
  // Helper para descargar el archivo
  private descargarPDF(blob: Blob, nombreArchivo: string) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${nombreArchivo}.pdf`;
    link.click();
    window.URL.revokeObjectURL(url);
  }
  cancelar() {
    if (this.modoManual) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/bandeja-entrada']);
    }
  }
}
