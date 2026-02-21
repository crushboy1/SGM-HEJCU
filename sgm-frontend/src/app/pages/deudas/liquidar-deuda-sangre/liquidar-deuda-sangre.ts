import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Librerías
import Swal from 'sweetalert2';

// Servicios
import { BusquedaExpediente } from '../../../services/busqueda-expediente';
import { DeudaSangre, GenerarCompromisoDTO } from '../../../services/deuda-sangre';
import { AuthService } from '../../../services/auth';
import { Expediente } from '../../../services/expediente';

// Models
import { DeudaSangreDTO, LiquidarDeudaSangreDTO } from '../../../models/deuda-sangre.model';

// Componentes Reutilizables
import { IconComponent } from '../../../components/icon/icon.component';
import { UploadPdf } from '../../../components/upload-pdf/upload-pdf';
import { VisorPdfModal } from '../../../components/visor-pdf-modal/visor-pdf-modal';

@Component({
  selector: 'app-liquidar-deuda-sangre',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, UploadPdf, VisorPdfModal],
  templateUrl: './liquidar-deuda-sangre.html'
})
export class LiquidarDeudaSangreComponent implements OnInit {
  private busquedaService = inject(BusquedaExpediente);
  private deudaSangreService = inject(DeudaSangre);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Estados de Búsqueda
  busqueda = {
    termino: '',
    tipo: 'HC' as 'HC' | 'DNI' | 'CODIGO',
    buscando: false,
    error: null as string | null
  };

  // Estado del Paciente Seleccionado
  expedienteActual: Expediente | null = null;
  deudaActual: DeudaSangreDTO | null = null;
  loadingDeuda = false;

  // Formulario de Compromiso (Datos del Familiar)
  datosFamiliar = {
    nombre: '',
    dni: ''
  };

  // Estado de Archivos
  archivoFirmado: File | null = null;

  // Estado del Visor PDF (Para imprimir)
  pdfBlobParaImprimir: Blob | null = null;
  mostrarVisor = false;
  generandoPdf = false;
  guardando = false;

  ngOnInit() { }

  // ================================================================
  // 1. BÚSQUEDA 
  // ================================================================
  buscarPaciente() {
    const termino = this.busqueda.termino.trim();

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
    this.busqueda.error = null;
    this.expedienteActual = null;
    this.deudaActual = null;

    this.busquedaService.buscar(termino, this.busqueda.tipo).subscribe({
      next: (expediente) => this.cargarDeuda(expediente),
      error: (err) => this.manejarError(err)
    });
  }

  private cargarDeuda(expediente: Expediente) {
    this.busqueda.buscando = false;
    this.expedienteActual = expediente;
    this.loadingDeuda = true;

    this.deudaSangreService.obtenerPorExpediente(expediente.expedienteID).subscribe({
      next: (deuda) => {
        this.loadingDeuda = false;

        if (!deuda) {
          Swal.fire({
            icon: 'info',
            title: 'Sin Deuda Registrada',
            text: 'Este expediente no tiene deuda de sangre registrada.',
            confirmButtonColor: '#DC2626'
          });
          return;
        }

        if (deuda.estado !== 'Pendiente') {
          Swal.fire({
            icon: 'info',
            title: 'Deuda Ya Procesada',
            text: `Esta deuda ya fue ${deuda.estado.toLowerCase()}.`,
            confirmButtonColor: '#DC2626'
          });
          return;
        }

        this.deudaActual = deuda;
      },
      error: () => {
        this.loadingDeuda = false;
        this.deudaActual = null;
        Swal.fire({
          icon: 'warning',
          title: 'Sin Deuda Registrada',
          text: 'Este paciente no tiene registro de deuda de sangre.',
          confirmButtonColor: '#DC2626'
        });
      }
    });
  }

  private manejarError(err: any) {
    this.busqueda.buscando = false;
    this.busqueda.error = err.message || 'Error en búsqueda';

    Swal.fire({
      icon: 'warning',
      title: 'Búsqueda Sin Resultados',
      text: err.message || 'No se encontró el paciente',
      confirmButtonColor: '#DC2626'
    });
  }

