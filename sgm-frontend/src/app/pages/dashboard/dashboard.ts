import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ExpedienteService, Expediente } from '../../services/expediente';
import { DashboardService, DashboardKPIs } from '../../services/dashboard';
import { IconComponent } from '../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../utils/badge-styles';
import { AuthService } from '../../services/auth';
import Swal from 'sweetalert2';
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private expedienteService = inject(ExpedienteService);
  private dashboardService = inject(DashboardService);
  private router = inject(Router);

  // ===================================================================
  // DATOS DEL DASHBOARD
  // ===================================================================
  kpis: DashboardKPIs | null = null;
  expedientes: Expediente[] = [];
  expedientesRecientes: Expediente[] = [];

  // Estados de carga
  isLoadingKPIs = true;
  isLoadingExpedientes = true;
  errorKPIs: string | null = null;
  errorExpedientes: string | null = null;

  // Usuario
  userName: string = '';
  userRole: string = '';

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================
  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getUserRole();

    this.cargarKPIs();
    this.cargarExpedientes();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Cargar KPIs del dashboard (bandejas, solicitudes, salidas)
   */
  cargarKPIs(): void {
    this.isLoadingKPIs = true;
    this.errorKPIs = null;

    this.dashboardService.getDashboardKPIs().subscribe({
      next: (data) => {
        this.kpis = data;
        this.isLoadingKPIs = false;
        console.log('✅ KPIs cargados:', data);
      },
      error: (err) => {
        console.error('❌ Error cargando KPIs:', err);
        this.errorKPIs = 'No se pudieron cargar las estadísticas';
        this.isLoadingKPIs = false;
      }
    });
  }

  /**
   * Cargar expedientes recientes (últimos 10)
   */
  cargarExpedientes(): void {
    this.isLoadingExpedientes = true;
    this.errorExpedientes = null;

    this.expedienteService.getAll().subscribe({
      next: (data) => {
        this.expedientes = data;
        // Tomar los 10 más recientes (asumiendo que vienen ordenados por fecha)
        this.expedientesRecientes = data.slice(0, 10);
        this.isLoadingExpedientes = false;
        console.log('✅ Expedientes cargados:', data.length);
      },
      error: (err) => {
        console.error('❌ Error cargando expedientes:', err);
        this.errorExpedientes = 'No se pudieron cargar los expedientes';
        this.isLoadingExpedientes = false;
      }
    });
  }

  /**
   * Recargar todos los datos del dashboard
   */
  recargarDatos(): void {
    this.cargarKPIs();
    this.cargarExpedientes();
  }

  // ===================================================================
  // HELPERS PARA EL TEMPLATE
  // ===================================================================

  /**
   * Obtiene la información del badge para un estado
   */
  getBadgeInfo(estado: string) {
    return getBadgeWithIcon(estado);
  }

  /**
   * Verifica si hay alertas críticas en las bandejas
   */
  get tieneAlertasCriticas(): boolean {
    if (!this.kpis) return false;
    return this.kpis.bandejas.conAlerta48h > 0;
  }

  /**
   * Verifica si hay alertas de 24h
   */
  get tieneAlertas24h(): boolean {
    if (!this.kpis) return false;
    return this.kpis.bandejas.conAlerta24h > 0;
  }

  /**
   * Verifica si hay solicitudes pendientes con alerta
   */
  get tieneSolicitudesAlerta(): boolean {
    if (!this.kpis) return false;
    return this.kpis.solicitudes.conAlerta > 0;
  }

  /**
   * Obtiene el color del indicador de ocupación
   */
  getOcupacionColor(): string {
    if (!this.kpis) return 'text-gray-500';
    const porcentaje = this.kpis.bandejas.porcentajeOcupacion;

    if (porcentaje >= 90) return 'text-red-600';
    if (porcentaje >= 70) return 'text-yellow-600';
    return 'text-green-600';
  }

  /**
   * Obtiene la clase de color para el badge de ocupación
   */
  getOcupacionBadgeClass(): string {
    if (!this.kpis) return 'bg-gray-100 text-gray-600';
    const porcentaje = this.kpis.bandejas.porcentajeOcupacion;

    if (porcentaje >= 90) return 'bg-red-100 text-red-800 border-red-300';
    if (porcentaje >= 70) return 'bg-yellow-100 text-yellow-800 border-yellow-300';
    return 'bg-green-100 text-green-800 border-green-300';
  }
  getProgressBarClass(): string {
    const porcentaje = this.kpis?.bandejas?.porcentajeOcupacion || 0;

    if (porcentaje >= 90) return 'bg-red-500';
    if (porcentaje >= 70) return 'bg-yellow-500';
    return 'bg-green-500';
  }
  // ===================================================================
  // NAVEGACIÓN
  // ===================================================================

  /**
   * Navegar a una ruta específica
   */
  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  /**
   * Ver detalle de un expediente
   */
  verExpediente(expedienteId: number | undefined): void {
    if (!expedienteId) {
      console.warn('ID de expediente inválido');
      return;
    }
    this.router.navigate(['/expediente', expedienteId]);
  }
  reimprimir(expedienteId: number): void {
    // Mostrar feedback visual (opcional, o usar un estado loading local)
    console.log('Solicitando reimpresión para:', expedienteId);

    this.expedienteService.reimprimirBrazalete(expedienteId).subscribe({
      next: (blob) => {
        // Crear URL y descargar
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `brazalete-SGM-${expedienteId}-reimpresion.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);

         
        Swal.fire('Reimpreso', 'El brazalete se ha descargado.', 'success');
      },
      error: (err) => {
        console.error(err);
        Swal.fire('Error', 'No se pudo reimprimir el brazalete.', 'error');
      }
    });
  }
}
