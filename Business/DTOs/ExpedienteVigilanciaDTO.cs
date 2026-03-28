namespace SisMortuorio.Business.DTOs.Vigilancia
{
    /// <summary>
    /// DTO tabla principal — Módulo Supervisor de Vigilancia.
    ///
    /// SEMÁFORO bool?:
    ///   null  = sin registro en SGM → verde "Sin deuda registrada"
    ///   true  = deuda activa que bloquea retiro → rojo
    ///   false = deuda resuelta / no bloquea → verde
    ///
    /// BYPASS: si BypassDeudaAutorizado=true, ambos semáforos vienen false
    /// con descripción "Bypass autorizado" independientemente del estado real.
    ///
    /// EXTERNOS: nunca generan deuda económica — null esperado en ese campo.
    /// </summary>
    public class ExpedienteVigilanciaDTO
    {
        // ── Identificación ───────────────────────────────────────────
        public int ExpedienteID { get; set; }
        public string CodigoExpediente { get; set; } = string.Empty;
        public string HC { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;

        // ── Fallecimiento ─────────────────────────────────────────────
        public string ServicioFallecimiento { get; set; } = string.Empty;
        public DateTime FechaHoraFallecimiento { get; set; }
        /// <summary>"Interno" | "Externo" </summary>
        public string TipoExpediente { get; set; } = string.Empty;
        public string EstadoActual { get; set; } = string.Empty;

        // ── Bandeja ───────────────────────────────────────────────────
        public string? CodigoBandeja { get; set; }
        public DateTime? FechaIngresoBandeja { get; set; }
        /// <summary>
        /// Tiempo en mortuorio al momento de la consulta. Formato: "3h 20m" | "2d 5h 10m".
        /// El frontend lo recalcula cada 60s usando FechaIngresoBandeja.
        /// </summary>
        public string? TiempoEnMortuorio { get; set; }

        // ── Semáforo ──────────────────────────────────────────────────
        public bool? BloqueaSangre { get; set; }
        public bool? BloqueaEconomica { get; set; }
        /// <summary>Texto para tooltip — sin montos.</summary>
        public string DescripcionSangre { get; set; } = "Sin deuda registrada";
        public string DescripcionEconomica { get; set; } = "Sin deuda registrada";

        // ── Bypass ────────────────────────────────────────────────────
        public bool BypassDeudaAutorizado { get; set; }
        public string? BypassDeudaJustificacion { get; set; }

        // ── Acta ──────────────────────────────────────────────────────
        public bool TieneActa { get; set; }
        public string? TipoSalida { get; set; }
    }

    /// <summary>
    /// DTO detalle para modal "Ver" — extiende ExpedienteVigilanciaDTO.
    /// </summary>
    public class DetalleVigilanciaDTO : ExpedienteVigilanciaDTO
    {
        // ── Datos adicionales ─────────────────────────────────────────
        public string? DiagnosticoFinal { get; set; }
        public bool CausaViolentaODudosa { get; set; }
        public bool EsNN { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string FuenteFinanciamiento { get; set; } = string.Empty;

        // ── Semáforo expandido (sin montos) ───────────────────────────
        /// <summary>"SinDeuda" | "Pendiente" | "Liquidado" | "Anulado" | "Sin registro"</summary>
        public string EstadoSangre { get; set; } = "Sin registro";
        public string? DetalleSangre { get; set; }
        /// <summary>"SinDeuda" | "Pendiente" | "Liquidado" | "Exonerado" | "Sin registro"</summary>
        public string EstadoEconomica { get; set; } = "Sin registro";
        /// <summary>
        /// Mensaje de estado económico. Si BypassDeudaAutorizado=true y estado=Pendiente,
        /// </summary>
        public string MensajeEconomica { get; set; } = "Sin deuda registrada";

        // ── Documentación ─────────────────────────────────────────────
        public bool DocumentacionCompleta { get; set; }

        // ── Retiro ────────────────────────────────────────────────────
        public string? ResponsableRetiro { get; set; }
        public string? ParentescoOCargo { get; set; }
        public string? Destino { get; set; }

        // ── Acta ──────────────────────────────────────────────────────
        public int? ActaRetiroID { get; set; }

        // ── Jefe de Guardia (cuaderno de permanencia) ─────────────────
        public string? JefeGuardiaNombre { get; set; }
        public string? JefeGuardiaCMP { get; set; }
    }
}