import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

// External libs
import Swal from 'sweetalert2';

// Services
import { BusquedaExpediente } from '../../../services/busqueda-expediente';
import { AuthService } from '../../../services/auth';
import { Expediente } from '../../../services/expediente';

// Models
import { DeudaSangreDTO } from '../../../models/deuda-sangre.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';
import { FormDeudaSangre } from '../../../components/form-deuda-sangre/form-deuda-sangre';

interface BusquedaState {
  terminoBusqueda: string;
  tipoBusqueda: 'HC' | 'DNI' | 'CODIGO';
  buscando: boolean;
  expedienteEncontrado: Expediente | null;
  errorBusqueda: string | null;
}

@Component({
  selector: 'app-registrar-deuda-sangre',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, FormDeudaSangre],
  templateUrl: './registrar-deuda-sangre.html'
})
export class RegistrarDeudaSangreComponent implements OnInit {
  private busquedaService = inject(BusquedaExpediente);
  private authService = inject(AuthService);
  private router = inject(Router);

  busqueda: BusquedaState = {
    terminoBusqueda: '',
    tipoBusqueda: 'HC', // Por defecto HC (lo más común para Banco Sangre)
    buscando: false,
    expedienteEncontrado: null,
    errorBusqueda: null
  };

  mostrarFormulario = false;
  userName: string = '';

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
  }

  // ===================================================================
  // LÓGICA DE BÚSQUEDA 
  // ===================================================================
  buscarExpediente(): void {
    const termino = this.busqueda.terminoBusqueda.trim();

    if (!termino) {
      Swal.fire({
        icon: 'warning',
        title: 'Campo Vacío',
        text: 'Ingrese un término de búsqueda',
        confirmButtonColor: '#DC2626'
      });
      return;
    }

    this.busqueda.buscando = true;
    this.busqueda.errorBusqueda = null;
    this.busqueda.expedienteEncontrado = null;
    this.mostrarFormulario = false;

    this.busquedaService.buscar(termino, this.busqueda.tipoBusqueda).subscribe({
      next: (expediente) => this.procesarResultado(expediente),
      error: (err) => this.manejarError(err)
    });
  }

  private procesarResultado(expediente: Expediente): void {
    this.busqueda.buscando = false;
    this.busqueda.expedienteEncontrado = expediente;
    this.mostrarFormulario = true;

    // Feedback visual sin toast
    Swal.fire({
      icon: 'success',
      title: 'Expediente Encontrado',
      text: `${expediente.nombreCompleto} - HC: ${expediente.hc}`,
      timer: 2000,
      showConfirmButton: false
    });
  }

  private manejarError(err: any): void {
    this.busqueda.buscando = false;
    console.error('[RegistrarDeudaSangre] Error en búsqueda:', err);

    const mensaje = err.message || 'Error al buscar expediente';
    this.busqueda.errorBusqueda = mensaje;

    Swal.fire({
      icon: 'warning',
      title: 'Búsqueda Sin Resultados',
      text: mensaje,
      confirmButtonColor: '#DC2626'
    });
  }

  limpiarBusqueda(): void {
    this.busqueda.terminoBusqueda = '';
    this.busqueda.expedienteEncontrado = null;
    this.busqueda.errorBusqueda = null;
    this.mostrarFormulario = false;
  }

  // ===================================================================
  // MANEJO DE EVENTOS DEL FORMULARIO HIJO
  // ===================================================================

  handleRegistroExitoso(deuda: DeudaSangreDTO): void {
    Swal.fire({
      icon: 'success',
      title: 'Bloqueo de Sangre Activado',
      html: `
        <div class="text-left space-y-2">
          <p>Se registró la deuda de sangre para el expediente <strong>${deuda.codigoExpediente}</strong>.</p>
          <p class="text-sm text-gray-600 mt-2">
            El Supervisor de Vigilancia verá el bloqueo inmediatamente en el semáforo.
          </p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Registrar Otro',
      cancelButtonText: 'Volver al Dashboard',
      confirmButtonColor: '#DC2626',
      cancelButtonColor: '#6B7280'
    }).then((result) => {
      if (result.isConfirmed) {
        this.limpiarBusqueda();
      } else {
        this.router.navigate(['/dashboard']);
      }
    });
  }

  handleCancelar(): void {
    this.limpiarBusqueda();
  }

  volverDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
