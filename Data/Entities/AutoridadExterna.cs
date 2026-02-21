using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Registro de autoridades externas que intervienen en casos de fallecimiento externo
    /// (Policía Nacional, Fiscalía, Medicina Legal)
    /// </summary>
    public class AutoridadExterna
    {
        /// <summary>
        /// Identificador único del registro de autoridad
        /// </summary>
        [Key]
        public int AutoridadID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIÓN CON EXPEDIENTE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente asociado (caso externo)
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public virtual Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// ID del expediente legal al que pertenece esta autoridad (opcional).
        /// Digitaliza el "Cuaderno de Ocurrencias" del Vigilante.
        /// </summary>
        public int? ExpedienteLegalID { get; set; }

        /// <summary>
        /// Navegación al expediente legal.
        /// </summary>
        public virtual ExpedienteLegal? ExpedienteLegal { get; set; }
        // ═══════════════════════════════════════════════════════════
        // DATOS DE LA AUTORIDAD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tipo de autoridad (Policía, Fiscal, Médico Legista, Otros)
        /// </summary>
        [Required]
        public TipoAutoridadExterna TipoAutoridad { get; set; }

        /// <summary>
        /// Apellido paterno de la autoridad
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        /// <summary>
        /// Apellido materno de la autoridad
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        /// <summary>
        /// Nombres de la autoridad
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo (calculado, denormalizado para reportes)
        /// Se genera automáticamente: ApellidoPaterno + ApellidoMaterno + Nombres
        /// </summary>
        [MaxLength(300)]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento de identidad
        /// DNI | Pasaporte | CarneExtranjeria | SinDocumento | NN
        /// </summary>
        [Required]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        /// <summary>
        /// Número de documento de identidad
        /// - DNI: 8 dígitos numéricos
        /// - Pasaporte: Alfanumérico según país
        /// - Carné Extranjería: Alfanumérico
        /// - NN: "NN-DDMMYYYY"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // DATOS ESPECÍFICOS SEGÚN TIPO DE AUTORIDAD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Código especial de la autoridad
        /// - Policía: Código de placa o carné PNP (opcional)
        /// - Fiscal: Código de Fiscal (ej: "F-2025-LIMA-001")
        /// - Médico Legista: CMP (Colegio Médico del Perú)
        /// </summary>
        [MaxLength(50)]
        public string? CodigoEspecial { get; set; }

        /// <summary>
        /// Institución a la que pertenece la autoridad
        /// Ejemplos:
        /// - Policía: "Comisaría San Antonio", "Comisaría Surquillo"
        /// - Fiscal: "Fiscalía Provincial de Miraflores", "Fiscalía de Turno"
        /// - Médico Legista: "Morgue Central de Lima", "Instituto de Medicina Legal"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Institucion { get; set; } = string.Empty;

        /// <summary>
        /// Placa del vehículo oficial (principalmente para Policía)
        /// Ej: "PNP-1234", "A1B-987"
        /// Útil para registro de vehículos en Cuaderno de Ocurrencias
        /// </summary>
        [MaxLength(20)]
        public string? PlacaVehiculo { get; set; }

        /// <summary>
        /// Teléfono de contacto de la autoridad o institución
        /// </summary>
        [MaxLength(20)]
        public string? Telefono { get; set; }

        /// <summary>
        /// Cargo específico de la autoridad
        /// Ej: "Suboficial PNP", "Fiscal Provincial", "Médico Legista II"
        /// </summary>
        [MaxLength(100)]
        public string? Cargo { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DOCUMENTACIÓN RELACIONADA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si la autoridad entregó documentación oficial
        /// Ej: Oficio PNP, Orden Fiscal, Acta de Levantamiento
        /// </summary>
        public bool DocumentoEntregado { get; set; } = false;

        /// <summary>
        /// Número de documento oficial entregado
        /// Ej: "Oficio Nº 045-2025-COMISARIA-SA", "Orden Fiscal Nº 123-2025"
        /// </summary>
        [MaxLength(100)]
        public string? NumeroDocumentoOficial { get; set; }

        /// <summary>
        /// Fecha del documento oficial
        /// </summary>
        public DateTime? FechaDocumentoOficial { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA Y OBSERVACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Fecha y hora en que la autoridad llegó al hospital
        /// Registrada por Sup. Vigilancia en Puerta Principal
        /// </summary>
        [Required]
        public DateTime FechaHoraLlegada { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora en que la autoridad se retiró del hospital
        /// Se registra tras el levantamiento de cadáver
        /// </summary>
        public DateTime? FechaHoraSalida { get; set; }

        /// <summary>
        /// Observaciones del Sup. Vigilancia sobre la autoridad
        /// Ej: "Presentó orden judicial", "Solicitó copia de Epicrisis"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Usuario que registró la autoridad (Sup. Vigilancia)
        /// </summary>
        [Required]
        public int UsuarioRegistroID { get; set; }

        /// <summary>
        /// Navegación al usuario que registró
        /// </summary>
        public virtual Usuario UsuarioRegistro { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de registro en el sistema
        /// </summary>
        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el nombre completo a partir de apellidos y nombres
        /// Debe llamarse antes de guardar en BD
        /// </summary>
        public void GenerarNombreCompleto()
        {
            NombreCompleto = $"{ApellidoPaterno} {ApellidoMaterno} {Nombres}".Trim();
        }

        /// <summary>
        /// Verifica si los datos de la autoridad están completos según su tipo
        /// </summary>
        public bool DatosCompletos()
        {
            bool datosBasicos = !string.IsNullOrWhiteSpace(ApellidoPaterno) &&
                               !string.IsNullOrWhiteSpace(Nombres) &&
                               !string.IsNullOrWhiteSpace(NumeroDocumento) &&
                               !string.IsNullOrWhiteSpace(Institucion);

            return TipoAutoridad switch
            {
                TipoAutoridadExterna.Policia => datosBasicos &&
                                                !string.IsNullOrWhiteSpace(PlacaVehiculo),

                TipoAutoridadExterna.Fiscal => datosBasicos &&
                                               !string.IsNullOrWhiteSpace(CodigoEspecial),

                TipoAutoridadExterna.MedicoLegista => datosBasicos &&
                                                       !string.IsNullOrWhiteSpace(CodigoEspecial), // CMP

                _ => datosBasicos
            };
        }

        /// <summary>
        /// Registra la salida de la autoridad del hospital
        /// </summary>
        public void RegistrarSalida(DateTime? fechaHoraSalida = null)
        {
            FechaHoraSalida = fechaHoraSalida ?? DateTime.Now;
        }

        /// <summary>
        /// Calcula el tiempo que la autoridad permaneció en el hospital
        /// </summary>
        public TimeSpan? TiempoEstancia()
        {
            if (FechaHoraSalida == null) return null;
            return FechaHoraSalida.Value - FechaHoraLlegada;
        }

        /// <summary>
        /// Genera un resumen legible de la autoridad
        /// Útil para logs y reportes
        /// </summary>
        public string GenerarResumen()
        {
            string tipo = TipoAutoridad switch
            {
                TipoAutoridadExterna.Policia => "Policía Nacional",
                TipoAutoridadExterna.Fiscal => "Fiscal",
                TipoAutoridadExterna.MedicoLegista => "Médico Legista",
                _ => "Autoridad"
            };

            return $"{tipo} - {NombreCompleto} ({TipoDocumento}: {NumeroDocumento}) - {Institucion}";
        }

        /// <summary>
        /// Valida que el documento oficial entregado esté completo
        /// </summary>
        public string ValidarDocumentoOficial()
        {
            if (!DocumentoEntregado)
                return "No se registró entrega de documento oficial";

            if (string.IsNullOrWhiteSpace(NumeroDocumentoOficial))
                return "Falta registrar número de documento oficial";

            if (FechaDocumentoOficial == null)
                return "Falta registrar fecha del documento oficial";

            return "Documento registrado correctamente";
        }
    }
}