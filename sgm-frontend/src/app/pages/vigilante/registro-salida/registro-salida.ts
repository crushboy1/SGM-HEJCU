import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal, { SweetAlertResult } from 'sweetalert2';

import { SalidaService, RegistrarSalidaRequest } from '../../../services/salida';
import { VerificacionService } from '../../../services/verificacion';
import { IconComponent } from '../../../components/icon/icon.component';

/**
 * Componente para registro de salida del mortuorio.
 * 
 * Flujo:
 * 1. Escanear QR del expediente
 * 2. Validar estado (EnBandeja o PendienteRetiro)
 * 3. Completar formulario de salida
 * 4. Registrar salida → Libera bandeja automáticamente
 * 
 * Roles: VigilanciaMortuorio, VigilanteSupervisor
 */
@Component({
  selector: 'app-registro-salida',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './registro-salida.html'
})
export class RegistroSalidaComponent implements OnInit {
  private salidaService = inject(SalidaService);
  private verificacionService = inject(VerificacionService);
  private router = inject(Router);

  paso: 'escanear' | 'formulario' = 'escanear';
  isLoading = false;

  // Datos del Expediente encontrado
  expediente: any = null;

  // ⭐ ACTUALIZADO: Modelo del Formulario alineado con backend
  form: RegistrarSalidaRequest = {
    expedienteID: 0,
    tipoSalida: 'Familiar',

    // Responsable
    responsableNombre: '',
    responsableTipoDocumento: 'DNI',
    responsableNumeroDocumento: '',
    responsableParentesco: '',
    responsableTelefono: '',

    // Validaciones (requeridas en backend)
    documentacionVerificada: false,
    pagoRealizado: false,
    numeroRecibo: '',

    // Funeraria (opcional)
    nombreFuneraria: '',
    conductorFuneraria: '',
    dniConductor: '',
    placaVehiculo: '',

    // Legal (opcional)
    numeroAutorizacion: '',
    entidadAutorizante: '',

    // Destino
    destino: '',
    observaciones: ''
  };

  // Variable para el input del escáner
  codigoQRInput = '';

  ngOnInit(): void {
    // Configurar foco en el input QR al cargar
    setTimeout(() => {
      const input = document.getElementById('qr-input');
      if (input) input.focus();
    }, 300);
  }

  // ===================================================================
  // PASO 1: BUSCAR EXPEDIENTE POR QR
  // ===================================================================

  /**
   * Busca expediente por código QR y valida su estado.
   * Solo permite continuar si está en EnBandeja o PendienteRetiro.
   */
  buscarQR(): void {
    if (!this.codigoQRInput.trim()) {
      Swal.fire('Error', 'Debe ingresar un código QR válido', 'warning');
      return;
    }

    this.isLoading = true;

    this.verificacionService.consultarPorQR(this.codigoQRInput).subscribe({
      next: (data: any) => {
        this.isLoading = false;

        // ⭐ Validación de estado mejorada
        const estadosPermitidos = ['EnBandeja', 'PendienteRetiro'];
        if (!estadosPermitidos.includes(data.estadoActual)) {
          Swal.fire({
            icon: 'error',
            title: 'Estado Inválido',
            html: `El expediente está en estado <strong>${data.estadoActual}</strong>.<br><br>
                   Solo se puede registrar salida si el expediente está en:<br>
                   - <strong>En Bandeja</strong><br>
                   - <strong>Pendiente Retiro</strong>`,
            confirmButtonColor: '#0891B2'
          });
          return;
        }

        // Guardar expediente y preparar formulario
        this.expediente = data;
        this.form.expedienteID = data.expedienteID;

        // ⭐ Pre-llenar según tipo de expediente
        this.prellenarFormularioSegunTipo(data);

        // Avanzar a formulario
        this.paso = 'formulario';
      },
      error: (err: any) => {
        console.error('Error al buscar QR:', err);
        this.isLoading = false;

        Swal.fire({
          icon: 'error',
          title: 'QR No Encontrado',
          text: err.error?.message || 'Código QR no válido o no encontrado en el sistema',
          confirmButtonColor: '#0891B2'
        });
      }
    });
  }

  /**
   * Pre-llena el formulario según el tipo de expediente.
   */
  private prellenarFormularioSegunTipo(expediente: any): void {
    // Caso 1: Expediente Externo (NN, sin familiar)
    if (expediente.tipoExpediente === 'Externo') {
      this.form.tipoSalida = 'AutoridadLegal';
      this.form.entidadAutorizante = 'Fiscalía';
      this.form.destino = 'Morgue Central de Lima';
      this.form.responsableNombre = 'Ministerio Público';
      this.form.responsableTipoDocumento = 'RUC';
      this.form.documentacionVerificada = true;
      this.form.pagoRealizado = true; // Exonerado
    }

    // Caso 2: Traslado a otro hospital
    else if (expediente.tipoSalida === 'TrasladoHospital') {
      this.form.tipoSalida = 'TrasladoHospital';
      this.form.destino = 'Hospital de referencia';
    }

    // Caso 3: Normal - Retiro Familiar (default)
    else {
      this.form.tipoSalida = 'Familiar';
      this.form.destino = 'Cementerio Local';
    }
  }

  // ===================================================================
  // PASO 2: REGISTRAR SALIDA
  // ===================================================================

