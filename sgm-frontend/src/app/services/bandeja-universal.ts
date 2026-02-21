import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, catchError } from 'rxjs';
import { AuthService } from './auth';
import { IntegracionService, PacientePendiente } from './integracion';

// ===================================================================
// INTERFACES REFACTORIZADAS
// ===================================================================

/**
 * Estructura unificada con campos EXPLÍCITOS (no abstracciones)
 * Permite ordenamiento correcto y tablas legibles
 */
export interface BandejaItem {
  // ===== IDENTIFICADORES =====
  id: string | number;              // ID principal (puede ser HC o Código SGM)
  tipoId: 'hc' | 'codigo_sgm';      // Indica qué tipo de ID es
  expedienteID?: number;            // ID numérico de la BD (para navegación)
  hc?: string;                      // Historia Clínica (SIEMPRE presente si existe)
  codigoExpediente?: string;        // Código SGM (solo para expedientes generados)
  numeroDocumento?: string;         // DNI, CE, Pasaporte
  tipoDocumento?: string;           // "DNI", "CE", "Pasaporte", "NN"

  // ===== DATOS DEL PACIENTE =====
  nombreCompleto: string;           // Para mostrar en tabla
  apellidoPaterno?: string;         // Para ordenamiento específico
  apellidoMaterno?: string;
  nombres?: string;

  // ===== DATOS ESPECÍFICOS DEL CONTEXTO =====
  // (Campos opcionales según el tipo de item)
  servicio?: string;                // "UCI", "Cirugía", "Emergencia"
  bandeja?: string;                 // "B-01", "B-05"

  // Deudas económicas
  monto?: number;                   // 1500.00 (número limpio)
  moneda?: string;                  // "PEN", "USD"

  // Deudas de sangre
  unidadesSangre?: number;          // 2, 4 (número)
  tipoSangre?: string;              // "O+", "AB-"

  // Documentos legales
  numeroOficio?: string;            // "2025-0345-DIVPOL"
  tipoDocumentoLegal?: string;      // "Oficio Policial", "Acta Fiscal"

  // ===== FECHAS (como Date para ordenamiento correcto) =====
  fechaFallecimiento?: Date;
  fechaIngreso?: Date;
  fechaSolicitud?: Date;
  fechaCreacion?: Date;

  // ===== METADATA DE NEGOCIO =====
  estado: string; 
  estadoTexto: string; 
  tipoItem: BandejaItemTipo;
  accionPrincipal: string;

  tiempoTranscurrido?: number;      // Horas (para cálculos)
  esUrgente?: boolean;
}

/**
 * Tipos de items que pueden aparecer en la bandeja
 */
export type BandejaItemTipo =
  | 'generacion_expediente'    // Enfermería: Crear expediente desde SIGEM
  | 'deuda_sangre'             // Banco de Sangre: Regularizar compromiso
  | 'deuda_economica'          // Cuentas/Caja: Gestionar pagos
  | 'autorizacion_retiro'      // Admisión: Autorizar entrega a familiar
  | 'validacion_legal'         // Vigilante Supervisor: Validar docs
  | 'solicitud_excepcion';     // Jefe de Guardia: Aprobar excepciones

// ===================================================================
// SERVICIO FACADE
// ===================================================================

