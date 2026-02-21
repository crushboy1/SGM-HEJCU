import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Librerías
import Swal from 'sweetalert2';

// Services
import { BusquedaExpediente } from '../../../services/busqueda-expediente';
import { DeudaEconomica } from '../../../services/deuda-economica';
import { AuthService } from '../../../services/auth';
import { Expediente } from '../../../services/expediente';

// Models
import { DeudaEconomicaDTO, LiquidarDeudaEconomicaDTO } from '../../../models/deuda-economica.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-liquidar-deuda-economica',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './liquidar-deuda-economica.html',
  styleUrl: './liquidar-deuda-economica.css'
})
export class LiquidarDeudaEconomicaComponent implements OnInit {
  private busquedaService = inject(BusquedaExpediente);
  private deudaService = inject(DeudaEconomica);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Estados
  busqueda = { termino: '', tipo: 'HC' as 'HC' | 'DNI' | 'CODIGO', buscando: false };

  expedienteActual: Expediente | null = null;
  deudaActual: DeudaEconomicaDTO | null = null;
  loadingDeuda = false;
  guardando = false;

  // Formulario
  formLiquidacion = {
    numeroBoleta: '',
    montoPagado: 0,
    observaciones: ''
  };

  ngOnInit() { }

  // ================================================================
  // BÚSQUEDA
  // ================================================================
  buscarPaciente() {
    const termino = this.busqueda.termino.trim();

    if (!termino) {
      Swal.fire({
        icon: 'warning',
        title: 'Campo Vacío',
        text: 'Ingrese un término de búsqueda',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    this.busqueda.buscando = true;
    this.expedienteActual = null;
    this.deudaActual = null;

    this.busquedaService.buscar(termino, this.busqueda.tipo).subscribe({
      next: (expediente) => this.cargarDeuda(expediente),
      error: (err) => this.manejarError(err.message)
    });
  }

  private cargarDeuda(expediente: Expediente) {
    this.busqueda.buscando = false;
    this.expedienteActual = expediente;
    this.loadingDeuda = true;

    this.deudaService.obtenerPorExpediente(expediente.expedienteID).subscribe({
      next: (deuda) => {
        this.loadingDeuda = false;
        if (deuda && deuda.estado === 'Pendiente') {
          this.deudaActual = deuda;
          // Pre-llenar con el monto restante para agilizar a Caja
          this.formLiquidacion.montoPagado = deuda.montoPendiente || deuda.montoDeuda;
        } else if (deuda) {
          Swal.fire({
            icon: 'info',
            title: 'Deuda Ya Procesada',
            text: `Esta deuda ya fue ${deuda.estado.toLowerCase()}.`,
            confirmButtonColor: '#0891B2'
          });
        } else {
          Swal.fire({
            icon: 'info',
            title: 'Sin Deuda Registrada',
            text: 'Este expediente no tiene deuda económica registrada.',
            confirmButtonColor: '#0891B2'
          });
        }
      },
      error: () => {
        this.loadingDeuda = false;
        this.deudaActual = null;
        Swal.fire({
          icon: 'info',
          title: 'Sin Deuda',
          text: 'No hay registro de deuda para este expediente.',
          confirmButtonColor: '#0891B2'
        });
      }
    });
  }

  private manejarError(mensaje: string) {
    this.busqueda.buscando = false;
    Swal.fire({
      icon: 'warning',
      title: 'Búsqueda Sin Resultados',
      text: mensaje,
      confirmButtonColor: '#0891B2'
    });
  }

  // ================================================================
  // ACCIONES
  // ================================================================
  confirmarPago() {
    if (!this.deudaActual || !this.expedienteActual) return;

    // Validación 1: Número de boleta
    if (!this.formLiquidacion.numeroBoleta || this.formLiquidacion.numeroBoleta.trim().length < 3) {
      Swal.fire({
        icon: 'warning',
        title: 'N° Boleta Requerido',
        text: 'Ingrese el número de boleta o recibo de pago.',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    // Validación 2: Monto válido
    if (this.formLiquidacion.montoPagado <= 0) {
      Swal.fire({
        icon: 'warning',
        title: 'Monto Inválido',
        text: 'El monto debe ser mayor a 0.',
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    // Validación 3: Monto no excede pendiente
    if (this.formLiquidacion.montoPagado > this.deudaActual.montoPendiente) {
      Swal.fire({
        icon: 'warning',
        title: 'Monto Excede Deuda',
        text: `El monto no puede ser mayor al pendiente (S/ ${this.deudaActual.montoPendiente.toFixed(2)}).`,
        confirmButtonColor: '#0891B2'
      });
      return;
    }

    Swal.fire({
      icon: 'question',
      title: '¿Confirmar Pago?',
      html: `
        <div class="text-left space-y-2">
          <p><strong>Expediente:</strong> ${this.expedienteActual.codigoExpediente}</p>
          <p><strong>N° Boleta:</strong> ${this.formLiquidacion.numeroBoleta}</p>
          <p><strong>Monto a Pagar:</strong> <span class="text-hospital-cyan font-bold">S/ ${this.formLiquidacion.montoPagado.toFixed(2)}</span></p>
          <p class="text-xs text-gray-500 mt-2">Esta acción quedará registrada en el historial de auditoría.</p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, Registrar Pago',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280'
    }).then((r) => {
      if (r.isConfirmed) this.ejecutarLiquidacion();
    });
  }

  private ejecutarLiquidacion() {
    this.guardando = true;

    try {
      const dto: LiquidarDeudaEconomicaDTO = {
        numeroBoleta: this.formLiquidacion.numeroBoleta,
        montoPagado: this.formLiquidacion.montoPagado,
        observaciones: this.formLiquidacion.observaciones || undefined,
        usuarioActualizacionID: this.authService.getUserId()
      };

      this.deudaService.liquidar(this.expedienteActual!.expedienteID, dto).subscribe({
        next: () => {
          this.guardando = false;
          Swal.fire({
            icon: 'success',
            title: 'Pago Registrado',
            text: 'La deuda ha sido actualizada correctamente.',
            confirmButtonColor: '#0891B2'
          }).then(() => {
            this.limpiar();
          });
        },
        error: (err) => {
          this.guardando = false;
          Swal.fire({
            icon: 'error',
            title: 'Error al Registrar',
            text: err.message || 'No se pudo registrar el pago.',
            confirmButtonColor: '#0891B2'
          });
        }
      });
    } catch (error: any) {
      // Capturar error de getUserId()
      this.guardando = false;
      Swal.fire({
        icon: 'error',
        title: 'Sesión Inválida',
        text: 'Por favor, inicie sesión nuevamente.',
        confirmButtonColor: '#0891B2'
      }).then(() => {
        this.router.navigate(['/login']);
      });
    }
  }

  limpiar() {
    this.expedienteActual = null;
    this.deudaActual = null;
    this.busqueda.termino = '';
    this.formLiquidacion = { numeroBoleta: '', montoPagado: 0, observaciones: '' };
  }

  volver() {
    this.router.navigate(['/dashboard']);
  }
}
