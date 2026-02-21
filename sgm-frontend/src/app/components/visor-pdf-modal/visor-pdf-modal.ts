import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IconComponent } from '../icon/icon.component';

/**
 * VisorPdfModal Component
 * * Modal flotante para visualizar archivos PDF directamente en el navegador.
 * Recibe un Blob (archivo binario), genera una URL segura y lo muestra en un iframe.
 * * Uso:
 * <app-visor-pdf-modal 
 * *ngIf="pdfBlobActual"
 * [titulo]="'Oficio PNP 123'" 
 * [archivoBlob]="pdfBlobActual"
 * (cerrar)="pdfBlobActual = null">
 * </app-visor-pdf-modal>
 */
@Component({
  selector: 'app-visor-pdf-modal',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './visor-pdf-modal.html',
  styleUrl: './visor-pdf-modal.css'
})
export class VisorPdfModal implements OnChanges, OnDestroy {
  private sanitizer = inject(DomSanitizer);

  // ===================================================================
  // INPUTS / OUTPUTS
  // ===================================================================
  @Input() titulo: string = 'Visor de Documento';
  @Input() archivoBlob: Blob | null = null;
  @Output() cerrar = new EventEmitter<void>();

  // ===================================================================
  // ESTADO INTERNO
  // ===================================================================
  pdfUrlSegura: SafeResourceUrl | null = null;
  private objectUrl: string | null = null;

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnChanges(changes: SimpleChanges): void {
    // Si cambia el archivo de entrada, regenerar la URL
    if (changes['archivoBlob'] && this.archivoBlob) {
      this.generarUrl();
    }
  }

  ngOnDestroy(): void {
    // Limpieza de memoria obligatoria
    this.limpiarUrl();
  }

  // ===================================================================
  // MÉTODOS PRIVADOS
  // ===================================================================

  private generarUrl(): void {
    this.limpiarUrl(); // Limpiar anterior si existe

    if (this.archivoBlob) {
      // Crear URL temporal en memoria del navegador
      this.objectUrl = URL.createObjectURL(this.archivoBlob);
      // Marcar como segura para Angular (evita error XSS)
      this.pdfUrlSegura = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
    }
  }

  private limpiarUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
      this.pdfUrlSegura = null;
    }
  }

  // ===================================================================
  // ACCIONES PÚBLICAS
  // ===================================================================

  onCerrar(): void {
    this.cerrar.emit();
  }

  descargar(): void {
    if (!this.objectUrl) return;

    // Crear enlace temporal para forzar descarga
    const link = document.createElement('a');
    link.href = this.objectUrl;
    link.download = `${this.titulo.replace(/\s+/g, '_')}.pdf`;
    link.click();
  }
}
