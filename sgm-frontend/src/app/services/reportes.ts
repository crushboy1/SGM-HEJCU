import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';


// ===================================================================
// INTERFACES — DTOs del backend
// ===================================================================

export interface RangoFecha {
  label: string;
  key: string;
  fechaInicio: Date;
  fechaFin: Date;
}

export interface DashboardReportesDTO {
  fechaInicio: Date;
  fechaFin: Date;
  generadoEn: Date;
  bandeja: EstadisticasBandejaReporte;
  salidas: EstadisticasSalida;
  verificaciones: EstadisticasVerificacion;
  deudas: DeudaStatsReporte;
}

export interface EstadisticasBandejaReporte {
  total: number;
  disponibles: number;
  ocupadas: number;
  enMantenimiento: number;
  fueraDeServicio: number;
  porcentajeOcupacion: number;
  conAlerta24h: number;
  conAlerta48h: number;
}

export interface EstadisticasSalida {
  totalSalidas: number;
  salidasFamiliar: number;
  salidasAutoridadLegal: number;
  conIncidentes: number;
  conFuneraria: number;
  porcentajeIncidentes: number;
}

export interface EstadisticasVerificacion {
  totalVerificaciones: number;
  aprobadas: number;
  rechazadas: number;
  porcentajeAprobacion: number;
  conDiscrepanciaHC: number;
  conDiscrepanciaDocumento: number;
  conDiscrepanciaNombre: number;
}

export interface DeudaStatsReporte {
  sangrePendientes: number;
  sangreLiquidadas: number;
  sangreAnuladas: number;
  economicasPendientes: number;
  economicasLiquidadas: number;
  economicasExoneradas: number;
}

export interface PermanenciaItemDTO {
  historialID: number;
  codigoBandeja: string;
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  hc: string;
  servicio: string;
  tipoExpediente: string;
  fechaHoraIngreso: Date;
  fechaHoraSalida?: Date;
  tiempoLegible: string;
  tiempoMinutos: number;
  estaActivo: boolean;
  excedioLimite: boolean;
  diasCompletos: number;
  usuarioAsignadorNombre?: string;
  observaciones?: string;
  diagnosticoFinal?: string;
  responsableRetiro?: string;
  destino?: string;
  observacionesMedico?: string;
}

export interface SalidaItemDTO {
  salidaID: number;
  expedienteID: number;
  codigoExpediente: string;
  nombrePaciente: string;
  fechaHoraSalida: Date;
  tipoSalida: string;
  responsableNombre: string;
  nombreFuneraria?: string;
  tiempoPermanenciaLegible?: string;
  tiempoPermanenciaMinutos?: number;
  excedioLimite: boolean;
  incidenteRegistrado: boolean;
  detalleIncidente?: string;
  placa?: string;
}

export interface SalidasReporteResponse {
  estadisticas: EstadisticasSalida;
  total: number;
  salidas: SalidaItemDTO[];
}

export interface ActaEstadisticasDTO {
  total: number;
  tipoFamiliar: number;
  tipoAutoridadLegal: number;
  conBypass: number;
  conMedicoExterno: number;
  firmadas: number;
  borrador: number;
  sinPDFFirmado: number;
}

export interface ActaReportesItemDTO {
  actaRetiroID: number;
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  hc: string;
  servicio: string;
  fechaRegistro: Date;
  tipoSalida: string;
  estadoActa: string;
  tieneBypass: boolean;
  tieneMedicoExterno: boolean;
  tienePDFFirmado: boolean;
  responsableNombre?: string;
  responsableDoc?: string;
  jefeGuardiaNombre?: string;
}

export interface ActasReporteResponse {
  estadisticas: ActaEstadisticasDTO;
  total: number;
  actas: ActaReportesItemDTO[];
}

export interface DeudaConsolidadaDTO {
  fechaInicio: Date;
  fechaFin: Date;
  sangrePendientes: number;
  sangreLiquidadas: number;
  sangreAnuladas: number;
  sangreSinDeuda: number;
  economicasPendientes: number;
  economicasLiquidadas: number;
  economicasExoneradas: number;
  economicasSinDeuda: number;
  montoTotalDeudas: number;
  montoTotalPendiente: number;
  montoTotalPagado: number;
  montoTotalExonerado: number;
  promedioExoneracion: number;
}

export interface ExpedienteServicioItemDTO {
  expedienteID: number;
  codigoExpediente: string;
  nombreCompleto: string;
  hc: string;
  servicio: string;
  estadoActual: string;
  fechaHoraFallecimiento: Date;
  fechaCreacion: Date;
  codigoBandeja?: string;
  tiempoEnMortuorio?: string;
  tieneActa: boolean;
  documentacionCompleta: boolean;
  usuarioCreadorNombre: string;
}

export interface ExportarReporteDTO {
  fechaInicio: Date;
  fechaFin: Date;
  soloActivos?: boolean;
}

// ===================================================================
// SERVICE
// ===================================================================

