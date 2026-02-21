import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../icon/icon.component';
import { ActaRetiroService } from '../../services/acta-retiro';
import { AuthService } from '../../services/auth';
import { Expediente } from '../../services/expediente';
import { CreateActaRetiroDTO, ActaRetiroDTO } from '../../services/acta-retiro';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import Swal from 'sweetalert2';

/**
 * Formulario para Crear Acta de Retiro (Familiar o AutoridadLegal)
 */
@Component({
  selector: 'app-formulario-acta-retiro',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './formulario-acta-retiro.html',
  styleUrl: './formulario-acta-retiro.css'
})
export class FormularioActaRetiroComponent implements OnInit {
  // ===================================================================
  // DEPENDENCY INJECTION
  // ===================================================================
  private actaRetiroService = inject(ActaRetiroService);
  private authService = inject(AuthService);
  private certificadoSubject = new Subject<string>();
  private oficioSubject = new Subject<string>();

  // ===================================================================
  // INPUTS Y OUTPUTS
  // ===================================================================
  @Input() expediente!: Expediente;
  @Output() actaCreada = new EventEmitter<ActaRetiroDTO>();
  @Output() onCancelar = new EventEmitter<void>();

  // ===================================================================
  // STATE
  // ===================================================================
  isSubmitting = false;

  // FORM DATA
  formData = {
    // Datos del fallecido (readonly)
    hc: '',
    dniPaciente: '',
    nombreCompletoPaciente: '',
    servicioFallecimiento: '',

    // Tipo de Salida
    tipoSalida: 'Familiar' as 'Familiar' | 'AutoridadLegal',

    // CERTIFICADO/OFICIO
    numeroCertificadoDefuncion: '',
    numeroOficioLegal: '',

    // Médico certificante (readonly)
    nombreMedicoCertificante: '',
    cmpMedicoCertificante: '',
    rneMedicoCertificante: '',

    // Jefe de Guardia
    nombreJefeGuardia: '',
    cmpJefeGuardia: '',

    // FAMILIAR RESPONSABLE (solo si tipoSalida = Familiar)
    nombreFamiliar: '',
    tipoDocumentoFamiliar: 1,
    dniFamiliar: '',
    parentesco: '',
    telefonoFamiliar: '',

    // AUTORIDAD LEGAL (solo si tipoSalida = AutoridadLegal)
    tipoAutoridad: 1,
    tipoDocumentoAutoridad: 1,
    nombreAutoridad: '',
    documentoAutoridad: '',
    cargoAutoridad: '',
    institucionAutoridad: '',
    placaVehiculoAutoridad: '',
    telefonoAutoridad: '',

    // Datos adicionales (opcionales)
    destinoCuerpo: '',
    observaciones: ''
  };

  // ===================================================================
  // VALIDATION ERRORS
  // ===================================================================
  errors: Record<string, string> = {};

  // ===================================================================
  // CATÁLOGOS / ENUMS
  // ===================================================================
  readonly tiposDocumento = [
    { value: 1, label: 'DNI' },
    { value: 2, label: 'Carnet de Extranjería' },
    { value: 3, label: 'Pasaporte' },
    { value: 4, label: 'Registro NN' }
  ];

  readonly tiposDocumentoFamiliar = [
    { value: 1, label: 'DNI' },
    { value: 2, label: 'Carnet de Extranjería' },
    { value: 3, label: 'Pasaporte' }
  ];

  readonly tiposAutoridad = [
    { value: 1, label: 'Policía Nacional del Perú (PNP)' },
    { value: 2, label: 'Ministerio Público - Fiscalía' },
    { value: 3, label: 'Médico Legista' }
  ];

  // ===================================================================
  // LIFECYCLE
  // ===================================================================
  ngOnInit(): void {
    if (!this.expediente) {
      console.error('❌ FormularioActaRetiro: No se recibió expediente');
      return;
    }

    this.preLlenarDatosExpediente();
    this.inicializarValidacionDocumentos();
  }

  // ═══════════════════════════════════════════════════════════
  // VALIDACIÓN EN TIEMPO REAL DE DOCUMENTOS
  // ═══════════════════════════════════════════════════════════

