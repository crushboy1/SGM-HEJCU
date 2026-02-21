using SisMortuorio.Business.DTOs.ExpedienteLegal;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestionar expedientes legales (casos externos).
    /// MODELO HÍBRIDO: Vigilancia → Admisión → Jefe Guardia
    /// </summary>
    public interface IExpedienteLegalService
    {
        // ═══════════════════════════════════════════════════════════
        // CRUD BÁSICO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Crea un nuevo expediente legal para un caso externo.
        /// Usado por Vigilancia cuando un fallecimiento requiere intervención policial/fiscal.
        /// Estado inicial: EnRegistro
        /// </summary>
        Task<ExpedienteLegalDTO> CrearExpedienteLegalAsync(CreateExpedienteLegalDTO dto);

        /// <summary>
        /// Obtiene un expediente legal por su ID.
        /// Incluye autoridades y documentos asociados.
        /// </summary>
        Task<ExpedienteLegalDTO?> ObtenerPorIdAsync(int expedienteLegalId);

        /// <summary>
        /// Obtiene el expediente legal asociado a un expediente específico.
        /// Devuelve null si el expediente no tiene caso legal.
        /// </summary>
        Task<ExpedienteLegalDTO?> ObtenerPorExpedienteIdAsync(int expedienteId);

        /// <summary>
        /// Actualiza observaciones generales del expediente legal.
        /// Usado por Vigilancia, Admisión o Jefe de Guardia.
        /// </summary>

        /// <summary>
        /// Listar todos.
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ListarTodosAsync();

        Task<ExpedienteLegalDTO> ActualizarObservacionesAsync(int expedienteLegalId, string observaciones, int usuarioId);

        // ═══════════════════════════════════════════════════════════
        // FLUJO HÍBRIDO - TRANSICIONES DE ESTADO
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Marca el expediente legal como listo para validación de Admisión.
        /// Transición: EnRegistro → PendienteValidacionAdmision
        /// </summary>
        /// <param name="dto">DTO con ID del expediente y observaciones</param>
        Task<ExpedienteLegalDTO> MarcarListoAdmisionAsync(MarcarListoAdmisionDTO dto);


        /// <summary>
        /// Admisión valida o rechaza la documentación.
        /// Aprobado: PendienteValidacionAdmision → ValidadoAdmision
        /// Rechazado: PendienteValidacionAdmision → RechazadoAdmision (vuelve a Vigilancia)
        /// </summary>
        Task<ExpedienteLegalDTO> ValidarPorAdmisionAsync(ValidarDocumentacionAdmisionDTO dto);

        /// <summary>
        /// Jefe de Guardia autoriza el levantamiento (firma oficio).
        /// Transición: ValidadoAdmision → AutorizadoJefeGuardia
        /// </summary>
        Task<ExpedienteLegalDTO> AutorizarPorJefeGuardiaAsync(ValidarExpedienteLegalDTO dto);

        // ═══════════════════════════════════════════════════════════
        // GESTIÓN DE AUTORIDADES EXTERNAS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra una nueva autoridad externa en el expediente legal.
        /// Usado por Vigilancia cuando llega policía/fiscal/legista.
        /// Digitaliza el "Cuaderno de Ocurrencias".
        /// </summary>
        Task<AutoridadExternaDTO> RegistrarAutoridadAsync(CreateAutoridadExternaDTO dto);

        /// <summary>
        /// Obtiene todas las autoridades de un expediente legal.
        /// </summary>
        Task<List<AutoridadExternaDTO>> ObtenerAutoridadesAsync(int expedienteLegalId);

        /// <summary>
        /// Elimina una autoridad externa del expediente legal.
        /// Solo permitido si el expediente está en estado EnRegistro.
        /// </summary>
        /// <param name="autoridadId">ID de la autoridad a eliminar</param>
        Task EliminarAutoridadAsync(int autoridadId);
        // ═══════════════════════════════════════════════════════════
        // GESTIÓN DE DOCUMENTOS LEGALES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra un nuevo documento legal escaneado.
        /// Usado por Vigilancia al subir PDFs de documentos físicos.
        /// Digitaliza el "Folder Físico" del Supervisor de Vigilancia.
        /// </summary>
        Task<DocumentoLegalDTO> RegistrarDocumentoAsync(CreateDocumentoLegalDTO dto);

        // <summary>
        /// Actualiza la ruta del archivo físico de un documento legal.
        /// Usado después de subir el PDF al filesystem.
        /// Flujo: 1) Registrar documento → 2) Upload archivo → 3) Actualizar ruta
        /// </summary>
        Task<DocumentoLegalDTO> ActualizarRutaArchivoAsync(
            int expedienteLegalId,
            int documentoId,
            string? rutaArchivo,
            string? nombreArchivo,
            long? tamañoArchivo,
            int usuarioActualizaId);

        /// <summary>
        /// Obtiene todos los documentos de un expediente legal.
        /// </summary>
        Task<List<DocumentoLegalDTO>> ObtenerDocumentosAsync(int expedienteLegalId);

        /// <summary>
        /// Obtiene un documento legal específico por su ID.
        /// Usado para descarga/visualización de archivos individuales.
        /// </summary>
        Task<DocumentoLegalDTO?> ObtenerDocumentoAsync(int expedienteLegalId, int documentoId);

        // ═══════════════════════════════════════════════════════════
        // CONSULTAS POR ESTADO (DASHBOARDS)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene expedientes en registro (siendo completados por Vigilancia).
        /// Estado: EnRegistro
        /// Dashboard: Vigilancia
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerEnRegistroAsync();

        /// <summary>
        /// Obtiene expedientes pendientes de validación por Admisión.
        /// Estado: PendienteValidacionAdmision
        /// Dashboard: Admisión
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerPendientesAdmisionAsync();

        /// <summary>
        /// Obtiene expedientes rechazados por Admisión (requieren corrección).
        /// Estado: RechazadoAdmision
        /// Dashboard: Vigilancia (alertas)
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerRechazadosAdmisionAsync();

        /// <summary>
        /// Obtiene expedientes validados por Admisión, pendientes de firma Jefe Guardia.
        /// Estado: ValidadoAdmision
        /// Dashboard: Jefe de Guardia
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerPendientesJefeGuardiaAsync();

        /// <summary>
        /// Obtiene expedientes autorizados por Jefe Guardia (listos para levantamiento).
        /// Estado: AutorizadoJefeGuardia
        /// Dashboard: Vigilancia Mortuorio (para coordinación de salida)
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerAutorizadosAsync();

        /// <summary>
        /// Obtiene expedientes con documentos incompletos.
        /// Genera alertas cuando faltan documentos requeridos.
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerConDocumentosIncompletosAsync();

        /// <summary>
        /// Obtiene expedientes con alertas de tiempo (>48h sin completar).
        /// Usado para dashboard con semáforos.
        /// </summary>
        Task<List<ExpedienteLegalDTO>> ObtenerConAlertaTiempoAsync();

        // ═══════════════════════════════════════════════════════════
        // HISTORIAL Y AUDITORÍA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el historial completo de un expediente legal.
        /// Incluye: creación, autoridades registradas, documentos subidos, validaciones.
        /// </summary>
        Task<List<HistorialExpedienteLegalDTO>> ObtenerHistorialAsync(int expedienteLegalId);
    }

    // ═══════════════════════════════════════════════════════════
    // DTO ADICIONAL PARA HISTORIAL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DTO con eventos del historial de un expediente legal.
    /// </summary>
    public class HistorialExpedienteLegalDTO
    {
        public DateTime FechaHora { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string? IPOrigen { get; set; }
    }
}