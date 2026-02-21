import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { ExpedienteService, Expediente } from '../../services/expediente';
import { IconComponent } from '../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../utils/badge-styles';

/**
 * Interfaz para el objeto de filtros.
 */
interface FiltrosExpediente {
  hc: string;
  nombre: string;
  documento: string;
  estado: string;
  fechaInicio: string;
  fechaFin: string;
}

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

  // ===================================================================
  // DATOS
  // ===================================================================
  expedientes: Expediente[] = [];
  expedientesFiltrados: Expediente[] = [];
  isLoading = true;

  // ===================================================================
  // FILTROS (objeto centralizado)
  // ===================================================================
  filtros: FiltrosExpediente = {
    hc: '',
    nombre: '',
    documento: '',
    estado: '',
    fechaInicio: '',
    fechaFin: ''
  };

  // ===================================================================
  // ORDENAMIENTO
  // ===================================================================
  ordenColumna: 'hc' | 'nombre' | 'fecha' | 'estado' | '' = '';
  ordenDireccion: 'asc' | 'desc' = 'desc';

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================
  paginaActual = 1;
  itemsPorPagina = 10;
  totalPaginas = 1;
  totalItems = 0;
  paginatedItems: Expediente[] = [];

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnInit(): void {
    this.cargarDatos();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Carga todos los expedientes desde el backend.
   */
  cargarDatos(): void {
    this.isLoading = true;

    this.expedienteService.getAll().subscribe({
      next: (data) => {
        this.expedientes = data;
        this.aplicarFiltros();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error al cargar expedientes:', err);
        this.isLoading = false;
        Swal.fire({
          icon: 'error',
          title: 'Error al Cargar',
          text: 'No se pudieron cargar los expedientes. Intente nuevamente.',
          confirmButtonColor: '#0891B2'
        });
      }
    });
  }

  // ===================================================================
  // FILTRADO
  // ===================================================================

  /**
   * Aplica todos los filtros activos y recalcula la paginación.
   */
  aplicarFiltros(): void {
    let resultados = [...this.expedientes];

    // Filtro: HC
    if (this.filtros.hc.trim()) {
      const hcLower = this.filtros.hc.toLowerCase();
      resultados = resultados.filter(e =>
        e.hc.toLowerCase().includes(hcLower)
      );
    }

    // Filtro: Nombre
    if (this.filtros.nombre.trim()) {
      const nombreLower = this.filtros.nombre.toLowerCase();
      resultados = resultados.filter(e =>
        e.nombreCompleto.toLowerCase().includes(nombreLower)
      );
    }

    // Filtro: Documento
    if (this.filtros.documento.trim()) {
      const docLower = this.filtros.documento.toLowerCase();
      resultados = resultados.filter(e =>
        e.numeroDocumento?.toLowerCase().includes(docLower)
      );
    }

    // Filtro: Estado
    if (this.filtros.estado) {
      resultados = resultados.filter(e =>
        e.estadoActual === this.filtros.estado
      );
    }

    // Filtro: Fecha Inicio
    if (this.filtros.fechaInicio) {
      const fechaInicio = new Date(this.filtros.fechaInicio);
      resultados = resultados.filter(e => {
        const fechaExpediente = new Date(e.fechaHoraFallecimiento);
        return fechaExpediente >= fechaInicio;
      });
    }

    // Filtro: Fecha Fin
    if (this.filtros.fechaFin) {
      const fechaFin = new Date(this.filtros.fechaFin);
      fechaFin.setHours(23, 59, 59, 999); // Incluir todo el día
      resultados = resultados.filter(e => {
        const fechaExpediente = new Date(e.fechaHoraFallecimiento);
        return fechaExpediente <= fechaFin;
      });
    }

    this.expedientesFiltrados = resultados;
    this.totalItems = resultados.length;

    // Resetear a página 1 después de filtrar
    this.paginaActual = 1;
    this.calcularPaginacion();
  }

  /**
   * Limpia todos los filtros y recarga la vista completa.
   */
  limpiarFiltros(): void {
    this.filtros = {
      hc: '',
      nombre: '',
      documento: '',
      estado: '',
      fechaInicio: '',
      fechaFin: ''
    };
    this.aplicarFiltros();
  }

  // ===================================================================
  // ORDENAMIENTO
  // ===================================================================

  /**
   * Ordena la tabla por la columna especificada.
   * Alterna entre ascendente y descendente si se hace clic en la misma columna.
   */
  ordenarPor(columna: 'hc' | 'nombre' | 'fecha' | 'estado'): void {
    // Si es la misma columna, alternar dirección
    if (this.ordenColumna === columna) {
      this.ordenDireccion = this.ordenDireccion === 'asc' ? 'desc' : 'asc';
    } else {
      // Nueva columna, ordenar ascendente por defecto
      this.ordenColumna = columna;
      this.ordenDireccion = 'asc';
    }

    // Aplicar ordenamiento
    this.expedientesFiltrados.sort((a, b) => {
      let valorA: any;
      let valorB: any;

      switch (columna) {
        case 'hc':
          valorA = a.hc;
          valorB = b.hc;
          break;
        case 'nombre':
          valorA = a.nombreCompleto.toLowerCase();
          valorB = b.nombreCompleto.toLowerCase();
          break;
        case 'fecha':
          valorA = new Date(a.fechaHoraFallecimiento).getTime();
          valorB = new Date(b.fechaHoraFallecimiento).getTime();
          break;
        case 'estado':
          valorA = a.estadoActual;
          valorB = b.estadoActual;
          break;
        default:
          return 0;
      }

      if (valorA < valorB) return this.ordenDireccion === 'asc' ? -1 : 1;
      if (valorA > valorB) return this.ordenDireccion === 'asc' ? 1 : -1;
      return 0;
    });

    // Recalcular paginación después de ordenar
    this.calcularPaginacion();
  }

  // ===================================================================
  // PAGINACIÓN
  // ===================================================================

  /**
   * Calcula el número total de páginas y actualiza la vista.
   */
  calcularPaginacion(): void {
    this.totalPaginas = Math.ceil(this.expedientesFiltrados.length / this.itemsPorPagina) || 1;

    // Ajustar página actual si quedó fuera de rango
    if (this.paginaActual > this.totalPaginas) {
      this.paginaActual = this.totalPaginas;
    }

    this.actualizarPagina();
  }

  /**
   * Actualiza los items visibles según la página actual.
   */
  actualizarPagina(): void {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    const fin = inicio + this.itemsPorPagina;
    this.paginatedItems = this.expedientesFiltrados.slice(inicio, fin);
  }

  /**
   * Navega a la página anterior.
   */
  paginaAnterior(): void {
    if (this.paginaActual > 1) {
      this.paginaActual--;
      this.actualizarPagina();
    }
  }

  /**
   * Navega a la página siguiente.
   */
  paginaSiguiente(): void {
    if (this.paginaActual < this.totalPaginas) {
      this.paginaActual++;
      this.actualizarPagina();
    }
  }

  // ===================================================================
  // ACCIONES
  // ===================================================================

  /**
   * Navega a la vista de detalle del expediente.
   */
  verDetalle(id: number): void {
    // TODO: Implementar vista de detalle
    console.log('Ver detalle del expediente:', id);

    Swal.fire({
      icon: 'info',
      title: 'Función en Desarrollo',
      text: 'La vista de detalle estará disponible próximamente.',
      confirmButtonColor: '#0891B2'
    });
  }

  /**
   * Reimprime el brazalete de un expediente.
   */
  reimprimir(id: number): void {
    Swal.fire({
      title: 'Reimprimiendo Brazalete',
      text: 'Generando PDF...',
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    this.expedienteService.reimprimirBrazalete(id).subscribe({
      next: (blob) => {
        Swal.close();

        // Descargar PDF
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `brazalete-expediente-${id}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);

        Swal.fire({
          icon: 'success',
          title: 'Brazalete Descargado',
          text: 'El archivo PDF se descargó correctamente.',
          showConfirmButton: true
        });
      },
      error: (err) => {
        console.error('Error al reimprimir:', err);
        Swal.fire({
          icon: 'error',
          title: 'Error al Reimprimir',
          text: err.error?.message || 'No se pudo generar el brazalete. Intente nuevamente.',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  // ===================================================================
  // HELPERS PARA TEMPLATE
  // ===================================================================

  /**
   * Obtiene la información de badge (colores e ícono) para un estado.
   */
  getBadgeInfo(estado: string) {
    return getBadgeWithIcon(estado);
  }
}
