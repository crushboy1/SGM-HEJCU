import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { ExpedienteService, Expediente } from '../../../services/expediente';
import { IconComponent } from '../../../components/icon/icon.component';
import { FormularioSalida } from '../../../components/formulario-salida/formulario-salida';
import { getBadgeClasses, getEstadoIcon, getEstadoLabel } from '../../../utils/badge-styles';

/**
 * Búsqueda manual de expedientes para registro de salida.
 *
 * FLUJO FALLBACK (10% casos excepcionales):
 * Vigilante accede → carga expedientes PendienteRetiro → filtra → abre FormularioSalida
 *
 * DECISIÓN: Solo muestra PendienteRetiro (ActaRetiro firmado, listo para retiro físico).
 * EnBandeja se excluye — aún no tiene autorización de Admisión completa.
 *
 * @version 2.0.0
 * @changelog
 * - v2.0.0: Filtro solo PendienteRetiro. Eliminados getEstadoBadgeClass() y
 *           formatearEstado() — reemplazados por badge-styles.ts.
 *           Filtrado local optimizado. Búsqueda sin mínimo de caracteres.
 * - v1.1.0: Carga inicial de todos los expedientes disponibles.
 */
@Component({
  selector: 'app-busqueda-salida',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, FormularioSalida],
  templateUrl: './busqueda-salida.html',
  styleUrl: './busqueda-salida.css'
})
export class BusquedaSalida implements OnInit {
  private expedienteService = inject(ExpedienteService);
  private router = inject(Router);

  // ===================================================================
  // ESTADO
  // ===================================================================

  textoBusqueda = '';
  isLoading = false;

  /** Todos los expedientes PendienteRetiro cargados del backend. */
  private todosLosExpedientes: Expediente[] = [];

  /** Lista filtrada que se muestra en la tabla. */
  resultados: Expediente[] = [];

  // ===================================================================
  // MODAL
  // ===================================================================

  mostrarModalSalida = false;
  expedienteSeleccionado: Expediente | null = null;

  // ===================================================================
  // HELPERS VISUALES
  // ===================================================================
  getBadgeClasses = getBadgeClasses;
  getEstadoIcon = getEstadoIcon;
  getEstadoLabel = getEstadoLabel;

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================

  ngOnInit(): void {
    this.cargarExpedientes();
    console.log('[BusquedaSalida] Inicializado');
  }

  // ===================================================================
  // CARGA INICIAL
  // ===================================================================

  /**
   * Carga todos los expedientes en estado PendienteRetiro.
   * ActaRetiro firmado = listo para retiro físico por el vigilante.
   */
  private cargarExpedientes(): void {
    this.isLoading = true;

    this.expedienteService.getAll().subscribe({
      next: (expedientes) => {
        // Solo PendienteRetiro — ActaRetiro completo y firmado
        this.todosLosExpedientes = expedientes.filter(
          exp => exp.estadoActual === 'PendienteRetiro'
        );
        this.resultados = [...this.todosLosExpedientes];
        this.isLoading = false;

        console.log('[BusquedaSalida] Expedientes PendienteRetiro:', this.resultados.length);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[BusquedaSalida] Error al cargar expedientes:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Cargar',
          text: err.error?.message ?? 'No se pudieron cargar los expedientes.',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  // ===================================================================
  // FILTRADO LOCAL
  // ===================================================================

  /**
   * Filtra localmente sobre los expedientes ya cargados.
   * HC/DNI: coincidencia exacta. Nombre: parcial case-insensitive.
   * Sin mínimo de caracteres — filtra en tiempo real.
   */
  filtrar(): void {
    const texto = this.textoBusqueda.trim();

    if (!texto) {
      this.resultados = [...this.todosLosExpedientes];
      return;
    }

    const textoLower = texto.toLowerCase();
    const esNumero = /^\d+$/.test(texto);

    this.resultados = this.todosLosExpedientes.filter(exp => {
      if (esNumero) {
        // Exacta: HC o DNI
        return exp.hc === texto || exp.numeroDocumento === texto;
      }
      // Parcial: nombre
      return exp.nombreCompleto.toLowerCase().includes(textoLower);
    });

    console.log('[BusquedaSalida] Filtrado "%s" → %d resultado(s)', texto, this.resultados.length);
  }

  /**
   * Limpia el filtro y muestra todos los expedientes.
   */
  limpiarFiltro(): void {
    this.textoBusqueda = '';
    this.resultados = [...this.todosLosExpedientes];
  }

  /**
   * Recarga desde el backend y limpia el filtro.
   */
  recargar(): void {
    this.textoBusqueda = '';
    this.cargarExpedientes();
  }

  // ===================================================================
  // MODAL
  // ===================================================================

  /** Abre el FormularioSalida para el expediente seleccionado. */
  abrirModal(expediente: Expediente): void {
    this.expedienteSeleccionado = expediente;
    this.mostrarModalSalida = true;
    console.log('[BusquedaSalida] Modal abierto para:', expediente.codigoExpediente);
  }

  /** Cierra el modal y limpia la selección. */
  cerrarModal(): void {
    this.mostrarModalSalida = false;
    this.expedienteSeleccionado = null;
  }

  /** Callback cuando FormularioSalida confirma la salida exitosamente. */
  onSalidaCompletada(response: any): void {
    console.log('[BusquedaSalida] Salida completada:', response);
    this.cerrarModal();
    this.recargar();

    Swal.fire({
      icon: 'success',
      title: 'Salida Registrada',
      text: 'El expediente fue retirado y la bandeja quedó liberada.',
      confirmButtonColor: '#16A34A',
      timer: 3000,
      timerProgressBar: true
    });
  }

  // ===================================================================
  // NAVEGACIÓN
  // ===================================================================

  volver(): void {
    this.router.navigate(['/dashboard']);
  }
}
