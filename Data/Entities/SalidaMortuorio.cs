using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa el registro de salida física de un cuerpo del mortuorio.
    /// Documenta la funeraria, vehículo y personal que realiza el retiro.
    /// 
    /// RESPONSABILIDADES:
    /// - Admisión: crea y firma ActaRetiro (datos del responsable familiar o autoridad legal)
    /// - Vigilante: registra datos de la funeraria y confirma la salida física
    /// 
    /// El tipo de salida y datos del responsable se leen SIEMPRE desde ActaRetiro.
    /// Esta entidad NO duplica esos datos.
    /// </summary>
    public class SalidaMortuorio
    {
        /// <summary>
        /// Identificador único del registro de salida
        /// </summary>
        [Key]
        public int SalidaID { get; set; }

        /// <summary>
        /// ID del expediente que está siendo retirado
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public virtual Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// ID del usuario que registra la salida física.
        /// Puede ser Vigilante, Administrador u otro rol autorizado.
        /// Se obtiene del token JWT en el controller — nunca del frontend.
        /// </summary>
        [Required]
        public int RegistradoPorID { get; set; }

        /// <summary>Navegación al usuario que registró la salida.</summary>
        public virtual Usuario RegistradoPor { get; set; } = null!;

        /// <summary>
        /// Fecha y hora exacta de la salida física del cuerpo
        /// </summary>
        [Required]
        public DateTime FechaHoraSalida { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════════
        // REFERENCIA AL ACTA DE RETIRO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del Acta de Retiro generada por Admisión. OBLIGATORIO.
        /// El acta maneja ambos tipos de salida (Familiar y AutoridadLegal).
        /// Desde aquí se lee: TipoSalida, datos del responsable, datos de la autoridad.
        /// No se puede registrar salida sin acta firmada.
        /// </summary>
        [Required]
        public int ActaRetiroID { get; set; }

        /// <summary>
        /// Navegación al Acta de Retiro
        /// </summary>
        public virtual ActaRetiro ActaRetiro { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // REFERENCIA AL EXPEDIENTE LEGAL (OPCIONAL - VIGILANCIA)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del Expediente Legal Digital. OPCIONAL.
        /// Referencia al archivador digital de Vigilancia que contiene documentos
        /// como oficios PNP, actas de levantamiento, epicrisis de casos externos.
        /// No tiene relación con el flujo de validación del Acta de Retiro.
        /// Su uso depende de decisión futura sobre el módulo ExpedienteLegal.
        /// </summary>
        public int? ExpedienteLegalID { get; set; }

        /// <summary>
        /// Navegación al Expediente Legal (opcional)
        /// </summary>
        public virtual ExpedienteLegal? ExpedienteLegal { get; set; }

        // ═══════════════════════════════════════════════════════════
        // BANDEJA Y MÉTRICAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Bandeja que se liberó al registrar esta salida.
        /// Se libera automáticamente al confirmar el retiro.
        /// Permite rastrear historial de ocupación de bandejas.
        /// </summary>
        public int? BandejaLiberadaID { get; set; }

        /// <summary>
        /// Navegación a la bandeja liberada
        /// </summary>
        public virtual Bandeja? BandejaLiberada { get; set; }

        /// <summary>
        /// Tiempo total de permanencia en el mortuorio expresado en minutos.
        /// Se calcula: FechaHoraSalida - FechaHoraIngresoMortuorio.
        /// Guardado como int (minutos) para evitar overflow del tipo TIME de SQL Server.
        /// Métrica crítica para alertas >24h, reportes DIRESA y auditorías.
        /// </summary>
        public int? TiempoPermanenciaMinutos { get; set; }

        // Propiedad calculada — NO mapeada a BD
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TimeSpan? TiempoPermanencia =>
            TiempoPermanenciaMinutos.HasValue
                ? TimeSpan.FromMinutes(TiempoPermanenciaMinutos.Value)
                : null;

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL SERVICIO FUNERARIO
        // Capturados por el Vigilante al momento del retiro físico.
        // Aplica para TipoSalida = Familiar.
        // Para AutoridadLegal estos campos son opcionales (vehículo policial).
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre de la funeraria que retira el cuerpo.
        /// Obligatorio si TipoSalida = Familiar.
        /// </summary>
        [MaxLength(200)]
        public string? NombreFuneraria { get; set; }

        /// <summary>
        /// RUC de la funeraria (11 dígitos). Opcional.
        /// </summary>
        [MaxLength(11)]
        public string? FunerariaRUC { get; set; }

        /// <summary>
        /// Teléfono de contacto de la funeraria. Opcional.
        /// </summary>
        [MaxLength(20)]
        public string? FunerariaTelefono { get; set; }

        /// <summary>
        /// Nombre completo del conductor que retira el cuerpo.
        /// Obligatorio si TipoSalida = Familiar.
        /// </summary>
        [MaxLength(200)]
        public string? ConductorFuneraria { get; set; }

        /// <summary>
        /// DNI del conductor. Obligatorio si TipoSalida = Familiar.
        /// </summary>
        [MaxLength(20)]
        public string? DNIConductor { get; set; }

        /// <summary>
        /// Nombre completo del ayudante. Opcional.
        /// </summary>
        [MaxLength(200)]
        public string? AyudanteFuneraria { get; set; }

        /// <summary>
        /// DNI del ayudante. Opcional.
        /// </summary>
        [MaxLength(20)]
        public string? DNIAyudante { get; set; }

        /// <summary>
        /// Placa del vehículo.
        /// - Familiar: placa del vehículo funerario (obligatorio)
        /// - AutoridadLegal: placa del patrullero o vehículo oficial (obligatorio)
        /// </summary>
        [MaxLength(20)]
        public string? PlacaVehiculo { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DESTINO E INCIDENTES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Destino final del cuerpo.
        /// Ejemplos: "Cementerio El Ángel", "Crematorio", "Morgue Central"
        /// </summary>
        [MaxLength(200)]
        public string? Destino { get; set; }

        /// <summary>
        /// Observaciones adicionales registradas por el Vigilante.
        /// Ejemplos: "Placa no coincide con registro", "Retiro urgente autorizado"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Indica si hubo alguna irregularidad durante la salida física.
        /// Marcado por el Vigilante.
        /// </summary>
        public bool IncidenteRegistrado { get; set; } = false;

        /// <summary>
        /// Descripción del incidente registrado por el Vigilante.
        /// Requerido si IncidenteRegistrado = true.
        /// </summary>
        [MaxLength(1000)]
        public string? DetalleIncidente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que el Acta de Retiro esté firmada antes de permitir la salida.
        /// El tipo de salida se lee desde ActaRetiro, no se duplica aquí.
        /// </summary>
        public string ValidarReferencias()
        {
            if (ActaRetiroID == 0)
                return "Se requiere un Acta de Retiro para registrar la salida";

            if (ActaRetiro is not null && string.IsNullOrWhiteSpace(ActaRetiro.RutaPDFFirmado))
                return "El Acta de Retiro debe estar firmada antes de registrar la salida";

            return "OK";
        }

        /// <summary>
        /// Verifica si el registro de salida está completo.
        /// Los datos del responsable no se validan aquí — viven en ActaRetiro.
        /// </summary>
        public bool EstaCompleta()
        {
            if (ActaRetiroID == 0) return false;
            if (string.IsNullOrWhiteSpace(PlacaVehiculo)) return false;

            // Para Familiar se requieren datos de funeraria
            if (ActaRetiro?.TipoSalida == TipoSalida.Familiar)
                return TieneDatosFunerariaCompletos();

            // Para AutoridadLegal solo se requiere placa (ya validada arriba)
            return true;
        }

        /// <summary>
        /// Verifica si los datos de la funeraria están completos.
        /// Solo aplica para TipoSalida = Familiar.
        /// </summary>
        public bool TieneDatosFunerariaCompletos()
        {
            return !string.IsNullOrWhiteSpace(NombreFuneraria) &&
                   !string.IsNullOrWhiteSpace(ConductorFuneraria) &&
                   !string.IsNullOrWhiteSpace(DNIConductor) &&
                   !string.IsNullOrWhiteSpace(PlacaVehiculo);
        }

        /// <summary>
        /// Valida la documentación requerida según el tipo de salida.
        /// Para AutoridadLegal, la placa se resuelve desde ActaRetiro si no viene del frontend.
        /// </summary>
        public string ValidarDocumentacion()
        {
            if (ActaRetiro is null)
                return "No se encontró el Acta de Retiro asociada";

            var placaEfectiva = ActaRetiro.TipoSalida == TipoSalida.AutoridadLegal
                ? (!string.IsNullOrWhiteSpace(PlacaVehiculo)
                    ? PlacaVehiculo
                    : ActaRetiro.AutoridadPlacaVehiculo)
                : PlacaVehiculo;

            return ActaRetiro.TipoSalida switch
            {
                TipoSalida.Familiar when !TieneDatosFunerariaCompletos()
                    => "Faltan datos completos de la funeraria",
                TipoSalida.Familiar when string.IsNullOrWhiteSpace(PlacaVehiculo)
                    => "Falta placa del vehículo funerario",
                TipoSalida.AutoridadLegal when string.IsNullOrWhiteSpace(placaEfectiva)
                    => "Falta placa del vehículo oficial en acta",
                _ => "Documentación completa"
            };
        }

        /// <summary>
        /// Registra un incidente ocurrido durante la salida física.
        /// Usado por el Vigilante al detectar irregularidades.
        /// </summary>
        public void RegistrarIncidente(string detalleIncidente)
        {
            if (string.IsNullOrWhiteSpace(detalleIncidente))
                throw new ArgumentException(
                    "Debe proporcionar detalles del incidente",
                    nameof(detalleIncidente));

            IncidenteRegistrado = true;
            DetalleIncidente = detalleIncidente;
        }

        /// <summary>
        /// Calcula el tiempo de permanencia en el mortuorio.
        /// </summary>
        /// <param name="fechaIngresoMortuorio">Fecha y hora de ingreso al mortuorio</param>
        public void CalcularTiempoPermanencia(DateTime fechaIngresoMortuorio)
        {
            var diferencia = FechaHoraSalida - fechaIngresoMortuorio;
            TiempoPermanenciaMinutos = (int)diferencia.TotalMinutes;
        }

        /// <summary>
        /// Verifica si el cuerpo excedió el límite de permanencia de 48 horas.
        /// </summary>
        public bool ExcedioLimitePermanencia()
        {
            if (!TiempoPermanenciaMinutos.HasValue) return false;
            return TiempoPermanenciaMinutos.Value > 48 * 60;
        }
    }
}