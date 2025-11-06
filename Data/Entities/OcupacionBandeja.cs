using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa el historial completo de ocupaciones de las bandejas del mortuorio.
    /// Cada registro documenta una asignación desde el ingreso hasta la salida.
    /// Sirve como auditoría y fuente de datos para reportes estadísticos.
    /// </summary>
    public class OcupacionBandeja
    {
        /// <summary>
        /// Identificador único del registro de ocupación
        /// </summary>
        [Key]
        public int OcupacionID { get; set; }

        /// <summary>
        /// ID de la bandeja ocupada (FK a Bandeja)
        /// </summary>
        [Required]
        public int BandejaID { get; set; }

        /// <summary>
        /// Navegación a la bandeja física
        /// </summary>
        public Bandeja Bandeja { get; set; } = null!;

        /// <summary>
        /// ID del expediente asignado a la bandeja
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente
        /// </summary>
        public Expediente Expediente { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de ingreso del cuerpo a la bandeja
        /// </summary>
        [Required]
        public DateTime FechaHoraIngreso { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de salida del cuerpo de la bandeja (nullable hasta que se retire)
        /// </summary>
        public DateTime? FechaHoraSalida { get; set; }

        /// <summary>
        /// ID del usuario que asignó el cuerpo a la bandeja (Técnico de Ambulancia)
        /// </summary>
        [Required]
        public int UsuarioAsignadorID { get; set; }

        /// <summary>
        /// Navegación al usuario asignador
        /// </summary>
        public Usuario UsuarioAsignador { get; set; } = null!;

        /// <summary>
        /// ID del usuario que liberó/retiró el cuerpo de la bandeja (Vigilante)
        /// Solo tiene valor cuando FechaHoraSalida != null
        /// </summary>
        public int? UsuarioLiberaID { get; set; }

        /// <summary>
        /// Navegación al usuario que liberó la bandeja
        /// </summary>
        public Usuario? UsuarioLibera { get; set; }

        /// <summary>
        /// Tipo de acción registrada (Asignacion, Liberacion, Reasignacion, etc.)
        /// Por defecto es Asignacion al crear el registro
        /// </summary>
        [Required]
        public AccionBandeja Accion { get; set; } = AccionBandeja.Asignacion;

        /// <summary>
        /// Observaciones sobre la ocupación o eventos especiales
        /// Ej: "Cuerpo movido de B-03 a B-05 por mantenimiento", "Retiro urgente"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS AUXILIARES Y PROPIEDADES CALCULADAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la ocupación aún está activa (cuerpo sigue en la bandeja)
        /// </summary>
        public bool EstaActiva()
        {
            return !FechaHoraSalida.HasValue;
        }

        /// <summary>
        /// Calcula el tiempo total de permanencia en la bandeja
        /// Si aún está ocupada, calcula hasta el momento actual
        /// </summary>
        public TimeSpan TiempoPermanencia()
        {
            var fechaFin = FechaHoraSalida ?? DateTime.Now;
            return fechaFin - FechaHoraIngreso;
        }

        /// <summary>
        /// Registra la salida del cuerpo de la bandeja
        /// </summary>
        public void RegistrarSalida(int usuarioLiberaId, string? observaciones = null)
        {
            if (!EstaActiva())
                throw new InvalidOperationException("Esta ocupación ya tiene registrada una salida");

            FechaHoraSalida = DateTime.Now;
            UsuarioLiberaID = usuarioLiberaId;
            Accion = AccionBandeja.Liberacion;

            if (!string.IsNullOrEmpty(observaciones))
                Observaciones = observaciones;
        }

        /// <summary>
        /// Verifica si la permanencia supera las 24 horas (alerta amarilla)
        /// </summary>
        public bool SuperaAlertaAmarilla()
        {
            return TiempoPermanencia().TotalHours >= 24;
        }

        /// <summary>
        /// Verifica si la permanencia supera las 48 horas (alerta roja)
        /// </summary>
        public bool SuperaAlertaRoja()
        {
            return TiempoPermanencia().TotalHours >= 48;
        }
    }
}