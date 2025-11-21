import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExpedienteService, Expediente } from '../../services/expediente';
import { IconComponent } from '../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../utils/badge-styles';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-mis-expedientes',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './mis-expedientes.html',
  styleUrl: './mis-expedientes.css'
})
export class MisExpedientesComponent implements OnInit {
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);

  // Datos
  expedientes: Expediente[] = [];
  expedientesFiltrados: Expediente[] = [];
  expedientesPaginados: Expediente[] = [];
  isLoading = true;

  // Filtros
  searchTerm = '';
  filtroEstado = '';

  // Paginación
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;

  ngOnInit() {
    this.cargarDatos();
  }

  cargarDatos() {
    this.isLoading = true;
    this.expedienteService.getAll().subscribe({
      next: (data) => {
        this.expedientes = data;
        this.aplicarFiltros();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  aplicarFiltros() {
    let resultados = [...this.expedientes];

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      resultados = resultados.filter(e =>
        e.hc.toLowerCase().includes(term) ||
        e.nombreCompleto.toLowerCase().includes(term) ||
        e.codigoExpediente.toLowerCase().includes(term)
      );
    }

    if (this.filtroEstado) {
      resultados = resultados.filter(e => e.estadoActual === this.filtroEstado);
    }

    this.expedientesFiltrados = resultados;
    this.calcularPaginacion();
  }

  calcularPaginacion() {
    this.totalPaginas = Math.ceil(this.expedientesFiltrados.length / this.itemsPorPagina) || 1;
    this.actualizarPagina();
  }

  actualizarPagina() {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    this.expedientesPaginados = this.expedientesFiltrados.slice(inicio, inicio + this.itemsPorPagina);
  }

  // Acciones
  verDetalle(id: number) {
    // this.router.navigate(['/expediente', id]); // Pendiente crear vista detalle
    console.log('Ver detalle', id);
  }

  reimprimir(id: number) {
    // Lógica de impresión que ya hicimos en el dashboard
    this.expedienteService.reimprimirBrazalete(id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `brazalete-${id}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
        Swal.fire('Éxito', 'Brazalete descargado', 'success');
      },
      error: () => Swal.fire('Error', 'No se pudo reimprimir', 'error')
    });
  }

  // Helper para HTML
  getBadgeInfo(estado: string) {
    return getBadgeWithIcon(estado);
  }

  paginaAnterior() { if (this.paginaActual > 1) { this.paginaActual--; this.actualizarPagina(); } }
  paginaSiguiente() { if (this.paginaActual < this.totalPaginas) { this.paginaActual++; this.actualizarPagina(); } }
}
