using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa el registro de salida física de un cuerpo del mortuorio.
    /// Documenta quién retira el cuerpo, con qué autorización y bajo qué condiciones.
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
        public Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// ID del vigilante que autoriza y registra la salida
        /// </summary>
        [Required]
        public int VigilanteID { get; set; }

        /// <summary>
        /// Navegación al vigilante
        /// </summary>
        public Usuario Vigilante { get; set; } = null!;

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
        // DATOS DEL RESPONSABLE QUE RETIRA EL CUERPO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre completo del responsable que retira el cuerpo
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ResponsableNombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del responsable (DNI, CE, Pasaporte)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ResponsableTipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del responsable
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ResponsableNumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Relación con el fallecido (Hijo/a, Esposo/a, Padre/Madre, Hermano/a, etc.)
        /// Solo aplica si TipoSalida = Familiar
        /// </summary>
        [MaxLength(50)]
        public string? ResponsableParentesco { get; set; }

        /// <summary>
        /// Teléfono de contacto del responsable
        /// </summary>
        [MaxLength(20)]
        public string? ResponsableTelefono { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUTORIZACIÓN Y DOCUMENTACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de documento de autorización oficial
        /// Ej: Orden fiscal, Resolución judicial, Autorización hospital
        /// </summary>
        [MaxLength(100)]
        public string? NumeroAutorizacion { get; set; }

        /// <summary>
        /// Entidad que emite la autorización
        /// Ej: "Fiscalía Provincial", "Juzgado 5to Civil", "Dirección Hospital"
        /// </summary>
        [MaxLength(200)]
        public string? EntidadAutorizante { get; set; }

        /// <summary>
        /// Indica si se verificó la documentación requerida
        /// </summary>
        [Required]
        public bool DocumentacionVerificada { get; set; } = false;

        /// <summary>
        /// Indica si se realizó pago de derechos mortuorios (si aplica)
        /// </summary>
        [Required]
        public bool PagoRealizado { get; set; } = false;

        /// <summary>
        /// Número de recibo/boleta de pago (si aplica)
        /// </summary>
        [MaxLength(50)]
        public string? NumeroRecibo { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL SERVICIO FUNERARIO (si aplica)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre de la funeraria que retira el cuerpo (si aplica)
        /// </summary>
        [MaxLength(200)]
        public string? NombreFuneraria { get; set; }

        /// <summary>
        /// Nombre del conductor/representante de la funeraria
        /// </summary>
        [MaxLength(200)]
        public string? ConductorFuneraria { get; set; }

        /// <summary>
        /// DNI del conductor de la funeraria
        /// </summary>
        [MaxLength(20)]
        public string? DNIConductor { get; set; }

        /// <summary>
        /// Placa del vehículo funerario
        /// </summary>
        [MaxLength(20)]
        public string? PlacaVehiculo { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DESTINO Y OBSERVACIONES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Destino final del cuerpo
        /// Ej: "Cementerio El Ángel", "Crematorio Municipal", "Morgue Central"
        /// </summary>
        [MaxLength(200)]
        public string? Destino { get; set; }

        /// <summary>
        /// Observaciones adicionales sobre la salida
        /// Ej: "Retiro urgente por orden judicial", "Familiar presentó copia de DNI"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Indica si hubo alguna irregularidad o incidente durante la salida
        /// </summary>
        public bool IncidenteRegistrado { get; set; } = false;

        /// <summary>
        /// Descripción del incidente (si aplica)
        /// </summary>
        [MaxLength(1000)]
        public string? DetalleIncidente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la salida está completa (todos los campos requeridos llenos)
        /// </summary>
        public bool EstaCompleta()
        {
            bool camposBasicosCompletos = !string.IsNullOrWhiteSpace(ResponsableNombre) &&
                                          !string.IsNullOrWhiteSpace(ResponsableTipoDocumento) &&
                                          !string.IsNullOrWhiteSpace(ResponsableNumeroDocumento) &&
                                          DocumentacionVerificada;

            // Validación específica por tipo de salida
            return TipoSalida switch
            {
                TipoSalida.Familiar => camposBasicosCompletos &&
                                       !string.IsNullOrWhiteSpace(ResponsableParentesco),

                TipoSalida.AutoridadLegal => camposBasicosCompletos &&
                                             !string.IsNullOrWhiteSpace(NumeroAutorizacion) &&
                                             !string.IsNullOrWhiteSpace(EntidadAutorizante),

                TipoSalida.TrasladoHospital => camposBasicosCompletos &&
                                               !string.IsNullOrWhiteSpace(Destino),

                _ => camposBasicosCompletos
            };
        }

        /// <summary>
        /// Verifica si los datos de la funeraria están completos
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
            if (!DocumentacionVerificada)
                return "Documentación no verificada";

            return TipoSalida switch
            {
                TipoSalida.Familiar when string.IsNullOrWhiteSpace(ResponsableParentesco)
                    => "Falta especificar el parentesco del familiar",

                TipoSalida.AutoridadLegal when string.IsNullOrWhiteSpace(NumeroAutorizacion)
                    => "Falta número de autorización oficial",

                TipoSalida.AutoridadLegal when string.IsNullOrWhiteSpace(EntidadAutorizante)
                    => "Falta especificar la entidad autorizante",

                TipoSalida.TrasladoHospital when string.IsNullOrWhiteSpace(Destino)
                    => "Falta especificar el destino del traslado",

                _ => "Documentación completa"
            };
        }
    }
}