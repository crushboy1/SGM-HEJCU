import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// External libs
import Swal from 'sweetalert2';

// Services
import { DeudaSangre } from '../../services/deuda-sangre';
import { AuthService } from '../../services/auth';

// Models
import { CreateDeudaSangreDTO, DeudaSangreDTO } from '../../models/deuda-sangre.model';

// Components
import { IconComponent } from '../icon/icon.component';

// ===================================================================
// INTERFACES LOCALES
// ===================================================================

interface FormState {
  expedienteId: number;
  cantidadUnidades: number;
  tipoSangre: string;
}

interface ValidationErrors {
  expedienteId?: string;
  cantidadUnidades?: string;
  tipoSangre?: string;
}

/**
 * FormDeudaSangre Component
 * 
 * Formulario reutilizable para registrar deudas de sangre.
 * Usado en: Página Registrar Deuda Sangre, Modal rápido desde Expediente
 * 
 * @Input expedienteId - ID del expediente (opcional, si se proporciona bloquea campo)
 * @Input codigoExpediente - Código del expediente (opcional, solo para mostrar)
 * @Output onRegistroExitoso - Emite la deuda creada al guardar exitosamente
 * @Output onCancelar - Emite cuando se cancela el formulario
 */
@Component({
  selector: 'app-form-deuda-sangre',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './form-deuda-sangre.html',
  styleUrl: './form-deuda-sangre.css'
})
export class FormDeudaSangre implements OnInit {
  // ===================================================================
  // SERVICES
  // ===================================================================
  private deudaSangreService = inject(DeudaSangre);
  private authService = inject(AuthService);

  // ===================================================================
  // INPUTS / OUTPUTS
  // ===================================================================
  @Input() expedienteId?: number;
  @Input() codigoExpediente?: string;
  @Output() onRegistroExitoso = new EventEmitter<DeudaSangreDTO>();
  @Output() onCancelar = new EventEmitter<void>();

  // ===================================================================
  // DATOS DEL FORMULARIO
  // ===================================================================
  formData: FormState = {
    expedienteId: 0,
    cantidadUnidades: 1,
    tipoSangre: ''
  };

  // ===================================================================
  // CATÁLOGOS
  // ===================================================================
  tiposSangre: string[] = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  // ===================================================================
  // ESTADOS
  // ===================================================================
  isSubmitting = false;
  errors: ValidationErrors = {};

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================

  ngOnInit(): void {
    // Si se proporciona expedienteId por Input, pre-cargar
    if (this.expedienteId) {
      this.formData.expedienteId = this.expedienteId;
    }
  }

  // ===================================================================
  // VALIDACIONES
  // ===================================================================

  /**
   * Valida todo el formulario antes de enviar
   */
  private validarFormulario(): boolean {
    this.errors = {};
    let isValid = true;

    // Validar Expediente ID
    if (!this.formData.expedienteId || this.formData.expedienteId <= 0) {
      this.errors.expedienteId = 'Debe ingresar un ID de expediente válido';
      isValid = false;
    }

    // Validar Cantidad Unidades
    if (!this.formData.cantidadUnidades || this.formData.cantidadUnidades < 1) {
      this.errors.cantidadUnidades = 'Debe ingresar al menos 1 unidad';
      isValid = false;
    }

    if (this.formData.cantidadUnidades > 20) {
      this.errors.cantidadUnidades = 'Cantidad máxima: 20 unidades';
      isValid = false;
    }

    // Validar Tipo Sangre (opcional pero si se ingresa debe ser válido)
    if (this.formData.tipoSangre && !this.tiposSangre.includes(this.formData.tipoSangre)) {
      this.errors.tipoSangre = 'Tipo de sangre no válido';
      isValid = false;
    }

    return isValid;
  }

