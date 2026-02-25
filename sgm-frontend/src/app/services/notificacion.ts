import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import {
  NotificacionDTO,
  EstadisticasBandejaDTO,
  BandejaDTO,
  SolicitudCorreccionDTO
} from '../models/notificacion.model';

/**
 * NotificacionService v5.0
 *
 * CHANGELOG v5.0:
 * - Agregados eventos de módulo Salidas:
 *   onExpedienteListoParaRetiro → categoría "expediente_listo_para_retiro"
 *   onSalidaRegistrada          → categoría "salida_mortuorio"
 *
 * CHANGELOG v4.0:
 * - Integración completa con backend Fase Administrativa
 * - Eventos Deudas: DeudaCreada, DeudaResuelta, DesbloqueoTotal, DesbloqueoParcial
 * - Helper agregarNotificacion() para evitar código duplicado
 * - Compatibilidad con NotificacionDeudaService y NotificacionBandejaService
 *
 * CHANGELOG v3.0:
 * - FIX CRÍTICO: Usar localStorage 'sgm_user' en lugar de decodificar token
 * - Prevención de colisión de datos entre usuarios
 * - Limpieza de keys huérfanas en localStorage
 */
@Injectable({
  providedIn: 'root'
})
export class NotificacionService {
  private http = inject(HttpClient);
  private readonly hubUrl = 'https://localhost:7153/sgmhub';
  private hubConnection?: signalR.HubConnection;

  // BehaviorSubjects
  private conexionEstablecida$ = new BehaviorSubject<boolean>(false);
  private notificaciones$ = new BehaviorSubject<NotificacionDTO[]>([]);
  private contadorNoLeidas$ = new BehaviorSubject<number>(0);

  // Subjects para eventos específicos - Bandejas
  private alertaOcupacion$ = new Subject<EstadisticasBandejaDTO>();
  private alertaPermanencia$ = new Subject<BandejaDTO[]>();
  private alertaSolicitudesVencidas$ = new Subject<SolicitudCorreccionDTO[]>();
  private actualizacionBandeja$ = new Subject<BandejaDTO>();

  // Subjects para eventos específicos - Expedientes
  private nuevoExpediente$ = new Subject<NotificacionDTO>();
  private expedienteActualizado$ = new Subject<NotificacionDTO>();

  // Subjects para eventos específicos - Deudas
  private notificacionDeudaCreada$ = new Subject<NotificacionDTO>();
  private notificacionDeudaResuelta$ = new Subject<NotificacionDTO>();
  private notificacionDesbloqueoTotal$ = new Subject<NotificacionDTO>();
  private notificacionDesbloqueoParcial$ = new Subject<NotificacionDTO>();

  // Subjects para eventos específicos - Salidas ⭐ NUEVO v5.0
  private expedienteListoParaRetiro$ = new Subject<NotificacionDTO>();
  private salidaRegistrada$ = new Subject<NotificacionDTO>();

  // Subjects genéricos
  private notificacionGenerica$ = new Subject<NotificacionDTO>();
  private confirmacionAccion$ = new Subject<{
    accion: string;
    exito: boolean;
    mensaje: string;
  }>();

  // Configuración de localStorage
  private readonly STORAGE_BASE_KEY = 'sgm_notificaciones_';
  private currentStorageKey = '';
  private readonly MAX_NOTIFICACIONES = 20;

  constructor() {
    this.limpiarKeysHuerfanas();
  }

  // ===================================================================
  // GESTIÓN DE CONEXIÓN SIGNALR
  // ===================================================================

