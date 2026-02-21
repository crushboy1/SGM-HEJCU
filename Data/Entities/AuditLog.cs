using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SisMortuorio.Data.Entities
{
    /// <summary>
    /// Registro de auditoría completo del sistema
    /// Documenta todas las acciones críticas realizadas por usuarios
    /// Retención: 5 años (cumplimiento normativo hospitalario)
    /// 
    /// ACCIONES AUDITADAS:
    /// - Creación/Modificación/Eliminación de expedientes
    /// - Transferencias de custodia
    /// - Asignación/Liberación de bandejas
    /// - Cambios en deudas económicas/sangre
    /// - Validaciones de expedientes legales
    /// - Autorizaciones de retiro
    /// - Registros de salida
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Identificador único del log de auditoría
        /// </summary>
        [Key]
        public int LogID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CONTEXTO DE LA ACCIÓN
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// ID del expediente afectado (nullable)
        /// Puede ser null para acciones que no afectan expedientes específicos
        /// Ejemplos sin expediente: login fallido, cambio de contraseña, etc.
        /// </summary>
        public int? ExpedienteID { get; set; }

        /// <summary>
        /// Navegación al expediente afectado
        /// </summary>
        public virtual Expediente? Expediente { get; set; }

        /// <summary>
        /// Usuario que realizó la acción
        /// </summary>
        [Required]
        public int UsuarioID { get; set; }

        /// <summary>
        /// Navegación al usuario que ejecutó la acción
        /// </summary>
        public virtual Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Módulo del sistema donde se realizó la acción
        /// Ejemplos: "Expedientes", "Custodia", "Bandejas", "DeudaSangre", 
        ///           "DeudaEconomica", "VerificacionMortuorio", "Salida",
        ///           "ExpedienteLegal", "AutoridadesExternas"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Modulo { get; set; } = string.Empty;

        /// <summary>
        /// Acción específica realizada
        /// Ejemplos: "Crear", "Actualizar", "Eliminar", "AsignarBandeja",
        ///           "LiberarBandeja", "TransferirCustodia", "ValidarExpediente",
        ///           "AutorizarRetiro", "RegistrarSalida", "AplicarExoneracion",
        ///           "MarcarLiquidado", "AdjuntarDocumento"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Accion { get; set; } = string.Empty;

        // ═══════════════════════════════════════════════════════════
        // DATOS DEL CAMBIO (BEFORE/AFTER)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Estado anterior de los datos (formato JSON)
        /// Serialización del objeto ANTES de la modificación
        /// null para acciones "Crear" (no hay estado previo)
        /// </summary>
        public string? DatosAntes { get; set; }

        /// <summary>
        /// Estado posterior de los datos (formato JSON)
        /// Serialización del objeto DESPUÉS de la modificación
        /// null para acciones "Eliminar" (ya no existe)
        /// </summary>
        public string? DatosDespues { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CONTEXTO TÉCNICO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Dirección IP desde donde se realizó la acción
        /// Útil para auditoría de seguridad y rastreo de accesos
        /// Formato: IPv4 (xxx.xxx.xxx.xxx) o IPv6
        /// </summary>
        [MaxLength(50)]
        public string? IPOrigen { get; set; }

        /// <summary>
        /// Fecha y hora exacta de la acción
        /// Se registra automáticamente al crear el log
        /// </summary>
        [Required]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        /// <summary>
        /// Observaciones adicionales sobre la acción
        /// Ejemplos:
        /// - "Bandeja reasignada por mantenimiento urgente"
        /// - "Expediente corregido por error en DNI"
        /// - "Retiro autorizado por Jefe de Guardia - Caso urgente"
        /// - "Exoneración aplicada por situación de extrema pobreza"
        /// </summary>
        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        // ═══════════════════════════════════════════════════════════
        // MÉTODOS HELPER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Serializa un objeto a JSON para guardarlo en DatosAntes o DatosDespues
        /// Usa System.Text.Json con opciones optimizadas
        /// </summary>
        /// <param name="objeto">Objeto a serializar</param>
        /// <returns>String JSON del objeto</returns>
        public static string SerializarObjeto(object objeto)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false, // Compacto para ahorrar espacio
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(objeto, options);
        }

        /// <summary>
        /// Deserializa el JSON de DatosAntes o DatosDespues a un objeto tipado
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a deserializar</typeparam>
        /// <param name="json">String JSON a deserializar</param>
        /// <returns>Objeto deserializado o null si el JSON es inválido</returns>
        public static T? DeserializarObjeto<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Crea un log de auditoría para una acción de CREACIÓN
        /// </summary>
        /// <param name="modulo">Módulo del sistema</param>
        /// <param name="usuarioId">ID del usuario que crea</param>
        /// <param name="objetoCreado">Objeto creado (se serializa a DatosDespues)</param>
        /// <param name="expedienteId">ID del expediente (opcional)</param>
        /// <param name="ipOrigen">IP del usuario (opcional)</param>
        /// <param name="observaciones">Observaciones (opcional)</param>
        /// <returns>Instancia de AuditLog lista para guardar</returns>
        public static AuditLog CrearLogCreacion(
            string modulo,
            int usuarioId,
            object objetoCreado,
            int? expedienteId = null,
            string? ipOrigen = null,
            string? observaciones = null)
        {
            return new AuditLog
            {
                Modulo = modulo,
                Accion = "Crear",
                UsuarioID = usuarioId,
                ExpedienteID = expedienteId,
                DatosAntes = null, // No hay estado previo
                DatosDespues = SerializarObjeto(objetoCreado),
                IPOrigen = ipOrigen,
                Observaciones = observaciones,
                FechaHora = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un log de auditoría para una acción de ACTUALIZACIÓN
        /// </summary>
        /// <param name="modulo">Módulo del sistema</param>
        /// <param name="usuarioId">ID del usuario que actualiza</param>
        /// <param name="objetoAntes">Objeto ANTES del cambio (se serializa a DatosAntes)</param>
        /// <param name="objetoDespues">Objeto DESPUÉS del cambio (se serializa a DatosDespues)</param>
        /// <param name="expedienteId">ID del expediente (opcional)</param>
        /// <param name="ipOrigen">IP del usuario (opcional)</param>
        /// <param name="observaciones">Observaciones (opcional)</param>
        /// <returns>Instancia de AuditLog lista para guardar</returns>
        public static AuditLog CrearLogActualizacion(
            string modulo,
            int usuarioId,
            object objetoAntes,
            object objetoDespues,
            int? expedienteId = null,
            string? ipOrigen = null,
            string? observaciones = null)
        {
            return new AuditLog
            {
                Modulo = modulo,
                Accion = "Actualizar",
                UsuarioID = usuarioId,
                ExpedienteID = expedienteId,
                DatosAntes = SerializarObjeto(objetoAntes),
                DatosDespues = SerializarObjeto(objetoDespues),
                IPOrigen = ipOrigen,
                Observaciones = observaciones,
                FechaHora = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un log de auditoría para una acción de ELIMINACIÓN
        /// </summary>
        /// <param name="modulo">Módulo del sistema</param>
        /// <param name="usuarioId">ID del usuario que elimina</param>
        /// <param name="objetoEliminado">Objeto ANTES de eliminar (se serializa a DatosAntes)</param>
        /// <param name="expedienteId">ID del expediente (opcional)</param>
        /// <param name="ipOrigen">IP del usuario (opcional)</param>
        /// <param name="motivo">Motivo de la eliminación (obligatorio)</param>
        /// <returns>Instancia de AuditLog lista para guardar</returns>
        public static AuditLog CrearLogEliminacion(
            string modulo,
            int usuarioId,
            object objetoEliminado,
            string motivo,
            int? expedienteId = null,
            string? ipOrigen = null)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Debe proporcionar motivo de eliminación", nameof(motivo));

            return new AuditLog
            {
                Modulo = modulo,
                Accion = "Eliminar",
                UsuarioID = usuarioId,
                ExpedienteID = expedienteId,
                DatosAntes = SerializarObjeto(objetoEliminado),
                DatosDespues = null, // Ya no existe
                IPOrigen = ipOrigen,
                Observaciones = $"Motivo: {motivo}",
                FechaHora = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un log de auditoría para una acción PERSONALIZADA
        /// </summary>
        /// <param name="modulo">Módulo del sistema</param>
        /// <param name="accion">Acción específica (ej: "AsignarBandeja", "AutorizarRetiro")</param>
        /// <param name="usuarioId">ID del usuario que ejecuta la acción</param>
        /// <param name="expedienteId">ID del expediente (opcional)</param>
        /// <param name="datosAntes">Datos antes del cambio (opcional)</param>
        /// <param name="datosDespues">Datos después del cambio (opcional)</param>
        /// <param name="ipOrigen">IP del usuario (opcional)</param>
        /// <param name="observaciones">Observaciones (opcional)</param>
        /// <returns>Instancia de AuditLog lista para guardar</returns>
        public static AuditLog CrearLogPersonalizado(
            string modulo,
            string accion,
            int usuarioId,
            int? expedienteId = null,
            object? datosAntes = null,
            object? datosDespues = null,
            string? ipOrigen = null,
            string? observaciones = null)
        {
            return new AuditLog
            {
                Modulo = modulo,
                Accion = accion,
                UsuarioID = usuarioId,
                ExpedienteID = expedienteId,
                DatosAntes = datosAntes != null ? SerializarObjeto(datosAntes) : null,
                DatosDespues = datosDespues != null ? SerializarObjeto(datosDespues) : null,
                IPOrigen = ipOrigen,
                Observaciones = observaciones,
                FechaHora = DateTime.Now
            };
        }
    }
}