  /**
   * Limpia error de un campo específico al editarlo
   */
  clearError(campo: keyof ValidationErrors): void {
    if (this.errors[campo]) {
      delete this.errors[campo];
    }
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  /**
   * Registra la deuda de sangre
   */
  registrar(): void {
    // Validar formulario
    if (!this.validarFormulario()) {
      Swal.fire({
        icon: 'warning',
        title: 'Formulario Incompleto',
        text: 'Por favor corrija los errores antes de continuar',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    // Confirmar acción
    Swal.fire({
      icon: 'question',
      title: '¿Registrar Deuda de Sangre?',
      html: `
        <div class="text-left space-y-2 text-sm">
          <p><strong>Expediente:</strong> ${this.codigoExpediente || `#${this.formData.expedienteId}`}</p>
          <p><strong>Unidades:</strong> ${this.formData.cantidadUnidades}</p>
          <p><strong>Tipo Sangre:</strong> ${this.formData.tipoSangre || 'No especificado'}</p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, Registrar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    }).then((result) => {
      if (result.isConfirmed) {
        this.ejecutarRegistro();
      }
    });
  }

  /**
   * Ejecuta el registro en el backend
   */
  private ejecutarRegistro(): void {
    this.isSubmitting = true;

    try {
      const dto: CreateDeudaSangreDTO = {
        expedienteID: this.formData.expedienteId,
        cantidadUnidades: this.formData.cantidadUnidades,
        tipoSangre: this.formData.tipoSangre || undefined,
        usuarioRegistroID: this.authService.getUserId()
      };

      this.deudaSangreService.registrar(dto).subscribe({
        next: (deudaCreada) => {
          this.isSubmitting = false;

          // Emitir evento de éxito (padre mostrará el Swal)
          this.onRegistroExitoso.emit(deudaCreada);

          // Limpiar formulario si NO estaba pre-cargado
          if (!this.expedienteId) {
            this.limpiarFormulario();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          console.error('[FormDeudaSangre] Error al registrar:', err);

          Swal.fire({
            icon: 'error',
            title: 'Error al Registrar',
            text: err.message || 'No se pudo registrar la deuda de sangre',
            confirmButtonColor: '#DC2626'
          });
        }
      });
    } catch (error: any) {
      // Capturar error de getUserId()
      this.isSubmitting = false;

      Swal.fire({
        icon: 'error',
        title: 'Sesión Inválida',
        text: 'Por favor, inicie sesión nuevamente.',
        confirmButtonColor: '#DC2626'
      }).then(() => {
        this.onCancelar.emit();
      });
    }
  }

  /**
   * Cancela el formulario
   */
  cancelar(): void {
    if (this.formHaCambiado()) {
      Swal.fire({
        icon: 'warning',
        title: '¿Cancelar Registro?',
        text: 'Se perderán los datos ingresados',
        showCancelButton: true,
        confirmButtonText: 'Sí, Cancelar',
        cancelButtonText: 'Continuar Editando',
        confirmButtonColor: '#DC3545',
        cancelButtonColor: '#6B7280'
      }).then((result) => {
        if (result.isConfirmed) {
          this.limpiarFormulario();
          this.onCancelar.emit();
        }
      });
    } else {
      this.limpiarFormulario();
      this.onCancelar.emit();
    }
  }

  /**
   * Limpia el formulario a su estado inicial
   */
  private limpiarFormulario(): void {
    this.formData = {
      expedienteId: this.expedienteId || 0,
      cantidadUnidades: 1,
      tipoSangre: ''
    };
    this.errors = {};
  }

  /**
   * Verifica si el formulario tiene cambios
   */
  private formHaCambiado(): boolean {
    return (
      this.formData.cantidadUnidades !== 1 ||
      this.formData.tipoSangre !== '' ||
      (!this.expedienteId && this.formData.expedienteId !== 0)
    );
  }

  // ===================================================================
  // HELPERS PARA TEMPLATE
  // ===================================================================

  get tituloFormulario(): string {
    return this.codigoExpediente
      ? `Registrar Deuda de Sangre - ${this.codigoExpediente}`
      : 'Registrar Deuda de Sangre';
  }

  get expedienteReadonly(): boolean {
    return !!this.expedienteId;
  }

  get puedeRegistrar(): boolean {
    return !this.isSubmitting && this.formData.expedienteId > 0;
  }
}
