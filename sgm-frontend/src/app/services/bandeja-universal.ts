import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, catchError } from 'rxjs';
import { AuthService } from './auth';
import { IntegracionService, PacientePendiente } from './integracion';

// ===================================================================
// INTERFACES
// ===================================================================

/** Estructura unificada para todos los roles de la bandeja */
export interface BandejaItem {
  // ── Identificadores ──────────────────────────────────────────────
  id: string | number;
  tipoId: 'hc' | 'codigo_sgm';
  expedienteID?: number;
  hc?: string;
  codigoExpediente?: string;
  numeroDocumento?: string;
  tipoDocumento?: string;           // "DNI", "CE", "Pasaporte", "NN"

  // ── Datos del paciente ───────────────────────────────────────────
  nombreCompleto: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  nombres?: string;
  edad?: number;
  esNN?: boolean;

  // ── Datos específicos por contexto ───────────────────────────────
  servicio?: string;
  bandeja?: string;
  tieneDatosSigem?: boolean;
  advertencias?: string[];

  // Deudas económicas
  monto?: number;
  moneda?: string;

  // Deudas de sangre
  unidadesSangre?: number;
  tipoSangre?: string;

  // Documentos legales
  numeroOficio?: string;
  tipoDocumentoLegal?: string;

  // ── Fechas ───────────────────────────────────────────────────────
  fechaFallecimiento?: Date;
  fechaIngreso?: Date;
  fechaSolicitud?: Date;
  fechaCreacion?: Date;

  // ── Metadata de negocio ──────────────────────────────────────────
  estado: string;
  estadoTexto: string;
  tipoItem: BandejaItemTipo;
  accionPrincipal: string;
  tiempoTranscurrido?: number;      // horas
  esUrgente?: boolean;
}

export type BandejaItemTipo =
  | 'generacion_expediente'
  | 'aceptar_custodia'
  | 'deuda_sangre'
  | 'deuda_economica'
  | 'autorizacion_retiro'
  | 'validacion_legal'
  | 'solicitud_excepcion';

// ===================================================================
// SERVICIO
// ===================================================================

