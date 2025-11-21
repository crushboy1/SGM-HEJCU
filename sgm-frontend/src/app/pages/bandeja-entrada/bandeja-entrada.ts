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

  // DATOS
  pendientes: PacientePendiente[] = [];
  pendientesFiltrados: PacientePendiente[] = [];
  pendientesPaginados: PacientePendiente[] = [];
  isLoading = true;
  errorMessage = '';

  // FILTROS
  searchTerm = '';
  filtroServicio = '';
  filtroFecha = 'todas';
  servicios: string[] = ['Medicina Interna', 'Cirugía General', 'UCI', 'UCINT', 'Emergencia', 'Trauma Shock'];

  // ORDENAMIENTO (Corrección de tipos)
  ordenColumna: 'hc' | 'nombre' | 'fecha' = 'fecha';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // PAGINACIÓN
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
      next: (data) => {
        this.pendientes = data;
        this.aplicarFiltros();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'No se pudieron cargar los pacientes pendientes';
        this.isLoading = false;
      }
    });
  }

  recargar(): void { this.cargarPendientes(); }

  aplicarFiltros(): void {
    let resultados = [...this.pendientes];

    // Búsqueda
    if (this.searchTerm.trim()) {
      const termino = this.searchTerm.toLowerCase().trim();
      resultados = resultados.filter(p =>
        p.hc.toLowerCase().includes(termino) ||
        p.nombres.toLowerCase().includes(termino) ||
        p.apellidoPaterno.toLowerCase().includes(termino) ||
        p.apellidoMaterno.toLowerCase().includes(termino) ||
        p.numeroDocumento.toLowerCase().includes(termino)
      );
    }

    // Filtro Servicio (Validamos que exista)
    if (this.filtroServicio) {
      resultados = resultados.filter(p => p.servicioFallecimiento === this.filtroServicio);
    }

    // Filtro Fecha
    if (this.filtroFecha !== 'todas') {
      const hoy = new Date();
      hoy.setHours(0, 0, 0, 0);

      resultados = resultados.filter(p => {
        if (!p.fechaHoraFallecimiento) return false;
        const fecha = new Date(p.fechaHoraFallecimiento);
        fecha.setHours(0, 0, 0, 0);

        switch (this.filtroFecha) {
          case 'hoy': return fecha.getTime() === hoy.getTime();
          case 'ayer':
            const ayer = new Date(hoy); ayer.setDate(ayer.getDate() - 1);
            return fecha.getTime() === ayer.getTime();
          case 'semana':
            const semana = new Date(hoy); semana.setDate(semana.getDate() - 7);
            return fecha >= semana;
          default: return true;
        }
      });
    }

    this.ordenarResultados(resultados);
    this.pendientesFiltrados = resultados;
    this.totalFiltrados = resultados.length;
    this.paginaActual = 1;
    this.calcularPaginacion();
    this.actualizarPagina();
  }

  ordenarPor(columna: 'hc' | 'nombre' | 'fecha'): void {
    if (this.ordenColumna === columna) {
      this.ordenDireccion = this.ordenDireccion === 'asc' ? 'desc' : 'asc';
    } else {
      this.ordenColumna = columna;
      this.ordenDireccion = 'asc';
    }
    this.aplicarFiltros();
  }

  private ordenarResultados(resultados: PacientePendiente[]): void {
    resultados.sort((a, b) => {
      let comparacion = 0;
      switch (this.ordenColumna) {
        case 'hc':
          comparacion = a.hc.localeCompare(b.hc);
          break;
        case 'nombre':
          const nA = `${a.apellidoPaterno} ${a.apellidoMaterno} ${a.nombres}`.toLowerCase();
          const nB = `${b.apellidoPaterno} ${b.apellidoMaterno} ${b.nombres}`.toLowerCase();
          comparacion = nA.localeCompare(nB);
          break;
        case 'fecha': // Lógica segura para fechas
          const fA = a.fechaHoraFallecimiento ? new Date(a.fechaHoraFallecimiento).getTime() : 0;
          const fB = b.fechaHoraFallecimiento ? new Date(b.fechaHoraFallecimiento).getTime() : 0;
          comparacion = fA - fB;
          break;
      }
      return this.ordenDireccion === 'asc' ? comparacion : -comparacion;
    });
  }

  // PAGINACIÓN
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

  // HELPERS
  get totalPendientes(): number { return this.pendientes.length; }

  getTipoDocumento(id: number): string {
    const tipos: Record<number, string> = { 1: 'DNI', 2: 'Pasaporte', 3: 'C. Ext.', 4: 'S/D', 5: 'NN' };
    return tipos[id] || 'Otro';
  }

  generarExpediente(hc: string): void {
    this.router.navigate(['/nuevo-expediente', hc]);
  }
}
