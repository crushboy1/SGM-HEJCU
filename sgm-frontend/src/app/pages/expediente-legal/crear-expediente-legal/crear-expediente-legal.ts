import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Librerías Externas
import Swal from 'sweetalert2';

// Services
import { ExpedienteLegal } from '../../../services/expediente-legal';
import { ExpedienteService, Expediente } from '../../../services/expediente';
import { AuthService } from '../../../services/auth';
// Models
import { CreateExpedienteLegalDTO, ExpedienteLegalDTO } from '../../../models/expediente-legal.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-crear-expediente-legal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './crear-expediente-legal.html',
  styleUrl: './crear-expediente-legal.css'
})
export class CrearExpedienteLegal {
  private router = inject(Router);
  private expedienteLegalService = inject(ExpedienteLegal);
  private expedienteService = inject(ExpedienteService);
  private authService = inject(AuthService);
  // ===================================================================
  // ESTADOS
  // ===================================================================

  busqueda = {
    termino: '',
    tipo: 'HC', // 'HC' | 'DNI'
    buscando: false,
    encontrado: false,
    data: null as Expediente | null,
    error: null as string | null
  };

  origenCaso: 'NATURAL' | 'LEGAL' = 'NATURAL';

  formData = {
    numeroCertificadoDefuncion: '',
    nombreMedico: '',
    dniMedico: '',
    numeroOficioPNP: '',
    comisaria: '',
    fiscalia: '',
    destino: '',
    observaciones: ''
  };

  guardando: boolean = false;

  // ===================================================================
  // MÉTODOS DE BÚSQUEDA
  // ===================================================================

  buscarPaciente(): void {
    if (!this.busqueda.termino.trim()) {
      this.mostrarAlerta('warning', 'Ingrese un término de búsqueda');
      return;
    }

    this.busqueda.buscando = true;
    this.busqueda.encontrado = false;
    this.busqueda.data = null;
    this.busqueda.error = null;

    this.expedienteService.getAll().subscribe({
      next: (resultados: Expediente[]) => {
        this.busqueda.buscando = false;

        // Filtrar manualmente según tipo de búsqueda
        let expedienteEncontrado: Expediente | undefined;

        if (this.busqueda.tipo === 'HC') {
          expedienteEncontrado = resultados.find(
            exp => exp.hc?.toLowerCase() === this.busqueda.termino.toLowerCase()
          );
        } else if (this.busqueda.tipo === 'DNI') {
          expedienteEncontrado = resultados.find(
            exp => exp.numeroDocumento === this.busqueda.termino
          );
        }

        if (expedienteEncontrado) {
          this.busqueda.data = expedienteEncontrado;
          this.busqueda.encontrado = true;

          // Verifica si el expediente es de tipo externo
          if (this.busqueda.data.tipoExpediente === 'Externo') {
            this.origenCaso = 'LEGAL';
          } else {
            this.origenCaso = 'NATURAL';
            if (this.busqueda.data.medicoCertificaNombre) {
              this.formData.nombreMedico = this.busqueda.data.medicoCertificaNombre;
            }
          }

          this.verificarSiYaTieneLegal(this.busqueda.data.expedienteID);
        } else {
          this.busqueda.error = 'No se encontró paciente con esos datos.';
        }
      },
      error: (err: any) => {
        this.busqueda.buscando = false;
        console.error('Error búsqueda:', err);
        this.busqueda.error = 'Error al conectar con el servidor.';
      }
    });
  }

  private verificarSiYaTieneLegal(expedienteId: number): void {
    this.expedienteLegalService.obtenerPorExpediente(expedienteId).subscribe({
      next: (legal: ExpedienteLegalDTO | null) => {
        if (legal) {
          Swal.fire({
            icon: 'info',
            title: 'Expediente Existente',
            text: `Este paciente ya tiene un trámite registrado (${legal.codigoExpediente}).`,
            confirmButtonText: 'Ir al Detalle',
            confirmButtonColor: '#0891B2'
          }).then(() => {
            this.router.navigate(['/administrativo/legal/detalle', legal.expedienteLegalID]);
          });
        }
      },
      error: () => { /* 404 es correcto */ }
    });
  }

