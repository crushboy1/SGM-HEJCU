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
import { DeudaEconomicaDTO } from '../../../models/deuda-economica.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';
import { FormDeudaEconomica } from '../../../components/form-deuda-economica/form-deuda-economica';

interface BusquedaState {
  terminoBusqueda: string;
  tipoBusqueda: 'HC' | 'DNI' | 'CODIGO';
  buscando: boolean;
  expedienteEncontrado: Expediente | null;
  errorBusqueda: string | null;
}

@Component({
  selector: 'app-registrar-deuda-economica',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, FormDeudaEconomica],
  templateUrl: './registrar-deuda-economica.html',
  styleUrl: './registrar-deuda-economica.css'
})
export class RegistrarDeudaEconomicaComponent implements OnInit {
  private busquedaService = inject(BusquedaExpediente);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Estado de la búsqueda
  busqueda: BusquedaState = {
    terminoBusqueda: '',
    tipoBusqueda: 'HC', // Por defecto buscar por Historia Clínica
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
      this.mostrarAlerta('warning', 'Ingrese un término de búsqueda');
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

  limpiarBusqueda(): void {
    this.busqueda.terminoBusqueda = '';
    this.busqueda.expedienteEncontrado = null;
    this.mostrarFormulario = false;
    this.busqueda.errorBusqueda = null;
  }

  private procesarResultado(expediente: Expediente): void {
    this.busqueda.buscando = false;
    this.busqueda.expedienteEncontrado = expediente;
    this.mostrarFormulario = true;

    // Pequeño feedback visual
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 2000
    });
    Toast.fire({ icon: 'success', title: 'Expediente encontrado' });
  }

  private manejarError(err: any): void {
    this.busqueda.buscando = false;
    console.error('[RegistrarDeudaEconomica] Error:', err);

    const mensaje = err.message || 'Error al buscar expediente';
    this.busqueda.errorBusqueda = mensaje;

    this.mostrarAlerta('warning', mensaje);
  }

  private mostrarAlerta(icon: 'success' | 'warning' | 'error', title: string): void {
    Swal.fire({
      icon,
      title,
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000
    });
  }

  // ===================================================================
  // MANEJO DE EVENTOS DEL FORMULARIO HIJO
  // ===================================================================

  handleRegistroExitoso(deuda: DeudaEconomicaDTO): void {
    // Preguntar si desea seguir registrando o salir
    Swal.fire({
      icon: 'success',
      title: 'Bloqueo Económico Activado',
      text: `Se ha registrado la deuda para ${deuda.codigoExpediente}. El retiro está bloqueado.`,
      showCancelButton: true,
      confirmButtonText: 'Registrar Otro',
      cancelButtonText: 'Volver al Dashboard',
      confirmButtonColor: '#0891B2',
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
