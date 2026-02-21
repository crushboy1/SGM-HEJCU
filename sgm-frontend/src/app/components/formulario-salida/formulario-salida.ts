import { Component, EventEmitter, Input, Output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import Swal from 'sweetalert2';

import { IconComponent } from '../icon/icon.component';
import { SemaforoDeudas } from '../semaforo-deudas/semaforo-deudas';
import { SalidaService, RegistrarSalidaRequest } from '../../services/salida';
import { DeudaEconomica } from '../../services/deuda-economica';
import { DeudaSangre } from '../../services/deuda-sangre';
import { DeudaEconomicaSemaforoDTO } from '../../models/deuda-economica.model';

/**
 * Modal reutilizable para registro de salida del mortuorio.
 * 
 * COMPONENTE REUTILIZABLE usado en dos flujos:
 * 
 * FLUJO 1 - Desde Mapa Mortuorio (90% casos):
 * - Usuario hace click en boton "Salida" de bandeja ocupada
 * - Mapa carga expediente via ExpedienteService
 * - Mapa abre este modal pasando expediente como Input
 * - Usuario completa formulario y registra
 * - Modal emite evento onSalidaRegistrada
 * - Mapa cierra modal y refresca datos
 * 
 * FLUJO 2 - Desde Busqueda Manual (10% excepcional):
 * - Usuario busca por HC/DNI/Nombre en pagina busqueda-salida
 * - Usuario selecciona expediente de resultados
 * - Pagina abre este modal pasando expediente como Input
 * - Usuario completa formulario y registra
 * - Modal emite evento onSalidaRegistrada
 * - Pagina cierra modal
 * 
 * CARACTERISTICAS:
 * - Sin navegacion (es modal)
 * - Recibe expediente pre-cargado
 * - Validaciones automaticas habilitadas
 * - Pre-llenado inteligente segun tipo
 * - Optimizado para tablet (Vigilante Mortuorio)
 * - FASE SEMAFORO: Verifica deudas antes de permitir salida
 * 
 * @version 1.1.0 (Fase Semáforo)
 * @author SGM Development Team
 */
@Component({
  selector: 'app-formulario-salida',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, SemaforoDeudas],
  templateUrl: './formulario-salida.html',
  styleUrl: './formulario-salida.css'
})
export class FormularioSalida implements OnInit {
  private salidaService = inject(SalidaService);
  private deudaEconomicaService = inject(DeudaEconomica);
  private deudaSangreService = inject(DeudaSangre);

  // Expediente recibido desde componente padre (ya validado)
  @Input() expediente: any = null;

  // Evento emitido cuando se registra salida exitosamente
  @Output() onSalidaRegistrada = new EventEmitter<any>();

  // Evento emitido cuando usuario cierra modal sin guardar
  @Output() onCerrar = new EventEmitter<void>();

  isLoading = false;

  // Semáforos de deudas
  semaforoEconomica?: DeudaEconomicaSemaforoDTO;
  semaforoSangre?: string;
  cargandoSemaforos = false;

  // Modelo del formulario alineado con RegistrarSalidaDTO.cs del backend
  form: RegistrarSalidaRequest = {
    expedienteID: 0,
    tipoSalida: 'Familiar',

    // Responsable del retiro
    responsableNombre: '',
    responsableTipoDocumento: 'DNI',
    responsableNumeroDocumento: '',
    responsableParentesco: '',
    responsableTelefono: '',

    // Validaciones administrativas (habilitadas automaticamente)
    documentacionVerificada: true,
    pagoRealizado: true,
    numeroRecibo: '',

    // Funeraria (opcional)
    nombreFuneraria: '',
    conductorFuneraria: '',
    dniConductor: '',
    placaVehiculo: '',
    ayudanteFuneraria: '',
    dniAyudante: '',

    // Autoridad legal (solo si tipoSalida = AutoridadLegal)
    numeroAutorizacion: '',
    entidadAutorizante: '',

    // Destino y observaciones
    destino: '',
    observaciones: ''
  };

