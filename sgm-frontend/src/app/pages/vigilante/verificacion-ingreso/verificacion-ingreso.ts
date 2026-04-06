import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { VerificacionService, VerificacionRequest } from '../../../services/verificacion';
import { IconComponent } from '../../../components/icon/icon.component';
import { getBadgeClasses } from '../../../utils/badge-styles';

@Component({
  selector: 'app-verificacion-ingreso',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './verificacion-ingreso.html'
})
export class VerificacionIngresoComponent {
  private verificacionService = inject(VerificacionService);
  private router = inject(Router);

  // ── Estado de la vista ───────────────────────────────────────────
  paso: 'escanear' | 'validar' = 'escanear';
  isLoading = false;

  // ── Datos ────────────────────────────────────────────────────────
  codigoQRInput = '';
  datosExpediente: any = null;

  // ── PASO 1: Buscar por QR ────────────────────────────────────────
  buscarQR() {
    if (!this.codigoQRInput.trim()) {
      Swal.fire({
        icon: 'warning',
        title: 'Campo requerido',
        text: 'Ingrese o escanee un código QR.',
        confirmButtonColor: '#0891b2'
      });
      return;
    }

    this.isLoading = true;
    this.verificacionService.consultarPorQR(this.codigoQRInput.trim()).subscribe({
      next: (data) => {
        this.datosExpediente = data;
        this.paso = 'validar';
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        Swal.fire({
          icon: 'error',
          title: 'No encontrado',
          text: 'El código escaneado no corresponde a un expediente válido o en traslado.',
          confirmButtonColor: '#0891b2'
        });
      }
    });
  }

  // ── PASO 2: Confirmar ingreso ────────────────────────────────────
  confirmarIngreso() {
    this.enviarVerificacion();
  }

  // ── Envío al backend ─────────────────────────────────────────────
  private enviarVerificacion() {
    this.isLoading = true;

    const request: VerificacionRequest = {
      codigoExpedienteBrazalete: this.datosExpediente.codigoExpediente,
      hcBrazalete: this.datosExpediente.hc,
      tipoDocumentoBrazalete: this.datosExpediente.tipoDocumento ?? '',
      numeroDocumentoBrazalete: this.datosExpediente.numeroDocumento ?? '',
      nombreCompletoBrazalete: this.datosExpediente.nombreCompleto,
      servicioBrazalete: this.datosExpediente.servicioFallecimiento,
      brazaletePresente: true,
      observaciones: 'Ingreso verificado por Vigilante Mortuorio.'
    };

    this.verificacionService.registrarIngreso(request).subscribe({
      next: (res) => {
        this.isLoading = false;

        if (res.aprobada) {
          Swal.fire({
            icon: 'success',
            title: 'Ingreso Registrado',
            html: `<p class="text-gray-600">El expediente <strong>${this.datosExpediente.codigoExpediente}</strong> ingresó al mortuorio.</p>
                   <p class="text-sm text-gray-500 mt-1">Ahora debe asignarse a una bandeja.</p>`,
            confirmButtonColor: '#0891b2',
            confirmButtonText: 'Entendido'
          }).then(() => this.router.navigate(['/dashboard']));
        }
      },
      error: (err) => {
        this.isLoading = false;
        const backendMsg: string = err.error?.message ?? '';
        const { title, text, icon } = this.resolverMensajeError(backendMsg);
        Swal.fire({ icon, title, text, confirmButtonColor: '#0891b2' });
      }
    });
  }

  // ── Resolución de mensajes de error del backend ──────────────────
  /** Mapea el mensaje de error del backend a un mensaje amigable para el vigilante */
  private resolverMensajeError(backendMsg: string): {
    title: string; text: string; icon: 'error' | 'warning' | 'info'
  } {
    if (backendMsg.includes('PendienteAsignacionBandeja'))
      return {
        icon: 'info',
        title: 'Ingreso ya registrado',
        text: 'Este expediente ya fue ingresado al mortuorio y está pendiente de asignación de bandeja.'
      };

    if (backendMsg.includes('EnBandeja'))
      return {
        icon: 'info',
        title: 'Expediente en bandeja',
        text: 'Este expediente ya tiene una bandeja asignada dentro del mortuorio.'
      };

    if (backendMsg.includes('PendienteRetiro'))
      return {
        icon: 'info',
        title: 'Pendiente de retiro',
        text: 'Este expediente ya está autorizado para retiro. No requiere nuevo ingreso.'
      };

    if (backendMsg.includes('Retirado'))
      return {
        icon: 'info',
        title: 'Expediente retirado',
        text: 'Este cuerpo ya fue retirado del mortuorio. El expediente está cerrado.'
      };

    if (backendMsg.includes('VerificacionRechazada'))
      return {
        icon: 'warning',
        title: 'Verificación rechazada previamente',
        text: 'Este expediente tiene una verificación rechazada. Contacte a Enfermería.'
      };

    if (backendMsg.includes('custodia') || backendMsg.includes('Ambulancia'))
      return {
        icon: 'warning',
        title: 'Sin custodia registrada',
        text: 'El expediente no registra entrega por parte del Técnico de Ambulancia. Verifique el estado del traslado.'
      };

    if (backendMsg.includes('QR') || backendMsg.includes('código'))
      return {
        icon: 'error',
        title: 'Código no válido',
        text: 'El código escaneado no corresponde a ningún expediente activo.'
      };

    return {
      icon: 'error',
      title: 'Error al procesar',
      text: backendMsg || 'No se pudo procesar el ingreso. Intente nuevamente o contacte a soporte.'
    };
  }

  // ── Cancelar y volver al paso 1 ──────────────────────────────────
  cancelar() {
    this.paso = 'escanear';
    this.datosExpediente = null;
    this.codigoQRInput = '';
  }

  // ── Helpers de template ──────────────────────────────────────────

  /** Retorna true si el paciente es NN */
  get esNN(): boolean {
    return this.datosExpediente?.esNN === true;
  }

  /** Retorna true si la causa es violenta o dudosa */
  get esCausaViolenta(): boolean {
    return this.datosExpediente?.causaViolentaODudosa === true;
  }

  /** Documento formateado para mostrar en pantalla */
  get documentoDisplay(): string {
    if (this.esNN) return 'No Identificado (NN)';
    const tipo = this.datosExpediente?.tipoDocumento ?? '';
    const num = this.datosExpediente?.numeroDocumento ?? '';
    return tipo && num ? `${tipo}: ${num}` : (num || '—');
  }

  getBadgeClasses(estado: string): string {
    return getBadgeClasses(estado);
  }
}
