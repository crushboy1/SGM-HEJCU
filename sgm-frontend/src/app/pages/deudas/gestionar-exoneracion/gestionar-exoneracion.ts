import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

// Services
import { BusquedaExpediente } from '../../../services/busqueda-expediente';
import { DeudaEconomica } from '../../../services/deuda-economica';
import { AuthService } from '../../../services/auth';
import { Expediente } from '../../../services/expediente';

// Models
import { DeudaEconomicaDTO, AplicarExoneracionDTO } from '../../../models/deuda-economica.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';
import { UploadPdf } from '../../../components/upload-pdf/upload-pdf';

@Component({
  selector: 'app-gestionar-exoneracion',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, UploadPdf],
  templateUrl: './gestionar-exoneracion.html',
  styleUrl: './gestionar-exoneracion.css'
})
export class GestionarExoneracionComponent implements OnInit {
  private busquedaService = inject(BusquedaExpediente);
  private deudaService = inject(DeudaEconomica);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Estados
  busqueda = { termino: '', tipo: 'HC' as 'HC' | 'DNI' | 'CODIGO', buscando: false };
  expedienteActual: Expediente | null = null;
  deudaActual: DeudaEconomicaDTO | null = null;

  archivoSustento: File | null = null;
  guardando = false;

  // Formulario
  formExoneracion = {
    tipo: 'Total', // "Total" | "Parcial"
    monto: 0,
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
        confirmButtonColor: '#8B5CF6'
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

    this.deudaService.obtenerPorExpediente(expediente.expedienteID).subscribe({
      next: (deuda) => {
        if (deuda && deuda.estado === 'Pendiente') {
          this.deudaActual = deuda;
        } else {
          Swal.fire({
            icon: 'info',
            title: 'Sin Deuda Pendiente',
            text: 'No hay deuda pendiente para exonerar en este expediente.',
            confirmButtonColor: '#8B5CF6'
          });
        }
      },
      error: () => {
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo cargar la información de la deuda.',
          confirmButtonColor: '#8B5CF6'
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
      confirmButtonColor: '#8B5CF6'
    });
  }

  // ================================================================
  // LOGICA EXONERACIÓN
  // ================================================================
  onArchivoSeleccionado(file: File) {
    this.archivoSustento = file;
  }

  iniciarExoneracion() {
    // Validación 1: Archivo PDF
    if (!this.archivoSustento) {
      Swal.fire({
        icon: 'warning',
        title: 'Falta Archivo',
        text: 'Debe subir el Informe Social (PDF) como sustento.',
        confirmButtonColor: '#8B5CF6'
      });
      return;
    }

    // Validación 2: Observaciones
    if (!this.formExoneracion.observaciones || this.formExoneracion.observaciones.trim().length < 20) {
      Swal.fire({
        icon: 'warning',
        title: 'Falta Motivo',
        text: 'Ingrese el motivo de la exoneración (mínimo 20 caracteres).',
        confirmButtonColor: '#8B5CF6'
      });
      return;
    }

    const montoPendiente = this.deudaActual!.montoPendiente;
    let montoAExonerar = 0;

    if (this.formExoneracion.tipo === 'Total') {
      montoAExonerar = montoPendiente;
    } else {
      montoAExonerar = this.formExoneracion.monto;
      if (montoAExonerar <= 0 || montoAExonerar >= montoPendiente) {
        Swal.fire({
          icon: 'warning',
          title: 'Monto Inválido',
          text: 'El monto parcial debe ser mayor a 0 y menor al total pendiente.',
          confirmButtonColor: '#8B5CF6'
        });
        return;
      }
    }

    Swal.fire({
      icon: 'question',
      title: '¿Aprobar Exoneración?',
      html: `
        <div class="text-left space-y-2">
          <p><strong>Expediente:</strong> ${this.expedienteActual!.codigoExpediente}</p>
          <p><strong>Tipo:</strong> ${this.formExoneracion.tipo}</p>
          <p><strong>Monto a Exonerar:</strong> <span class="text-purple-600 font-bold">S/ ${montoAExonerar.toFixed(2)}</span></p>
          <p class="text-xs text-gray-500 mt-2">Esta acción quedará registrada en el historial de auditoría.</p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, Aprobar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#8B5CF6',
      cancelButtonColor: '#6B7280'
    }).then((r) => {
      if (r.isConfirmed) this.subirYGuardar(montoAExonerar);
    });
  }

  private subirYGuardar(montoFinal: number) {
    this.guardando = true;

    // PASO 1: Subir Archivo PDF
    this.deudaService.uploadPDF(this.archivoSustento!).subscribe({
      next: (ruta) => {

        try {
          // PASO 2: Registrar Exoneración con la ruta obtenida
          const dto: AplicarExoneracionDTO = {
            expedienteID: this.expedienteActual!.expedienteID,
            montoExonerado: montoFinal,
            tipoExoneracion: this.formExoneracion.tipo,
            observaciones: this.formExoneracion.observaciones,
            asistentaSocialID: this.authService.getUserId(),
            rutaPDFSustento: ruta
          };

          this.deudaService.exonerar(this.expedienteActual!.expedienteID, dto).subscribe({
            next: () => {
              this.guardando = false;
              Swal.fire({
                icon: 'success',
                title: 'Exoneración Registrada',
                text: 'La exoneración se aplicó correctamente.',
                confirmButtonColor: '#8B5CF6'
              }).then(() => this.limpiar());
            },
            error: (err) => {
              this.guardando = false;
              Swal.fire({
                icon: 'error',
                title: 'Error al Registrar',
                text: err.message || 'No se pudo registrar la exoneración.',
                confirmButtonColor: '#8B5CF6'
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
            confirmButtonColor: '#8B5CF6'
          }).then(() => {
            this.router.navigate(['/login']);
          });
        }
      },
      error: (err) => {
        this.guardando = false;
        Swal.fire({
          icon: 'error',
          title: 'Error al Subir Archivo',
          text: 'No se pudo subir el archivo PDF. Verifique el formato y tamaño.',
          confirmButtonColor: '#8B5CF6'
        });
      }
    });
  }

  limpiar() {
    this.expedienteActual = null;
    this.deudaActual = null;
    this.archivoSustento = null;
    this.formExoneracion = { tipo: 'Total', monto: 0, observaciones: '' };
    this.busqueda.termino = '';
  }

  volver() {
    this.router.navigate(['/dashboard']);
  }
}