  /**
   * Valida formulario y muestra confirmación antes de registrar.
   */
  confirmarSalida(): void {
    // ⭐ Validaciones mejoradas
    if (!this.validarFormulario()) {
      return;
    }

    Swal.fire({
      title: '¿Confirmar Salida?',
      html: `
        <div class="text-left space-y-2">
          <p><strong>Expediente:</strong> ${this.expediente.codigoExpediente}</p>
          <p><strong>Paciente:</strong> ${this.expediente.nombreCompleto}</p>
          <p><strong>Responsable:</strong> ${this.form.responsableNombre}</p>
          <p><strong>Tipo:</strong> ${this.form.tipoSalida}</p>
          <br>
          <p class="text-red-600 font-semibold">⚠️ Esta acción liberará la bandeja y cerrará el expediente.</p>
          <p class="text-gray-600 text-sm">No se puede deshacer.</p>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, Registrar Salida',
      cancelButtonText: 'Cancelar'
    }).then((result: SweetAlertResult) => {
      if (result.isConfirmed) {
        this.procesarSalida();
      }
    });
  }

  /**
   * Valida que todos los campos requeridos estén completos.
   */
  private validarFormulario(): boolean {
    // 1. Responsable
    if (!this.form.responsableNombre?.trim()) {
      Swal.fire('Faltan Datos', 'Debe ingresar el nombre del responsable del retiro.', 'warning');
      return false;
    }

    if (!this.form.responsableNumeroDocumento?.trim()) {
      Swal.fire('Faltan Datos', 'Debe ingresar el número de documento del responsable.', 'warning');
      return false;
    }

    // 2. Si es Familiar, validar parentesco
    if (this.form.tipoSalida === 'Familiar' && !this.form.responsableParentesco?.trim()) {
      Swal.fire('Faltan Datos', 'Debe especificar el parentesco del responsable.', 'warning');
      return false;
    }

    // 3. Si es Autoridad Legal, validar autorización
    if (this.form.tipoSalida === 'AutoridadLegal') {
      if (!this.form.numeroAutorizacion?.trim()) {
        Swal.fire('Faltan Datos', 'Debe ingresar el número de oficio/autorización legal.', 'warning');
        return false;
      }
      if (!this.form.entidadAutorizante?.trim()) {
        Swal.fire('Faltan Datos', 'Debe especificar la entidad autorizante (Fiscalía, PNP, etc).', 'warning');
        return false;
      }
    }

    // 4. Validar documentación verificada
    if (!this.form.documentacionVerificada) {
      Swal.fire('Documentación Pendiente', 'Debe verificar la documentación antes de proceder.', 'warning');
      return false;
    }

    // 5. Si hay funeraria, validar datos completos
    if (this.form.nombreFuneraria?.trim()) {
      if (!this.form.conductorFuneraria?.trim() || !this.form.placaVehiculo?.trim()) {
        Swal.fire('Datos de Funeraria Incompletos', 'Si registra una funeraria, debe completar conductor y placa.', 'warning');
        return false;
      }
    }

    return true;
  }

  /**
   * Envía la solicitud de registro de salida al backend.
   */
  private procesarSalida(): void {
    this.isLoading = true;

    this.salidaService.registrarSalida(this.form).subscribe({
      next: (response) => {
        this.isLoading = false;

        Swal.fire({
          icon: 'success',
          title: 'Salida Registrada',
          html: `
            <div class="text-left space-y-2">
              <p>✅ El cuerpo ha sido retirado del mortuorio.</p>
              <p>✅ La bandeja <strong>${this.expediente.codigoBandeja || 'asignada'}</strong> ha sido liberada.</p>
              <p>✅ El expediente <strong>${this.expediente.codigoExpediente}</strong> está cerrado.</p>
            </div>
          `,
          confirmButtonColor: '#10B981',
          timer: 5000,
          timerProgressBar: true
        }).then(() => {
          this.router.navigate(['/dashboard']);
        });
      },
      error: (err: any) => {
        this.isLoading = false;
        console.error('Error al registrar salida:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Registrar',
          text: err.error?.message || 'No se pudo registrar la salida. Intente nuevamente.',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  // ===================================================================
  // NAVEGACIÓN
  // ===================================================================

  /**
   * Vuelve al paso de escaneo.
   */
  volverAEscanear(): void {
    this.paso = 'escanear';
    this.expediente = null;
    this.codigoQRInput = '';

    // Reset formulario
    this.form = {
      expedienteID: 0,
      tipoSalida: 'Familiar',
      responsableNombre: '',
      responsableTipoDocumento: 'DNI',
      responsableNumeroDocumento: '',
      responsableParentesco: '',
      responsableTelefono: '',
      documentacionVerificada: false,
      pagoRealizado: false,
      numeroRecibo: '',
      nombreFuneraria: '',
      conductorFuneraria: '',
      dniConductor: '',
      placaVehiculo: '',
      numeroAutorizacion: '',
      entidadAutorizante: '',
      destino: '',
      observaciones: ''
    };
  }

  /**
   * Cancela el proceso y vuelve al dashboard.
   */
  cancelar(): void {
    if (this.paso === 'formulario') {
      Swal.fire({
        title: '¿Cancelar Registro?',
        text: 'Se perderán los datos ingresados',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, Cancelar',
        cancelButtonText: 'Continuar Editando'
      }).then((result) => {
        if (result.isConfirmed) {
          this.router.navigate(['/dashboard']);
        }
      });
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
