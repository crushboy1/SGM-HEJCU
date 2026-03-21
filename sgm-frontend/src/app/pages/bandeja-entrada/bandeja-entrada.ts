import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IntegracionService, PacientePendiente } from '../../services/integracion';
import { IconComponent } from '../../components/icon/icon.component';

@Component({
  selector: 'app-bandeja-entrada',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './bandeja-entrada.html',
  styleUrl: './bandeja-entrada.css'
})
export class BandejaEntradaComponent implements OnInit {
  private integracionService = inject(IntegracionService);
  private router = inject(Router);

  // ── Datos ────────────────────────────────────────────────────────
  pendientes: PacientePendiente[] = [];
  pendientesFiltrados: PacientePendiente[] = [];
  pendientesPaginados: PacientePendiente[] = [];
  isLoading = true;
  errorMessage = '';

  // ── Filtros ──────────────────────────────────────────────────────
  searchTerm = '';
  filtroServicio = '';
  filtroFecha = 'todas';
  /** Derivado dinámicamente de los datos recibidos */
  servicios: string[] = [];

  // ── Ordenamiento ─────────────────────────────────────────────────
  ordenColumna: 'hc' | 'nombre' | 'fecha' | 'edad' = 'fecha';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // ── Paginación ───────────────────────────────────────────────────
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  totalFiltrados = 0;
  paginaInicio = 0;
  paginaFin = 0;

  ngOnInit(): void {
    this.cargarPendientes();
  }

  cargarPendientes(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.integracionService.getPendientes().subscribe({
      next: data => {
        this.pendientes = data;
        this.derivarServiciosDisponibles();
        this.aplicarFiltros();
        this.isLoading = false;
      },
      error: err => {
        console.error(err);
        this.errorMessage = 'No se pudieron cargar los pacientes pendientes';
        this.isLoading = false;
      }
    });
  }

  recargar(): void { this.cargarPendientes(); }

  /** Extrae servicios únicos de los datos para el filtro dropdown */
  private derivarServiciosDisponibles(): void {
    this.servicios = Array.from(
      new Set(this.pendientes.map(p => p.servicioFallecimiento).filter(Boolean) as string[])
    ).sort();
  }

  aplicarFiltros(): void {
    let r = [...this.pendientes];

    // Búsqueda por nombreCompleto, hc (campos reales del DTO)
    if (this.searchTerm.trim()) {
      const t = this.searchTerm.toLowerCase();
      r = r.filter(p =>
        p.hc.toLowerCase().includes(t) ||
        p.nombreCompleto.toLowerCase().includes(t)
      );
    }

    // Filtro servicio
    if (this.filtroServicio) {
      r = r.filter(p => p.servicioFallecimiento === this.filtroServicio);
    }

    // Filtro fecha
    if (this.filtroFecha !== 'todas') {
      const hoy = new Date(); hoy.setHours(0, 0, 0, 0);
      r = r.filter(p => {
        if (!p.fechaHoraFallecimiento) return false;
        const f = new Date(p.fechaHoraFallecimiento); f.setHours(0, 0, 0, 0);
        switch (this.filtroFecha) {
          case 'hoy': return f.getTime() === hoy.getTime();
          case 'ayer': {
            const ayer = new Date(hoy); ayer.setDate(ayer.getDate() - 1);
            return f.getTime() === ayer.getTime();
          }
          case 'semana': {
            const semana = new Date(hoy); semana.setDate(semana.getDate() - 7);
            return f >= semana;
          }
          default: return true;
        }
      });
    }

    this.ordenarResultados(r);
    this.pendientesFiltrados = r;
    this.totalFiltrados = r.length;
    this.paginaActual = 1;
    this.calcularPaginacion();
    this.actualizarPagina();
  }

  ordenarPor(columna: typeof this.ordenColumna): void {
    this.ordenDireccion = this.ordenColumna === columna
      ? (this.ordenDireccion === 'asc' ? 'desc' : 'asc')
      : 'asc';
    this.ordenColumna = columna;
    this.aplicarFiltros();
  }

  private ordenarResultados(r: PacientePendiente[]): void {
    r.sort((a, b) => {
      let cmp = 0;
      switch (this.ordenColumna) {
        case 'hc':
          cmp = a.hc.localeCompare(b.hc);
          break;
        case 'nombre':
          // nombreCompleto ya viene formateado desde backend
          cmp = a.nombreCompleto.localeCompare(b.nombreCompleto);
          break;
        case 'edad':
          cmp = (a.edad ?? 0) - (b.edad ?? 0);
          break;
        case 'fecha':
          cmp = (a.fechaHoraFallecimiento ? new Date(a.fechaHoraFallecimiento).getTime() : 0)
            - (b.fechaHoraFallecimiento ? new Date(b.fechaHoraFallecimiento).getTime() : 0);
          break;
      }
      return this.ordenDireccion === 'asc' ? cmp : -cmp;
    });
  }

  // ── Paginación ───────────────────────────────────────────────────

  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.totalFiltrados / this.itemsPorPagina) || 1;
    if (this.paginaActual > this.totalPaginas) this.paginaActual = this.totalPaginas;
  }

  actualizarPagina(): void {
    this.paginaInicio = (this.paginaActual - 1) * this.itemsPorPagina;
    this.paginaFin = Math.min(this.paginaInicio + this.itemsPorPagina, this.totalFiltrados);
    this.pendientesPaginados = this.pendientesFiltrados.slice(this.paginaInicio, this.paginaFin);
  }

  paginaAnterior(): void { if (this.paginaActual > 1) { this.paginaActual--; this.actualizarPagina(); } }
  paginaSiguiente(): void { if (this.paginaActual < this.totalPaginas) { this.paginaActual++; this.actualizarPagina(); } }

  // ── Helpers ──────────────────────────────────────────────────────

  get totalPendientes(): number { return this.pendientes.length; }

  /**
   * Navega al formulario pasando solo el HC.
   * El formulario consulta /consultar-paciente/{hc} para obtener datos completos.
   */
  generarExpediente(hc: string): void {
    this.router.navigate(['/nuevo-expediente', hc]);
  }
}
