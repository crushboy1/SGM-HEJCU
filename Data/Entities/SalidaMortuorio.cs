using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa el registro de salida física de un cuerpo del mortuorio.
    /// Documenta quién retira el cuerpo, con qué autorización y bajo qué condiciones.
    /// Soporta tanto casos internos (Familiar) como externos (AutoridadLegal).
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
        /// ID del vigilante que autoriza y registra la salida física
        /// Vigilante solo confirma el retiro, no valida documentación (ya lo hizo Admisión)
        /// </summary>
        [Required]
        public int VigilanteID { get; set; }

        /// <summary>
        /// Navegación al vigilante
        /// </summary>
        public virtual Usuario Vigilante { get; set; } = null!;

        /// <summary>
        /// Fecha y hora exacta de la salida física del cuerpo
        /// </summary>
        [Required]
        public DateTime FechaHoraSalida { get; set; } = DateTime.Now;

        /// <summary>
        /// Tipo de salida (Familiar, AutoridadLegal, TrasladoHospital, Otro)
        /// </summary>
        [Required]
        public TipoSalida TipoSalida { get; set; }

        // ═══════════════════════════════════════════════════════════
        // REFERENCIAS POLIMÓRFICAS (TIPO DE SALIDA)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del Acta de Retiro (OBLIGATORIO si TipoSalida = Familiar)
        /// Relación con documento tripartito generado por Admisión
        /// Se usa para pre-cargar datos del familiar autorizado
        /// </summary>
        public int? ActaRetiroID { get; set; }

        /// <summary>
        /// Navegación al Acta de Retiro
        /// </summary>
        public virtual ActaRetiro? ActaRetiro { get; set; }

        /// <summary>
        /// ID del Expediente Legal (OBLIGATORIO si TipoSalida = AutoridadLegal)
        /// Relación con expediente de caso externo (muerte < 24/48h)
        /// Se usa para pre-cargar datos de autoridades y documentos legales
        /// </summary>
        public int? ExpedienteLegalID { get; set; }

        /// <summary>
        /// Navegación al Expediente Legal
        /// </summary>
        public virtual ExpedienteLegal? ExpedienteLegal { get; set; }

        // ═══════════════════════════════════════════════════════════
        // BANDEJA Y MÉTRICAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Bandeja que se liberó al registrar esta salida
        /// Permite rastrear historial de ocupación
        /// Se libera automáticamente al confirmar la salida
        /// </summary>
        public int? BandejaLiberadaID { get; set; }

        /// <summary>
        /// Navegación a la bandeja liberada
        /// </summary>
        public virtual Bandeja? BandejaLiberada { get; set; }

        /// <summary>
        /// Tiempo total que el cuerpo permaneció en el mortuorio
        /// Se calcula automáticamente: FechaHoraSalida - FechaHoraIngresoMortuorio
        /// Métrica crítica para:
        /// - Alertas >24hrs
        /// - Reportes de permanencia promedio
        /// - Auditorías DIRESA
        /// </summary>
        public TimeSpan? TiempoPermanencia { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL RESPONSABLE QUE RETIRA EL CUERPO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre completo del responsable que retira el cuerpo
        /// - Caso Interno: Familiar (desde ActaRetiro)
        /// - Caso Externo: Médico Legista (desde ExpedienteLegal)
        /// Registrado por Admisión, solo lectura para Vigilante
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ResponsableNombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del responsable
        /// Siempre DNI en Perú (8 dígitos)
        /// Excepción: Pasaporte o CE para extranjeros (hasta 12 dígitos)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ResponsableTipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del responsable
        /// - DNI: 8 dígitos
        /// - Pasaporte/CE: hasta 12 caracteres alfanuméricos
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ResponsableNumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Relación con el fallecido (Hijo/a, Esposo/a, Padre/Madre, Hermano/a, etc.)
        /// OBLIGATORIO si TipoSalida = Familiar
        /// NULL si TipoSalida = AutoridadLegal
        /// Registrado por Admisión en ActaRetiro
        /// </summary>
        [MaxLength(50)]
        public string? ResponsableParentesco { get; set; }

        /// <summary>
        /// Teléfono de contacto del responsable
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(20)]
        public string? ResponsableTelefono { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUTORIZACIÓN (SOLO CASOS EXTERNOS)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de oficio policial (OBLIGATORIO para TipoSalida = AutoridadLegal)
        /// Ejemplo: "OFICIO N° 1262-2025-REG.POL-LIMA/DIVPOL-SUR-1-CSA-DEINPOL-SIAT"
        /// Registrado por Admisión cuando sube el documento PDF
        /// Firmado por Jefe de Guardia (campo AutorizadoJefeGuardia en ExpedienteLegal)
        /// NULL para casos internos (Familiar)
        /// </summary>
        [MaxLength(150)]
        public string? NumeroOficio { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL SERVICIO FUNERARIO (SOLO CASOS INTERNOS)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre de la funeraria que retira el cuerpo
        /// OBLIGATORIO si TipoSalida = Familiar
        /// Registrado por Admisión
        /// NULL para casos externos (AutoridadLegal)
        /// </summary>
        [MaxLength(200)]
        public string? NombreFuneraria { get; set; }

        /// <summary>
        /// RUC de la funeraria
        /// Opcional (solo si la funeraria es empresa formal)
        /// Registrado por Admisión desde ActaRetiro
        /// Formato: 11 dígitos numéricos
        /// </summary>
        [MaxLength(11)]
        public string? FunerariaRUC { get; set; }

        /// <summary>
        /// Teléfono de contacto de la funeraria
        /// Opcional
        /// Registrado por Admisión desde ActaRetiro
        /// </summary>
        [MaxLength(20)]
        public string? FunerariaTelefono { get; set; }
        /// <summary>
        /// Nombre del conductor/representante de la funeraria
        /// OBLIGATORIO si TipoSalida = Familiar
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(200)]
        public string? ConductorFuneraria { get; set; }

        /// <summary>
        /// DNI del conductor de la funeraria
        /// OBLIGATORIO si TipoSalida = Familiar
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(20)]
        public string? DNIConductor { get; set; }

        /// <summary>
        /// Nombre completo del ayudante de la funeraria
        /// Opcional
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(200)]
        public string? AyudanteFuneraria { get; set; }

        /// <summary>
        /// DNI del ayudante de la funeraria
        /// Opcional
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(20)]
        public string? DNIAyudante { get; set; }

        /// <summary>
        /// Placa del vehículo
        /// - Caso Interno: Placa de vehículo funerario (OBLIGATORIO)
        /// - Caso Externo: Placa de patrullero (OBLIGATORIO)
        /// Registrado por Admisión desde:
        /// - ActaRetiro (interno)
        /// - AutoridadExterna-Policia (externo)
        /// Vigilante solo confirma visualmente
        /// </summary>
        [MaxLength(20)]
        public string? PlacaVehiculo { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DESTINO Y OBSERVACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Destino final del cuerpo
        /// - Caso Interno: Cementerio/Crematorio
        /// - Caso Externo: "Morgue Central"
        /// Registrado por Admisión
        /// </summary>
        [MaxLength(200)]
        public string? Destino { get; set; }

        /// <summary>
        /// Observaciones adicionales sobre la salida
        /// Puede ser agregado/modificado por Vigilante si detecta alguna inconsistencia
        /// Ej: "Placa del vehículo no coincide con registro", "Retiro urgente autorizado"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Indica si hubo alguna irregularidad o incidente durante la salida
        /// Vigilante puede marcar esto al momento del retiro físico
        /// </summary>
        public bool IncidenteRegistrado { get; set; } = false;

        /// <summary>
        /// Descripción del incidente (si aplica)
        /// Registrado por Vigilante
        /// </summary>
        [MaxLength(1000)]
        public string? DetalleIncidente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que las referencias polimórficas sean consistentes con el tipo de salida
        /// </summary>
        public string ValidarReferencias()
        {
            if (TipoSalida == TipoSalida.Familiar)
            {
                if (ActaRetiroID is null)
                    return "Salida tipo Familiar requiere referencia a Acta de Retiro";

                if (ExpedienteLegalID is not null)
                    return "Salida tipo Familiar no debe tener Expediente Legal asociado";

                return "OK";
            }

            if (TipoSalida == TipoSalida.AutoridadLegal)
            {
                if (ExpedienteLegalID is null)
                    return "Salida tipo Autoridad Legal requiere referencia a Expediente Legal";

                if (ActaRetiroID is not null)
                    return "Salida tipo Autoridad Legal no debe tener Acta de Retiro asociada";

                return "OK";
            }

            // Tipo "Otro" puede o no tener referencias
            return "OK";
        }

        /// <summary>
        /// Verifica si la salida está completa (todos los campos requeridos llenos)
        /// </summary>
        public bool EstaCompleta()
        {
            // Validar referencias polimórficas primero
            var validacionReferencias = ValidarReferencias();
            if (validacionReferencias != "OK")
                return false;

            bool camposBasicosCompletos = !string.IsNullOrWhiteSpace(ResponsableNombre) &&
                                          !string.IsNullOrWhiteSpace(ResponsableTipoDocumento) &&
                                          !string.IsNullOrWhiteSpace(ResponsableNumeroDocumento);

            // Validación específica por tipo de salida
            return TipoSalida switch
            {
                TipoSalida.Familiar => camposBasicosCompletos &&
                                       !string.IsNullOrWhiteSpace(ResponsableParentesco) &&
                                       !string.IsNullOrWhiteSpace(PlacaVehiculo) &&
                                       ActaRetiroID is not null,

                TipoSalida.AutoridadLegal => camposBasicosCompletos &&
                                             !string.IsNullOrWhiteSpace(NumeroOficio) &&
                                             !string.IsNullOrWhiteSpace(PlacaVehiculo) &&
                                             ExpedienteLegalID is not null,

                _ => camposBasicosCompletos
            };
        }

        /// <summary>
        /// Verifica si los datos de la funeraria están completos
        /// SOLO APLICA para TipoSalida = Familiar
        /// </summary>
        public bool TieneDatosFunerariaCompletos()
        {
            return !string.IsNullOrWhiteSpace(NombreFuneraria) &&
                   !string.IsNullOrWhiteSpace(ConductorFuneraria) &&
                   !string.IsNullOrWhiteSpace(DNIConductor) &&
                   !string.IsNullOrWhiteSpace(PlacaVehiculo);
        }

        /// <summary>
        /// Registra un incidente durante la salida
        /// Usado por Vigilante al detectar irregularidades
        /// </summary>
        public void RegistrarIncidente(string detalleIncidente)
        {
            if (string.IsNullOrWhiteSpace(detalleIncidente))
                throw new ArgumentException("Debe proporcionar detalles del incidente", nameof(detalleIncidente));

            IncidenteRegistrado = true;
            DetalleIncidente = detalleIncidente;
        }

        /// <summary>
        /// Valida que la documentación requerida esté completa según el tipo de salida
        /// </summary>
        public string ValidarDocumentacion()
        {
            return TipoSalida switch
            {
                TipoSalida.Familiar when string.IsNullOrWhiteSpace(ResponsableParentesco)
                    => "Falta especificar el parentesco del familiar",

                TipoSalida.Familiar when !TieneDatosFunerariaCompletos()
                    => "Faltan datos completos de la funeraria",

                TipoSalida.AutoridadLegal when string.IsNullOrWhiteSpace(NumeroOficio)
                    => "Falta número de oficio policial",

                TipoSalida.AutoridadLegal when string.IsNullOrWhiteSpace(PlacaVehiculo)
                    => "Falta placa del vehículo policial",

                _ => "Documentación completa"
            };
        }

        /// <summary>
        /// Calcula el tiempo de permanencia en el mortuorio
        /// </summary>
        /// <param name="fechaIngresoMortuorio">Fecha/hora de ingreso al mortuorio</param>
        public void CalcularTiempoPermanencia(DateTime fechaIngresoMortuorio)
        {
            TiempoPermanencia = FechaHoraSalida - fechaIngresoMortuorio;
        }

        /// <summary>
        /// Verifica si excedió el límite de permanencia (48 horas)
        /// </summary>
        public bool ExcedioLimitePermanencia()
        {
            if (TiempoPermanencia == null) return false;
            return TiempoPermanencia.Value.TotalHours > 48;
        }
    }
}