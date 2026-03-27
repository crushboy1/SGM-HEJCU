using SisMortuorio.Data.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Key]
        public int BandejaID { get; set; }

        [Required]
        [MaxLength(10)]
        public string Codigo { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // ESTADO ACTUAL
        // ═══════════════════════════════════════════════════════════

        [Required]
        public EstadoBandeja Estado { get; set; } = EstadoBandeja.Disponible;

        // ═══════════════════════════════════════════════════════════
        // EXPEDIENTE ACTUAL (DESNORMALIZADO)
        // ═══════════════════════════════════════════════════════════

        public int? ExpedienteID { get; set; }
        public virtual Expediente? Expediente { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ÚLTIMA ASIGNACIÓN
        // ═══════════════════════════════════════════════════════════

        public int? UsuarioAsignaID { get; set; }
        public virtual Usuario? UsuarioAsigna { get; set; }
        public DateTime? FechaHoraAsignacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ÚLTIMA LIBERACIÓN
        // ═══════════════════════════════════════════════════════════

        public int? UsuarioLiberaID { get; set; }
        public virtual Usuario? UsuarioLibera { get; set; }
        public DateTime? FechaHoraLiberacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DATOS DE MANTENIMIENTO ACTIVO
        // Campos temporales: se limpian al finalizar el mantenimiento.
        // El historial permanente queda en BandejaHistorial.
        // Nueva migración necesaria: Fase5_MantenimientoBandeja
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Motivo del mantenimiento en curso.
        /// Valores: Limpieza | Reparacion | InspeccionSanitaria | FallaTecnica | Otro
        /// Solo tiene valor cuando Estado = Mantenimiento.
        /// </summary>
        [MaxLength(50)]
        public string? MotivoMantenimiento { get; set; }

        /// <summary>
        /// Descripción libre del mantenimiento (opcional).
        /// Solo tiene valor cuando Estado = Mantenimiento.
        /// </summary>
        [MaxLength(500)]
        public string? DetalleMantenimiento { get; set; }

        /// <summary>
        /// Fecha y hora de inicio del mantenimiento actual.
        /// </summary>
        public DateTime? FechaInicioMantenimiento { get; set; }

        /// <summary>
        /// Fecha y hora estimada de finalización (para planificación del turno).
        /// Opcional — el usuario puede no indicarla.
        /// </summary>
        public DateTime? FechaEstimadaFinMantenimiento { get; set; }

        /// <summary>
        /// Nombre del responsable externo del mantenimiento (texto libre).
        /// Ejemplo: "Tec. García - Servicios Generales"
        /// No es FK — el personal externo no tiene usuario en SGM.
        /// </summary>
        [MaxLength(200)]
        public string? ResponsableMantenimiento { get; set; }

        /// <summary>
        /// ID del usuario SGM que registró el inicio del mantenimiento.
        /// FK → Usuario (Admin / JefeGuardia / VigilanteSupervisor)
        /// </summary>
        public int? UsuarioRegistraMantenimientoID { get; set; }

        [ForeignKey(nameof(UsuarioRegistraMantenimientoID))]
        public virtual Usuario? UsuarioRegistraMantenimiento { get; set; }

        // ═══════════════════════════════════════════════════════════
        // OBSERVACIONES GENERALES (campo legacy — se mantiene)
        // ═══════════════════════════════════════════════════════════

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public bool Eliminado { get; set; } = false;

        [MaxLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIONES
        // ═══════════════════════════════════════════════════════════

        public virtual ICollection<BandejaHistorial> Historial { get; set; } =
            new List<BandejaHistorial>();

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE VALIDACIÓN Y LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        public bool EstaDisponible() => Estado == EstadoBandeja.Disponible && !Eliminado;
        public bool EstaOcupada() => Estado == EstadoBandeja.Ocupada && ExpedienteID.HasValue;
        public bool EstaEnMantenimiento() => Estado == EstadoBandeja.Mantenimiento;

        public TimeSpan? TiempoOcupada()
        {
            if (!EstaOcupada() || !FechaHoraAsignacion.HasValue) return null;
            return DateTime.Now - FechaHoraAsignacion.Value;
        }

        public bool SuperaAlertaAmarilla()
        {
            var t = TiempoOcupada();
            return t.HasValue && t.Value.TotalHours >= 24;
        }

        public bool SuperaAlertaRoja()
        {
            var t = TiempoOcupada();
            return t.HasValue && t.Value.TotalHours >= 48;
        }

        public void Ocupar(int expedienteId, int usuarioAsignaId)
        {
            if (!EstaDisponible())
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está disponible. Estado actual: {Estado}");

            Estado = EstadoBandeja.Ocupada;
            ExpedienteID = expedienteId;
            UsuarioAsignaID = usuarioAsignaId;
            FechaHoraAsignacion = DateTime.Now;
            FechaModificacion = DateTime.Now;
        }

        public void Liberar(int usuarioLiberaId)
        {
            if (!EstaOcupada())
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está ocupada. No se puede liberar.");

            Estado = EstadoBandeja.Disponible;
            ExpedienteID = null;
            UsuarioLiberaID = usuarioLiberaId;
            FechaHoraLiberacion = DateTime.Now;
            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Inicia el mantenimiento de la bandeja con datos completos del modal SGM.
        /// </summary>
        /// <param name="motivo">Motivo (Limpieza | Reparacion | InspeccionSanitaria | FallaTecnica | Otro)</param>
        /// <param name="detalle">Descripción libre opcional</param>
        /// <param name="fechaInicio">Fecha inicio — si null, usa DateTime.Now</param>
        /// <param name="fechaEstimadaFin">Fecha estimada fin (opcional)</param>
        /// <param name="responsableExterno">Nombre del responsable externo (opcional)</param>
        /// <param name="usuarioRegistraId">ID del usuario SGM que registra</param>
        /// <exception cref="InvalidOperationException">Si la bandeja está ocupada</exception>
        /// <exception cref="ArgumentException">Si motivo está vacío</exception>
        public void IniciarMantenimiento(
            string motivo,
            string? detalle,
            DateTime? fechaInicio,
            DateTime? fechaEstimadaFin,
            string? responsableExterno,
            int usuarioRegistraId)
        {
            if (EstaOcupada())
                throw new InvalidOperationException(
                    $"No se puede iniciar mantenimiento. La bandeja {Codigo} está ocupada " +
                    $"con expediente {ExpedienteID}.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException(
                    "Debe indicar el motivo del mantenimiento.", nameof(motivo));

            Estado = EstadoBandeja.Mantenimiento;
            MotivoMantenimiento = motivo.Trim();
            DetalleMantenimiento = detalle?.Trim();
            FechaInicioMantenimiento = fechaInicio ?? DateTime.Now;
            FechaEstimadaFinMantenimiento = fechaEstimadaFin;
            ResponsableMantenimiento = responsableExterno?.Trim();
            UsuarioRegistraMantenimientoID = usuarioRegistraId;

            // Actualizar campo legacy para compatibilidad con código existente
            Observaciones = $"[{motivo}] {detalle}".Trim().TrimEnd(']').Trim();

            FechaModificacion = DateTime.Now;
        }

        /// <summary>
        /// Finaliza el mantenimiento y deja la bandeja Disponible.
        /// Limpia los campos temporales de mantenimiento.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si no está en mantenimiento</exception>
        public void FinalizarMantenimiento()
        {
            if (Estado != EstadoBandeja.Mantenimiento)
                throw new InvalidOperationException(
                    $"La bandeja {Codigo} no está en mantenimiento. Estado actual: {Estado}");

            // Guardar resumen en campo legacy antes de limpiar
            Observaciones = $"Mantenimiento finalizado el {DateTime.Now:dd/MM/yyyy HH:mm}. " +
                            $"Motivo: {MotivoMantenimiento}. {DetalleMantenimiento}".Trim();

            Estado = EstadoBandeja.Disponible;

            // Limpiar campos temporales de mantenimiento
            MotivoMantenimiento = null;
            DetalleMantenimiento = null;
            FechaInicioMantenimiento = null;
            FechaEstimadaFinMantenimiento = null;
            ResponsableMantenimiento = null;
            UsuarioRegistraMantenimientoID = null;

            FechaModificacion = DateTime.Now;
        }

        public void MarcarFueraDeServicio(string motivo)
        {
            if (EstaOcupada())
                throw new InvalidOperationException(
                    $"No se puede marcar fuera de servicio. La bandeja {Codigo} está ocupada.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException(
                    "Debe proporcionar el motivo.", nameof(motivo));

            Estado = EstadoBandeja.FueraDeServicio;
            Observaciones = motivo;
            FechaModificacion = DateTime.Now;
        }

        public string GenerarResumen()
        {
            string estadoTexto = Estado switch
            {
                EstadoBandeja.Disponible => "Disponible",
                EstadoBandeja.Ocupada => $"Ocupada (Expediente: {ExpedienteID})",
                EstadoBandeja.Mantenimiento => $"En Mantenimiento ({MotivoMantenimiento})",
                EstadoBandeja.FueraDeServicio => "Fuera de Servicio",
                _ => "Estado Desconocido"
            };

            if (EstaOcupada() && TiempoOcupada().HasValue)
            {
                var horas = TiempoOcupada()!.Value.TotalHours;
                var alerta = horas >= 48 ? " [URGENTE]" : horas >= 24 ? " [ATENCIÓN]" : "";
                estadoTexto += $" - {horas:F1}h{alerta}";
            }

            return $"{Codigo}: {estadoTexto}";
        }
    }
}