import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { ExpedienteService, Expediente } from '../../../services/expediente';
import { IconComponent } from '../../../components/icon/icon.component';
import { FormularioSalida } from '../../../components/formulario-salida/formulario-salida';

/**
 * Componente para busqueda manual de expedientes y registro de salida.
 * 
 * FLUJO FALLBACK (10% casos excepcionales):
 * 1. Vigilante accede desde sidebar "Registro Salida"
 * 2. Sistema carga TODOS los expedientes disponibles por defecto
 * 3. Vigilante puede filtrar por HC, DNI o Nombre (opcional)
 * 4. Selecciona expediente de la tabla
 * 5. Se abre modal FormularioSalida (mismo que usa Mapa)
 * 6. Usuario completa y registra salida
 * 
 * CASOS DE USO:
 * - Familiar llega directo sin pasar por flujo completo
 * - Busqueda por datos cuando no se conoce bandeja
 * - Casos excepcionales fuera del flujo normal
 * 
 * CARACTERISTICAS:
 * - Carga inicial: Todos los expedientes disponibles
 * - Input grande optimizado para tablet
 * - Filtrado local con coincidencia EXACTA para numeros
 * - Tabla de resultados responsive
 * - Modal reutilizable FormularioSalida
 * - Solo muestra expedientes en estado valido para salida
 * 
 * @version 1.1.0
 * @author SGM Development Team
 * 
 * CHANGELOG v1.1.0:
 * - Carga inicial de todos los expedientes disponibles
 * - Filtrado local en lugar de busqueda en backend
 * - Coincidencia EXACTA para HC/DNI
 * - Coincidencia parcial para Nombre
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

  // Busqueda
  textoBusqueda = '';
  isLoading = false;
  busquedaRealizada = false;

  // Resultados
  resultados: Expediente[] = [];

  // Modal
  mostrarModalSalida = false;
  expedienteSeleccionado: any = null;

  ngOnInit(): void {
    console.log('[BusquedaSalida] Componente inicializado');

    // Cargar expedientes disponibles por defecto
    this.cargarExpedientesDisponibles();
  }

  /**
   * Carga todos los expedientes disponibles para salida.
   * Se ejecuta al iniciar el componente.
   */
  private cargarExpedientesDisponibles(): void {
    this.isLoading = true;

    this.expedienteService.getAll().subscribe({
      next: (expedientes) => {
        // Filtrar solo estados validos para salida
        const estadosValidos = ['EnBandeja', 'PendienteRetiro'];
        this.resultados = expedientes.filter(exp =>
          estadosValidos.includes(exp.estadoActual)
        );

        this.isLoading = false;
        this.busquedaRealizada = true;

        console.log('[BusquedaSalida] Expedientes disponibles cargados:', this.resultados.length);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[BusquedaSalida] Error al cargar expedientes:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Cargar',
          text: 'No se pudieron cargar los expedientes disponibles',
          confirmButtonColor: '#EF4444',
          showConfirmButton: true
        });
      }
    });
  }

  /**
   * Filtra los expedientes cargados segun el texto de busqueda.
   * Busca coincidencias EXACTAS en HC/DNI o parciales en Nombre.
   */
  buscar(): void {
    const texto = this.textoBusqueda.trim();

    if (!texto) {
      // Si esta vacio, recargar todos
      this.cargarExpedientesDisponibles();
      return;
    }

    if (texto.length < 3) {
      Swal.fire({
        icon: 'warning',
        title: 'Texto Muy Corto',
        text: 'Ingrese al menos 3 caracteres para buscar',
        confirmButtonColor: '#F59E0B',
        showConfirmButton: true
      });
      return;
    }

    this.isLoading = true;

    // Cargar todos los expedientes y filtrar localmente
    this.expedienteService.getAll().subscribe({
      next: (expedientes) => {
        const textoLower = texto.toLowerCase();
        const esNumero = /^\d+$/.test(texto);

        // Filtrar por estado valido primero
        const estadosValidos = ['EnBandeja', 'PendienteRetiro'];
        let expedientesFiltrados = expedientes.filter(exp =>
          estadosValidos.includes(exp.estadoActual)
        );

        // Aplicar filtro de busqueda
        if (esNumero) {
          // Busqueda EXACTA por HC o DNI
          expedientesFiltrados = expedientesFiltrados.filter(exp =>
            exp.hc === texto || exp.numeroDocumento === texto
          );
        } else {
          // Busqueda PARCIAL por nombre
          expedientesFiltrados = expedientesFiltrados.filter(exp =>
            exp.nombreCompleto.toLowerCase().includes(textoLower)
          );
        }

        this.resultados = expedientesFiltrados;
        this.isLoading = false;
        this.busquedaRealizada = true;

        console.log('[BusquedaSalida] Resultados filtrados:', this.resultados.length);

        if (this.resultados.length === 0) {
          Swal.fire({
            icon: 'info',
            title: 'Sin Resultados',
            html: `
              <p class="text-sm text-gray-600">
                No se encontraron expedientes que coincidan con: 
                <strong class="text-gray-800">"${texto}"</strong>
              </p>
              <p class="text-xs text-gray-500 mt-2">
                Verifique el dato ingresado e intente nuevamente
              </p>
            `,
            confirmButtonColor: '#0891B2',
            showConfirmButton: true
          });
        }
      },
      error: (err) => {
        this.manejarError(err);
      }
    });
  }

  /**
   * Maneja errores de busqueda.
   */
  private manejarError(err: any): void {
    this.isLoading = false;
    console.error('[BusquedaSalida] Error en busqueda:', err);

    Swal.fire({
      icon: 'error',
      title: 'Error en Busqueda',
      text: err.error?.message || 'No se pudo realizar la busqueda. Intente nuevamente.',
      confirmButtonColor: '#EF4444',
      showConfirmButton: true
    });
  }

  /**
   * Selecciona un expediente y abre el modal de salida.
   */
  seleccionarExpediente(expediente: Expediente): void {
    console.log('[BusquedaSalida] Expediente seleccionado:', expediente.codigoExpediente);

    // Guardar expediente y abrir modal
    this.expedienteSeleccionado = expediente;
    this.mostrarModalSalida = true;
  }

  /**
   * Maneja el evento cuando se registra salida exitosamente.
   */
  onSalidaCompletada(response: any): void {
    console.log('[BusquedaSalida] Salida completada:', response);

    // Cerrar modal
    this.cerrarModalSalida();

    // Recargar expedientes disponibles (actualiza la lista)
    this.limpiarBusqueda();

    // Mostrar notificacion
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000
    });

    Toast.fire({
      icon: 'success',
      title: 'Salida Registrada Exitosamente'
    });
  }

  /**
   * Cierra el modal de registro de salida.
   */
  cerrarModalSalida(): void {
    this.mostrarModalSalida = false;
    this.expedienteSeleccionado = null;
  }

  /**
   * Limpia el formulario de busqueda y recarga todos los expedientes.
   */
  limpiarBusqueda(): void {
    this.textoBusqueda = '';
    this.cargarExpedientesDisponibles();
  }

  /**
   * Vuelve al dashboard.
   */
  volver(): void {
    this.router.navigate(['/dashboard']);
  }

  /**
   * Obtiene el color del badge segun el estado.
   */
  getEstadoBadgeClass(estado: string): string {
    switch (estado) {
      case 'EnBandeja':
        return 'bg-blue-100 text-blue-700 border-blue-300';
      case 'PendienteRetiro':
        return 'bg-green-100 text-green-700 border-green-300';
      default:
        return 'bg-gray-100 text-gray-700 border-gray-300';
    }
  }

  /**
   * Formatea el estado para mostrar.
   */
  formatearEstado(estado: string): string {
    const estados: { [key: string]: string } = {
      'EnBandeja': 'En Bandeja',
      'PendienteRetiro': 'Pendiente Retiro'
    };
    return estados[estado] || estado;
  }
}
