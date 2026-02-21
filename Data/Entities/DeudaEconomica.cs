using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Registro de deuda económica del fallecido
    /// Determina si se bloquea retiro según RN-19 (SIS) y RN-20 (Particular)
    /// 
    /// REGLAS DE NEGOCIO:
    /// RN-19: Pacientes SIS siempre tienen deuda económica = 0 (cubierto 100%)
    /// RN-20: Si tipo seguro es PARTICULAR, verificación económica es obligatoria
    /// 
    /// FLUJO ACTUAL (SIN INTEGRACIÓN CON CAJA/CUENTAS PACIENTES):
    /// 1. Sup. Vigilancia pregunta verbalmente a Cuentas Pacientes si hay deuda
    /// 2. Cuentas Pacientes responde: "Sí, debe S/ 1,200" (verbal)
    /// 3. Sup. Vigilancia registra en SGM: TieneDeuda = true, MontoDeuda = 1200
    /// 4. Familiar va a Servicio Social → Exoneración (si aplica)
    /// 5. Servicio Social entra a SGM → Registra exoneración + PDF sustento
    /// 6. Familiar paga en Caja → Recibe boleta física
    /// 7. Familiar muestra boleta a Sup. Vigilancia
    /// 8. Sup. Vigilancia copia Nº Boleta manualmente → Marca "Liquidado" en SGM
    /// 
    /// FUTURO (CON INTEGRACIÓN ERP):
    /// - Cuentas Pacientes registrará directamente en ERP
    /// - Caja actualizará estado "Liquidado" automáticamente
    /// - Sup. Vigilancia solo consultará el semáforo (DEBE / NO DEBE)
    /// </summary>
    public class DeudaEconomica
    {
        /// <summary>
        /// Identificador único del registro de deuda económica
        /// </summary>
        [Key]
        public int DeudaEconomicaID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIÓN CON EXPEDIENTE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente asociado (relación 1:1)
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public virtual Expediente Expediente { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // ESTADO DE LA DEUDA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual de la deuda económica
        /// SinDeuda | Liquidado | Exonerado | Pendiente
        /// 
        /// LÓGICA DE BLOQUEO (PARA SUP. VIGILANCIA):
        /// - SinDeuda → NO DEBE (paciente SIS o sin consumos)
        /// - Liquidado →  NO DEBE (familiar pagó en Caja)
        /// - Exonerado →  NO DEBE (Servicio Social exoneró 100%)
        /// - Pendiente →  DEBE (tiene deuda pendiente)
        /// 
        /// SUP. VIGILANCIA SOLO VE:
        /// - "Sin deuda - Puede retirar"
        /// -  "Tiene deuda - Enviar a Cuentas Pacientes/Servicio Social"
        /// </summary>
        [Required]
        public EstadoDeudaEconomica Estado { get; set; } = EstadoDeudaEconomica.Pendiente;

        // ═══════════════════════════════════════════════════════════
        // MONTOS (VISIBLES SOLO PARA ROLES AUTORIZADOS)
        // Roles con acceso: CuentasPacientes, ServicioSocial, Admin, JefeGuardia
        // Sup. Vigilancia NO ve estos montos (solo semáforo DEBE/NO DEBE)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Monto total de la deuda generada durante hospitalización
        /// Origen: Sistema de Cuentas Pacientes (consulta verbal por ahora)
        /// Registrado por: Sup. Vigilancia (temporal) o Cuentas Pacientes (futuro)
        /// 
        /// IMPORTANTE: Sup. Vigilancia NO ve este campo en su interfaz
        /// Solo lo registra cuando Cuentas Pacientes le informa verbalmente
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoDeuda { get; set; }

        /// <summary>
        /// Monto exonerado por Servicio Social (en soles)
        /// Puede ser parcial o total según evaluación socioeconómica
        /// Registrado por: Servicio Social
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoExonerado { get; set; } = 0;

        /// <summary>
        /// Monto pagado por familiar en Caja
        /// Registrado por: Sup. Vigilancia (copia de boleta física - temporal)
        ///                 o Caja (automático - futuro con integración)
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoPagado { get; set; } = 0;

        /// <summary>
        /// Monto final pendiente (propiedad calculada)
        /// Fórmula: MontoDeuda - MontoExonerado - MontoPagado
        /// 
        /// IMPORTANTE: NotMapped - No se guarda en BD, se calcula en tiempo real
        /// Usado internamente para validar si Estado debe cambiar a Liquidado/Exonerado
        /// </summary>
        [NotMapped]
        public decimal MontoPendiente => MontoDeuda - MontoExonerado - MontoPagado;

        // ═══════════════════════════════════════════════════════════
        // PAGO EN CAJA (REGISTRO TEMPORAL HASTA INTEGRACIÓN)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de boleta de pago emitida por Caja
        /// Copiado MANUALMENTE por Sup. Vigilancia desde boleta física que muestra familiar
        /// 
        /// FLUJO ACTUAL:
        /// 1. Familiar paga en Caja → Recibe boleta física "B001-12345"
        /// 2. Familiar muestra boleta a Sup. Vigilancia
        /// 3. Sup. Vigilancia copia número visualmente → Registra en SGM
        /// 
        /// FUTURO (Integración ERP):
        /// - Caja registrará automáticamente el pago
        /// - Este campo se llenará automáticamente
        /// </summary>
        [MaxLength(50)]
        public string? NumeroBoleta { get; set; }

        /// <summary>
        /// Fecha en que se realizó el pago en Caja
        /// Registrada por: Sup. Vigilancia (temporal) o Caja (futuro)
        /// </summary>
        public DateTime? FechaPago { get; set; }

        /// <summary>
        /// Observaciones sobre el pago
        /// Ej: "Pago verificado con boleta física", "Familiar presentó boleta B001-12345"
        /// </summary>
        [MaxLength(500)]
        public string? ObservacionesPago { get; set; }

        // ═══════════════════════════════════════════════════════════
        // EXONERACIÓN (SERVICIO SOCIAL)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tipo de exoneración aplicada
        /// SinExoneracion | Parcial | Total
        /// </summary>
        [Required]
        public TipoExoneracion TipoExoneracion { get; set; } = TipoExoneracion.SinExoneracion;

        /// <summary>
        /// Observaciones de Servicio Social sobre la exoneración
        /// Debe documentar criterios de evaluación socioeconómica
        /// Ejemplos:
        /// - "Familia en situación de extrema pobreza - Exoneración total"
        /// - "Ingresos familiares bajos - Exoneración parcial del 70%"
        /// - "Paciente SIS pero con consumos no cubiertos - Exoneración total"
        /// </summary>
        [MaxLength(1000)]
        public string? ObservacionesExoneracion { get; set; }

        /// <summary>
        /// Ruta del PDF escaneado de Ficha Socioeconómica o Boleta de Exoneración
        /// Subido por Asistenta Social como sustento de la decisión
        /// Formato: "uploads/exoneraciones/2025/01/SGM-2025-00152-ficha-socioeconomica.pdf"
        /// </summary>
        [MaxLength(500)]
        public string? RutaPDFSustento { get; set; }

        /// <summary>
        /// Nombre original del archivo PDF de sustento
        /// Para mostrar en UI sin exponer ruta completa
        /// </summary>
        [MaxLength(255)]
        public string? NombreArchivoSustento { get; set; }

        /// <summary>
        /// Tamaño del archivo PDF en bytes
        /// </summary>
        public long? TamañoArchivoSustento { get; set; }

        /// <summary>
        /// Asistenta Social que autorizó la exoneración
        /// Solo aplica si TipoExoneracion != SinExoneracion
        /// </summary>
        public int? AsistentaSocialID { get; set; }

        /// <summary>
        /// Navegación a la Asistenta Social
        /// </summary>
        public virtual Usuario? AsistentaSocial { get; set; }

        /// <summary>
        /// Fecha en que se autorizó la exoneración
        /// </summary>
        public DateTime? FechaExoneracion { get; set; }

        /// <summary>
        /// Número de boleta de exoneración emitida por Servicio Social.
        /// Ejemplo: "003822" (número correlativo impreso en boleta física)
        /// Diferente de NumeroBoleta que es para pagos en Caja.
        /// </summary>
        [MaxLength(50)]
        public string? NumeroBoletaExoneracion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Usuario que registró la deuda inicialmente
        /// Normalmente: Sup. Vigilancia (quien pregunta a Cuentas Pacientes)
        /// Futuro: CuentasPacientes (registro directo en ERP)
        /// </summary>
        [Required]
        public int UsuarioRegistroID { get; set; }

        /// <summary>
        /// Navegación al usuario que registró
        /// </summary>
        public virtual Usuario UsuarioRegistro { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de registro inicial
        /// </summary>
        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>
        /// Último usuario que actualizó el estado
        /// Puede ser: Servicio Social (exoneración), Sup. Vigilancia (pago), Admin (corrección)
        /// </summary>
        public int? UsuarioActualizacionID { get; set; }

        /// <summary>
        /// Navegación al usuario que actualizó
        /// </summary>
        public virtual Usuario? UsuarioActualizacion { get; set; }

        /// <summary>
        /// Fecha y hora de última actualización
        /// </summary>
        public DateTime? FechaActualizacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la deuda económica bloquea el retiro del cuerpo
        /// Retorna TRUE si tiene deuda pendiente
        /// 
        /// USADO POR: Dashboard Sup. Vigilancia (semáforo DEBE/NO DEBE)
        /// LÓGICA: Solo bloquea si Estado = Pendiente Y MontoPendiente > 0
        /// </summary>
        /// <returns>true si bloquea retiro, false en caso contrario</returns>
        public bool BloqueaRetiro()
        {
            return Estado == EstadoDeudaEconomica.Pendiente && MontoPendiente > 0;
        }

        /// <summary>
        /// Obtiene el texto del semáforo para Sup. Vigilancia
        /// SOLO muestra DEBE / NO DEBE
        /// Formato estandarizado para toda la aplicación
        /// </summary>
        /// <returns>Texto para mostrar a Sup. Vigilancia</returns>
        public string ObtenerSemaforoSupVigilancia()
        {
            return BloqueaRetiro() ? "DEBE" : "NO DEBE";
        }

        /// <summary>
        /// Obtiene un mensaje detallado del estado (con contexto)
        /// USADO POR: Interfaces que necesitan más información que el semáforo simple
        /// </summary>
        public string ObtenerMensajeEstado()
        {
            return Estado switch
            {
                EstadoDeudaEconomica.SinDeuda => "Sin deuda - Puede retirar",
                EstadoDeudaEconomica.Liquidado => "Deuda liquidada - Puede retirar",
                EstadoDeudaEconomica.Exonerado when MontoPendiente <= 0 => "Deuda exonerada totalmente - Puede retirar",
                EstadoDeudaEconomica.Exonerado => "Deuda exonerada parcialmente - Verificar pago restante",
                EstadoDeudaEconomica.Pendiente => "Tiene deuda pendiente - Dirigirse a Caja o Servicio Social",
                _ => "Estado desconocido"
            };
        }

        /// <summary>
        /// Calcula el porcentaje de exoneración aplicado
        /// USADO POR: Reportes y Dashboard de roles autorizados (NO Sup. Vigilancia)
        /// </summary>
        public decimal ObtenerPorcentajeExoneracion()
        {
            if (MontoDeuda == 0) return 0;
            return Math.Round((MontoExonerado / MontoDeuda) * 100, 2);
        }

        /// <summary>
        /// Marca la deuda como liquidada (pagada en Caja)
        /// USADO POR: Sup. Vigilancia (temporal) cuando familiar muestra boleta
        /// VALIDACIÓN: Verifica que el pago cubra la deuda restante
        /// </summary>
        /// <param name="numeroBoleta">Número de boleta copiado de boleta física</param>
        /// <param name="montoPagado">Monto pagado (copiado de boleta)</param>
        /// <param name="usuarioID">Usuario que registra (Sup. Vigilancia)</param>
        /// <param name="observaciones">Observaciones opcionales</param>
        public void MarcarLiquidado(string numeroBoleta, decimal montoPagado, int usuarioID, string? observaciones = null)
        {
            if (string.IsNullOrWhiteSpace(numeroBoleta))
                throw new ArgumentException("Debe proporcionar número de boleta", nameof(numeroBoleta));

            if (montoPagado <= 0)
                throw new ArgumentException("Monto pagado debe ser mayor a 0", nameof(montoPagado));

            MontoPagado += montoPagado; // Acumular pagos (permite pagos parciales)
            NumeroBoleta = numeroBoleta;
            FechaPago = DateTime.Now;
            ObservacionesPago = observaciones ?? "Pago verificado con boleta física presentada por familiar";

            // Si el monto pagado + exonerado cubre toda la deuda
            if (MontoPendiente <= 0)
            {
                Estado = EstadoDeudaEconomica.Liquidado;
            }
            else
            {
                // Pago parcial, mantener en Pendiente
                Estado = EstadoDeudaEconomica.Pendiente;
            }

            UsuarioActualizacionID = usuarioID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Aplica exoneración por Servicio Social
        /// USADO POR: Servicio Social (rol autorizado)
        /// VALIDACIÓN: Verifica que el monto no supere la deuda y que haya sustento
        /// </summary>
        public void AplicarExoneracion(
            decimal montoExonerado,
            TipoExoneracion tipoExoneracion,
            int asistentaSocialID,
            string observaciones,
            string? numeroBoletaExoneracion = null,
            string? rutaPDFSustento = null,
            string? nombreArchivo = null,
            long? tamañoArchivo = null)
        {
            if (montoExonerado <= 0)
                throw new ArgumentException("Monto exonerado debe ser mayor a 0", nameof(montoExonerado));

            if (montoExonerado > MontoDeuda)
                throw new ArgumentException("Monto exonerado no puede superar la deuda total", nameof(montoExonerado));

            if (string.IsNullOrWhiteSpace(observaciones))
                throw new ArgumentException("Debe proporcionar observaciones/justificación", nameof(observaciones));

            if (Estado == EstadoDeudaEconomica.Liquidado)
                throw new InvalidOperationException("No se puede exonerar una deuda ya liquidada");

            MontoExonerado += montoExonerado; // Acumular exoneraciones (permite exoneraciones parciales sucesivas)
            TipoExoneracion = tipoExoneracion;
            ObservacionesExoneracion = observaciones;
            NumeroBoletaExoneracion = numeroBoletaExoneracion;
            RutaPDFSustento = rutaPDFSustento;
            NombreArchivoSustento = nombreArchivo;
            TamañoArchivoSustento = tamañoArchivo;
            AsistentaSocialID = asistentaSocialID;
            FechaExoneracion = DateTime.Now;

            // Actualizar estado según monto pendiente
            if (MontoPendiente <= 0)
            {
                Estado = EstadoDeudaEconomica.Exonerado;
            }
            else
            {
                // Aún hay monto pendiente (exoneración parcial)
                Estado = EstadoDeudaEconomica.Pendiente;
            }

            UsuarioActualizacionID = asistentaSocialID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Marca la deuda como sin deuda (paciente SIS o sin consumos)
        /// USADO POR: Sup. Vigilancia o Cuentas Pacientes
        /// EFECTO: Resetea todos los montos y cambia estado a SinDeuda
        /// </summary>
        public void MarcarSinDeuda(int usuarioID)
        {
            Estado = EstadoDeudaEconomica.SinDeuda;
            MontoDeuda = 0;
            MontoExonerado = 0;
            MontoPagado = 0;

            UsuarioActualizacionID = usuarioID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Genera un resumen detallado del estado de la deuda
        /// USADO POR: Reportes, Dashboard de roles autorizados (Admin, Jefe Guardia, Cuentas, etc.)
        /// NO SE MUESTRA A: Sup. Vigilancia (él solo ve semáforo DEBE/NO DEBE)
        /// </summary>
        public string GenerarResumenDetallado()
        {
            return Estado switch
            {
                EstadoDeudaEconomica.SinDeuda =>
                    "Sin deuda económica",

                EstadoDeudaEconomica.Liquidado =>
                    $"Liquidado - Pagado S/ {MontoPagado:N2} (Boleta: {NumeroBoleta})",

                EstadoDeudaEconomica.Exonerado when MontoPendiente <= 0 =>
                    $"Exonerado Total - S/ {MontoExonerado:N2} ({ObtenerPorcentajeExoneracion()}%) - Boleta: {NumeroBoletaExoneracion ?? "N/A"}",

                EstadoDeudaEconomica.Exonerado =>
                    $"Exonerado Parcial - S/ {MontoExonerado:N2} ({ObtenerPorcentajeExoneracion()}%) - Pendiente: S/ {MontoPendiente:N2}",

                EstadoDeudaEconomica.Pendiente when MontoExonerado > 0 && MontoPagado > 0 =>
                    $"Pendiente - Deuda: S/ {MontoDeuda:N2} | Exonerado: S/ {MontoExonerado:N2} | Pagado: S/ {MontoPagado:N2} | Pendiente: S/ {MontoPendiente:N2}",

                EstadoDeudaEconomica.Pendiente when MontoExonerado > 0 =>
                    $"Pendiente - Deuda: S/ {MontoDeuda:N2} | Exonerado: S/ {MontoExonerado:N2} | Pendiente: S/ {MontoPendiente:N2}",

                EstadoDeudaEconomica.Pendiente =>
                    $"Pendiente - Deuda total: S/ {MontoDeuda:N2}",

                _ => "Estado desconocido"
            };
        }

        /// <summary>
        /// Obtiene el tamaño del archivo PDF de sustento en formato legible
        /// </summary>
        public string? ObtenerTamañoArchivoLegible()
        {
            if (TamañoArchivoSustento == null) return null;

            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = TamañoArchivoSustento.Value;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Valida que el PDF de sustento esté presente cuando hay exoneración
        /// </summary>
        public string ValidarSustento()
        {
            if (TipoExoneracion == TipoExoneracion.SinExoneracion)
                return "No aplica (sin exoneración)";

            if (string.IsNullOrWhiteSpace(RutaPDFSustento))
                return "Falta adjuntar PDF de Ficha Socioeconómica";

            return "PDF de sustento adjunto correctamente";
        }
    }
}