  ngOnInit(): void {
    if (!this.expediente) {
      console.error('[FormularioSalida] Modal abierto sin expediente');
      Swal.fire({
        icon: 'error',
        title: 'Error de Configuración',
        text: 'No se proporcionó expediente al modal',
        confirmButtonColor: '#EF4444'
      }).then(() => {
        this.cerrarModal();
      });
      return;
    }

    // Configurar formulario con datos del expediente
    this.inicializarFormulario();

    // Cargar semáforos de deudas
    this.cargarSemaforos();

    console.log('[FormularioSalida] Modal inicializado para expediente:', this.expediente.codigoExpediente);
  }

  /**
   * Inicializa el formulario con datos del expediente recibido.
   * Habilita validaciones administrativas y pre-llena segun tipo.
   */
  private inicializarFormulario(): void {
    // 1. Asignar ID del expediente
    this.form.expedienteID = this.expediente.expedienteID;

    // 2. Habilitar validaciones administrativas automaticamente
    // Razon: Si el expediente llego a este modal, ya paso por Admision
    this.form.documentacionVerificada = true;
    this.form.pagoRealizado = true;

    // 3. Pre-llenar formulario segun tipo de expediente
    this.prellenarFormularioSegunTipo();
  }

  /**
   * Carga los semáforos de deudas del expediente
   */
  private cargarSemaforos(): void {
    this.cargandoSemaforos = true;

    forkJoin({
      economica: this.deudaEconomicaService.obtenerSemaforo(this.expediente.expedienteID),
      sangre: this.deudaSangreService.obtenerSemaforo(this.expediente.expedienteID)
    }).subscribe({
      next: (semaforos) => {
        this.semaforoEconomica = semaforos.economica;
        this.semaforoSangre = semaforos.sangre;
        this.cargandoSemaforos = false;

        console.log('[FormularioSalida] Semáforos cargados:', semaforos);
      },
      error: (err) => {
        console.error('[FormularioSalida] Error al cargar semáforos:', err);
        this.cargandoSemaforos = false;

        // No bloquear el modal, pero avisar
        Swal.fire({
          icon: 'warning',
          title: 'Advertencia',
          text: 'No se pudieron verificar las deudas. Contacte con el administrador.',
          confirmButtonColor: '#F59E0B'
        });
      }
    });
  }

  /**
   * Pre-llena el formulario segun el tipo de expediente.
   * Detecta casos especiales y configura valores por defecto.
   */
  private prellenarFormularioSegunTipo(): void {
    // Caso 1: Expediente Externo (NN, sin familiar identificado)
    if (this.expediente.tipoExpediente === 'Externo') {
      this.form.tipoSalida = 'AutoridadLegal';
      this.form.entidadAutorizante = 'Fiscalia';
      this.form.destino = 'Morgue Central de Lima';
      this.form.responsableNombre = 'Ministerio Publico';
      this.form.responsableTipoDocumento = 'RUC';
      this.form.documentacionVerificada = true;
      this.form.pagoRealizado = true;

      console.log('[FormularioSalida] Pre-llenado: Expediente Externo');
      return;
    }

    // Caso 2: Traslado a otro hospital
    if (this.expediente.tipoSalida === 'TrasladoHospital') {
      this.form.tipoSalida = 'TrasladoHospital';
      this.form.destino = 'Hospital de referencia';

      console.log('[FormularioSalida] Pre-llenado: Traslado Hospital');
      return;
    }

    // Caso 3: Normal - Retiro Familiar (default)
    this.form.tipoSalida = 'Familiar';
    this.form.destino = 'Cementerio Local';

    console.log('[FormularioSalida] Pre-llenado: Retiro Familiar');
  }