  limpiarBusqueda(): void {
    this.busqueda.termino = '';
    this.busqueda.encontrado = false;
    this.busqueda.data = null;
    this.busqueda.error = null;
    this.resetForm();
  }

  // ===================================================================
  // ACCIONES PRINCIPALES
  // ===================================================================

  guardarRegistro(): void {
    if (!this.busqueda.encontrado || !this.busqueda.data) return;

    if (this.origenCaso === 'NATURAL') {
      if (!this.formData.numeroCertificadoDefuncion) {
        this.mostrarAlerta('warning', 'El N° Certificado de Defunción es obligatorio.');
        return;
      }
    } else {
      if (!this.formData.numeroOficioPNP && !this.formData.fiscalia) {
        this.mostrarAlerta('warning', 'Debe ingresar N° Oficio PNP o Fiscalía.');
        return;
      }
    }

    this.guardando = true;

    const dto: CreateExpedienteLegalDTO = {
      expedienteID: this.busqueda.data.expedienteID,
      tipoCaso: this.origenCaso === 'LEGAL' ? 'CasoExterno' : 'MuerteNatural',
      usuarioRegistroID: this.authService.getUserId(),
      numeroOficioPNP: this.origenCaso === 'LEGAL' ? this.formData.numeroOficioPNP : undefined,
      comisaria: this.origenCaso === 'LEGAL' ? this.formData.comisaria : undefined,
      fiscalia: this.origenCaso === 'LEGAL' ? this.formData.fiscalia : undefined,
      destino: this.formData.destino || undefined,
      observaciones: this.construirObservaciones()
    };

    this.expedienteLegalService.crear(dto).subscribe({
      next: (resp: ExpedienteLegalDTO) => {
        this.guardando = false;

        Swal.fire({
          icon: 'success',
          title: 'Registro Exitoso',
          text: `Se ha registrado el caso ${this.origenCaso === 'NATURAL' ? 'con Certificado' : 'Legal'}.`,
          confirmButtonColor: '#0891B2',
          confirmButtonText: 'Subir Documentos'
        }).then(() => {
          this.router.navigate(['/administrativo/legal/detalle', resp.expedienteLegalID]);
        });
      },
      error: (err: any) => {
        this.guardando = false;
        const msg = err.error?.message || 'Error al guardar el registro.';
        this.mostrarAlerta('error', msg);
      }
    });
  }

  private construirObservaciones(): string {
    const baseObs = this.formData.observaciones || '';

    if (this.origenCaso === 'NATURAL') {
      return `[CERTIFICADO SINADEF: ${this.formData.numeroCertificadoDefuncion}] [MÉDICO: ${this.formData.nombreMedico} CMP:${this.formData.dniMedico}] ${baseObs}`;
    }

    return baseObs;
  }

  cancelar(): void {
    this.router.navigate(['/administrativo/legal/lista']);
  }

  // ===================================================================
  // HELPERS
  // ===================================================================

  private resetForm(): void {
    this.formData = {
      numeroCertificadoDefuncion: '',
      nombreMedico: '',
      dniMedico: '',
      numeroOficioPNP: '',
      comisaria: '',
      fiscalia: '',
      destino: '',
      observaciones: ''
    };
  }

  private mostrarAlerta(icon: 'success' | 'warning' | 'error', title: string): void {
    Swal.fire({ icon, title, toast: true, position: 'top-end', showConfirmButton: false, timer: 3000 });
  }

  getUserId(): number {
    const userIdStr = localStorage.getItem('sgm_user_id');
    return userIdStr ? parseInt(userIdStr, 10) : 0;
  }
}