  async iniciarConexion(): Promise<void> {
    const token = localStorage.getItem('sgm_token');
    if (!token) {
      console.error('[NotificacionService] No se encontró token JWT');
      return;
    }

    const username = this.obtenerUsernameSeguro();
    if (!username) {
      console.error('[NotificacionService] No se pudo obtener username del usuario');
      return;
    }

    this.currentStorageKey = `${this.STORAGE_BASE_KEY}${username}`;
    console.log(`[NotificacionService] Storage key configurada: ${this.currentStorageKey}`);

    this.cargarNotificacionesDesdeStorage();

    try {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: () => token,
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount === 0) return 0;
            if (retryContext.previousRetryCount < 3) return 2000;
            if (retryContext.previousRetryCount < 5) return 10000;
            if (retryContext.previousRetryCount < 10) return 30000;
            return 60000;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.configurarManejadoresDeEventos();

      this.hubConnection.onreconnecting((error) => {
        console.warn('[SignalR] Reconectando...', error);
        this.conexionEstablecida$.next(false);
      });

      this.hubConnection.onreconnected((connectionId) => {
        console.log('[SignalR] Reconexión exitosa:', connectionId);
        this.conexionEstablecida$.next(true);
      });

      this.hubConnection.onclose((error) => {
        console.error('[SignalR] Conexión cerrada', error);
        this.conexionEstablecida$.next(false);
      });

      await this.hubConnection.start();
      console.log('[SignalR] Conexión establecida');
      this.conexionEstablecida$.next(true);

      await this.ping();

    } catch (error) {
      console.error('[SignalR] Error al iniciar conexión:', error);
      this.conexionEstablecida$.next(false);
    }
  }

  async detenerConexion(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        console.log('[SignalR] Conexión detenida correctamente');
        this.conexionEstablecida$.next(false);
      } catch (error) {
        console.error('[SignalR] Error al detener conexión:', error);
      }
    }
  }

  // ===================================================================
  // CONFIGURACIÓN DE MANEJADORES SIGNALR
  // ===================================================================

  private configurarManejadoresDeEventos(): void {
    if (!this.hubConnection) return;

    // ========== EVENTO GENÉRICO ==========
    this.hubConnection.on('RecibirNotificacion', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Notificación recibida:', notificacion.titulo);
      this.agregarNotificacion(notificacion);
      this.clasificarNotificacion(notificacion);
    });

    // ========== EVENTOS BANDEJAS ==========
    this.hubConnection.on('RecibirAlertaOcupacion', (estadisticas: EstadisticasBandejaDTO) => {
      console.log('[SignalR] Alerta ocupación:', estadisticas.porcentajeOcupacion + '%');
      this.alertaOcupacion$.next(estadisticas);
    });

    this.hubConnection.on('RecibirAlertaPermanencia', (bandejas: BandejaDTO[]) => {
      console.log('[SignalR] Alerta permanencia:', bandejas.length + ' bandejas');
      bandejas.forEach(b => {
        if (b.fechaHoraAsignacion) b.fechaHoraAsignacion = new Date(b.fechaHoraAsignacion);
      });
      this.alertaPermanencia$.next(bandejas);
    });

    this.hubConnection.on('RecibirAlertaSolicitudesVencidas', (solicitudes: SolicitudCorreccionDTO[]) => {
      console.log('[SignalR] Alerta solicitudes vencidas:', solicitudes.length);
      solicitudes.forEach(s => {
        s.fechaHoraSolicitud = new Date(s.fechaHoraSolicitud);
        if (s.fechaHoraResolucion) s.fechaHoraResolucion = new Date(s.fechaHoraResolucion);
      });
      this.alertaSolicitudesVencidas$.next(solicitudes);
    });

    this.hubConnection.on('RecibirActualizacionBandeja', (bandeja: BandejaDTO) => {
      console.log('[SignalR] Actualización bandeja:', bandeja.codigo);
      if (bandeja.fechaHoraAsignacion) bandeja.fechaHoraAsignacion = new Date(bandeja.fechaHoraAsignacion);
      if (bandeja.fechaHoraLiberacion) bandeja.fechaHoraLiberacion = new Date(bandeja.fechaHoraLiberacion);
      this.actualizacionBandeja$.next(bandeja);
    });

    // ========== EVENTOS EXPEDIENTES ==========
    this.hubConnection.on('RecibirNuevoExpediente', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Nuevo expediente:', notificacion.mensaje);
      this.agregarNotificacion(notificacion);
      this.nuevoExpediente$.next(notificacion);
    });

    this.hubConnection.on('RecibirExpedienteActualizado', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Expediente actualizado:', notificacion.mensaje);
      this.agregarNotificacion(notificacion);
      this.expedienteActualizado$.next(notificacion);
    });

    // ========== EVENTOS DEUDAS ==========
    this.hubConnection.on('RecibirNotificacionDeudaCreada', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Deuda creada:', notificacion.categoriaNotificacion);
      this.agregarNotificacion(notificacion);
      this.notificacionDeudaCreada$.next(notificacion);
    });

    this.hubConnection.on('RecibirNotificacionDeudaResuelta', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Deuda resuelta:', notificacion.categoriaNotificacion);
      this.agregarNotificacion(notificacion);
      this.notificacionDeudaResuelta$.next(notificacion);
    });

    this.hubConnection.on('RecibirNotificacionDesbloqueoTotal', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Desbloqueo total:', notificacion.mensaje);
      this.agregarNotificacion(notificacion);
      this.notificacionDesbloqueoTotal$.next(notificacion);
    });

    this.hubConnection.on('RecibirNotificacionDesbloqueoParcial', (notificacion: NotificacionDTO) => {
      console.log('[SignalR] Desbloqueo parcial:', notificacion.mensaje);
      this.agregarNotificacion(notificacion);
      this.notificacionDesbloqueoParcial$.next(notificacion);
    });

    // ========== EVENTO CONFIRMACIÓN ACCIÓN ==========
    this.hubConnection.on('RecibirConfirmacionAccion', (data: { accion: string; exito: boolean; mensaje: string }) => {
      console.log('[SignalR] Confirmación acción:', data.accion, data.exito ? 'EXITO' : 'FALLO');
      this.confirmacionAccion$.next(data);
    });
  }

  // ===================================================================
  // HELPER: AGREGAR NOTIFICACIÓN
  // ===================================================================

  private agregarNotificacion(notificacion: NotificacionDTO): void {
    notificacion.fechaHora = new Date(notificacion.fechaHora);
    if (notificacion.fechaExpiracion) {
      notificacion.fechaExpiracion = new Date(notificacion.fechaExpiracion);
    }
    notificacion.leida = false;

    const notificacionesActuales = this.notificaciones$.value;
    const notificacionesActualizadas = [notificacion, ...notificacionesActuales];

    if (notificacionesActualizadas.length > this.MAX_NOTIFICACIONES) {
      notificacionesActualizadas.pop();
    }

    this.notificaciones$.next(notificacionesActualizadas);
    this.actualizarContadorNoLeidas();
    this.guardarNotificacionesEnStorage();
    this.notificacionGenerica$.next(notificacion);
    this.mostrarNotificacionNavegador(notificacion);
  }

  // ===================================================================
  // CLASIFICACIÓN AUTOMÁTICA DE NOTIFICACIONES
  // ===================================================================

  private clasificarNotificacion(notificacion: NotificacionDTO): void {
    const categoria = notificacion.categoriaNotificacion?.toLowerCase() || '';

    if (categoria.includes('expediente_nuevo')) {
      this.nuevoExpediente$.next(notificacion);
    } else if (categoria.includes('expediente_actualizado')) {
      this.expedienteActualizado$.next(notificacion);
    } else if (categoria.includes('deuda_creada')) {
      this.notificacionDeudaCreada$.next(notificacion);
    } else if (categoria.includes('deuda_resuelta')) {
      this.notificacionDeudaResuelta$.next(notificacion);
    } else if (categoria.includes('desbloqueo_total')) {
      this.notificacionDesbloqueoTotal$.next(notificacion);
    } else if (categoria.includes('desbloqueo_parcial')) {
      this.notificacionDesbloqueoParcial$.next(notificacion);
    } else if (categoria.includes('expediente_listo_para_retiro')) {
      // ⭐ NUEVO v5.0 — Vigilancia recibe alerta de retiro autorizado
      this.expedienteListoParaRetiro$.next(notificacion);
    } else if (categoria.includes('salida_mortuorio')) {
      // ⭐ NUEVO v5.0 — Admisión/JefeGuardia reciben confirmación de salida
      this.salidaRegistrada$.next(notificacion);
    }
  }

  // ===================================================================
  // MÉTODOS DE SERVIDOR (INVOKE)
  // ===================================================================

  async ping(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.warn('[SignalR] No se puede hacer ping: conexión no establecida');
      return;
    }
    try {
      const inicio = Date.now();
      await this.hubConnection.invoke('Ping');
      const latencia = Date.now() - inicio;
      console.log(`[SignalR] Ping exitoso - Latencia: ${latencia}ms`);
    } catch (error) {
      console.error('[SignalR] Error en ping:', error);
    }
  }

  async solicitarEstadisticasBandejas(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.warn('[SignalR] No se pueden solicitar estadísticas: conexión no establecida');
      return;
    }
    try {
      await this.hubConnection.invoke('SolicitarEstadisticasBandejas');
    } catch (error) {
      console.error('[SignalR] Error al solicitar estadísticas:', error);
    }
  }

  // ===================================================================
  // OBSERVABLES PÚBLICOS
  // ===================================================================

  get conexionEstablecida(): Observable<boolean> {
    return this.conexionEstablecida$.asObservable();
  }

  get notificaciones(): Observable<NotificacionDTO[]> {
    return this.notificaciones$.asObservable();
  }

  get contadorNoLeidas(): Observable<number> {
    return this.contadorNoLeidas$.asObservable();
  }

  // Bandejas
  get onAlertaOcupacion(): Observable<EstadisticasBandejaDTO> {
    return this.alertaOcupacion$.asObservable();
  }

  get onAlertaPermanencia(): Observable<BandejaDTO[]> {
    return this.alertaPermanencia$.asObservable();
  }

  get onAlertaSolicitudesVencidas(): Observable<SolicitudCorreccionDTO[]> {
    return this.alertaSolicitudesVencidas$.asObservable();
  }

  get onActualizacionBandeja(): Observable<BandejaDTO> {
    return this.actualizacionBandeja$.asObservable();
  }

  // Expedientes
  get onNuevoExpediente(): Observable<NotificacionDTO> {
    return this.nuevoExpediente$.asObservable();
  }

  get onExpedienteActualizado(): Observable<NotificacionDTO> {
    return this.expedienteActualizado$.asObservable();
  }

  // Deudas
  get onNotificacionDeudaCreada(): Observable<NotificacionDTO> {
    return this.notificacionDeudaCreada$.asObservable();
  }

  get onNotificacionDeudaResuelta(): Observable<NotificacionDTO> {
    return this.notificacionDeudaResuelta$.asObservable();
  }

  get onNotificacionDesbloqueoTotal(): Observable<NotificacionDTO> {
    return this.notificacionDesbloqueoTotal$.asObservable();
  }

  get onNotificacionDesbloqueoParcial(): Observable<NotificacionDTO> {
    return this.notificacionDesbloqueoParcial$.asObservable();
  }

  // Salidas ⭐ NUEVO v5.0
  get onExpedienteListoParaRetiro(): Observable<NotificacionDTO> {
    return this.expedienteListoParaRetiro$.asObservable();
  }

  get onSalidaRegistrada(): Observable<NotificacionDTO> {
    return this.salidaRegistrada$.asObservable();
  }

  // Genéricos
  get onNotificacionGenerica(): Observable<NotificacionDTO> {
    return this.notificacionGenerica$.asObservable();
  }

  get onConfirmacionAccion(): Observable<{ accion: string; exito: boolean; mensaje: string }> {
    return this.confirmacionAccion$.asObservable();
  }

  // ===================================================================
  // GESTIÓN DE NOTIFICACIONES
  // ===================================================================

  marcarComoLeida(notificacionId: string): void {
    const notificaciones = this.notificaciones$.value;
    const notificacion = notificaciones.find(n => n.id === notificacionId);
    if (notificacion && !notificacion.leida) {
      notificacion.leida = true;
      this.notificaciones$.next([...notificaciones]);
      this.actualizarContadorNoLeidas();
      this.guardarNotificacionesEnStorage();
    }
  }

  marcarTodasComoLeidas(): void {
    const notificaciones = this.notificaciones$.value;
    notificaciones.forEach(n => n.leida = true);
    this.notificaciones$.next([...notificaciones]);
    this.actualizarContadorNoLeidas();
    this.guardarNotificacionesEnStorage();
  }

  eliminarNotificacion(notificacionId: string): void {
    const notificaciones = this.notificaciones$.value.filter(n => n.id !== notificacionId);
    this.notificaciones$.next(notificaciones);
    this.actualizarContadorNoLeidas();
    this.guardarNotificacionesEnStorage();
  }

  limpiarTodas(): void {
    this.notificaciones$.next([]);
    this.contadorNoLeidas$.next(0);
    if (this.currentStorageKey) {
      localStorage.removeItem(this.currentStorageKey);
    }
  }

  // ===================================================================
  // GESTIÓN DE LOCALSTORAGE
  // ===================================================================

  private obtenerUsernameSeguro(): string {
    const username = localStorage.getItem('sgm_user');
    if (username && username.trim() !== '') return username;
    console.warn('[NotificacionService] No se encontró username en localStorage (sgm_user)');
    return 'anonimo';
  }

  private limpiarKeysHuerfanas(): void {
    try {
      const todasLasKeys = Object.keys(localStorage);
      const keysNotificaciones = todasLasKeys.filter(k => k.startsWith(this.STORAGE_BASE_KEY));

      if (keysNotificaciones.length > 5) {
        keysNotificaciones
          .sort((a, b) => {
            const dataA = localStorage.getItem(a);
            const dataB = localStorage.getItem(b);
            if (!dataA || !dataB) return 0;
            try {
              const parsedA = JSON.parse(dataA);
              const parsedB = JSON.parse(dataB);
              const maxDateA = Math.max(...parsedA.map((n: any) => new Date(n.fechaHora).getTime()));
              const maxDateB = Math.max(...parsedB.map((n: any) => new Date(n.fechaHora).getTime()));
              return maxDateB - maxDateA;
            } catch { return 0; }
          })
          .slice(3)
          .forEach(key => localStorage.removeItem(key));
      }
    } catch (error) {
      console.error('[NotificacionService] Error al limpiar keys huérfanas:', error);
    }
  }

  private actualizarContadorNoLeidas(): void {
    const count = this.notificaciones$.value.filter(n => !n.leida).length;
    this.contadorNoLeidas$.next(count);
  }

  private guardarNotificacionesEnStorage(): void {
    if (!this.currentStorageKey) return;
    try {
      localStorage.setItem(this.currentStorageKey, JSON.stringify(this.notificaciones$.value));
    } catch (error) {
      console.error('[NotificacionService] Error al guardar notificaciones:', error);
      if (error instanceof DOMException && error.name === 'QuotaExceededError') {
        this.limpiarKeysHuerfanas();
      }
    }
  }

  private cargarNotificacionesDesdeStorage(): void {
    if (!this.currentStorageKey) return;
    try {
      const stored = localStorage.getItem(this.currentStorageKey);
      if (stored) {
        const parsed: NotificacionDTO[] = JSON.parse(stored);
        parsed.forEach(n => {
          n.fechaHora = new Date(n.fechaHora);
          if (n.fechaExpiracion) n.fechaExpiracion = new Date(n.fechaExpiracion);
        });
        const validas = parsed.filter(n => !n.fechaExpiracion || n.fechaExpiracion > new Date());
        this.notificaciones$.next(validas);
        this.actualizarContadorNoLeidas();
      } else {
        this.notificaciones$.next([]);
        this.contadorNoLeidas$.next(0);
      }
    } catch (error) {
      console.error('[NotificacionService] Error al cargar notificaciones:', error);
      this.notificaciones$.next([]);
      this.contadorNoLeidas$.next(0);
    }
  }

  // ===================================================================
  // NOTIFICACIONES DE NAVEGADOR
  // ===================================================================

  private async mostrarNotificacionNavegador(notificacion: NotificacionDTO): Promise<void> {
    if (!document.hidden) return;
    if ('Notification' in window) {
      if (Notification.permission === 'default') await Notification.requestPermission();
      if (Notification.permission === 'granted') {
        new Notification(notificacion.titulo, {
          body: notificacion.mensaje,
          icon: '/assets/logo-hejcu.png',
          badge: '/assets/badge-sgm.png',
          tag: notificacion.id,
          requireInteraction: notificacion.requiereAccion
        });
      }
    }
  }
}
