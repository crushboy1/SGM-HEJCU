// ===================================================================
// IMPORTS
// ===================================================================

// Angular core
import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

// Services
import { DeudaSangre } from '../../services/deuda-sangre';
import { DeudaEconomica } from '../../services/deuda-economica';

// Models
import { DeudaSangreDTO } from '../../models/deuda-sangre.model';
import { DeudaEconomicaSemaforoDTO } from '../../models/deuda-economica.model';

// Components
import { IconComponent } from '../icon/icon.component';

//Utils
import { getSemaforoDeudaClasses, getSemaforoDeudaIcon, getSemaforoDeudaTextColor } from '../../utils/badge-styles';
// ===================================================================
// INTERFACES LOCALES
// ===================================================================

interface SemaforoState {
  tieneDeudaSangre: boolean;
  tieneDeudaEconomica: boolean;
  bloqueaRetiro: boolean;
  colorSemaforo: string;
  iconoSemaforo: string;
  textoSemaforo: string;
  detalleSangre?: string;
  detalleEconomica?: string;
}

/**
 * SemaforoDeudas Component
 * 
 * Componente reutilizable que muestra el estado de deudas de un expediente.
 * Usado en: Mapa Mortuorio, Dashboard Vigilancia, Registro Salida
 * 
 * @Input expedienteId - ID del expediente a consultar
 * @Input mostrarDetalle - Si true, muestra detalles adicionales (default: false)
 * @Input compacto - Si true, muestra versión reducida (default: false)
 */
@Component({
  selector: 'app-semaforo-deudas',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './semaforo-deudas.html',
  styleUrl: './semaforo-deudas.css'
})
export class SemaforoDeudas implements OnInit {
  // ===================================================================
  // SERVICES
  // ===================================================================
  private deudaSangreService = inject(DeudaSangre);
  private deudaEconomicaService = inject(DeudaEconomica);

  // ===================================================================
  // INPUTS
  // ===================================================================
  @Input({ required: true }) expedienteId!: number;
  @Input() mostrarDetalle: boolean = false;
  @Input() compacto: boolean = false;

  // ===================================================================
  // DATOS
  // ===================================================================
  deudaSangre: DeudaSangreDTO | null = null;
  deudaEconomica: DeudaEconomicaSemaforoDTO | null = null;

  // ===================================================================
  // ESTADOS
  // ===================================================================
  isLoading = true;
  error: string | null = null;

  // Estado calculado del semáforo
  semaforoState: SemaforoState = {
    tieneDeudaSangre: false,
    tieneDeudaEconomica: false,
    bloqueaRetiro: false,
    colorSemaforo: 'text-gray-500',
    iconoSemaforo: 'help-circle',
    textoSemaforo: 'Cargando...'
  };

  // ===================================================================
  // INICIALIZACIÓN
  // ===================================================================

  ngOnInit(): void {
    this.cargarDeudas();
  }

  // ===================================================================
  // CARGA DE DATOS
  // ===================================================================

  /**
   * Carga ambas deudas en paralelo y calcula el estado del semáforo
   */
  private cargarDeudas(): void {
    this.isLoading = true;
    this.error = null;

    // Cargar deuda de sangre
    this.deudaSangreService.obtenerPorExpediente(this.expedienteId).subscribe({
      next: (deudaSangre) => {
        this.deudaSangre = deudaSangre;
        this.verificarDeudaEconomica();
      },
      error: (err) => {
        console.error('[SemaforoDeudas] Error al cargar deuda sangre:', err);
        this.verificarDeudaEconomica();
      }
    });
  }

  /**
   * Verifica deuda económica y calcula estado final
   */
  private verificarDeudaEconomica(): void {
    this.deudaEconomicaService.obtenerSemaforo(this.expedienteId).subscribe({
      next: (semaforo) => {
        this.deudaEconomica = semaforo;
        this.calcularEstadoSemaforo();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('[SemaforoDeudas] Error al cargar deuda económica:', err);
        this.calcularEstadoSemaforo();
        this.isLoading = false;
      }
    });
  }

  // ===================================================================
  // LÓGICA DE NEGOCIO
  // ===================================================================

  /**
   * Calcula el estado visual del semáforo basado en ambas deudas
   */
  private calcularEstadoSemaforo(): void {
    // Verificar deuda de sangre
    const tieneSangre = this.deudaSangre?.bloqueaRetiro || false;
    const tieneEconomica = this.deudaEconomica?.tieneDeuda || false;

    this.semaforoState.tieneDeudaSangre = tieneSangre;
    this.semaforoState.tieneDeudaEconomica = tieneEconomica;
    this.semaforoState.bloqueaRetiro = tieneSangre || tieneEconomica;
    this.semaforoState.colorSemaforo = getSemaforoDeudaTextColor(this.semaforoState.bloqueaRetiro);
    this.semaforoState.iconoSemaforo = getSemaforoDeudaIcon(this.semaforoState.bloqueaRetiro);
    this.semaforoState.textoSemaforo = this.semaforoState.bloqueaRetiro ? 'DEBE' : 'NO DEBE';

    // Generar detalles
    if (this.mostrarDetalle) {
      this.generarDetalles();
    }
  }

  /**
   * Genera textos de detalle para cada tipo de deuda
   */
  private generarDetalles(): void {
    // Detalle Sangre
    if (this.deudaSangre) {
      if (this.deudaSangre.bloqueaRetiro) {
        this.semaforoState.detalleSangre = `${this.deudaSangre.cantidadUnidades} unidad(es) pendiente(s)`;
      } else if (this.deudaSangre.anuladaPorMedico) {
        this.semaforoState.detalleSangre = 'Anulada por médico';
      } else if (this.deudaSangre.fechaLiquidacion) {
        this.semaforoState.detalleSangre = 'Compromiso firmado';
      } else {
        this.semaforoState.detalleSangre = 'Sin deuda';
      }
    }

    // Detalle Económica
    if (this.deudaEconomica) {
      this.semaforoState.detalleEconomica = this.deudaEconomica.semaforo;
    }
  }

  // ===================================================================
  // GETTERS PARA TEMPLATE
  // ===================================================================

  get badgeClasses(): string {
    return getSemaforoDeudaClasses(this.semaforoState.bloqueaRetiro, this.compacto);
  }
  get iconSize(): number {
    return this.compacto ? 14 : 18;
  }
}
