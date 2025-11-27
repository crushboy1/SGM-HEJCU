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
 * NotificacionService v3.0
 * 
 * CHANGELOG v3.0:
 * - ✅ FIX CRÍTICO: Usar localStorage 'sgm_user' en lugar de decodificar token
 * - ✅ Prevención de colisión de datos entre usuarios
 * - ✅ Limpieza de keys huérfanas en localStorage
 * - ✅ Validación de username antes de crear storage key
 * - ✅ Logs mejorados para debugging
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

  // Subjects para eventos específicos
  private alertaOcupacion$ = new Subject<EstadisticasBandejaDTO>();
  private alertaPermanencia$ = new Subject<BandejaDTO[]>();
  private alertaSolicitudesVencidas$ = new Subject<SolicitudCorreccionDTO[]>();
  private actualizacionBandeja$ = new Subject<BandejaDTO>();
  private nuevoExpediente$ = new Subject<NotificacionDTO>();
  private expedienteActualizado$ = new Subject<NotificacionDTO>();
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
    // Limpieza inicial de keys huérfanas
    this.limpiarKeysHuerfanas();
  }

  // ===================================================================
  // GESTIÓN DE CONEXIÓN SIGNALR
  // ===================================================================

  async iniciarConexion(): Promise<void> {
    const token = localStorage.getItem('sgm_token');
    if (!token) {
      console.error('❌ NotificacionService: No se encontró token JWT');
      return;
    }

    // ⭐ FIX CRÍTICO: Usar directamente 'sgm_user' guardado por AuthService
    const username = this.obtenerUsernameSeguro();
    if (!username) {
      console.error('❌ NotificacionService: No se pudo obtener username del usuario');
      return;
    }

    // Configurar clave de storage única por usuario
    this.currentStorageKey = `${this.STORAGE_BASE_KEY}${username}`;
    console.log(`✅ Storage key configurada: ${this.currentStorageKey}`);

    // Cargar notificaciones del usuario desde localStorage
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
        console.warn('⚠️ SignalR: Reconectando...', error);
        this.conexionEstablecida$.next(false);
      });

      this.hubConnection.onreconnected((connectionId) => {
        console.log('✅ SignalR: Reconexión exitosa:', connectionId);
        this.conexionEstablecida$.next(true);
      });

      this.hubConnection.onclose((error) => {
        console.error('❌ SignalR: Conexión cerrada', error);
        this.conexionEstablecida$.next(false);
      });

      await this.hubConnection.start();
      console.log('✅ SignalR: Conexión establecida');
      this.conexionEstablecida$.next(true);

      // Ping inicial para verificar latencia
      await this.ping();

    } catch (error) {
      console.error('❌ SignalR: Error al iniciar conexión:', error);
      this.conexionEstablecida$.next(false);
    }
  }

  async detenerConexion(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        console.log('✅ SignalR: Conexión detenida correctamente');
        this.conexionEstablecida$.next(false);
      } catch (error) {
        console.error('❌ SignalR: Error al detener conexión:', error);
      }
    }
  }

  // ===================================================================
  // CONFIGURACIÓN DE MANEJADORES SIGNALR
  // ===================================================================

  private configurarManejadoresDeEventos(): void {
    if (!this.hubConnection) return;

    // Evento: RecibirNotificacion
    this.hubConnection.on('RecibirNotificacion', (notificacion: NotificacionDTO) => {
      console.log('📬 SignalR: Notificación recibida:', notificacion);

      // Convertir fechas de string a Date
      notificacion.fechaHora = new Date(notificacion.fechaHora);
      if (notificacion.fechaExpiracion) {
        notificacion.fechaExpiracion = new Date(notificacion.fechaExpiracion);
      }
      notificacion.leida = false;

      // Agregar al inicio del array (las más recientes primero)
      const notificacionesActuales = this.notificaciones$.value;
      const notificacionesActualizadas = [notificacion, ...notificacionesActuales];

      // Limitar a MAX_NOTIFICACIONES
      if (notificacionesActualizadas.length > this.MAX_NOTIFICACIONES) {
        notificacionesActualizadas.pop();
      }

      this.notificaciones$.next(notificacionesActualizadas);
      this.actualizarContadorNoLeidas();
      this.guardarNotificacionesEnStorage();

      // Emitir en observable genérico
      this.notificacionGenerica$.next(notificacion);

      // Clasificar automáticamente
      this.clasificarNotificacion(notificacion);

      // Mostrar notificación de navegador si la pestaña está oculta
      this.mostrarNotificacionNavegador(notificacion);
    });

    // Evento: RecibirAlertaOcupacion
    this.hubConnection.on('RecibirAlertaOcupacion', (estadisticas: EstadisticasBandejaDTO) => {
      console.log('⚠️ SignalR: Alerta ocupación:', estadisticas);
      this.alertaOcupacion$.next(estadisticas);
    });

    // Evento: RecibirAlertaPermanencia
    this.hubConnection.on('RecibirAlertaPermanencia', (bandejas: BandejaDTO[]) => {
      console.log('⏱️ SignalR: Alerta permanencia:', bandejas);
      bandejas.forEach(b => {
        if (b.fechaHoraAsignacion) {
          b.fechaHoraAsignacion = new Date(b.fechaHoraAsignacion);
        }
      });
      this.alertaPermanencia$.next(bandejas);
    });

    // Evento: RecibirAlertaSolicitudesVencidas
    this.hubConnection.on('RecibirAlertaSolicitudesVencidas', (solicitudes: SolicitudCorreccionDTO[]) => {
      console.log('📋 SignalR: Alerta solicitudes vencidas:', solicitudes);
      solicitudes.forEach(s => {
        s.fechaHoraSolicitud = new Date(s.fechaHoraSolicitud);
        if (s.fechaHoraResolucion) {
          s.fechaHoraResolucion = new Date(s.fechaHoraResolucion);
        }
      });
      this.alertaSolicitudesVencidas$.next(solicitudes);
    });

    // Evento: RecibirActualizacionBandeja
    this.hubConnection.on('RecibirActualizacionBandeja', (bandeja: BandejaDTO) => {
      console.log('🔄 SignalR: Actualización bandeja:', bandeja);
      if (bandeja.fechaHoraAsignacion) {
        bandeja.fechaHoraAsignacion = new Date(bandeja.fechaHoraAsignacion);
      }
      this.actualizacionBandeja$.next(bandeja);
    });

    // Evento: RecibirConfirmacionAccion
    this.hubConnection.on('RecibirConfirmacionAccion',
      (accion: string, exito: boolean, mensaje: string) => {
        console.log('✅ SignalR: Confirmación:', { accion, exito, mensaje });
        this.confirmacionAccion$.next({ accion, exito, mensaje });
      }
    );
  }

  // ===================================================================
  // CLASIFICACIÓN AUTOMÁTICA DE NOTIFICACIONES
  // ===================================================================

  private clasificarNotificacion(notificacion: NotificacionDTO): void {
    const titulo = notificacion.titulo.toLowerCase();
    const mensaje = notificacion.mensaje.toLowerCase();

    // Clasificar: Nuevo Expediente
    if (titulo.includes('nuevo expediente') || mensaje.includes('expediente creado')) {
      console.log('📋 Clasificado: Nuevo Expediente');
      this.nuevoExpediente$.next(notificacion);
    }

    // Clasificar: Expediente Actualizado
    if (titulo.includes('expediente actualizado') ||
      titulo.includes('cambio de estado') ||
      mensaje.includes('estado ha cambiado')) {
      console.log('🔄 Clasificado: Expediente Actualizado');
      this.expedienteActualizado$.next(notificacion);
    }
  }

  // ===================================================================
  // UTILIDADES SIGNALR
  // ===================================================================

  async ping(): Promise<number | null> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return null;
    }
    try {
      const clientTime = Date.now();
      const serverTime = await this.hubConnection.invoke<number>('Ping');
      const latency = Date.now() - clientTime;
      console.log(`🏓 SignalR Ping: ${latency}ms`);
      return latency;
    } catch (error) {
      console.error('❌ SignalR: Error en Ping:', error);
      return null;
    }
  }

  async obtenerInfoConexion(): Promise<any> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return null;
    }
    try {
      const info = await this.hubConnection.invoke('GetConnectionInfo');
      console.log('ℹ️ SignalR: Info conexión:', info);
      return info;
    } catch (error) {
      console.error('❌ SignalR: Error obtener info:', error);
      return null;
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

  get onNuevoExpediente(): Observable<NotificacionDTO> {
    return this.nuevoExpediente$.asObservable();
  }

  get onExpedienteActualizado(): Observable<NotificacionDTO> {
    return this.expedienteActualizado$.asObservable();
  }

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
      console.log(`✅ Notificación marcada como leída: ${notificacionId}`);
    }
  }

  marcarTodasComoLeidas(): void {
    const notificaciones = this.notificaciones$.value;
    notificaciones.forEach(n => n.leida = true);
    this.notificaciones$.next([...notificaciones]);
    this.actualizarContadorNoLeidas();
    this.guardarNotificacionesEnStorage();
    console.log('✅ Todas las notificaciones marcadas como leídas');
  }

  eliminarNotificacion(notificacionId: string): void {
    const notificaciones = this.notificaciones$.value.filter(n => n.id !== notificacionId);
    this.notificaciones$.next(notificaciones);
    this.actualizarContadorNoLeidas();
    this.guardarNotificacionesEnStorage();
    console.log(`🗑️ Notificación eliminada: ${notificacionId}`);
  }

  limpiarTodas(): void {
    this.notificaciones$.next([]);
    this.contadorNoLeidas$.next(0);
    if (this.currentStorageKey) {
      localStorage.removeItem(this.currentStorageKey);
      console.log('🧹 Todas las notificaciones limpiadas');
    }
  }

  // ===================================================================
  // GESTIÓN DE LOCALSTORAGE (CORREGIDO)
  // ===================================================================

  /**
   * ⭐ FIX CRÍTICO: Obtener username de forma segura desde localStorage
   * Ya no decodifica el token JWT manualmente (propenso a errores)
   * Usa directamente el valor guardado por AuthService en 'sgm_user'
   */
  private obtenerUsernameSeguro(): string {
    // Intentar obtener desde 'sgm_user' (guardado por AuthService)
    const username = localStorage.getItem('sgm_user');

    if (username && username.trim() !== '') {
      console.log(`✅ Username obtenido de localStorage: ${username}`);
      return username;
    }

    // Fallback: si no existe, usar 'anonimo' temporalmente
    console.warn('⚠️ No se encontró username en localStorage (sgm_user)');
    return 'anonimo';
  }

  /**
   * Limpia keys de notificaciones huérfanas en localStorage
   * (de sesiones anteriores o usuarios eliminados)
   */
  private limpiarKeysHuerfanas(): void {
    try {
      const todasLasKeys = Object.keys(localStorage);
      const keysNotificaciones = todasLasKeys.filter(k => k.startsWith(this.STORAGE_BASE_KEY));

      if (keysNotificaciones.length > 5) {
        console.warn(`⚠️ Se encontraron ${keysNotificaciones.length} keys de notificaciones. Limpiando...`);

        // Mantener solo las 3 más recientes (en caso de múltiples usuarios en el mismo navegador)
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
              return maxDateB - maxDateA; // Más reciente primero
            } catch {
              return 0;
            }
          })
          .slice(3) // Eliminar todo excepto las 3 primeras (más recientes)
          .forEach(key => {
            localStorage.removeItem(key);
            console.log(`🗑️ Key huérfana eliminada: ${key}`);
          });
      }
    } catch (error) {
      console.error('❌ Error al limpiar keys huérfanas:', error);
    }
  }

  private actualizarContadorNoLeidas(): void {
    const count = this.notificaciones$.value.filter(n => !n.leida).length;
    this.contadorNoLeidas$.next(count);
  }

  private guardarNotificacionesEnStorage(): void {
    if (!this.currentStorageKey) {
      console.warn('⚠️ No se puede guardar: currentStorageKey no está configurada');
      return;
    }

    try {
      const data = JSON.stringify(this.notificaciones$.value);
      localStorage.setItem(this.currentStorageKey, data);
      console.log(`💾 Notificaciones guardadas en: ${this.currentStorageKey}`);
    } catch (error) {
      console.error('❌ Error al guardar notificaciones:', error);

      // Si el error es por cuota excedida, limpiar keys antiguas
      if (error instanceof DOMException && error.name === 'QuotaExceededError') {
        console.warn('⚠️ Cuota de localStorage excedida. Limpiando...');
        this.limpiarKeysHuerfanas();
      }
    }
  }

  private cargarNotificacionesDesdeStorage(): void {
    if (!this.currentStorageKey) {
      console.warn('⚠️ No se puede cargar: currentStorageKey no está configurada');
      return;
    }

    try {
      const stored = localStorage.getItem(this.currentStorageKey);

      if (stored) {
        const parsed: NotificacionDTO[] = JSON.parse(stored);

        // Revivir fechas (convertir strings a Date)
        parsed.forEach(n => {
          n.fechaHora = new Date(n.fechaHora);
          if (n.fechaExpiracion) {
            n.fechaExpiracion = new Date(n.fechaExpiracion);
          }
        });

        // Filtrar notificaciones expiradas
        const validas = parsed.filter(n => {
          if (!n.fechaExpiracion) return true;
          return n.fechaExpiracion > new Date();
        });

        this.notificaciones$.next(validas);
        this.actualizarContadorNoLeidas();
        console.log(`✅ Cargadas ${validas.length} notificaciones desde: ${this.currentStorageKey}`);

      } else {
        // Usuario nuevo en este navegador, empezar con array vacío
        this.notificaciones$.next([]);
        this.contadorNoLeidas$.next(0);
        console.log('ℹ️ No hay notificaciones previas para este usuario');
      }
    } catch (error) {
      console.error('❌ Error al cargar notificaciones:', error);
      // En caso de error, empezar limpio
      this.notificaciones$.next([]);
      this.contadorNoLeidas$.next(0);
    }
  }

  // ===================================================================
  // NOTIFICACIONES DE NAVEGADOR
  // ===================================================================

  private async mostrarNotificacionNavegador(notificacion: NotificacionDTO): Promise<void> {
    // Solo mostrar si la pestaña está oculta
    if (!document.hidden) return;

    if ('Notification' in window) {
      // Solicitar permiso si no se ha hecho antes
      if (Notification.permission === 'default') {
        await Notification.requestPermission();
      }

      // Mostrar notificación si se tiene permiso
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
