using SisMortuorio.Data.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Registro de deuda de sangre del fallecido.
    /// Determina si se bloquea retiro según RN-21.
    /// Deuda de sangre NO bloquea retiro si hay compromiso firmado o anulación médica.
    /// </summary>
    public class DeudaSangre
    {
        /// <summary>
        /// Identificador único del registro de deuda de sangre.
        /// </summary>
        [Key]
        public int DeudaSangreID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // RELACIÓN CON EXPEDIENTE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente asociado. Relación 1:1.
        /// </summary>
        [Required]
        public int ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente.
        /// </summary>
        public virtual Expediente Expediente { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════
        // ESTADO DE LA DEUDA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado actual de la deuda de sangre.
        /// SinDeuda: No bloquea retiro.
        /// Liquidado: Compromiso firmado, no bloquea retiro.
        /// Anulado: Anulación médica, no bloquea retiro.
        /// Pendiente: Bloquea retiro del cuerpo.
        /// </summary>
        [Required]
        public EstadoDeudaSangre Estado { get; set; } = EstadoDeudaSangre.Pendiente;

        // ═══════════════════════════════════════════════════════════
        // DETALLE DE LA DEUDA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Descripción detallada de la deuda.
        /// Ejemplos: "3 unidades de sangre O+ pendientes" o "Compromiso firmado el 15/01/2025 - 2 unidades A+".
        /// </summary>
        [MaxLength(500)]
        public string? Detalle { get; set; }

        /// <summary>
        /// Número de unidades de sangre adeudadas.
        /// Útil para estadísticas y reportes.
        /// </summary>
        public int? CantidadUnidades { get; set; }

        /// <summary>
        /// Tipo de sangre utilizado.
        /// Ejemplos: "O+", "A-", "AB+", "B-".
        /// </summary>
        [MaxLength(10)]
        public string? TipoSangre { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ANULACIÓN MÉDICA (CASO EXCEPCIONAL)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Indica si la deuda fue anulada por decisión médica.
        /// </summary>
        public bool AnuladaPorMedico { get; set; }

        /// <summary>
        /// Justificación del médico para anular deuda sin reposición.
        /// Requerido cuando Estado = Anulado.
        /// Ejemplos: "Familiar sin recursos económicos" o "Caso social extremo".
        /// </summary>
        [MaxLength(1000)]
        public string? JustificacionAnulacion { get; set; }

        /// <summary>
        /// Médico de Banco de Sangre que autorizó la anulación.
        /// Solo aplica si Estado = Anulado.
        /// </summary>
        public int? MedicoAnulaID { get; set; }

        /// <summary>
        /// Navegación al médico que autorizó anulación.
        /// </summary>
        public virtual Usuario? MedicoAnula { get; set; }

        /// <summary>
        /// Fecha y hora en que se autorizó la anulación médica.
        /// </summary>
        public DateTime? FechaAnulacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // COMPROMISO DE REPOSICIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Fecha en que se firmó el compromiso de reposición.
        /// Firmantes: Familiar, Médico Banco Sangre, Vigilante.
        /// </summary>
        public DateTime? FechaCompromisoFirmado { get; set; }

        /// <summary>
        /// Fecha en que se marcó como liquidado el compromiso.
        /// Alias de FechaCompromisoFirmado para compatibilidad con Service.
        /// </summary>
        public DateTime? FechaLiquidacion => FechaCompromisoFirmado;

        /// <summary>
        /// Nombre completo del familiar que firmó el compromiso.
        /// </summary>
        [MaxLength(200)]
        public string? NombreFamiliarCompromiso { get; set; }

        /// <summary>
        /// DNI del familiar que firmó el compromiso.
        /// </summary>
        [MaxLength(20)]
        public string? DNIFamiliarCompromiso { get; set; }

        /// <summary>
        /// Ruta del PDF escaneado del compromiso firmado.
        /// Documento físico con 3 firmas: Familiar, Médico, Vigilante.
        /// </summary>
        [MaxLength(500)]
        public string? RutaPDFCompromiso { get; set; }

        // ═══════════════════════════════════════════════════════════
        // AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Usuario que registró inicialmente la deuda.
        /// Personal de Banco de Sangre.
        /// </summary>
        [Required]
        public int UsuarioRegistroID { get; set; }

        /// <summary>
        /// Navegación al usuario que registró.
        /// </summary>
        public virtual Usuario UsuarioRegistro { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de registro inicial.
        /// </summary>
        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>
        /// Último usuario que actualizó el estado.
        /// Puede ser: Banco Sangre (anulación) o Sup. Vigilancia (compromiso).
        /// </summary>
        public int? UsuarioActualizacionID { get; set; }

        /// <summary>
        /// Navegación al usuario que actualizó.
        /// </summary>
        public virtual Usuario? UsuarioActualizacion { get; set; }

        /// <summary>
        /// Fecha y hora de última actualización.
        /// </summary>
        public DateTime? FechaActualizacion { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS DE LÓGICA DE NEGOCIO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica si la deuda de sangre bloquea el retiro del cuerpo.
        /// Retorna TRUE solo si Estado = Pendiente.
        /// </summary>
        /// <returns>true si bloquea retiro, false en caso contrario</returns>
        public bool BloqueaRetiro()
        {
            return Estado == EstadoDeudaSangre.Pendiente;
        }

        /// <summary>
        /// Marca la deuda como resuelta por compromiso de reposición.
        /// Alias de MarcarCompromisoFirmado para compatibilidad con Service.
        /// </summary>
        /// <param name="nombreFamiliar">Nombre completo del familiar</param>
        /// <param name="dniFamiliar">DNI del familiar</param>
        /// <param name="rutaPDF">Ruta del PDF escaneado del compromiso firmado</param>
        /// <param name="usuarioID">Usuario que registra</param>
        /// <param name="observaciones">Observaciones adicionales opcionales</param>
        public void MarcarLiquidada(
            string nombreFamiliar,
            string dniFamiliar,
            string rutaPDF,
            int usuarioID,
            string? observaciones = null)
        {
            if (string.IsNullOrWhiteSpace(nombreFamiliar))
                throw new ArgumentException("Debe proporcionar nombre del familiar", nameof(nombreFamiliar));

            if (string.IsNullOrWhiteSpace(dniFamiliar))
                throw new ArgumentException("Debe proporcionar DNI del familiar", nameof(dniFamiliar));

            if (Estado == EstadoDeudaSangre.Liquidado)
                throw new InvalidOperationException("Esta deuda ya fue liquidada anteriormente");

            if (Estado == EstadoDeudaSangre.Anulado)
                throw new InvalidOperationException("No se puede liquidar una deuda anulada");

            Estado = EstadoDeudaSangre.Liquidado;
            NombreFamiliarCompromiso = nombreFamiliar;
            DNIFamiliarCompromiso = dniFamiliar;
            FechaCompromisoFirmado = DateTime.Now;
            RutaPDFCompromiso = rutaPDF;

            if (!string.IsNullOrEmpty(observaciones))
                Detalle = observaciones;

            UsuarioActualizacionID = usuarioID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Marca la deuda como resuelta por compromiso de reposición.
        /// Método original mantenido para compatibilidad.
        /// </summary>
        /// <param name="nombreFamiliar">Nombre completo del familiar</param>
        /// <param name="dniFamiliar">DNI del familiar</param>
        /// <param name="usuarioID">Usuario que registra</param>
        /// <param name="rutaPDF">Ruta del PDF escaneado del compromiso firmado</param>
        public void MarcarCompromisoFirmado(
            string nombreFamiliar,
            string dniFamiliar,
            int usuarioID,
            string? rutaPDF = null)
        {
            MarcarLiquidada(nombreFamiliar, dniFamiliar, rutaPDF ?? string.Empty, usuarioID);
        }

        /// <summary>
        /// Marca la deuda como anulada por decisión médica.
        /// Alias de AplicarAnulacionMedica para compatibilidad con Service.
        /// </summary>
        /// <param name="medicoID">ID del médico que autoriza</param>
        /// <param name="justificacion">Justificación obligatoria de la anulación</param>
        public void AnularDeuda(int medicoID, string justificacion)
        {
            if (string.IsNullOrWhiteSpace(justificacion))
                throw new ArgumentException("Debe proporcionar justificación de la anulación", nameof(justificacion));

            if (justificacion.Length < 20)
                throw new ArgumentException("La justificación debe tener al menos 20 caracteres", nameof(justificacion));

            if (Estado == EstadoDeudaSangre.Anulado)
                throw new InvalidOperationException("Esta deuda ya fue anulada anteriormente");

            Estado = EstadoDeudaSangre.Anulado;
            AnuladaPorMedico = true;
            JustificacionAnulacion = justificacion;
            MedicoAnulaID = medicoID;
            FechaAnulacion = DateTime.Now;

            UsuarioActualizacionID = medicoID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Marca la deuda como anulada por decisión médica.
        /// Método original mantenido para compatibilidad.
        /// </summary>
        /// <param name="medicoID">ID del médico que autoriza</param>
        /// <param name="justificacion">Justificación obligatoria de la anulación</param>
        public void AplicarAnulacionMedica(int medicoID, string justificacion)
        {
            AnularDeuda(medicoID, justificacion);
        }

        /// <summary>
        /// Marca la deuda como sin deuda.
        /// Paciente no usó sangre durante hospitalización.
        /// </summary>
        /// <param name="usuarioID">Usuario que registra</param>
        public void MarcarSinDeuda(int usuarioID)
        {
            Estado = EstadoDeudaSangre.SinDeuda;
            CantidadUnidades = 0;
            Detalle = "Paciente no utilizó unidades de sangre durante hospitalización";

            UsuarioActualizacionID = usuarioID;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Obtiene el semáforo visual de la deuda.
        /// Formato: "PENDIENTE (5 unidades)" o "LIQUIDADO".
        /// </summary>
        /// <returns>Descripción textual con indicador de estado</returns>
        public string ObtenerSemaforo()
        {
            return Estado switch
            {
                EstadoDeudaSangre.SinDeuda => "SIN DEUDA",
                EstadoDeudaSangre.Pendiente => $"PENDIENTE ({CantidadUnidades ?? 0} unidades)",
                EstadoDeudaSangre.Liquidado => "LIQUIDADO",
                EstadoDeudaSangre.Anulado => "ANULADO POR MEDICO",
                _ => "DESCONOCIDO"
            };
        }

        /// <summary>
        /// Genera un resumen legible del estado de la deuda.
        /// </summary>
        /// <returns>Descripción textual del estado</returns>
        public string GenerarResumenEstado()
        {
            return Estado switch
            {
                EstadoDeudaSangre.SinDeuda => "Sin deuda de sangre",
                EstadoDeudaSangre.Liquidado => $"Compromiso firmado el {FechaCompromisoFirmado:dd/MM/yyyy} - {CantidadUnidades} unidades",
                EstadoDeudaSangre.Anulado => $"Anulación médica autorizada el {FechaAnulacion:dd/MM/yyyy}",
                EstadoDeudaSangre.Pendiente => $"Pendiente - {CantidadUnidades} unidades adeudadas",
                _ => "Estado desconocido"
            };
        }
    }
}