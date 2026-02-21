import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

// Services
import { ExpedienteLegal } from '../../../services/expediente-legal';

// Models
import { ExpedienteLegalDTO, ExpedienteLegalHelper } from '../../../models/expediente-legal.model';

// Components
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-lista-expedientes-legales',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, IconComponent],
  templateUrl: './lista-expedientes-legales.html',
  styleUrl: './lista-expedientes-legales.css'
})
export class ListaExpedientesLegales implements OnInit {
  private router = inject(Router);
  private expedienteLegalService = inject(ExpedienteLegal);

  // Datos
  expedientes: ExpedienteLegalDTO[] = [];
  expedientesFiltrados: ExpedienteLegalDTO[] = [];

  // UI States
  loading: boolean = true;
  error: string | null = null;
  terminoBusqueda: string = '';

  // Helper para usar en el HTML
  helper = ExpedienteLegalHelper;

  ngOnInit(): void {
    this.cargarExpedientes();
  }

  cargarExpedientes(): void {
    this.loading = true;
    this.error = null;

    this.expedienteLegalService.listarExpedientes().subscribe({
      next: (data) => {
        this.expedientes = data;
        this.expedientesFiltrados = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error cargando lista:', err);
        this.error = 'No se pudieron cargar los expedientes. Intente nuevamente.';
        this.loading = false;
      }
    });
  }

  filtrar(): void {
    const termino = this.terminoBusqueda.toLowerCase().trim();

    if (!termino) {
      this.expedientesFiltrados = this.expedientes;
      return;
    }

    this.expedientesFiltrados = this.expedientes.filter(item => {
      // Concatenamos campos para buscar en todo
      const busqueda = `
        ${item.codigoExpediente} 
        ${item.apellidoPaterno} 
        ${item.apellidoMaterno} 
        ${item.nombres} 
        ${item.hc} 
        ${item.numeroDocumento}
      `.toLowerCase();

      return busqueda.includes(termino);
    });
  }

  irACrear(): void {
    this.router.navigate(['/administrativo/legal/crear']);
  }

  irADetalle(id: number): void {
    this.router.navigate(['/administrativo/legal/detalle', id]);
  }
}
