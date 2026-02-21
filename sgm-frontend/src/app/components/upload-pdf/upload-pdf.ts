import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon.component';

/**
 * UploadPdf Component
 * * Componente reutilizable para cargar archivos PDF (drag & drop o click).
 * Valida tamaño y extensión antes de emitir el evento.
 * * @Input label - Texto de la etiqueta (ej: "Subir Oficio")
 * @Input disabled - Si true, bloquea la interacción
 * @Input currentFileName - Nombre del archivo actual (si ya existe uno cargado)
 * @Output fileSelected - Emite el archivo File validado
 */
@Component({
  selector: 'app-upload-pdf',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './upload-pdf.html',
  styleUrl: './upload-pdf.css'
})
export class UploadPdf {
  // ===================================================================
  // INPUTS / OUTPUTS
  // ===================================================================
  @Input() label: string = 'Subir documento PDF';
  @Input() disabled: boolean = false;
  @Input() currentFileName: string | null = null;
  @Output() fileSelected = new EventEmitter<File>();

  // ===================================================================
  // ESTADOS INTERNOS
  // ===================================================================
  isDragging = false;
  errorMessage: string | null = null;
  selectedFile: File | null = null;

  // Configuración
  readonly MAX_SIZE_MB = 10;
  readonly ACCEPTED_TYPE = 'application/pdf';

  // ===================================================================
  // MANEJADORES DE EVENTOS
  // ===================================================================

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.disabled) {
      this.isDragging = true;
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (this.disabled) return;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.validarYEmitir(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.validarYEmitir(input.files[0]);
    }
  }

  removeFile(event: Event): void {
    event.stopPropagation();
    this.selectedFile = null;
    this.currentFileName = null;
    this.errorMessage = null;
    // Emitir null o undefined podría requerir cambiar el Output, 
    // por ahora no emitimos nada o podríamos emitir null si se necesitara limpiar en el padre.
    // Para simplificar, asumimos que el padre maneja la limpieza si currentFileName cambia.
  }

  // ===================================================================
  // VALIDACIÓN
  // ===================================================================

  private validarYEmitir(file: File): void {
    this.errorMessage = null;

    // 1. Validar Tipo
    if (file.type !== this.ACCEPTED_TYPE) {
      this.errorMessage = 'Solo se permiten archivos PDF.';
      return;
    }

    // 2. Validar Tamaño (10MB)
    const sizeMB = file.size / (1024 * 1024);
    if (sizeMB > this.MAX_SIZE_MB) {
      this.errorMessage = `El archivo excede el tamaño máximo de ${this.MAX_SIZE_MB}MB.`;
      return;
    }

    // 3. Éxito
    this.selectedFile = file;
    this.fileSelected.emit(file);
  }

  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================

  get fileNameDisplay(): string {
    if (this.selectedFile) return this.selectedFile.name;
    if (this.currentFileName) return this.currentFileName;
    return 'Seleccione o arrastre un archivo aquí';
  }

  get fileSizeDisplay(): string {
    if (!this.selectedFile) return '';
    const sizeMB = this.selectedFile.size / (1024 * 1024);
    return `(${sizeMB.toFixed(2)} MB)`;
  }
}