  // ================================================================
  // 2. GENERAR PDF (Paso 1)
  // ================================================================
  generarImprimible() {
    if (!this.expedienteActual || !this.deudaActual) return;

    // Validación datos del familiar
    if (!this.datosFamiliar.nombre || this.datosFamiliar.nombre.trim().length < 5) {
      Swal.fire({
        icon: 'warning',
        title: 'Datos Incompletos',
        text: 'Ingrese el nombre completo del familiar responsable.',
        confirmButtonColor: '#DC2626'
      });
      return;
    }

    if (!this.datosFamiliar.dni || !/^\d{8}$/.test(this.datosFamiliar.dni)) {
      Swal.fire({
        icon: 'warning',
        title: 'DNI Inválido',
        text: 'Ingrese un DNI válido (8 dígitos).',
        confirmButtonColor: '#DC2626'
      });
      return;
    }

    this.generandoPdf = true;

    const dto: GenerarCompromisoDTO = {
      expedienteID: this.expedienteActual.expedienteID,
      nombrePaciente: this.expedienteActual.nombreCompleto,
      nombreFamiliar: this.datosFamiliar.nombre,
      dniFamiliar: this.datosFamiliar.dni,
      cantidadUnidades: this.deudaActual.cantidadUnidades
    };

    this.deudaSangreService.generarCompromisoPDF(dto).subscribe({
      next: (blob) => {
        this.generandoPdf = false;
        this.pdfBlobParaImprimir = blob;
        this.mostrarVisor = true;
      },
      error: (err) => {
        this.generandoPdf = false;
        Swal.fire({
          icon: 'error',
          title: 'Error al Generar PDF',
          text: 'No se pudo generar el documento de compromiso.',
          confirmButtonColor: '#DC2626'
        });
      }
    });
  }

  // ================================================================
  // 3. SUBIR Y LIQUIDAR (Paso 2 - FLUJO REAL IMPLEMENTADO)
  // ================================================================
  onArchivoSeleccionado(file: File) {
    this.archivoFirmado = file;
  }

  confirmarCompromiso() {
    if (!this.archivoFirmado || !this.expedienteActual || !this.deudaActual) {
      Swal.fire({
        icon: 'warning',
        title: 'Falta Archivo',
        text: 'Debe subir el compromiso firmado y escaneado.',
        confirmButtonColor: '#DC2626'
      });
      return;
    }

    Swal.fire({
      icon: 'question',
      title: '¿Confirmar Compromiso?',
      html: `
        <div class="text-left space-y-2">
          <p><strong>Expediente:</strong> ${this.expedienteActual.codigoExpediente}</p>
          <p><strong>Familiar:</strong> ${this.datosFamiliar.nombre}</p>
          <p><strong>DNI:</strong> ${this.datosFamiliar.dni}</p>
          <p class="text-sm text-gray-600 mt-2">
            Esto desbloqueará al paciente en Vigilancia (Semáforo Verde).
          </p>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Sí, Liquidar Deuda',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#22C55E',
      cancelButtonColor: '#6B7280'
    }).then((r) => {
      if (r.isConfirmed) {
        this.ejecutarLiquidacion();
      }
    });
  }

  private ejecutarLiquidacion() {
    this.guardando = true;

    // PASO 1: Subir archivo PDF escaneado
    this.deudaSangreService.uploadCompromiso(this.archivoFirmado!).subscribe({
      next: (rutaArchivo) => {

        try {
          // PASO 2: Liquidar deuda con la ruta obtenida
          const dto: LiquidarDeudaSangreDTO = {
            nombreFamiliarCompromiso: this.datosFamiliar.nombre,
            dniFamiliarCompromiso: this.datosFamiliar.dni,
            rutaPDFCompromiso: rutaArchivo,
            usuarioActualizacionID: this.authService.getUserId(),
            observaciones: 'Compromiso firmado y digitalizado'
          };

          this.deudaSangreService.liquidar(this.expedienteActual!.expedienteID, dto).subscribe({
            next: () => {
              this.guardando = false;
              Swal.fire({
                icon: 'success',
                title: 'Deuda Liquidada',
                text: 'El compromiso fue registrado. El semáforo cambió a verde.',
                confirmButtonColor: '#22C55E'
              }).then(() => {
                this.volver();
              });
            },
            error: (err) => {
              this.guardando = false;
              Swal.fire({
                icon: 'error',
                title: 'Error al Liquidar',
                text: err.message || 'No se pudo registrar la liquidación.',
                confirmButtonColor: '#DC2626'
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
            confirmButtonColor: '#DC2626'
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
          text: 'No se pudo subir el PDF escaneado. Verifique el formato.',
          confirmButtonColor: '#DC2626'
        });
      }
    });
  }

  // ================================================================
  // HELPERS
  // ================================================================
  limpiarBusqueda() {
    this.busqueda.termino = '';
    this.expedienteActual = null;
    this.deudaActual = null;
    this.datosFamiliar = { nombre: '', dni: '' };
    this.archivoFirmado = null;
    this.pdfBlobParaImprimir = null;
    this.busqueda.error = null;
  }

  volver() {
    this.router.navigate(['/dashboard']);
  }
}
