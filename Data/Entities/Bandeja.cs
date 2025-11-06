using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa una bandeja física del mortuorio.
    /// Gestiona el estado actual y la ocupación de cada espacio disponible.
    /// </summary>
    public class Bandeja
    {
        /// <summary>
        /// Identificador único de la bandeja
        /// </summary>
        [Key]
        public int BandejaID { get; set; }

        /// <summary>
        /// Código identificador de la bandeja (B-01, B-02, ..., B-08)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Estado actual de la bandeja
        /// </summary>
        [Required]
        public EstadoBandeja Estado { get; set; } = EstadoBandeja.Disponible;

        /// <summary>
        /// ID del expediente actualmente asignado a esta bandeja (nullable)
        /// Solo tiene valor cuando Estado = Ocupada
        /// </summary>
        public int? ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente actual (si está ocupada)
        /// </summary>
        public Expediente? Expediente { get; set; }

        /// <summary>
        /// ID del usuario que asignó el cuerpo a esta bandeja
        /// </summary>
        public int? UsuarioAsignaID { get; set; }

        /// <summary>
        /// Navegación al usuario que asignó (Técnico de Ambulancia)
        /// </summary>
        public Usuario? UsuarioAsigna { get; set; }

        /// <summary>
        /// Fecha y hora de la última asignación de un cuerpo
        /// </summary>
        public DateTime? FechaHoraAsignacion { get; set; }

        /// <summary>
        /// ID del usuario que liberó la bandeja (último retiro)
        /// </summary>
        public int? UsuarioLiberaID { get; set; }

        /// <summary>
        /// Navegación al usuario que liberó (Vigilante)
        /// </summary>
        public Usuario? UsuarioLibera { get; set; }

        /// <summary>
        /// Fecha y hora de la última liberación
        /// </summary>
        public DateTime? FechaHoraLiberacion { get; set; }

        /// <summary>
        /// Observaciones sobre el estado de la bandeja
        /// Ej: "En mantenimiento por limpieza profunda", "Refrigeración con falla"
        /// </summary>
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Fecha de creación del registro (auditoría)
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última modificación (auditoría)
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// Indica si la bandeja fue dada de baja permanentemente
        /// </summary>
        public bool Eliminado { get; set; } = false;

        /// <summary>
        /// Motivo de la eliminación (si aplica)
        /// </summary>
        [MaxLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la bandeja está disponible para asignar un cuerpo
        /// </summary>
        public bool EstaDisponible()
        {
            return Estado == EstadoBandeja.Disponible && !Eliminado;
        }

        /// <summary>
        /// Verifica si la bandeja está ocupada
        /// </summary>
        public bool EstaOcupada()
        {
            return Estado == EstadoBandeja.Ocupada && ExpedienteID.HasValue;
        }

        /// <summary>
        /// Calcula el tiempo que lleva ocupada la bandeja (si está ocupada)
        /// </summary>
        public TimeSpan? TiempoOcupada()
        {
            if (!EstaOcupada() || !FechaHoraAsignacion.HasValue)
                return null;

            return DateTime.Now - FechaHoraAsignacion.Value;
        }

        /// <summary>
        /// Marca la bandeja como ocupada
        /// </summary>
        public void Ocupar(int expedienteId, int usuarioAsignaId)
        {
            if (!EstaDisponible())
                throw new InvalidOperationException($"La bandeja {Codigo} no está disponible");

            Estado = EstadoBandeja.Ocupada;
            ExpedienteID = expedienteId;
            UsuarioAsignaID = usuarioAsignaId;
            FechaHoraAsignacion = DateTime.Now;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Libera la bandeja (marca como disponible)
        /// </summary>
        public void Liberar(int usuarioLiberaId)
        {
            if (!EstaOcupada())
                throw new InvalidOperationException($"La bandeja {Codigo} no está ocupada");

            Estado = EstadoBandeja.Disponible;
            ExpedienteID = null;
            UsuarioLiberaID = usuarioLiberaId;
            FechaHoraLiberacion = DateTime.Now;
            FechaModificacion = DateTime.Now;

            // Mantener UsuarioAsignaID y FechaHoraAsignacion para historial
        }

        /// <summary>
        /// Marca la bandeja en mantenimiento
        /// </summary>
        public void IniciarMantenimiento(string observaciones)
        {
            if (EstaOcupada())
                throw new InvalidOperationException($"No se puede iniciar mantenimiento. La bandeja {Codigo} está ocupada");

            Estado = EstadoBandeja.Mantenimiento;
            Observaciones = observaciones;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Finaliza el mantenimiento y marca como disponible
        /// </summary>
        public void FinalizarMantenimiento()
        {
            if (Estado != EstadoBandeja.Mantenimiento)
                throw new InvalidOperationException($"La bandeja {Codigo} no está en mantenimiento");

            Estado = EstadoBandeja.Disponible;
            Observaciones = $"Mantenimiento finalizado: {Observaciones}";
            FechaModificacion = DateTime.Now;
        }
    }
}