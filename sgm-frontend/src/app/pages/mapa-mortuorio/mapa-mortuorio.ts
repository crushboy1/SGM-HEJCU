import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { BandejaService, Bandeja } from '../../services/bandeja';
import { IconComponent } from '../../components/icon/icon.component';

@Component({
  selector: 'app-mapa-mortuorio',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './mapa-mortuorio.html',
  styleUrls: ['./mapa-mortuorio.css']
})
export class MapaMortuorioComponent implements OnInit, OnDestroy {
  private bandejaService = inject(BandejaService);
  private router = inject(Router);

  // ====================================================
  // DATOS
  // ====================================================
  bandejas: Bandeja[] = [];
  isLoading = true;
  errorMessage = '';

  bandejaSeleccionada: Bandeja | null = null;

  private refreshInterval: any;

  // ====================================================
  // INICIALIZACIÓN
  // ====================================================
  ngOnInit(): void {
    this.cargarMapa();

    // Auto-refresh cada 30s
    this.refreshInterval = setInterval(() => this.cargarMapa(), 30000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  // ====================================================
  // CARGA DE DATOS
  // ====================================================
  cargarMapa(): void {
    if (this.bandejas.length === 0) this.isLoading = true;
    this.errorMessage = '';

    this.bandejaService.getDashboard().subscribe({
      next: (data) => {
        this.bandejas = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('❌ Error cargando mapa:', err);
        this.errorMessage = 'No se pudo cargar el estado del mortuorio';
        this.isLoading = false;
      }
    });
  }

  recargar(): void {
    this.isLoading = true;
    this.cargarMapa();
  }

  // ====================================================
  // HELPERS VISUALES
  // ====================================================
  getBandejaClasses(b: Bandeja): string {
    const base = 'relative overflow-hidden group rounded-xl shadow-sm transition-all hover:shadow-md cursor-pointer border-l-4';
    switch (b.estado) {
      case 'Disponible':
        return `${base} border-green-500 bg-white hover:bg-green-50/30`;
      case 'Ocupada':
        const alerta = b.tieneAlerta ? 'ring-2 ring-red-500 ring-offset-2' : '';
        return `${base} border-red-500 bg-white hover:bg-red-50/30 ${alerta}`;
      case 'Mantenimiento':
        return `${base} border-yellow-500 bg-yellow-50 hover:bg-yellow-100`;
      default:
        return `${base} border-gray-300 bg-gray-50`;
    }
  }

  getBandejaIcon(b: Bandeja): string {
    switch (b.estado) {
      case 'Disponible': return 'circle-check';
      case 'Ocupada': return 'archive';
      case 'Mantenimiento': return 'settings';
      default: return 'info';
    }
  }

  getBandejaIconColor(b: Bandeja): string {
    switch (b.estado) {
      case 'Disponible': return 'text-green-500';
      case 'Ocupada': return 'text-red-500';
      case 'Mantenimiento': return 'text-yellow-600';
      default: return 'text-gray-400';
    }
  }

  // ====================================================
  // ESTADÍSTICAS
  // ====================================================
  get totalDisponibles(): number { return this.bandejas.filter(b => b.estado === 'Disponible').length; }
  get totalOcupadas(): number { return this.bandejas.filter(b => b.estado === 'Ocupada').length; }
  get totalMantenimiento(): number { return this.bandejas.filter(b => b.estado === 'Mantenimiento').length; }
  get porcentajeOcupacion(): number { return this.bandejas.length ? Math.round((this.totalOcupadas / this.bandejas.length) * 100) : 0; }
  get bandejaConAlertas(): number { return this.bandejas.filter(b => b.tieneAlerta).length; }

  // ====================================================
  // ACCIONES
  // ====================================================
  seleccionarBandeja(b: Bandeja): void {
    this.bandejaSeleccionada = b;

    if (b.estado === 'Disponible') {
      // Navegar directo sin preguntar (la confirmación se hace en la siguiente pantalla)
      this.asignarBandeja(b.bandejaID);
    }
  }

  verExpediente(expedienteId?: number): void {
    if (expedienteId) this.router.navigate(['/expediente', expedienteId]);
  }

  asignarBandeja(bandejaId: number): void {
    // Navega a la ruta de asignación
    this.router.navigate(['/asignar-bandeja', bandejaId]);
  }

  liberarBandeja(b: Bandeja): void {
    Swal.fire({
      title: `¿Liberar ${b.codigo}?`,
      text: "Esta acción registrará la salida del cuerpo.",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#DC3545',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, liberar bandeja'
    }).then(result => {
      if (result.isConfirmed) {
        this.bandejaService.liberar({ bandejaID: b.bandejaID }).subscribe({
          next: () => this.cargarMapa(),
          error: (err) => Swal.fire('Error', 'No se pudo liberar la bandeja', 'error')
        });
      }
    });
  }
}