  /**
   * Valida formulario y muestra confirmacion antes de registrar.
   */
  confirmarSalida(): void {
    // VALIDACIÓN 1: Verificar deudas pendientes
    if (!this.verificarSemaforos()) {
      return;
    }

    // VALIDACIÓN 2: Validar formulario
    if (!this.validarFormulario()) {
      return;
    }

    // CONFIRMACIÓN
    Swal.fire({
      title: 'Confirmar Registro de Salida',
      html: `
        <div class="text-left space-y-2 text-sm">
          <div class="bg-blue-50 p-3 rounded-lg mb-3">
            <p class="font-semibold text-gray-700">Expediente: <span class="text-blue-600">${this.expediente.codigoExpediente}</span></p>
            <p class="font-semibold text-gray-700">Paciente: <span class="text-blue-600">${this.expediente.nombreCompleto}</span></p>
          </div>
          
          <div class="bg-green-50 p-3 rounded-lg mb-3">
            <p class="font-semibold text-green-800">Verificación de Deudas</p>
            <p class="text-xs text-green-700">Económica: ${this.semaforoEconomica?.semaforo || 'NO DEBE'}</p>
            <p class="text-xs text-green-700">Sangre: ${this.semaforoSangre || 'SIN DEUDA'}</p>
          </div>
          
          <p><strong>Responsable:</strong> ${this.form.responsableNombre}</p>
          <p><strong>Documento:</strong> ${this.form.responsableTipoDocumento} ${this.form.responsableNumeroDocumento}</p>
          <p><strong>Tipo Salida:</strong> ${this.form.tipoSalida}</p>
          ${this.form.nombreFuneraria ? `<p><strong>Funeraria:</strong> ${this.form.nombreFuneraria}</p>` : ''}
          
          <div class="bg-red-50 border border-red-200 p-3 rounded-lg mt-3">
            <p class="text-red-700 font-semibold">ATENCIÓN</p>
            <p class="text-red-600 text-xs mt-1">Esta acción liberará la bandeja y cerrará el expediente.</p>
            <p class="text-red-600 text-xs">No se puede deshacer.</p>
          </div>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#22C55E',
      cancelButtonColor: '#6B7280',
      confirmButtonText: 'Sí, Registrar Salida',
      cancelButtonText: 'Cancelar'
    }).then((result) => {
      if (result.isConfirmed) {
        this.procesarSalida();
      }
    });
  }

  /**
   * Verifica los semáforos de deudas antes de permitir salida
   * @returns true si puede continuar, false si hay deudas pendientes
   */
  private verificarSemaforos(): boolean {
    const tieneDeudaEconomica = this.semaforoEconomica?.tieneDeuda || false;
    const tieneDeudaSangre = this.semaforoSangre?.includes('PENDIENTE') || false;

    if (tieneDeudaEconomica || tieneDeudaSangre) {
      let detallesDeudas = '';

      if (tieneDeudaEconomica) {
        detallesDeudas += `
          <div class="flex items-center gap-3 text-red-600 mb-3">
            <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            <div class="text-left">
              <p class="font-semibold">Deuda Económica Pendiente</p>
              <p class="text-sm">${this.semaforoEconomica?.instruccion}</p>
            </div>
          </div>
        `;
      }

      if (tieneDeudaSangre) {
        detallesDeudas += `
          <div class="flex items-center gap-3 text-red-600">
            <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
            </svg>
            <div class="text-left">
              <p class="font-semibold">Deuda de Sangre Pendiente</p>
              <p class="text-sm">${this.semaforoSangre}</p>
            </div>
          </div>
        `;
      }

      Swal.fire({
        icon: 'error',
        title: 'No se Puede Registrar Salida',
        html: `
          <div class="text-left space-y-2 mb-4">
            <p class="font-semibold">${this.expediente.nombreCompleto}</p>
            <p class="text-sm text-gray-600">Expediente: ${this.expediente.codigoExpediente}</p>
          </div>
          <div class="p-4 bg-red-50 rounded-lg">
            ${detallesDeudas}
          </div>
          <p class="text-sm text-gray-600 mt-4">
            El expediente debe regularizar sus deudas antes de autorizar la salida.
          </p>
        `,
        confirmButtonText: 'Entendido',
        confirmButtonColor: '#DC2626'
      });

      return false;
    }

    return true;
  }

  /**
   * Valida que todos los campos requeridos esten completos.
   */
  private validarFormulario(): boolean {
    // 1. Responsable obligatorio
    if (!this.form.responsableNombre?.trim()) {
      Swal.fire({
        icon: 'warning',
        title: 'Faltan Datos Obligatorios',
        text: 'Debe ingresar el nombre completo del responsable del retiro',
        confirmButtonColor: '#F59E0B'
      });
      return false;
    }

    if (!this.form.responsableNumeroDocumento?.trim()) {
      Swal.fire({
        icon: 'warning',
        title: 'Faltan Datos Obligatorios',
        text: 'Debe ingresar el número de documento del responsable',
        confirmButtonColor: '#F59E0B'
      });
      return false;
    }

    // 2. Si es Familiar, validar parentesco
    if (this.form.tipoSalida === 'Familiar' && !this.form.responsableParentesco?.trim()) {
      Swal.fire({
        icon: 'warning',
        title: 'Faltan Datos Obligatorios',
        text: 'Debe especificar el parentesco del responsable con el fallecido',
        confirmButtonColor: '#F59E0B'
      });
      return false;
    }

    // 3. Si es Autoridad Legal, validar autorizacion
    if (this.form.tipoSalida === 'AutoridadLegal') {
      if (!this.form.numeroAutorizacion?.trim()) {
        Swal.fire({
          icon: 'warning',
          title: 'Faltan Datos Obligatorios',
          text: 'Debe ingresar el número de oficio o autorización legal',
          confirmButtonColor: '#F59E0B'
        });
        return false;
      }
      if (!this.form.entidadAutorizante?.trim()) {
        Swal.fire({
          icon: 'warning',
          title: 'Faltan Datos Obligatorios',
          text: 'Debe especificar la entidad autorizante',
          confirmButtonColor: '#F59E0B'
        });
        return false;
      }
    }

    // 4. Si hay funeraria, validar datos completos
    if (this.form.nombreFuneraria?.trim()) {
      if (!this.form.conductorFuneraria?.trim() || !this.form.placaVehiculo?.trim()) {
        Swal.fire({
          icon: 'warning',
          title: 'Datos de Funeraria Incompletos',
          text: 'Si registra una funeraria, debe completar conductor y placa',
          confirmButtonColor: '#F59E0B'
        });
        return false;
      }
    }

    return true;
  }

  /**
   * Envia solicitud al backend para registrar salida.
   */
  private procesarSalida(): void {
    this.isLoading = true;

    this.salidaService.registrarSalida(this.form).subscribe({
      next: (response) => {
        this.isLoading = false;

        Swal.fire({
          icon: 'success',
          title: 'Salida Registrada Exitosamente',
          html: `
            <div class="text-left space-y-2 text-sm">
              <p class="text-green-600 font-semibold">El cuerpo ha sido retirado del mortuorio</p>
              <div class="bg-green-50 p-3 rounded-lg mt-3 space-y-1">
                <p>Bandeja liberada: <strong>${this.expediente.codigoBandeja || 'N/A'}</strong></p>
                <p>Expediente cerrado: <strong>${this.expediente.codigoExpediente}</strong></p>
                <p>Responsable: <strong>${this.form.responsableNombre}</strong></p>
              </div>
            </div>
          `,
          confirmButtonColor: '#10B981',
          confirmButtonText: 'Aceptar'
        }).then(() => {
          // Emitir evento con respuesta del backend
          this.onSalidaRegistrada.emit(response);
        });

        console.log('[FormularioSalida] Salida registrada:', response);
      },
      error: (err: any) => {
        this.isLoading = false;
        console.error('[FormularioSalida] Error al registrar:', err);

        Swal.fire({
          icon: 'error',
          title: 'Error al Registrar Salida',
          text: err.message || 'No se pudo registrar la salida. Intente nuevamente.',
          confirmButtonColor: '#EF4444'
        });
      }
    });
  }

  /**
   * Cierra el modal sin guardar.
   */
  cerrarModal(): void {
    if (this.formularioTieneDatos()) {
      Swal.fire({
        title: 'Cerrar Formulario',
        text: 'Se perderán los datos ingresados',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#EF4444',
        cancelButtonColor: '#6B7280',
        confirmButtonText: 'Sí, Cerrar',
        cancelButtonText: 'Continuar Editando'
      }).then((result) => {
        if (result.isConfirmed) {
          this.onCerrar.emit();
        }
      });
    } else {
      this.onCerrar.emit();
    }
  }

  /**
   * Verifica si el formulario tiene datos ingresados.
   */
  private formularioTieneDatos(): boolean {
    return !!(
      this.form.responsableNombre?.trim() ||
      this.form.responsableNumeroDocumento?.trim() ||
      this.form.nombreFuneraria?.trim()
    );
  }
}
