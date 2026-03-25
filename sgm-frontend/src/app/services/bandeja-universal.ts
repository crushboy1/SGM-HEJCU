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
  tipoDocumento?: string;

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
  tiempoTranscurrido?: number;
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
  /**
   * Umbral en horas para marcar un registro como urgente.
   */
  private readonly UMBRAL_URGENTE_HORAS = 4;

  // ── Método principal ─────────────────────────────────────────────

  getItems(): Observable<BandejaItem[]> {
    const rol = this.authService.getUserRole();

    const mappers: Record<string, () => Observable<BandejaItem[]>> = {
      // Roles con bandeja real
      'EnfermeriaTecnica': () => this.getParaEnfermeria(),
      'EnfermeriaLicenciada': () => this.getParaEnfermeria(),
      'SupervisoraEnfermeria': () => this.getParaEnfermeria(),
      'Ambulancia': () => this.getParaAmbulancia(),
      'Administrador': () => this.getParaEnfermeria(),

      // TODO: VigilanteSupervisor y JefeGuardia tendrán módulos propios.
      // De momento retornan lista vacía para no mostrar datos mock.
      'VigilanteSupervisor': () => of([]),
      'JefeGuardia': () => of([]),
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
   * ENFERMERÍA / ADMINISTRADOR
   * Pacientes fallecidos pendientes de generar expediente mortuorio.
   * Fuente: IntegracionService.getPendientes() → SIGEM/Galenhos mock.
   * Urgente: más de 4 horas sin expediente generado.
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
            esUrgente: horasTranscurridas > this.UMBRAL_URGENTE_HORAS
          } as BandejaItem;
        })
      ),
      catchError(err => {
        console.error('[BandejaUniversal] Error enfermería:', err);
        return of([]);
      })
    );
  }

  /**
   * AMBULANCIA
   * Expedientes pendientes de recojo (EnPiso / PendienteDeRecojo).
   * TODO: cuando se defina el flujo sin hardware móvil, revisar si
   * la aceptación de custodia se confirma desde PC mortuorio al llegar.
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
            esUrgente: horasTranscurridas > this.UMBRAL_URGENTE_HORAS
          } as BandejaItem;
        })
      ),
      catchError(err => {
        console.error('[BandejaUniversal] Error ambulancia:', err);
        return of([]);
      })
    );
  }

  // ── Helper ───────────────────────────────────────────────────────

  /** Convierte TipoDocumentoIdentidad (int) a etiqueta legible */
  private resolverTipoDocumento(id?: number): string | undefined {
    const mapa: Record<number, string> = {
      1: 'DNI', 2: 'CE', 3: 'Pasaporte', 4: 'RUC'
    };
    return id != null ? (mapa[id] ?? 'Doc') : undefined;
  }
}
