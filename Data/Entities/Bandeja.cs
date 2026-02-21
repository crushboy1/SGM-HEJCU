using SisMortuorio.Data.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Representa una bandeja física del mortuorio.
    /// Gestiona el estado actual y la ocupación de cada espacio disponible.
    /// 
    /// CONTEXTO DE NEGOCIO:
    /// - Total de bandejas físicas: 8 (B-01 a B-08)
    /// - Cada bandeja puede estar: Disponible, Ocupada, Mantenimiento, FueraDeServicio
    /// - Se registra expediente actual (desnormalizado) para queries rápidas
    /// - Historial completo se mantiene en BandejaHistorial (auditoría)
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
        /// Único en el sistema
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Codigo { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // ESTADO ACTUAL DE LA BANDEJA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual de la bandeja
        /// Disponible | Ocupada | Mantenimiento | FueraDeServicio
        /// </summary>
        [Required]
        public EstadoBandeja Estado { get; set; } = EstadoBandeja.Disponible;

        // ═══════════════════════════════════════════════════════════
        // EXPEDIENTE ACTUAL (DESNORMALIZADO)
        // Duplica info de BandejaHistorial para queries rápidas
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente actualmente asignado a esta bandeja (nullable)
        /// Solo tiene valor cuando Estado = Ocupada
        /// 
        /// IMPORTANTE: Campo desnormalizado para queries rápidas
        /// También existe en BandejaHistorial (fuente de verdad)
        /// Se actualiza cuando:
        /// - Se asigna bandeja → ExpedienteID = X
        /// - Se libera bandeja → ExpedienteID = null
        /// </summary>
        public int? ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente actual (si está ocupada)
        /// </summary>
        public virtual Expediente? Expediente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ÚLTIMA ASIGNACIÓN 
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del usuario que asignó el último cuerpo a esta bandeja
        /// Se mantiene incluso después de liberar (para auditoría rápida)
        /// </summary>
        public int? UsuarioAsignaID { get; set; }

        /// <summary>
        /// Navegación al usuario que asignó (Técnico de Ambulancia)
        /// </summary>
        public virtual Usuario? UsuarioAsigna { get; set; }

        /// <summary>
        /// Fecha y hora de la última asignación de un cuerpo
        /// Se mantiene incluso después de liberar (para auditoría rápida)
        /// </summary>
        public DateTime? FechaHoraAsignacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ÚLTIMA LIBERACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del usuario que liberó la bandeja (último retiro)
        /// </summary>
        public int? UsuarioLiberaID { get; set; }

        /// <summary>
        /// Navegación al usuario que liberó (Vigilante)
        /// </summary>
        public virtual Usuario? UsuarioLibera { get; set; }

        /// <summary>
        /// Fecha y hora de la última liberación
        /// </summary>
        public DateTime? FechaHoraLiberacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // OBSERVACIONES Y ESTADO FÍSICO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Observaciones sobre el estado de la bandeja
        /// Ejemplos:
        /// - "En mantenimiento por limpieza profunda"
        /// - "Refrigeración con falla - Reparación programada"
        /// - "Fuera de servicio por daño estructural"
        /// </summary>
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Fecha de creación del registro (auditoría)
        /// Se crea una sola vez al inicializar el sistema (seeder)
        /// </summary>
        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última modificación (auditoría)
        /// Se actualiza en cada cambio de estado
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// Indica si la bandeja fue dada de baja permanentemente
        /// Soft delete - No se elimina físicamente de la BD
        /// </summary>
        public bool Eliminado { get; set; } = false;

        /// <summary>
        /// Motivo de la eliminación (si aplica)
        /// Ej: "Bandeja retirada por deterioro irreparable"
        /// </summary>
        [MaxLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIONES DE NAVEGACIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Historial completo de ocupaciones de esta bandeja
        /// Relación 1:N con BandejaHistorial
        /// Contiene todos los expedientes que han pasado por esta bandeja
        /// </summary>
        public virtual ICollection<BandejaHistorial> Historial { get; set; } = new List<BandejaHistorial>();

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la bandeja está disponible para asignar un cuerpo
        /// </summary>
        /// <returns>true si está disponible y no eliminada, false en caso contrario</returns>
        public bool EstaDisponible()
        {
            return Estado == EstadoBandeja.Disponible && !Eliminado;
        }

        /// <summary>
        /// Verifica si la bandeja está ocupada
        /// </summary>
        /// <returns>true si está ocupada y tiene expediente asignado, false en caso contrario</returns>
        public bool EstaOcupada()
        {
            return Estado == EstadoBandeja.Ocupada && ExpedienteID.HasValue;
        }

        /// <summary>
        /// Verifica si la bandeja está en mantenimiento
        /// </summary>
        /// <returns>true si está en mantenimiento, false en caso contrario</returns>
        public bool EstaEnMantenimiento()
        {
            return Estado == EstadoBandeja.Mantenimiento;
        }

        /// <summary>
        /// Calcula el tiempo que lleva ocupada la bandeja (si está ocupada)
        /// </summary>
        /// <returns>TimeSpan desde FechaHoraAsignacion hasta ahora, o null si no está ocupada</returns>
        public TimeSpan? TiempoOcupada()
        {
            if (!EstaOcupada() || !FechaHoraAsignacion.HasValue)
                return null;

            return DateTime.Now - FechaHoraAsignacion.Value;
        }

        /// <summary>
        /// Verifica si el cuerpo lleva más de 24 horas en la bandeja (Alerta Amarilla)
        /// Usado para Dashboard y alertas automáticas
        /// </summary>
        /// <returns>true si supera 24 horas, false en caso contrario</returns>
        public bool SuperaAlertaAmarilla()
        {
            var tiempo = TiempoOcupada();
            return tiempo.HasValue && tiempo.Value.TotalHours >= 24;
        }

        /// <summary>
        /// Verifica si el cuerpo lleva más de 48 horas en la bandeja (Alerta Roja)
        /// Requiere coordinación urgente con Admisión
        /// </summary>
        /// <returns>true si supera 48 horas, false en caso contrario</returns>
        public bool SuperaAlertaRoja()
        {
            var tiempo = TiempoOcupada();
            return tiempo.HasValue && tiempo.Value.TotalHours >= 48;
        }

        /// <summary>
        /// Marca la bandeja como ocupada
        /// IMPORTANTE: Este método solo actualiza el estado de la bandeja
        /// El Service layer debe crear el registro en BandejaHistorial por separado
        /// </summary>
        /// <param name="expedienteId">ID del expediente a asignar</param>
        /// <param name="usuarioAsignaId">ID del usuario que asigna (Técnico Ambulancia)</param>
        /// <exception cref="InvalidOperationException">Si la bandeja no está disponible</exception>
        public void Ocupar(int expedienteId, int usuarioAsignaId)
        {
            if (!EstaDisponible())
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está disponible. Estado actual: {Estado}"
                );

            Estado = EstadoBandeja.Ocupada;
            ExpedienteID = expedienteId;
            UsuarioAsignaID = usuarioAsignaId;
            FechaHoraAsignacion = DateTime.Now;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Libera la bandeja (marca como disponible)
        /// IMPORTANTE: Este método solo actualiza el estado de la bandeja
        /// El Service layer debe actualizar BandejaHistorial.FechaHoraSalida por separado
        /// </summary>
        /// <param name="usuarioLiberaId">ID del usuario que libera (Vigilante)</param>
        /// <exception cref="InvalidOperationException">Si la bandeja no está ocupada</exception>
        public void Liberar(int usuarioLiberaId)
        {
            if (!EstaOcupada())
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está ocupada. No se puede liberar."
                );

            Estado = EstadoBandeja.Disponible;
            ExpedienteID = null;
            UsuarioLiberaID = usuarioLiberaId;
            FechaHoraLiberacion = DateTime.Now;
            FechaModificacion = DateTime.Now;

            // Mantener UsuarioAsignaID y FechaHoraAsignacion para auditoría rápida
            // El historial completo está en BandejaHistorial
        }

        /// <summary>
        /// Marca la bandeja en mantenimiento
        /// </summary>
        /// <param name="observaciones">Motivo del mantenimiento (obligatorio)</param>
        /// <exception cref="InvalidOperationException">Si la bandeja está ocupada</exception>
        /// <exception cref="ArgumentException">Si no se proporcionan observaciones</exception>
        public void IniciarMantenimiento(string observaciones)
        {
            if (EstaOcupada())
                throw new InvalidOperationException(
                    $"No se puede iniciar mantenimiento. La bandeja {Codigo} está ocupada con expediente {ExpedienteID}"
                );

            if (string.IsNullOrWhiteSpace(observaciones))
                throw new ArgumentException(
                    "Debe proporcionar observaciones sobre el motivo del mantenimiento",
                    nameof(observaciones)
                );

            Estado = EstadoBandeja.Mantenimiento;
            Observaciones = observaciones;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Finaliza el mantenimiento y marca como disponible
        /// </summary>
        /// <exception cref="InvalidOperationException">Si la bandeja no está en mantenimiento</exception>
        public void FinalizarMantenimiento()
        {
            if (Estado != EstadoBandeja.Mantenimiento)
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está en mantenimiento. Estado actual: {Estado}"
                );

            Estado = EstadoBandeja.Disponible;
            Observaciones = $"Mantenimiento finalizado el {DateTime.Now:dd/MM/yyyy HH:mm}. Motivo previo: {Observaciones}";
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Marca la bandeja como fuera de servicio (daño grave)
        /// </summary>
        /// <param name="motivo">Motivo de la baja (obligatorio)</param>
        /// <exception cref="InvalidOperationException">Si la bandeja está ocupada</exception>
        /// <exception cref="ArgumentException">Si no se proporciona motivo</exception>
        public void MarcarFueraDeServicio(string motivo)
        {
            if (EstaOcupada())
                throw new InvalidOperationException(
                    $"No se puede marcar fuera de servicio. La bandeja {Codigo} está ocupada"
                );

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException(
                    "Debe proporcionar el motivo por el cual se marca fuera de servicio",
                    nameof(motivo)
                );

            Estado = EstadoBandeja.FueraDeServicio;
            Observaciones = motivo;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Genera un resumen legible del estado de la bandeja
        /// Útil para Dashboard y reportes
        /// </summary>
        /// <returns>Descripción textual del estado actual</returns>
        public string GenerarResumen()
        {
            string estadoTexto = Estado switch
            {
                EstadoBandeja.Disponible => "🟢Disponible",
                EstadoBandeja.Ocupada => $" Ocupada (Expediente: {ExpedienteID})",
                EstadoBandeja.Mantenimiento => "🟡 En Mantenimiento",
                EstadoBandeja.FueraDeServicio => " Fuera de Servicio",
                _ => " Estado Desconocido"
            };

            if (EstaOcupada() && TiempoOcupada().HasValue)
            {
                var horas = TiempoOcupada()!.Value.TotalHours;
                string alerta = horas >= 48 ? "URGENTE" : horas >= 24 ? " ATENCIÓN" : "";
                estadoTexto += $" - {horas:F1}h {alerta}";
            }

            return $"{Codigo}: {estadoTexto}";
        }
    }
}