@Injectable({
  providedIn: 'root'
})
export class BandejaUniversalService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private integracionService = inject(IntegracionService);

  private readonly apiUrl = 'https://localhost:7153/api';

  // ===================================================================
  // MÉTODO PRINCIPAL
  // ===================================================================

  getItems(): Observable<BandejaItem[]> {
    const rol = this.authService.getUserRole();
    console.log('🔄 [BandejaUniversal] Cargando items para rol:', rol);

    const mappers: Record<string, () => Observable<BandejaItem[]>> = {
      'EnfermeriaTecnica': () => this.getParaEnfermeria(),
      'EnfermeriaLicenciada': () => this.getParaEnfermeria(),
      'SupervisoraEnfermeria': () => this.getParaEnfermeria(),
      'Ambulancia': () => this.getParaAmbulancia(),
      'BancoSangre': () => this.getParaBancoSangre(),
      'CuentasPacientes': () => this.getParaCuentas(),
      'ServicioSocial': () => this.getParaCuentas(),
      'Admision': () => this.getParaAdmision(),
      'VigilanteSupervisor': () => this.getParaVigilancia(),
      'JefeGuardia': () => this.getParaJefeGuardia(),
      'Administrador': () => this.getParaEnfermeria()
    };

    const mapper = mappers[rol];

    if (!mapper) {
      console.warn('[BandejaUniversal] Rol sin bandeja configurada:', rol);
      return of([]);
    }

    return mapper().pipe(
      catchError(error => {
        console.error('[BandejaUniversal] Error al cargar items:', error);
        return of([]);
      })
    );
  }

  // ===================================================================
  // MAPPERS ESPECÍFICOS POR ROL 
  // ===================================================================

  /**
   * ENFERMERÍA: Pacientes fallecidos pendientes de generar expediente
   */
  private getParaEnfermeria(): Observable<BandejaItem[]> {
    return this.integracionService.getPendientes().pipe(
      map((pacientes: PacientePendiente[]) =>
        pacientes.map(p => {
          const fechaFallecimiento = p.fechaHoraFallecimiento
            ? new Date(p.fechaHoraFallecimiento)
            : undefined;

          const horasTranscurridas = fechaFallecimiento
            ? (Date.now() - fechaFallecimiento.getTime()) / (1000 * 60 * 60)
            : 0;

          return {
            // Identificadores
            id: p.hc, 
            tipoId: 'hc' as const,
            hc: p.hc,
            numeroDocumento: p.numeroDocumento,
            tipoDocumento: this.obtenerTipoDocumento(p.tipoDocumentoID),

            // Datos del paciente (separados)
            nombreCompleto: this.formatearNombreCompleto(
              p.apellidoPaterno,
              p.apellidoMaterno,
              p.nombres
            ),
            apellidoPaterno: p.apellidoPaterno, 
            apellidoMaterno: p.apellidoMaterno,
            nombres: p.nombres,

            // Datos específicos 
            servicio: p.servicioFallecimiento || 'No especificado', 
            fechaFallecimiento: fechaFallecimiento, 

            // Metadata
            estado: 'PendienteGeneracion',
            estadoTexto: 'Pendiente de Expediente',
            tipoItem: 'generacion_expediente',
            accionPrincipal: 'Generar Expediente',
            tiempoTranscurrido: horasTranscurridas,
            esUrgente: horasTranscurridas > 4
          } as BandejaItem;
        })
      ),
      catchError(error => {
        console.error('❌ Error al obtener pendientes de enfermería:', error);
        return of([]);
      })
    );
  }

  /**
   * BANCO DE SANGRE: Expedientes con deudas de sangre pendientes
   * CAMBIO: Campos separados (unidadesSangre, tipoSangre), tipoId = 'codigo_sgm'
   */
  private getParaBancoSangre(): Observable<BandejaItem[]> {
    // TODO: Reemplazar con endpoint real
    const mockData: BandejaItem[] = [
      {
        id: 'SGM-2025-00015',
        tipoId: 'codigo_sgm' as const, 
        codigoExpediente: 'SGM-2025-00015',
        hc: '654321',
        numeroDocumento: '47812345',
        tipoDocumento: 'DNI',

        nombreCompleto: 'Vargas León, María Estefany',
        apellidoPaterno: 'Vargas',
        apellidoMaterno: 'León',
        nombres: 'María Estefany',

        // Datos específicos
        unidadesSangre: 2,
        tipoSangre: 'O+',  
        servicio: 'UCI', 

        estado: 'DeudaPendiente',
        estadoTexto: 'Compromiso Pendiente',
        tipoItem: 'deuda_sangre',
        accionPrincipal: 'Regularizar Deuda',
        tiempoTranscurrido: 12,
        esUrgente: false
      },
      {
        id: 'SGM-2025-00018',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00018',
        hc: '789456',
        numeroDocumento: '41236547',
        tipoDocumento: 'DNI',

        nombreCompleto: 'Castillo Ruiz, Jorge Antonio',
        apellidoPaterno: 'Castillo',
        apellidoMaterno: 'Ruiz',
        nombres: 'Jorge Antonio',

        unidadesSangre: 4,
        tipoSangre: 'AB-',
        servicio: 'Cirugía',

        estado: 'DeudaPendiente',
        estadoTexto: 'Compromiso Pendiente',
        tipoItem: 'deuda_sangre',
        accionPrincipal: 'Regularizar Deuda',
        tiempoTranscurrido: 36,
        esUrgente: true
      }
    ];
    return of(mockData);
  }

  /**
   CUENTAS PACIENTES: Deudas económicas pendientes
   */
  private getParaCuentas(): Observable<BandejaItem[]> {
    const mockData: BandejaItem[] = [
      {
        id: 'SGM-2025-00012',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00012',
        hc: '123456',
        numeroDocumento: '71234567',
        tipoDocumento: 'DNI',

        nombreCompleto: 'Ramos Galindo, Erick Jesús',
        apellidoPaterno: 'Ramos',
        apellidoMaterno: 'Galindo',
        nombres: 'Erick Jesús',

        // Datos específicos
        monto: 1500.00,
        moneda: 'PEN', 
        servicio: 'Emergencia',

        estado: 'DeudaPendiente',
        estadoTexto: 'Pago Pendiente',
        tipoItem: 'deuda_economica',
        accionPrincipal: 'Gestionar Pago',
        tiempoTranscurrido: 24,
        esUrgente: true
      }
    ];
    return of(mockData);
  }

  /**
   * ADMISIÓN: Expedientes en mortuorio listos para autorizar retiro
   CAMBIO: bandeja y fechaIngreso separados
   */
  private getParaAdmision(): Observable<BandejaItem[]> {
    const mockData: BandejaItem[] = [
      {
        id: 'SGM-2025-00010',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00010',
        hc: '789012',
        numeroDocumento: '43219876',
        tipoDocumento: 'DNI',

        nombreCompleto: 'Chuquipiondo Ikari, Diego Armando',
        apellidoPaterno: 'Chuquipiondo',
        apellidoMaterno: 'Ikari',
        nombres: 'Diego Armando',

        // Datos específicos (SIN emojis)
        bandeja: 'B-01',                                 
        fechaIngreso: new Date('2025-11-14T10:00:00'),  

        estado: 'EnBandeja',
        estadoTexto: 'En Mortuorio',
        tipoItem: 'autorizacion_retiro',
        accionPrincipal: 'Autorizar Retiro',
        tiempoTranscurrido: 48,
        esUrgente: true
      },
      {
        id: 'SGM-2025-00011',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00011',
        hc: '456789',
        numeroDocumento: '40987654',
        tipoDocumento: 'DNI',

        nombreCompleto: 'García Mendoza, Ana Patricia',
        apellidoPaterno: 'García',
        apellidoMaterno: 'Mendoza',
        nombres: 'Ana Patricia',

        bandeja: 'B-03',
        fechaIngreso: new Date('2025-11-15T08:30:00'),

        estado: 'EnBandeja',
        estadoTexto: 'En Mortuorio',
        tipoItem: 'autorizacion_retiro',
        accionPrincipal: 'Autorizar Retiro',
        tiempoTranscurrido: 18,
        esUrgente: false
      }
    ];
    return of(mockData);
  }

  /**
   * VIGILANTE SUPERVISOR: Documentos legales por validar
  
   */
  private getParaVigilancia(): Observable<BandejaItem[]> {
    const mockData: BandejaItem[] = [
      {
        id: 'SGM-2025-00020',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00020',

        nombreCompleto: 'Paciente NN - Validación Oficio Policial',
        tipoDocumento: 'NN',

        // Datos específicos 
        numeroOficio: '2025-0345-DIVPOL',             
        tipoDocumentoLegal: 'Oficio Policial',          
        fechaSolicitud: new Date('2025-11-16T00:00:00'), 

        estado: 'PendienteValidacion',
        estadoTexto: 'Requiere Validación',
        tipoItem: 'validacion_legal',
        accionPrincipal: 'Validar Documentos',
        tiempoTranscurrido: 6,
        esUrgente: false
      }
    ];
    return of(mockData);
  }

  /**
   * JEFE DE GUARDIA: Solicitudes de excepción
   */
  private getParaJefeGuardia(): Observable<BandejaItem[]> {
    const mockData: BandejaItem[] = [
      {
        id: 'SGM-2025-00022',
        tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00022',

        nombreCompleto: 'Paciente NN - Solicitud: Retiro por Primo',
        tipoDocumento: 'NN',

        // El "solicitante" podría estar en un campo separado en el futuro
        // Por ahora lo incluimos en observaciones o campos adicionales
        fechaSolicitud: new Date('2025-11-16T14:30:00'),

        estado: 'PendienteAutorizacion',
        estadoTexto: 'Requiere Aprobación',
        tipoItem: 'solicitud_excepcion',
        accionPrincipal: 'Revisar Solicitud',
        tiempoTranscurrido: 3,
        esUrgente: false
      }
    ];
    return of(mockData);
  }

  /**
 * AMBULANCIA: Expedientes pendientes de recojo
 * Estados: EnPiso, PendienteDeRecojo
 */
  private getParaAmbulancia(): Observable<BandejaItem[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Expedientes/pendientes-recojo`).pipe(
      map((expedientes: any[]) =>
        expedientes.map(exp => {
          const fechaFallecimiento = exp.fechaHoraFallecimiento
            ? new Date(exp.fechaHoraFallecimiento)
            : undefined;

          const horasTranscurridas = fechaFallecimiento
            ? (Date.now() - fechaFallecimiento.getTime()) / (1000 * 60 * 60)
            : 0;

          return {
            // Identificadores
            id: exp.codigoExpediente,
            tipoId: 'codigo_sgm' as const,
            expedienteID: exp.expedienteID,
            codigoExpediente: exp.codigoExpediente,
            hc: exp.hc,
            numeroDocumento: exp.numeroDocumento,
            tipoDocumento: this.obtenerTipoDocumento(exp.tipoDocumentoID),

            // Datos del paciente
            nombreCompleto: exp.nombreCompleto,
            apellidoPaterno: exp.apellidoPaterno,
            apellidoMaterno: exp.apellidoMaterno,
            nombres: exp.nombres,

            // Datos específicos
            servicio: exp.servicioFallecimiento || 'No especificado',
            fechaFallecimiento: fechaFallecimiento,

            // Metadata
            estado: exp.estadoActual,
            estadoTexto: exp.estadoActual === 'EnPiso' ? 'En Piso' : 'Pendiente Recojo',
            tipoItem: 'generacion_expediente',
            accionPrincipal: 'Aceptar Custodia',
            tiempoTranscurrido: horasTranscurridas,
            esUrgente: horasTranscurridas > 4
          } as BandejaItem;
        })
      ),
      catchError(error => {
        console.error('❌ Error al obtener pendientes de ambulancia:', error);
        return of([]);
      })
    );
  }

  // ===================================================================
  // MÉTODOS AUXILIARES
  // ===================================================================

  private formatearNombreCompleto(paterno: string, materno: string, nombres: string): string {
    const apellidos = [paterno, materno].filter(a => a && a.trim()).join(' ');
    return `${apellidos}, ${nombres}`;
  }

  private obtenerTipoDocumento(id: number): string {
    const tipos: Record<number, string> = {
      1: 'DNI',
      2: 'CE',
      3: 'Pasaporte',
      4: 'RUC',
      5: 'NN'
    };
    return tipos[id] || 'Doc';
  }
}