@Injectable({ providedIn: 'root' })
export class BandejaUniversalService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private integracionService = inject(IntegracionService);

  private readonly apiUrl = 'https://localhost:7153/api';

  // ── Método principal ─────────────────────────────────────────────

  getItems(): Observable<BandejaItem[]> {
    const rol = this.authService.getUserRole();

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
      catchError(err => {
        console.error('[BandejaUniversal] Error al cargar items:', err);
        return of([]);
      })
    );
  }

  // ── Mappers por rol ──────────────────────────────────────────────

  /**
   * ENFERMERÍA: Pacientes fallecidos pendientes de generar expediente.
   * Usa nombreCompleto y esNN directamente desde BandejaEntradaDTO.
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
            id: p.hc,
            tipoId: 'hc' as const,
            hc: p.hc,
            numeroDocumento: p.esNN ? undefined : p.numeroDocumento,
            tipoDocumento: p.esNN ? 'NN' : this.resolverTipoDocumento(p.tipoDocumentoID),
            nombreCompleto: p.nombreCompleto,
            edad: p.edad,
            esNN: p.esNN,
            servicio: p.servicioFallecimiento ?? 'No especificado',
            fechaFallecimiento,
            tieneDatosSigem: p.tieneDatosSigem,
            advertencias: p.advertencias,
            estado: 'PendienteGeneracion',
            estadoTexto: 'Pendiente de Expediente',
            tipoItem: 'generacion_expediente' as const,
            accionPrincipal: 'Generar Expediente',
            tiempoTranscurrido: horasTranscurridas,
            esUrgente: horasTranscurridas > 4
          } as BandejaItem;
        })
      ),
      catchError(err => {
        console.error('Error al obtener pendientes de enfermería:', err);
        return of([]);
      })
    );
  }

  /**
   * AMBULANCIA: Expedientes pendientes de recojo.
   * tipoItem 'aceptar_custodia' para distinguir de generacion_expediente.
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
            id: exp.codigoExpediente,
            tipoId: 'codigo_sgm' as const,
            expedienteID: exp.expedienteID,
            codigoExpediente: exp.codigoExpediente,
            hc: exp.hc,
            nombreCompleto: exp.nombreCompleto,
            servicio: exp.servicioFallecimiento ?? 'No especificado',
            fechaFallecimiento,
            estado: exp.estadoActual,
            estadoTexto: exp.estadoActual === 'EnPiso' ? 'En Piso' : 'Pendiente Recojo',
            tipoItem: 'aceptar_custodia' as const,
            accionPrincipal: 'Aceptar Custodia',
            tiempoTranscurrido: horasTranscurridas,
            esUrgente: horasTranscurridas > 4
          } as BandejaItem;
        })
      ),
      catchError(err => {
        console.error('Error al obtener pendientes de ambulancia:', err);
        return of([]);
      })
    );
  }

  /** BANCO DE SANGRE: Deudas de sangre pendientes */
  private getParaBancoSangre(): Observable<BandejaItem[]> {
    // TODO: reemplazar con endpoint real
    return of([
      {
        id: 'SGM-2025-00015', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00015', hc: '654321',
        numeroDocumento: '47812345', tipoDocumento: 'DNI',
        nombreCompleto: 'Vargas León, María Estefany',
        unidadesSangre: 2, tipoSangre: 'O+', servicio: 'UCI',
        estado: 'DeudaPendiente', estadoTexto: 'Compromiso Pendiente',
        tipoItem: 'deuda_sangre' as const, accionPrincipal: 'Regularizar Deuda',
        tiempoTranscurrido: 12, esUrgente: false
      },
      {
        id: 'SGM-2025-00018', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00018', hc: '789456',
        numeroDocumento: '41236547', tipoDocumento: 'DNI',
        nombreCompleto: 'Castillo Ruiz, Jorge Antonio',
        unidadesSangre: 4, tipoSangre: 'AB-', servicio: 'Cirugía',
        estado: 'DeudaPendiente', estadoTexto: 'Compromiso Pendiente',
        tipoItem: 'deuda_sangre' as const, accionPrincipal: 'Regularizar Deuda',
        tiempoTranscurrido: 36, esUrgente: true
      }
    ] as BandejaItem[]);
  }

  /** CUENTAS PACIENTES / SERVICIO SOCIAL: Deudas económicas */
  private getParaCuentas(): Observable<BandejaItem[]> {
    // TODO: reemplazar con endpoint real
    return of([
      {
        id: 'SGM-2025-00012', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00012', hc: '123456',
        numeroDocumento: '71234567', tipoDocumento: 'DNI',
        nombreCompleto: 'Ramos Galindo, Erick Jesús',
        monto: 1500.00, moneda: 'PEN', servicio: 'Emergencia',
        estado: 'DeudaPendiente', estadoTexto: 'Pago Pendiente',
        tipoItem: 'deuda_economica' as const, accionPrincipal: 'Gestionar Pago',
        tiempoTranscurrido: 24, esUrgente: true
      }
    ] as BandejaItem[]);
  }

  /** ADMISIÓN: Expedientes listos para autorizar retiro */
  private getParaAdmision(): Observable<BandejaItem[]> {
    // TODO: reemplazar con endpoint real
    return of([
      {
        id: 'SGM-2025-00010', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00010', hc: '789012',
        numeroDocumento: '43219876', tipoDocumento: 'DNI',
        nombreCompleto: 'Chuquipiondo Ikari, Diego Armando',
        bandeja: 'B-01', fechaIngreso: new Date('2025-11-14T10:00:00'),
        estado: 'EnBandeja', estadoTexto: 'En Mortuorio',
        tipoItem: 'autorizacion_retiro' as const, accionPrincipal: 'Autorizar Retiro',
        tiempoTranscurrido: 48, esUrgente: true
      },
      {
        id: 'SGM-2025-00011', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00011', hc: '456789',
        numeroDocumento: '40987654', tipoDocumento: 'DNI',
        nombreCompleto: 'García Mendoza, Ana Patricia',
        bandeja: 'B-03', fechaIngreso: new Date('2025-11-15T08:30:00'),
        estado: 'EnBandeja', estadoTexto: 'En Mortuorio',
        tipoItem: 'autorizacion_retiro' as const, accionPrincipal: 'Autorizar Retiro',
        tiempoTranscurrido: 18, esUrgente: false
      }
    ] as BandejaItem[]);
  }

  /** VIGILANTE SUPERVISOR: Documentos legales por validar */
  private getParaVigilancia(): Observable<BandejaItem[]> {
    // TODO: reemplazar con endpoint real
    return of([
      {
        id: 'SGM-2025-00020', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00020',
        nombreCompleto: 'Paciente NN', tipoDocumento: 'NN', esNN: true,
        numeroOficio: '2025-0345-DIVPOL', tipoDocumentoLegal: 'Oficio Policial',
        fechaSolicitud: new Date('2025-11-16T00:00:00'),
        estado: 'PendienteValidacion', estadoTexto: 'Requiere Validación',
        tipoItem: 'validacion_legal' as const, accionPrincipal: 'Validar Documentos',
        tiempoTranscurrido: 6, esUrgente: false
      }
    ] as BandejaItem[]);
  }

  /** JEFE DE GUARDIA: Solicitudes de excepción */
  private getParaJefeGuardia(): Observable<BandejaItem[]> {
    // TODO: reemplazar con endpoint real
    return of([
      {
        id: 'SGM-2025-00022', tipoId: 'codigo_sgm' as const,
        codigoExpediente: 'SGM-2025-00022',
        nombreCompleto: 'Paciente NN — Retiro por Primo', tipoDocumento: 'NN', esNN: true,
        fechaSolicitud: new Date('2025-11-16T14:30:00'),
        estado: 'PendienteAutorizacion', estadoTexto: 'Requiere Aprobación',
        tipoItem: 'solicitud_excepcion' as const, accionPrincipal: 'Revisar Solicitud',
        tiempoTranscurrido: 3, esUrgente: false
      }
    ] as BandejaItem[]);
  }

  /** Convierte TipoDocumentoIdentidad (int) a etiqueta legible */
  private resolverTipoDocumento(id?: number): string | undefined {
    const map: Record<number, string> = { 1: 'DNI', 2: 'CE', 3: 'Pasaporte', 4: 'RUC' };
    return id != null ? (map[id] ?? 'Doc') : undefined;
  }
}
