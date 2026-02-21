import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

// Librerías Externas
import Swal from 'sweetalert2';

// Services
import { ExpedienteLegal } from '../../../services/expediente-legal';

// Models
import { ExpedienteLegalDTO, ExpedienteLegalHelper } from '../../../models/expediente-legal.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';

// Utils
import { getBadgeClasses } from '../../../utils/badge-styles';

@Component({
  selector: 'app-autorizar-jefe-guardia',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './autorizar-jefe-guardia.html',
  styleUrl: './autorizar-jefe-guardia.css'
})
export class AutorizarJefeGuardia implements OnInit {
  private router = inject(Router);
  private expedienteLegalService = inject(ExpedienteLegal);

  // ===================================================================
  // ESTADO
  // ===================================================================
  expedientes: ExpedienteLegalDTO[] = [];
  loading = true;
  error: string | null = null;
  procesando: number | null = null;

  // ===================================================================
  // HELPERS
  // ===================================================================
  helper = ExpedienteLegalHelper;

  // ===================================================================
  // LIFECYCLE
  // ===================================================================
  ngOnInit(): void {
    this.cargarPendientes();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================
  cargarPendientes(): void {
    this.loading = true;
    this.error = null;

    this.expedienteLegalService.obtenerPendientesAutorizacion().subscribe({
      next: (data) => {
        this.expedientes = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar pendientes:', err);
        this.error = 'Error al cargar expedientes pendientes';
        this.loading = false;
      }
    });
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================
  autorizar(expediente: ExpedienteLegalDTO): void {
    Swal.fire({
      title: '¿Autorizar Levantamiento?',
      html: `
        <div class="text-left space-y-2">
          <p class="font-semibold">${expediente.nombrePaciente}</p>
          <p class="text-sm text-gray-600">Código: ${expediente.codigoExpediente}</p>
          <p class="text-sm text-gray-600">Comisaría: ${expediente.comisaria || 'N/A'}</p>
        </div>
        <div class="mt-4 p-3 bg-green-50 rounded-lg text-sm text-left">
          <p class="font-medium text-green-900">Al autorizar:</p>
          <ul class="mt-2 space-y-1 text-green-800">
            <li>✓ El expediente quedará listo para levantamiento</li>
            <li>✓ Vigilancia podrá proceder con el retiro</li>
            <li>✓ Se notificará a las áreas correspondientes</li>
          </ul>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Sí, Autorizar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#10B981',
      cancelButtonColor: '#6B7280',
      showConfirmButton: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.ejecutarAutorizacion(expediente);
      }
    });
  }

  private ejecutarAutorizacion(expediente: ExpedienteLegalDTO): void {
    this.procesando = expediente.expedienteLegalID;

    this.expedienteLegalService.autorizarJefeGuardia({
      expedienteLegalID: expediente.expedienteLegalID,
      validado: true,
      observacionesValidacion: 'Autorizado para levantamiento'
    }).subscribe({
      next: () => {
        this.procesando = null;

        Swal.fire({
          icon: 'success',
          title: 'Expediente Autorizado',
          text: `El expediente ${expediente.codigoExpediente} está listo para levantamiento`,
          confirmButtonColor: '#10B981',
          showConfirmButton: true
        });

        // Remover de la lista
        this.expedientes = this.expedientes.filter(
          e => e.expedienteLegalID !== expediente.expedienteLegalID
        );
      },
      error: (err) => {
        this.procesando = null;
        console.error('Error al autorizar:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: err.error?.message || 'No se pudo autorizar el expediente',
          confirmButtonColor: '#EF4444',
          showConfirmButton: true
        });
      }
    });
  }

  verDetalle(expedienteId: number): void {
    this.router.navigate(['/administrativo/legal/detalle', expedienteId]);
  }

  // ===================================================================
  // HELPERS
  // ===================================================================
  getEstadoBadge(estado: string): string {
    return getBadgeClasses(estado);
  }
}