  /**
   * Inicializa la validación en tiempo real de certificados y oficios
   */
  private inicializarValidacionDocumentos(): void {
    // Validar certificado SINADEF (con debounce)
    this.certificadoSubject
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap(numeroCertificado =>
          this.actaRetiroService.verificarCertificadoSINADEF(numeroCertificado)
        )
      )
      .subscribe({
        next: (existe) => {
          if (existe) {
            this.errors['numeroCertificadoDefuncion'] =
              'Este certificado SINADEF ya está registrado en otra acta';
          } else {
            this.clearError('numeroCertificadoDefuncion');
          }
        },
        error: (err) => {
          console.error('❌ Error al verificar certificado SINADEF:', err);
        }
      });

    // Validar oficio legal (con debounce)
    this.oficioSubject
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap(numeroOficio =>
          this.actaRetiroService.verificarOficioLegal(numeroOficio)
        )
      )
      .subscribe({
        next: (existe) => {
          if (existe) {
            this.errors['numeroOficioLegal'] =
              'Este número de oficio ya está registrado en otra acta';
          } else {
            this.clearError('numeroOficioLegal');
          }
        },
        error: (err) => {
          console.error('❌ Error al verificar oficio legal:', err);
        }
      });
  }

  /**
   * Valida certificado SINADEF mientras el usuario escribe
   */
  onCertificadoSINADEFChange(value: string): void {
    if (value.trim().length >= 10) {
      this.certificadoSubject.next(value.trim());
    }
  }

  /**
   * Valida oficio legal mientras el usuario escribe
   */
  onOficioLegalChange(value: string): void {
    if (value.trim().length >= 5) {
      this.oficioSubject.next(value.trim());
    }
  }
  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  private preLlenarDatosExpediente(): void {
    this.formData.hc = this.expediente.hc;
    this.formData.dniPaciente = this.expediente.numeroDocumento || '';
    this.formData.nombreCompletoPaciente = this.expediente.nombreCompleto;
    this.formData.servicioFallecimiento = this.expediente.servicioFallecimiento || '';
    this.formData.numeroCertificadoDefuncion = this.expediente.numeroCertificadoSINADEF || '';

    this.formData.nombreMedicoCertificante = this.expediente.medicoCertificaNombre || '';
    this.formData.cmpMedicoCertificante = this.expediente.medicoCertificaCMP || '';
    this.formData.rneMedicoCertificante = this.expediente.medicoCertificaRNE || '';

    console.log('✅ Datos del expediente pre-llenados:', this.formData);
  }

  // ===================================================================
  // VALIDACIÓN
  // ===================================================================
  private validarFormulario(): boolean {
    this.errors = {};

    // Validar Certificado/Oficio según tipo
    if (this.formData.tipoSalida === 'Familiar') {
      if (!this.formData.numeroCertificadoDefuncion.trim()) {
        this.errors['numeroCertificadoDefuncion'] = 'El N° de Certificado SINADEF es requerido';
      }
    } else if (this.formData.tipoSalida === 'AutoridadLegal') {
      if (!this.formData.numeroOficioLegal.trim()) {
        this.errors['numeroOficioLegal'] = 'El N° de Oficio Legal es requerido';
      }
    }

    // Validar Jefe de Guardia (siempre obligatorio)
    if (!this.formData.nombreJefeGuardia.trim()) {
      this.errors['nombreJefeGuardia'] = 'El nombre del Jefe de Guardia es requerido';
    }

    if (!this.formData.cmpJefeGuardia.trim()) {
      this.errors['cmpJefeGuardia'] = 'El CMP del Jefe de Guardia es requerido';
    } else if (!/^\d{6}$/.test(this.formData.cmpJefeGuardia)) {
      this.errors['cmpJefeGuardia'] = 'El CMP debe tener 6 dígitos';
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAR SEGÚN TIPO DE SALIDA
    // ═══════════════════════════════════════════════════════════

    if (this.formData.tipoSalida === 'Familiar') {
      // Validar Familiar
      if (!this.formData.nombreFamiliar.trim()) {
        this.errors['nombreFamiliar'] = 'El nombre del familiar es requerido';
      }
      if (!this.formData.tipoDocumentoFamiliar) {
        this.errors['tipoDocumentoFamiliar'] = 'Seleccione el tipo de documento';
      }
      if (!this.formData.dniFamiliar.trim()) {
        this.errors['dniFamiliar'] = 'El documento del familiar es requerido';
      } else if (this.formData.tipoDocumentoFamiliar === 1 && !/^\d{8}$/.test(this.formData.dniFamiliar)) {
        this.errors['dniFamiliar'] = 'El DNI debe tener 8 dígitos';
      }

      if (!this.formData.parentesco.trim()) {
        this.errors['parentesco'] = 'El parentesco es requerido';
      }

      // Teléfono opcional para Familiar
      if (this.formData.telefonoFamiliar.trim() && !/^\d{9}$/.test(this.formData.telefonoFamiliar)) {
        this.errors['telefonoFamiliar'] = 'El teléfono debe tener 9 dígitos';
      }

    } else if (this.formData.tipoSalida === 'AutoridadLegal') {
      // Validar Autoridad Legal
      if (!this.formData.tipoAutoridad) {
        this.errors['tipoAutoridad'] = 'Seleccione el tipo de autoridad';
      }

      if (!this.formData.tipoDocumentoAutoridad) {
        this.errors['tipoDocumentoAutoridad'] = 'Seleccione el tipo de documento';
      }

      if (!this.formData.nombreAutoridad.trim()) {
        this.errors['nombreAutoridad'] = 'El nombre de la autoridad es requerido';
      }

      if (!this.formData.documentoAutoridad.trim()) {
        this.errors['documentoAutoridad'] = 'El documento de identidad es requerido';
      } else if (this.formData.tipoDocumentoAutoridad === 1 && !/^\d{8}$/.test(this.formData.documentoAutoridad)) {
        this.errors['documentoAutoridad'] = 'El DNI debe tener 8 dígitos';
      }

      if (!this.formData.cargoAutoridad.trim()) {
        this.errors['cargoAutoridad'] = 'El cargo es requerido';
      }

      if (!this.formData.institucionAutoridad.trim()) {
        this.errors['institucionAutoridad'] = 'La institución es requerida';
      }

      // Placa y teléfono opcionales para Autoridad
      if (this.formData.telefonoAutoridad.trim() && !/^\d{9}$/.test(this.formData.telefonoAutoridad)) {
        this.errors['telefonoAutoridad'] = 'El teléfono debe tener 9 dígitos';
      }
    }

    return Object.keys(this.errors).length === 0;
  }

  clearError(campo: string): void {
    if (this.errors[campo]) {
      delete this.errors[campo];
    }
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================
  async crear(): Promise<void> {
    if (!this.validarFormulario()) {
      Swal.fire({
        icon: 'warning',
        title: 'Formulario Incompleto',
        text: 'Por favor, corrija los errores antes de continuar',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    // Confirmación con resumen CONDICIONAL
    const confirmacion = await Swal.fire({
      icon: 'question',
      title: 'Confirmar Creación de Acta',
      html: this.generarHTMLConfirmacion(),
      showCancelButton: true,
      confirmButtonText: 'Sí, crear acta',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    });

    if (!confirmacion.isConfirmed) return;

    this.ejecutarCreacion();
  }

  /**
   * Genera HTML de confirmación según tipo de salida
   */
  private generarHTMLConfirmacion(): string {
    let html = `
      <div class="text-left text-sm space-y-2">
        <p class="font-semibold text-gray-800">Paciente fallecido:</p>
        <p class="text-gray-600">${this.formData.nombreCompletoPaciente}</p>
    `;

    if (this.formData.tipoSalida === 'Familiar') {
      html += `
        <p class="font-semibold text-gray-800 mt-3">Familiar responsable:</p>
        <p class="text-gray-600">${this.formData.nombreFamiliar}</p>
        <p class="text-gray-500 text-xs">DNI: ${this.formData.dniFamiliar} - ${this.formData.parentesco}</p>
      `;
    } else {
      html += `
        <p class="font-semibold text-gray-800 mt-3">Autoridad Legal:</p>
        <p class="text-gray-600">${this.formData.nombreAutoridad}</p>
        <p class="text-gray-500 text-xs">${this.formData.cargoAutoridad} - ${this.formData.institucionAutoridad}</p>
      `;
    }

    html += `
        <p class="font-semibold text-gray-800 mt-3">Jefe de Guardia:</p>
        <p class="text-gray-600">${this.formData.nombreJefeGuardia}</p>
        <p class="text-gray-500 text-xs">CMP: ${this.formData.cmpJefeGuardia}</p>
      </div>
    `;

    return html;
  }

  /**
   * Ejecuta la creación del acta en el backend
   */
  private ejecutarCreacion(): void {
    this.isSubmitting = true;

    const usuarioId = this.authService.getUserId();

    const dto: CreateActaRetiroDTO = {
      expedienteID: this.expediente.expedienteID,

      // Tipo de salida
      tipoSalida: this.formData.tipoSalida,

      // Documento legal (condicional)
      numeroCertificadoDefuncion: this.formData.tipoSalida === 'Familiar'
        ? this.formData.numeroCertificadoDefuncion
        : undefined,
      numeroOficioLegal: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.numeroOficioLegal
        : undefined,

      // Datos del fallecido
      nombreCompletoFallecido: this.formData.nombreCompletoPaciente,
      historiaClinica: this.formData.hc,
      tipoDocumentoFallecido: this.mapearTipoDocumento(this.expediente.tipoDocumento || 'DNI'),
      numeroDocumentoFallecido: this.formData.dniPaciente,
      servicioFallecimiento: this.formData.servicioFallecimiento,
      fechaHoraFallecimiento: this.expediente.fechaHoraFallecimiento,

      // Médico certificante
      medicoCertificaNombre: this.formData.nombreMedicoCertificante,
      medicoCMP: this.formData.cmpMedicoCertificante,
      medicoRNE: this.formData.rneMedicoCertificante || undefined,

      // Jefe de Guardia
      jefeGuardiaNombre: this.formData.nombreJefeGuardia,
      jefeGuardiaCMP: this.formData.cmpJefeGuardia,

      // ═══════════════════════════════════════════════════════════
      // FAMILIAR (solo si tipoSalida = Familiar)
      // ═══════════════════════════════════════════════════════════
      familiarApellidoPaterno: this.formData.tipoSalida === 'Familiar'
        ? this.extraerApellidoPaterno(this.formData.nombreFamiliar)
        : undefined,
      familiarApellidoMaterno: this.formData.tipoSalida === 'Familiar'
        ? this.extraerApellidoMaterno(this.formData.nombreFamiliar)
        : undefined,
      familiarNombres: this.formData.tipoSalida === 'Familiar'
        ? this.extraerNombres(this.formData.nombreFamiliar)
        : undefined,
      familiarTipoDocumento: this.formData.tipoSalida === 'Familiar'
        ? this.formData.tipoDocumentoFamiliar
        : undefined,
      familiarNumeroDocumento: this.formData.tipoSalida === 'Familiar'
        ? this.formData.dniFamiliar
        : undefined,
      familiarParentesco: this.formData.tipoSalida === 'Familiar'
        ? this.formData.parentesco
        : undefined,
      familiarTelefono: this.formData.tipoSalida === 'Familiar'
        ? this.formData.telefonoFamiliar || undefined
        : undefined,

      // ═══════════════════════════════════════════════════════════
      // AUTORIDAD LEGAL (solo si tipoSalida = AutoridadLegal)
      // ═══════════════════════════════════════════════════════════
      autoridadApellidoPaterno: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerApellidoPaterno(this.formData.nombreAutoridad)
        : undefined,
      autoridadApellidoMaterno: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerApellidoMaterno(this.formData.nombreAutoridad)
        : undefined,
      autoridadNombres: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerNombres(this.formData.nombreAutoridad)
        : undefined,
      tipoAutoridad: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.tipoAutoridad
        : undefined,
      autoridadTipoDocumento: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.tipoDocumentoAutoridad
        : undefined,
      autoridadNumeroDocumento: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.documentoAutoridad
        : undefined,
      autoridadCargo: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.cargoAutoridad
        : undefined,
      autoridadInstitucion: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.institucionAutoridad
        : undefined,
      autoridadPlacaVehiculo: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.placaVehiculoAutoridad || undefined
        : undefined,
      autoridadTelefono: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.telefonoAutoridad || undefined
        : undefined,

      // Datos adicionales
      datosAdicionales: undefined,
      destino: this.formData.destinoCuerpo || undefined,
      observaciones: this.formData.observaciones || undefined,

      // Usuario
      usuarioAdmisionID: usuarioId
    };

    console.log('📤 Enviando DTO al backend:', dto);

    this.actaRetiroService.crear(dto).subscribe({
      next: (actaCreada) => {
        this.isSubmitting = false;

        Swal.fire({
          icon: 'success',
          title: 'Acta Creada Exitosamente',
          text: 'El acta de retiro ha sido registrada correctamente',
          showConfirmButton: true
        });

        this.actaCreada.emit(actaCreada);
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('❌ Error al crear acta:', err);
        console.error('📋 DTO enviado:', dto);

        Swal.fire({
          icon: 'error',
          title: 'Error al Crear Acta',
          text: err.error?.mensaje || 'No se pudo crear el acta. Intente nuevamente.',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  private mapearTipoDocumento(tipo: string): number {
    const mapeo: Record<string, number> = {
      'DNI': 1,
      'CE': 2,
      'Pasaporte': 3,
      'RUC': 4,
      'NN': 5
    };
    return mapeo[tipo] || 1;
  }

  async cancelarFormulario(): Promise<void> {
    const hayCambios = this.formHaCambiado();

    if (hayCambios) {
      const confirmacion = await Swal.fire({
        icon: 'warning',
        title: '¿Cancelar Creación?',
        text: 'Los datos ingresados se perderán',
        showCancelButton: true,
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'Seguir editando',
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280'
      });

      if (!confirmacion.isConfirmed) return;
    }

    this.onCancelar.emit();
  }

  private formHaCambiado(): boolean {
    return (
      this.formData.nombreJefeGuardia.trim() !== '' ||
      this.formData.cmpJefeGuardia.trim() !== '' ||
      this.formData.nombreFamiliar.trim() !== '' ||
      this.formData.dniFamiliar.trim() !== '' ||
      this.formData.nombreAutoridad.trim() !== '' ||
      this.formData.documentoAutoridad.trim() !== '' ||
      this.formData.destinoCuerpo.trim() !== '' ||
      this.formData.observaciones.trim() !== ''
    );
  }

  // ===================================================================
  // GETTERS
  // ===================================================================
  get puedeCrear(): boolean {
    return !this.isSubmitting;
  }

  get tituloFormulario(): string {
    return 'Crear Acta de Retiro';
  }

  // ===================================================================
  // HELPERS
  // ===================================================================
  private extraerApellidoPaterno(nombreCompleto: string): string {
    if (!nombreCompleto) return '';
    const partes = nombreCompleto.split(',');
    if (partes.length > 1) {
      const apellidos = partes[0].trim().split(' ');
      return apellidos[0] || '';
    }
    const palabras = nombreCompleto.trim().split(' ');
    return palabras[0] || '';
  }

  private extraerApellidoMaterno(nombreCompleto: string): string {
    if (!nombreCompleto) return '';
    const partes = nombreCompleto.split(',');
    if (partes.length > 1) {
      const apellidos = partes[0].trim().split(' ');
      return apellidos[1] || '';
    }
    const palabras = nombreCompleto.trim().split(' ');
    return palabras[1] || '';
  }

  private extraerNombres(nombreCompleto: string): string {
    if (!nombreCompleto) return '';
    const partes = nombreCompleto.split(',');
    if (partes.length > 1) {
      return partes[1].trim();
    }
    const palabras = nombreCompleto.trim().split(' ');
    if (palabras.length > 2) {
      return palabras.slice(2).join(' ');
    }
    return palabras[palabras.length - 1] || '';
  }

  obtenerPlaceholderDocumento(tipoDocumento: number): string {
    switch (tipoDocumento) {
      case 1: return '12345678';
      case 2: return '001234567';
      case 3: return 'ABC123456';
      default: return 'Número de documento';
    }
  }
}
