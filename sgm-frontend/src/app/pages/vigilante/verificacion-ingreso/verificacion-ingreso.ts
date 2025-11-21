import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VerificacionService, VerificacionRequest } from '../../../services/verificacion'; // 3 niveles arriba
import { IconComponent } from '../../../components/icon/icon.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-verificacion-ingreso',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './verificacion-ingreso.html'
})
export class VerificacionIngresoComponent {
  private verificacionService = inject(VerificacionService);
  private router = inject(Router);

  // Estados de la vista
  paso: 'escanear' | 'validar' = 'escanear';
  isLoading = false;

  // Datos
  codigoQRInput = '';
  datosExpediente: any = null;

  // Checklist Visual (El vigilante debe marcar todo)
  checks = {
    hc: false,
    documento: false,
    nombre: false,
    servicio: false
  };

  // --- PASO 1: ESCANEAR (Simulado) ---
  buscarQR() {
    if (!this.codigoQRInput.trim()) {
      Swal.fire('Error', 'Ingrese un código QR', 'warning');
      return;
    }

    this.isLoading = true;
    this.verificacionService.consultarPorQR(this.codigoQRInput).subscribe({
      next: (data) => {
        this.datosExpediente = data;
        this.paso = 'validar';
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        Swal.fire('No encontrado', 'El código escaneado no corresponde a un expediente válido o en traslado.', 'error');
      }
    });
  }

  // --- PASO 2: VALIDAR Y CONFIRMAR ---
  confirmarIngreso() {
    // Validar checklist
    if (!this.checks.hc || !this.checks.documento || !this.checks.nombre || !this.checks.servicio) {
      Swal.fire('Atención', 'Debe verificar físicamente todos los datos del brazalete y marcar las casillas.', 'warning');
      return;
    }

    this.enviarVerificacion(null);
  }

  rechazarIngreso() {
    Swal.fire({
      title: 'Rechazar Ingreso',
      text: 'Indique el motivo del rechazo (ej. Datos no coinciden, brazalete roto)',
      input: 'textarea',
      inputPlaceholder: 'Escriba el motivo...',
      showCancelButton: true,
      confirmButtonColor: '#DC3545',
      confirmButtonText: 'Rechazar y Devolver'
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        // Enviamos los datos "como si fueran correctos" pero con una observación que podría gatillar lógica en backend
        // OJO: En tu lógica actual de backend, el rechazo es automático si los datos NO coinciden.
        // Para simular un rechazo manual por "Brazalete roto" (aunque datos coincidan), 
        // usamos el campo Observaciones.
        this.enviarVerificacion(result.value);
      }
    });
  }

  private enviarVerificacion(observaciones: string | null) {
    this.isLoading = true;

    // Construimos el request con los datos que "leímos" (que son los del sistema en este caso feliz)
    // Si quisiéramos simular error de datos, alteraríamos estos valores.
    const request: VerificacionRequest = {
      codigoExpedienteBrazalete: this.datosExpediente.codigoExpediente,
      hcBrazalete: this.datosExpediente.hc,
      tipoDocumentoBrazalete: this.datosExpediente.tipoDocumento,
      numeroDocumentoBrazalete: this.datosExpediente.numeroDocumento,
      nombreCompletoBrazalete: this.datosExpediente.nombreCompleto,
      servicioBrazalete: this.datosExpediente.servicioFallecimiento,
      observaciones: observaciones || 'Ingreso verificado correctamente.'
    };

    // SI ES RECHAZO MANUAL (Observaciones llenas), podriamos alterar un dato a propósito
    // para que el backend detecte "No coincidencia" y lo rechace.
    if (observaciones) {
      request.nombreCompletoBrazalete = "ERROR FORZADO: " + request.nombreCompletoBrazalete;
    }

    this.verificacionService.registrarIngreso(request).subscribe({
      next: (res) => {
        this.isLoading = false;

        if (res.aprobada) {
          Swal.fire(' Ingreso Exitoso', 'El cuerpo ha sido ingresado al mortuorio. Ahora debe ser ubicado en una bandeja.', 'success')
            .then(() => this.router.navigate(['/dashboard']));
        } else {
          Swal.fire(' Ingreso Rechazado', `Se ha generado una solicitud de corrección.\nMotivo: ${res.mensajeResultado}`, 'warning')
            .then(() => this.router.navigate(['/dashboard']));
        }
      },
      error: (err) => {
        this.isLoading = false;
        Swal.fire('Error', err.error?.message || 'Error al procesar ingreso', 'error');
      }
    });
  }

  cancelar() {
    this.paso = 'escanear';
    this.datosExpediente = null;
    this.checks = { hc: false, documento: false, nombre: false, servicio: false };
    this.codigoQRInput = '';
  }
}
