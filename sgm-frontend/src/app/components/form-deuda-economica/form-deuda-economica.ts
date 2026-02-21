import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// External libs
import Swal from 'sweetalert2';

// Services
import { DeudaEconomica } from '../../services/deuda-economica';
import { AuthService } from '../../services/auth';

// Models
import { CreateDeudaEconomicaDTO, DeudaEconomicaDTO } from '../../models/deuda-economica.model';

// Components
import { IconComponent } from '../icon/icon.component';

// ===================================================================
// INTERFACES LOCALES
// ===================================================================

interface FormState {
  expedienteId: number;
  montoDeuda: number | null; // Nullable para que el input arranque vacío visualmente
}

interface ValidationErrors {
  expedienteId?: string;
  montoDeuda?: string;
}

/**
 * FormDeudaEconomica Component
 * Formulario para registrar deudas económicas (Cuentas Pacientes).
 * Usado para el caso "Híbrido" (inyectar deuda a SIS) o registro inicial Particular.
 * 
 * @Input expedienteId - ID del expediente (opcional, pre-carga y bloquea)
 * @Input codigoExpediente - Código visual (opcional)
 */
@Component({
  selector: 'app-form-deuda-economica',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './form-deuda-economica.html',
  styleUrl: './form-deuda-economica.css'
})
export class FormDeudaEconomica implements OnInit {
  // ===================================================================
  // SERVICES
  // ===================================================================
  private deudaEconomicaService = inject(DeudaEconomica);
  private authService = inject(AuthService);

  // ===================================================================
  // INPUTS / OUTPUTS
  // ===================================================================
  @Input() expedienteId?: number;
  @Input() codigoExpediente?: string;
  @Output() onRegistroExitoso = new EventEmitter<DeudaEconomicaDTO>();
  @Output() onCancelar = new EventEmitter<void>();

  // ===================================================================
  // ESTADO DEL FORMULARIO
  // ===================================================================
  formData: FormState = {
    expedienteId: 0,
    montoDeuda: null
  };

  isSubmitting = false;
  errors: ValidationErrors = {};

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit(): void {
    if (this.expedienteId) {
      this.formData.expedienteId = this.expedienteId;
    }
  }

  // ===================================================================
  // VALIDACIONES
  // ===================================================================
  private validarFormulario(): boolean {
    this.errors = {};
    let isValid = true;

    // Validar Expediente
    if (!this.formData.expedienteId || this.formData.expedienteId <= 0) {
      this.errors.expedienteId = 'Debe ingresar un ID de expediente válido';
      isValid = false;
    }

    // Validar Monto
    if (this.formData.montoDeuda === null || this.formData.montoDeuda <= 0) {
      this.errors.montoDeuda = 'El monto debe ser mayor a 0';
      isValid = false;
    }

    // Límite lógico de seguridad (ej. 100k soles)
    if (this.formData.montoDeuda && this.formData.montoDeuda > 100000) {
      this.errors.montoDeuda = 'El monto excede el límite permitido por seguridad (S/ 100,000)';
      isValid = false;
    }

    return isValid;
  }

  clearError(campo: keyof ValidationErrors): void {
    if (this.errors[campo]) {
      delete this.errors[campo];
    }
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  registrar(): void {
    if (!this.validarFormulario()) {
      Swal.fire({
        icon: 'warning',
        title: 'Formulario Incompleto',
        text: 'Por favor corrija los errores antes de continuar',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    const montoFormateado = new Intl.NumberFormat('es-PE', {
      style: 'currency',
      currency: 'PEN'
    }).format(this.formData.montoDeuda || 0);

    Swal.fire({
      icon: 'question',
      title: '¿Registrar Deuda Económica?',
      html: `
        <div class="text-left space-y-2 text-sm">
          <p><strong>Expediente:</strong> ${this.codigoExpediente || `#${this.formData.expedienteId}`}</p>
          <p><strong>Monto:</strong> <span class="text-hospital-cyan font-bold text-lg">${montoFormateado}</span></p>
          <p class="text-xs text-gray-500 mt-2">
            <i class="fas fa-info-circle"></i> Esto bloqueará la salida del cuerpo hasta que se regularice en Caja o Servicio Social.
          </p>
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

  private ejecutarRegistro(): void {
    this.isSubmitting = true;

    try {
      const dto: CreateDeudaEconomicaDTO = {
        expedienteID: this.formData.expedienteId,
        montoDeuda: this.formData.montoDeuda || 0,
        usuarioRegistroID: this.authService.getUserId()
      };

      this.deudaEconomicaService.registrar(dto).subscribe({
        next: (deudaCreada) => {
          this.isSubmitting = false;
          // Emitir evento al padre (él mostrará el Swal de éxito)
          this.onRegistroExitoso.emit(deudaCreada);

          if (!this.expedienteId) {
            this.limpiarFormulario();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          console.error('[FormDeudaEconomica] Error:', err);

          Swal.fire({
            icon: 'error',
            title: 'Error al Registrar',
            text: err.message || 'No se pudo procesar la solicitud.',
            confirmButtonColor: '#0891B2'
          });
        }
      });
    } catch (error: any) {
      this.isSubmitting = false;

      // Capturar error de authService.getUserId()
      Swal.fire({
        icon: 'error',
        title: 'Sesión Inválida',
        text: 'Por favor, inicie sesión nuevamente.',
        confirmButtonColor: '#0891B2'
      }).then(() => {
        // Redirigir a login o emitir evento
        this.onCancelar.emit();
      });
    }
  }

  cancelar(): void {
    if (this.formHaCambiado()) {
      Swal.fire({
        icon: 'warning',
        title: '¿Descartar cambios?',
        text: 'Se perderán los datos ingresados',
        showCancelButton: true,
        confirmButtonText: 'Sí, Descartar',
        cancelButtonText: 'Continuar Editando',
        confirmButtonColor: '#DC3545',
        cancelButtonColor: '#6B7280'
      }).then((r) => {
        if (r.isConfirmed) {
          this.limpiarFormulario();
          this.onCancelar.emit();
        }
      });
    } else {
      this.onCancelar.emit();
    }
  }

  // ===================================================================
  // HELPERS
  // ===================================================================
  private limpiarFormulario(): void {
    this.formData = {
      expedienteId: this.expedienteId || 0,
      montoDeuda: null
    };
    this.errors = {};
  }

  private formHaCambiado(): boolean {
    return (
      this.formData.montoDeuda !== null ||
      (!this.expedienteId && this.formData.expedienteId !== 0)
    );
  }

  get tituloFormulario(): string {
    return this.codigoExpediente
      ? `Registrar Deuda - ${this.codigoExpediente}`
      : 'Registrar Deuda Económica';
  }

  get expedienteReadonly(): boolean {
    return !!this.expedienteId;
  }

  get puedeRegistrar(): boolean {
    return !this.isSubmitting &&
      this.formData.expedienteId > 0 &&
      (this.formData.montoDeuda || 0) > 0;
  }
}
