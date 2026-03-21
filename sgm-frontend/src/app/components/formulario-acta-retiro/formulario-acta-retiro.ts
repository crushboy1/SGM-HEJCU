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

  /**
   * Controla el acordeón de médico externo.
   * false por defecto — el admisionista lo activa solo si la familia trae médico propio.
   * Al desmarcar limpia los campos para no enviar datos residuales al backend.
   */
  tieneMedicoExterno = false;

  // Datos del expediente para mostrar en readonly (digitaliza cuaderno VigSup)
  edadPaciente: number = 0;
  diagnosticoFinal: string = '';

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
    numeroOficioPolicial: '',

    // Médico certificante (readonly — viene del expediente)
    nombreMedicoCertificante: '',
    cmpMedicoCertificante: '',
    rneMedicoCertificante: '',

    // Médico externo (opcional — solo cuando causaViolentaODudosa = false)
    medicoExternoNombre: '',
    medicoExternoCMP: '',

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
    telefonoAutoridad: '',

    // Datos adicionales
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
  // GETTERS
  // ═══════════════════════════════════════════════════════════

  /** true si el tipo de salida viene definido desde gestión de documentos */
  get tipoSalidaBloqueado(): boolean {
    return !!this.expediente?.tipoSalidaPreliminar;
  }

  /**
   * true si se puede mostrar el bloque de médico externo.
   * Bloqueado cuando CausaViolentaODudosa = true (siempre AutoridadLegal + PNP).
   */
  get medicoExternoHabilitado(): boolean {
    return !this.expediente?.causaViolentaODudosa &&
      this.formData.tipoSalida === 'Familiar';
  }

  /** SINADEF siempre obligatorio para Familiar.
   * El médico externo genera el SINADEF antes de venir — sin excepción. */
  get sinadefRequerido(): boolean {
    return this.formData.tipoSalida === 'Familiar';
  }

  get puedeCrear(): boolean {
    return !this.isSubmitting;
  }

  get tituloFormulario(): string {
    return 'Crear Acta de Retiro';
  }

  // ═══════════════════════════════════════════════════════════
  // VALIDACIÓN EN TIEMPO REAL DE DOCUMENTOS
  // ═══════════════════════════════════════════════════════════

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
        error: (err) => console.error('❌ Error al verificar certificado SINADEF:', err)
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
            this.errors['numeroOficioPolicial'] =
              'Este número de oficio ya está registrado en otra acta';
          } else {
            this.clearError('numeroOficioPolicial');
          }
        },
        error: (err) => console.error('❌ Error al verificar oficio legal:', err)
      });
  }

  onCertificadoSINADEFChange(value: string): void {
    if (value.trim().length >= 10) {
      this.certificadoSubject.next(value.trim());
    }
  }

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
    this.formData.numeroCertificadoDefuncion = '';
    this.formData.nombreMedicoCertificante = this.expediente.medicoCertificaNombre || '';
    this.formData.cmpMedicoCertificante = this.expediente.medicoCMP || '';
    this.formData.rneMedicoCertificante = this.expediente.medicoRNE || '';

    // Edad y diagnóstico para mostrar readonly (digitalizan cuaderno VigSup)
    this.edadPaciente = this.expediente.edad ?? 0;
    this.diagnosticoFinal = this.expediente.diagnosticoFinal || '';

    if (this.expediente.tipoSalidaPreliminar) {
      this.formData.tipoSalida = this.expediente.tipoSalidaPreliminar as 'Familiar' | 'AutoridadLegal';
    }

    console.log('✅ Datos del expediente pre-llenados:', this.formData);
  }

  // ===================================================================
  // VALIDACIÓN
  // ===================================================================
  private validarFormulario(): boolean {
    this.errors = {};

    // Validar SINADEF / Oficio según tipo de salida
    if (this.formData.tipoSalida === 'Familiar') {
      // SINADEF siempre obligatorio para Familiar
      if (!this.formData.numeroCertificadoDefuncion.trim()) {
        this.errors['numeroCertificadoDefuncion'] =
          'El N° de Certificado SINADEF es obligatorio';
      }

      // Si hay médico externo, nombre y CMP son obligatorios
      if (this.tieneMedicoExterno) {
        if (!this.formData.medicoExternoNombre.trim()) {
          this.errors['medicoExternoNombre'] = 'El nombre del médico externo es obligatorio';
        }
        if (!this.formData.medicoExternoCMP.trim()) {
          this.errors['medicoExternoCMP'] = 'El CMP del médico externo es obligatorio';
        }
      }

    } else if (this.formData.tipoSalida === 'AutoridadLegal') {
      if (!this.formData.numeroOficioPolicial.trim()) {
        this.errors['numeroOficioPolicial'] = 'El N° de Oficio Legal es requerido';
      }
    }

    // Jefe de Guardia (siempre obligatorio)
    if (!this.formData.nombreJefeGuardia.trim()) {
      this.errors['nombreJefeGuardia'] = 'El nombre del Jefe de Guardia es requerido';
    }

    if (!this.formData.cmpJefeGuardia.trim()) {
      this.errors['cmpJefeGuardia'] = 'El CMP del Jefe de Guardia es requerido';
    } else if (!/^\d{6}$/.test(this.formData.cmpJefeGuardia)) {
      this.errors['cmpJefeGuardia'] = 'El CMP debe tener 6 dígitos';
    }

    // Validar según tipo de salida
    if (this.formData.tipoSalida === 'Familiar') {
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
      if (this.formData.telefonoFamiliar.trim() && !/^\d{9}$/.test(this.formData.telefonoFamiliar)) {
        this.errors['telefonoFamiliar'] = 'El teléfono debe tener 9 dígitos';
      }

    } else if (this.formData.tipoSalida === 'AutoridadLegal') {
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
        this.errors['cargoAutoridad'] = 'El cargo es requerido (ej: SO3 PNP)';
      }
      if (!this.formData.institucionAutoridad.trim()) {
        this.errors['institucionAutoridad'] = 'La institución/comisaría es requerida';
      }
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

  /**
   * Callback al marcar/desmarcar el checkbox de médico externo.
   * Al desmarcar: limpia los campos para no enviar datos residuales.
   * Al marcar: simplemente habilita la sección — SINADEF pasa a opcional.
   */
  onToggleMedicoExterno(habilitado: boolean): void {
    if (!habilitado) {
      this.formData.medicoExternoNombre = '';
      this.formData.medicoExternoCMP = '';
      this.clearError('medicoExternoNombre');
      this.clearError('medicoExternoCMP');
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

  private generarHTMLConfirmacion(): string {
    let html = `
      <div class="text-left text-sm space-y-2">
        <p class="font-semibold text-gray-800">Paciente fallecido:</p>
        <p class="text-gray-600">${this.formData.nombreCompletoPaciente}</p>
    `;

    if (this.edadPaciente > 0) {
      html += `<p class="text-gray-500 text-xs">Edad: ${this.edadPaciente} años</p>`;
    }

    if (this.formData.tipoSalida === 'Familiar') {
      html += `
        <p class="font-semibold text-gray-800 mt-3">Familiar responsable:</p>
        <p class="text-gray-600">${this.formData.nombreFamiliar}</p>
        <p class="text-gray-500 text-xs">DNI: ${this.formData.dniFamiliar} — ${this.formData.parentesco}</p>
      `;
      if (this.formData.medicoExternoNombre.trim()) {
        html += `
          <p class="font-semibold text-gray-800 mt-3">Médico externo:</p>
          <p class="text-gray-600">${this.formData.medicoExternoNombre}</p>
          <p class="text-gray-500 text-xs">CMP: ${this.formData.medicoExternoCMP}</p>
        `;
      }
    } else {
      html += `
        <p class="font-semibold text-gray-800 mt-3">Autoridad Legal:</p>
        <p class="text-gray-600">${this.formData.nombreAutoridad}</p>
        <p class="text-gray-500 text-xs">${this.formData.cargoAutoridad} — ${this.formData.institucionAutoridad}</p>
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

  private ejecutarCreacion(): void {
    this.isSubmitting = true;

    const usuarioId = this.authService.getUserId();
    const tieneMedicoExterno = this.tieneMedicoExterno;

    const dto: CreateActaRetiroDTO = {
      expedienteID: this.expediente.expedienteID,

      tipoSalida: this.formData.tipoSalida,

      // Documento legal (condicional)
      numeroCertificadoDefuncion: this.formData.tipoSalida === 'Familiar'
        ? this.formData.numeroCertificadoDefuncion || undefined
        : undefined,
      numeroOficioPolicial: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.numeroOficioPolicial
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

      // Médico externo (solo si aplica y tipoSalida = Familiar)
      medicoExternoNombre: this.formData.tipoSalida === 'Familiar' && tieneMedicoExterno
        ? this.formData.medicoExternoNombre
        : undefined,
      medicoExternoCMP: this.formData.tipoSalida === 'Familiar' && tieneMedicoExterno
        ? this.formData.medicoExternoCMP
        : undefined,

      // Jefe de Guardia
      jefeGuardiaNombre: this.formData.nombreJefeGuardia,
      jefeGuardiaCMP: this.formData.cmpJefeGuardia,

      // Familiar
      familiarApellidoPaterno: this.formData.tipoSalida === 'Familiar'
        ? this.extraerApellidoPaterno(this.formData.nombreFamiliar) : undefined,
      familiarApellidoMaterno: this.formData.tipoSalida === 'Familiar'
        ? this.extraerApellidoMaterno(this.formData.nombreFamiliar) : undefined,
      familiarNombres: this.formData.tipoSalida === 'Familiar'
        ? this.extraerNombres(this.formData.nombreFamiliar) : undefined,
      familiarTipoDocumento: this.formData.tipoSalida === 'Familiar'
        ? this.formData.tipoDocumentoFamiliar : undefined,
      familiarNumeroDocumento: this.formData.tipoSalida === 'Familiar'
        ? this.formData.dniFamiliar : undefined,
      familiarParentesco: this.formData.tipoSalida === 'Familiar'
        ? this.formData.parentesco : undefined,
      familiarTelefono: this.formData.tipoSalida === 'Familiar'
        ? this.formData.telefonoFamiliar || undefined : undefined,

      // Autoridad Legal
      autoridadApellidoPaterno: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerApellidoPaterno(this.formData.nombreAutoridad) : undefined,
      autoridadApellidoMaterno: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerApellidoMaterno(this.formData.nombreAutoridad) : undefined,
      autoridadNombres: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.extraerNombres(this.formData.nombreAutoridad) : undefined,
      tipoAutoridad: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.tipoAutoridad : undefined,
      autoridadTipoDocumento: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.tipoDocumentoAutoridad : undefined,
      autoridadNumeroDocumento: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.documentoAutoridad : undefined,
      autoridadCargo: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.cargoAutoridad : undefined,
      autoridadInstitucion: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.institucionAutoridad : undefined,
      autoridadTelefono: this.formData.tipoSalida === 'AutoridadLegal'
        ? this.formData.telefonoAutoridad || undefined : undefined,

      destino: this.formData.destinoCuerpo || undefined,
      observaciones: this.formData.observaciones || undefined,
      usuarioAdmisionID: usuarioId
    };

    this.actaRetiroService.crear(dto).subscribe({
      next: (actaCreada) => {
        this.isSubmitting = false;
        Swal.fire({
          icon: 'success',
          title: 'Acta Creada Exitosamente',
          text: 'El acta de retiro ha sido registrada correctamente',
          showConfirmButton: true,
          confirmButtonColor: '#0891B2'
        });
        this.actaCreada.emit(actaCreada);
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('❌ Error al crear acta:', err);
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
    if (this.formHaCambiado()) {
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
      this.formData.medicoExternoNombre.trim() !== '' ||
      this.formData.nombreAutoridad.trim() !== '' ||
      this.formData.documentoAutoridad.trim() !== '' ||
      this.formData.destinoCuerpo.trim() !== '' ||
      this.formData.observaciones.trim() !== ''
    );
  }

  // ===================================================================
  // HELPERS
  // ===================================================================
  private mapearTipoDocumento(tipo: string): number {
    const mapeo: Record<string, number> = {
      'DNI': 1, 'CE': 2, 'Pasaporte': 3, 'RUC': 4, 'NN': 5
    };
    return mapeo[tipo] || 1;
  }

  private extraerApellidoPaterno(nombreCompleto: string): string {
    if (!nombreCompleto) return '';
    const partes = nombreCompleto.split(',');
    if (partes.length > 1) {
      const apellidos = partes[0].trim().split(' ');
      return apellidos[0] || '';
    }
    return nombreCompleto.trim().split(' ')[0] || '';
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
    if (partes.length > 1) return partes[1].trim();
    const palabras = nombreCompleto.trim().split(' ');
    return palabras.length > 2 ? palabras.slice(2).join(' ') : palabras[palabras.length - 1] || '';
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