@Injectable({ providedIn: 'root' })
export class ReportesService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7153/api';

  // ─── Rangos de fecha predefinidos ────────────────────────────────

  /**
   * Genera los rangos de fecha disponibles para el selector.
   * "Este mes" y "Mes anterior" calculan correctamente días variables.
   */
  getRangosFecha(): RangoFecha[] {
    const hoy = new Date();
    const h = hoy.getFullYear();
    const m = hoy.getMonth();
    const d = hoy.getDate();

    const inicio = (year: number, month: number, day: number): Date =>
      new Date(year, month, day, 0, 0, 0, 0);
    const fin = (year: number, month: number, day: number): Date =>
      new Date(year, month, day, 23, 59, 59, 999);

    const ultimoDiaMesActual = new Date(h, m + 1, 0).getDate();
    const ultimoDiaMesAnterior = new Date(h, m, 0).getDate();

    return [
      {
        key: 'hoy',
        label: 'Hoy',
        fechaInicio: inicio(h, m, d),
        fechaFin: fin(h, m, d),
      },
      {
        key: '3dias',
        label: '3 días',
        fechaInicio: inicio(h, m, d - 2),
        fechaFin: fin(h, m, d),
      },
      {
        key: '7dias',
        label: '7 días',
        fechaInicio: inicio(h, m, d - 6),
        fechaFin: fin(h, m, d),
      },
      {
        key: '15dias',
        label: '15 días',
        fechaInicio: inicio(h, m, d - 14),
        fechaFin: fin(h, m, d),
      },
      {
        key: 'mes',
        label: 'Este mes',
        fechaInicio: inicio(h, m, 1),
        fechaFin: fin(h, m, ultimoDiaMesActual),
      },
      {
        key: 'mes_anterior',
        label: 'Mes anterior',
        fechaInicio: inicio(h, m - 1, 1),
        fechaFin: fin(h, m, 0),  // día 0 del mes actual = último día del mes anterior
      },
      {
        key: 'personalizado',
        label: 'Personalizado',
        fechaInicio: inicio(h, m, d - 29),
        fechaFin: fin(h, m, d),
      },
    ];
  }

  // ─── Endpoints GET ────────────────────────────────────────────────

  getDashboard(fi: Date, ff: Date): Observable<DashboardReportesDTO> {
    const params = this.buildParams(fi, ff);
    return this.http.get<DashboardReportesDTO>(`${this.apiUrl}/Reportes/dashboard`, { params });
  }

  getPermanencia(fi: Date, ff: Date, soloActivos = false): Observable<PermanenciaItemDTO[]> {
    const params = this.buildParams(fi, ff).set('soloActivos', soloActivos.toString());
    return this.http.get<PermanenciaItemDTO[]>(`${this.apiUrl}/Reportes/permanencia`, { params });
  }

  getSalidas(
    fi: Date,
    ff: Date,
    tipoSalida?: string,
    soloIncidentes = false,
    soloExcedieronLimite = false
  ): Observable<SalidasReporteResponse> {
    let params = this.buildParams(fi, ff)
      .set('soloIncidentes', soloIncidentes.toString())
      .set('soloExcedieronLimite', soloExcedieronLimite.toString());
    if (tipoSalida) params = params.set('tipoSalida', tipoSalida);
    return this.http.get<SalidasReporteResponse>(`${this.apiUrl}/Reportes/salidas`, { params });
  }

  getActas(
    fi: Date,
    ff: Date,
    tipoSalida?: string,
    conBypass = false
  ): Observable<ActasReporteResponse> {
    let params = this.buildParams(fi, ff).set('conBypass', conBypass.toString());
    if (tipoSalida) params = params.set('tipoSalida', tipoSalida);
    return this.http.get<ActasReporteResponse>(`${this.apiUrl}/Reportes/actas`, { params });
  }

  getDeudas(fi: Date, ff: Date): Observable<DeudaConsolidadaDTO> {
    const params = this.buildParams(fi, ff);
    return this.http.get<DeudaConsolidadaDTO>(`${this.apiUrl}/Reportes/deudas`, { params });
  }

  getExpedientesPorServicio(
    fi: Date,
    ff: Date,
    servicio?: string,
    estado?: string
  ): Observable<ExpedienteServicioItemDTO[]> {
    let params = this.buildParams(fi, ff);
    if (servicio) params = params.set('servicio', servicio);
    if (estado) params = params.set('estado', estado);
    return this.http.get<ExpedienteServicioItemDTO[]>(
      `${this.apiUrl}/Reportes/expedientes-servicio`, { params });
  }

  // ─── Endpoints POST — Exportar PDF ───────────────────────────────

  exportarPermanenciaPdf(dto: ExportarReporteDTO): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/Reportes/exportar/permanencia`, dto,
      { responseType: 'blob' });
  }

  exportarSalidasPdf(dto: ExportarReporteDTO): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/Reportes/exportar/salidas`, dto,
      { responseType: 'blob' });
  }

  exportarActasPdf(dto: ExportarReporteDTO): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/Reportes/exportar/actas`, dto,
      { responseType: 'blob' });
  }

  exportarDeudasPdf(dto: ExportarReporteDTO): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/Reportes/exportar/deudas`, dto,
      { responseType: 'blob' });
  }

  // ─── Exportar Excel (SheetJS — frontend) ─────────────────────────

  /**
   * Descarga un blob como archivo en el navegador.
   * Usado tanto para PDFs del backend como para Excel del frontend.
   */
  descargarArchivo(blob: Blob, nombreArchivo: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 100);
  }

  /**
   * Genera nombre de archivo con fechas del período.
   * Ej: "Permanencia_SGM_01-03-2026_31-03-2026.pdf"
   */
  generarNombreArchivo(tipo: string, fi: Date, ff: Date, ext: 'pdf' | 'xlsx'): string {
    const fmt = (d: Date) =>
      `${String(d.getDate()).padStart(2, '0')}-` +
      `${String(d.getMonth() + 1).padStart(2, '0')}-` +
      `${d.getFullYear()}`;
    return `${tipo}_SGM_${fmt(fi)}_${fmt(ff)}.${ext}`;
  }

  // ─── Helpers privados ─────────────────────────────────────────────

  private toLocalISOString(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T` +
      `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
  }

  private buildParams(fi: Date, ff: Date): HttpParams {
    return new HttpParams()
      .set('fechaInicio', this.toLocalISOString(fi))
      .set('fechaFin', this.toLocalISOString(ff));
  }
}
