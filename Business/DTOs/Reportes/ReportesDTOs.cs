using SisMortuorio.Business.DTOs.Bandeja;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Business.DTOs.Verificacion;

namespace SisMortuorio.Business.DTOs.Reportes
{
    // ═══════════════════════════════════════════════════════════
    // DASHBOARD — KPIs consolidados
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// KPIs consolidados para la pantalla principal de Reportes.
    /// Combina estadísticas de bandeja, salidas, verificaciones y deudas.
    /// Roles: VigilanteSupervisor, JefeGuardia, Administrador.
    /// </summary>
    public class DashboardReportesDTO
    {
        /// <summary>Período consultado.</summary>
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public DateTime GeneradoEn { get; set; } = DateTime.Now;

        // Ocupación actual del mortuorio
        public EstadisticasBandejaDTO Bandeja { get; set; } = new();
        // Salidas en el período
        public EstadisticasSalidaDTO Salidas { get; set; } = new();
        // Ingresos verificados en el período
        public EstadisticasVerificacionDTO Verificaciones { get; set; } = new();
        // Deudas activas (solo conteos — sin montos para VigSup)
        public DeudaStatsReportesDTO Deudas { get; set; } = new();
    }

    /// <summary>
    /// Estadísticas de deudas sin montos.
    /// VigSup solo ve conteos. Admin y JG ven montos en el endpoint /deudas.
    /// </summary>
    public class DeudaStatsReportesDTO
    {
        public int SangrePendientes { get; set; }
        public int SangreLiquidadas { get; set; }
        public int SangreAnuladas { get; set; }
        public int EconomicasPendientes { get; set; }
        public int EconomicasLiquidadas { get; set; }
        public int EconomicasExoneradas { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // CUADERNO DE PERMANENCIA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Una fila del cuaderno digital de permanencia.
    /// Digitaliza el cuaderno físico del Supervisor de Vigilancia.
    /// Fuente: BandejaHistorial + Expediente + ActaRetiro.
    /// Roles: VigilanteSupervisor, JefeGuardia, Administrador.
    /// </summary>
    public class PermanenciaItemDTO
    {
        public int HistorialID { get; set; }
        public string CodigoBandeja { get; set; } = string.Empty;

        // Expediente
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public string TipoExpediente { get; set; } = string.Empty;

        // ── Campos del cuaderno físico (antes faltaban) ───────────────
        /// <summary>Expediente.DiagnosticoFinal</summary>
        public string DiagnosticoFinal { get; set; } = "—";

        /// <summary>
        /// Parentesco del familiar (ej: "Hijo") o cargo de la autoridad (ej: "Teniente PNP").
        /// Fuente: ActaRetiro.FamiliarParentesco | ActaRetiro.AutoridadCargo
        /// </summary>
        public string ResponsableRetiro { get; set; } = "—";

        /// <summary>
        /// Destino del cuerpo. Fuente: ActaRetiro.Destino
        /// </summary>
        public string Destino { get; set; } = "—";

        /// <summary>
        /// Médico JG o certificante. Fuente: ActaRetiro.JefeGuardiaNombre | MedicoCertificaNombre
        /// </summary>
        public string ObservacionesMedico { get; set; } = "—";

        // ── Tiempos ───────────────────────────────────────────────────
        public DateTime FechaHoraIngreso { get; set; }
        public DateTime? FechaHoraSalida { get; set; }

        /// <summary>Formato: "Xd Yh Zm". Si activo → tiempo desde ingreso hasta ahora.</summary>
        public string TiempoLegible { get; set; } = string.Empty;
        public int TiempoMinutos { get; set; }

        /// <summary>true si el cuerpo aún está en el mortuorio.</summary>
        public bool EstaActivo { get; set; }

        /// <summary>true si superó las 48h de permanencia.</summary>
        public bool ExcedioLimite { get; set; }

        /// <summary>Días completos para ordenar y filtrar en frontend.</summary>
        public int DiasCompletos => TiempoMinutos / (60 * 24);

        public string? UsuarioAsignadorNombre { get; set; }
        public string? Observaciones { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // ACTAS DE RETIRO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// KPIs de actas de retiro para el reporte de Admisión.
    /// </summary>
    public class ActaEstadisticasDTO
    {
        public int Total { get; set; }
        public int TipoFamiliar { get; set; }
        public int TipoAutoridadLegal { get; set; }
        public int ConBypass { get; set; }
        public int ConMedicoExterno { get; set; }
        public int Firmadas { get; set; }  // EstadoActa = Firmada
        public int Borrador { get; set; }  // EstadoActa = Borrador
        public int SinPDFFirmado { get; set; }  // tieneActa pero sin PDF subido
    }

    /// <summary>
    /// Una fila de la tabla de actas en el reporte.
    /// Roles: Admision, JefeGuardia, Administrador.
    /// </summary>
    public class ActaReportesItemDTO
    {
        public int ActaRetiroID { get; set; }
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string TipoSalida { get; set; } = string.Empty; // Familiar | AutoridadLegal
        public string EstadoActa { get; set; } = string.Empty; // Borrador | Firmada | Anulada
        public bool TieneBypass { get; set; }
        public bool TieneMedicoExterno { get; set; }
        public bool TienePDFFirmado { get; set; }

        /// <summary>Nombre del familiar o autoridad responsable del retiro.</summary>
        public string? ResponsableNombre { get; set; }
        public string? ResponsableDoc { get; set; }

        /// <summary>Nombre del Jefe de Guardia que firmó.</summary>
        public string? JefeGuardiaNombre { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // DEUDAS CONSOLIDADAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Estadísticas consolidadas de deudas para el reporte.
    /// Montos solo visibles para Admin y JefeGuardia.
    /// VigSup accede vía DashboardReportesDTO (sin montos).
    /// Roles: Admin, JefeGuardia.
    /// </summary>
    public class DeudaConsolidadaDTO
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Deuda Sangre
        public int SangrePendientes { get; set; }
        public int SangreLiquidadas { get; set; }
        public int SangreAnuladas { get; set; }
        public int SangreSinDeuda { get; set; }

        // Deuda Económica — con montos
        public int EconomicasPendientes { get; set; }
        public int EconomicasLiquidadas { get; set; }
        public int EconomicasExoneradas { get; set; }
        public int EconomicasSinDeuda { get; set; }
        public decimal MontoTotalDeudas { get; set; }
        public decimal MontoTotalPendiente { get; set; }
        public decimal MontoTotalPagado { get; set; }
        public decimal MontoTotalExonerado { get; set; }
        public double PromedioExoneracion { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // EXPEDIENTES POR SERVICIO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Una fila del reporte de expedientes por servicio.
    /// SupervisoraEnfermeria: todos los servicios.
    /// EnfermeriaLicenciada: solo su servicio (claim JWT).
    /// </summary>
    public class ExpedienteServicioItemDTO
    {
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public string EstadoActual { get; set; } = string.Empty;
        public DateTime FechaHoraFallecimiento { get; set; }
        public DateTime FechaCreacion { get; set; }

        /// <summary>Bandeja asignada actualmente. null si no está en mortuorio.</summary>
        public string? CodigoBandeja { get; set; }

        /// <summary>Tiempo en mortuorio formateado. null si no ha ingresado.</summary>
        public string? TiempoEnMortuorio { get; set; }

        public bool TieneActa { get; set; }
        public bool DocumentacionCompleta { get; set; }
        public string UsuarioCreadorNombre { get; set; } = string.Empty;
    }
}