import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

// Librerías Externas
import Swal from 'sweetalert2';

// Services
import { ExpedienteLegal } from '../../../services/expediente-legal';

// Models
import {
  ExpedienteLegalDTO,
  AutoridadExternaDTO,
  DocumentoLegalDTO,
  CreateAutoridadExternaDTO,
  ExpedienteLegalHelper
} from '../../../models/expediente-legal.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';

// Utils
import { getBadgeClasses } from '../../../utils/badge-styles';

@Component({
  selector: 'app-detalle-expediente-legal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './detalle-expediente-legal.html',
  styleUrl: './detalle-expediente-legal.css'
})
export class DetalleExpedienteLegal implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private expedienteLegalService = inject(ExpedienteLegal);

  // ===================================================================
  // ESTADO PRINCIPAL
  // ===================================================================
  expediente: ExpedienteLegalDTO | null = null;
  loading = true;
  error: string | null = null;

  // ===================================================================
  // SUB-ESTADOS
  // ===================================================================
  procesando = false;
  subiendoArchivo = false;
  agregandoAutoridad = false;

  // ===================================================================
  // FORMULARIOS
  // ===================================================================
  nuevaAutoridad: CreateAutoridadExternaDTO = {
    expedienteLegalID: 0,
    tipoAutoridad: 'Policia',
    nombreCompleto: '',
    numeroPlaca: '',
    institucion: '',
    fechaHoraLlegada: new Date().toISOString().slice(0, 16)
  };

  archivoSeleccionado: File | null = null;
  tipoDocumentoSubir: 'Epicrisis' | 'OficioPNP' | 'ActaLevantamiento' = 'OficioPNP';
  numeroDocumentoSubir = '';

  // ===================================================================
  // HELPERS
  // ===================================================================
  helper = ExpedienteLegalHelper;

  // ===================================================================
  // LIFECYCLE
  // ===================================================================
  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.cargarExpediente(id);
    } else {
      this.error = 'ID de expediente inválido';
      this.loading = false;
    }
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================
  cargarExpediente(id: number): void {
    this.loading = true;
    this.error = null;

    this.expedienteLegalService.obtenerPorId(id).subscribe({
      next: (data) => {
        this.expediente = data;
        this.nuevaAutoridad.expedienteLegalID = data.expedienteLegalID;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar expediente:', err);
        this.error = 'No se pudo cargar el expediente legal';
        this.loading = false;
      }
    });
  }

  // ===================================================================
  // GESTIÓN DE AUTORIDADES
  // ===================================================================
  mostrarFormularioAutoridad(): void {
    this.agregandoAutoridad = !this.agregandoAutoridad;
    this.resetFormAutoridad();
  }

  guardarAutoridad(): void {
    if (!this.validarFormAutoridad()) return;

    this.procesando = true;

    this.expedienteLegalService.agregarAutoridad(this.nuevaAutoridad).subscribe({
      next: (autoridad) => {
        this.procesando = false;
        this.agregandoAutoridad = false;

        // Agregar a la lista local
        if (this.expediente) {
          this.expediente.autoridades.push(autoridad);
          this.expediente.cantidadAutoridades++;
        }

        Swal.fire({
          icon: 'success',
          title: 'Autoridad Registrada',
          text: `${autoridad.nombreCompleto} agregado correctamente`,
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });

        this.resetFormAutoridad();
      },
      error: (err) => {
        this.procesando = false;
        console.error('Error al agregar autoridad:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: err.error?.message || 'No se pudo registrar la autoridad',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
      }
    });
  }

  eliminarAutoridad(autoridadId: number): void {
    Swal.fire({
      title: '¿Eliminar Autoridad?',
      text: 'Esta acción no se puede deshacer',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, Eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#6B7280',
      showConfirmButton: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.ejecutarEliminacionAutoridad(autoridadId);
      }
    });
  }

  private ejecutarEliminacionAutoridad(autoridadId: number): void {
    this.expedienteLegalService.eliminarAutoridad(autoridadId).subscribe({
      next: () => {
        // Remover de la lista local
        if (this.expediente) {
          this.expediente.autoridades = this.expediente.autoridades.filter(
            a => a.autoridadExternaID !== autoridadId
          );
          this.expediente.cantidadAutoridades--;
        }

        Swal.fire({
          icon: 'success',
          title: 'Eliminado',
          text: 'Autoridad eliminada correctamente',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
      },
      error: (err) => {
        console.error('Error al eliminar autoridad:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo eliminar la autoridad',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
      }
    });
  }

  private validarFormAutoridad(): boolean {
    if (!this.nuevaAutoridad.nombreCompleto.trim()) {
      Swal.fire('Campo Requerido', 'Ingrese el nombre completo', 'warning');
      return false;
    }

    if (this.nuevaAutoridad.tipoAutoridad === 'Policia' && !this.nuevaAutoridad.numeroPlaca?.trim()) {
      Swal.fire('Campo Requerido', 'Ingrese el número de placa', 'warning');
      return false;
    }

    if (!this.nuevaAutoridad.institucion?.trim()) {
      Swal.fire('Campo Requerido', 'Ingrese la institución', 'warning');
      return false;
    }

    return true;
  }

  private resetFormAutoridad(): void {
    this.nuevaAutoridad = {
      expedienteLegalID: this.expediente?.expedienteLegalID || 0,
      tipoAutoridad: 'Policia',
      nombreCompleto: '',
      numeroPlaca: '',
      institucion: '',
      fechaHoraLlegada: new Date().toISOString().slice(0, 16)
    };
  }

  // ===================================================================
  // GESTIÓN DE DOCUMENTOS
  // ===================================================================
  seleccionarArchivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];

      // Validar tamaño (10MB)
      if (file.size > 10 * 1024 * 1024) {
        Swal.fire({
          icon: 'error',
          title: 'Archivo muy grande',
          text: 'El archivo no debe superar 10MB',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
        input.value = '';
        return;
      }

      // Validar tipo
      if (!file.type.includes('pdf') && !file.type.includes('image')) {
        Swal.fire({
          icon: 'error',
          title: 'Tipo no permitido',
          text: 'Solo se permiten archivos PDF o imágenes',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
        input.value = '';
        return;
      }

      this.archivoSeleccionado = file;
    }
  }

  subirDocumento(): void {
    if (!this.archivoSeleccionado) {
      Swal.fire('Archivo Requerido', 'Seleccione un archivo', 'warning');
      return;
    }

    if (!this.numeroDocumentoSubir.trim()) {
      Swal.fire('Número Requerido', 'Ingrese el número del documento', 'warning');
      return;
    }

    this.subiendoArchivo = true;

    this.expedienteLegalService.subirDocumento(
      this.expediente!.expedienteLegalID,
      this.tipoDocumentoSubir,
      this.numeroDocumentoSubir,
      this.archivoSeleccionado
    ).subscribe({
      next: (documento) => {
        this.subiendoArchivo = false;

        // Agregar a la lista local
        if (this.expediente) {
          this.expediente.documentos.push(documento);
          this.expediente.cantidadDocumentos++;
        }

        Swal.fire({
          icon: 'success',
          title: 'Documento Subido',
          text: 'El archivo se subió correctamente',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });

        this.resetFormDocumento();
      },
      error: (err) => {
        this.subiendoArchivo = false;
        console.error('Error al subir documento:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: err.error?.message || 'No se pudo subir el documento',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
      }
    });
  }

  private resetFormDocumento(): void {
    this.archivoSeleccionado = null;
    this.numeroDocumentoSubir = '';
    const input = document.getElementById('file-input') as HTMLInputElement;
    if (input) input.value = '';
  }

  // ===================================================================
  // ACCIONES DE FLUJO
  // ===================================================================
  marcarListoParaAdmision(): void {
    Swal.fire({
      title: '¿Marcar como Listo?',
      html: `
        <p class="text-sm text-gray-600 mb-3">
          Al confirmar, el expediente pasará a validación de Admisión.
        </p>
        <div class="text-left bg-blue-50 p-3 rounded-lg">
          <p class="font-semibold text-blue-900 mb-2">Verificar que se hayan registrado:</p>
          <ul class="text-sm text-blue-800 space-y-1">
            <li>✓ Autoridades presentes (PNP/Fiscal/Legista)</li>
            <li>✓ Documentos escaneados (Epicrisis/Oficio/Acta)</li>
          </ul>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Sí, Marcar Listo',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#0891B2',
      cancelButtonColor: '#6B7280',
      showConfirmButton: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.ejecutarMarcarListo();
      }
    });
  }

  private ejecutarMarcarListo(): void {
    this.procesando = true;

    this.expedienteLegalService.marcarListoAdmision(
      this.expediente!.expedienteLegalID
    ).subscribe({
      next: () => {
        this.procesando = false;

        Swal.fire({
          icon: 'success',
          title: 'Expediente Actualizado',
          text: 'El expediente está listo para validación de Admisión',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });

        // Recargar para actualizar estado
        this.cargarExpediente(this.expediente!.expedienteLegalID);
      },
      error: (err) => {
        this.procesando = false;
        console.error('Error:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: err.error?.message || 'No se pudo actualizar el estado',
          confirmButtonColor: '#0891B2',
          showConfirmButton: true
        });
      }
    });
  }

  // ===================================================================
  // NAVEGACIÓN
  // ===================================================================
  volver(): void {
    this.router.navigate(['/administrativo/legal/lista']);
  }

  // ===================================================================
  // HELPERS
  // ===================================================================
  getEstadoBadge(estado: string): string {
    return getBadgeClasses(estado);
  }

  getTipoAutoridadIcon(tipo: string): string {
    const iconos: Record<string, string> = {
      'Policia': 'shield',
      'Fiscal': 'scale',
      'MedicoLegista': 'activity'
    };
    return iconos[tipo] || 'user';
  }

  getTipoDocumentoIcon(tipo: string): string {
    const iconos: Record<string, string> = {
      'Epicrisis': 'file-text',
      'OficioPNP': 'file-check',
      'ActaLevantamiento': 'clipboard'
    };
    return iconos[tipo] || 'file';
  }
}
