using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio para gestión de documentos digitalizados del expediente.
    /// Reemplaza el proceso manual de "juegos de copias físicas".
    /// RESPONSABILIDADES:
    /// - Subir y almacenar archivos via LocalFileStorageService
    /// - Verificar/rechazar documentos contra originales físicos
    /// - Calcular DocumentacionCompleta según TipoSalida del ActaRetiro
    /// </summary>
    public class DocumentoExpedienteService : IDocumentoExpedienteService
    {
        private readonly IDocumentoExpedienteRepository _documentoRepository;
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly IActaRetiroRepository _actaRetiroRepository;
        private readonly ILocalFileStorageService _fileStorage;
        private readonly ILogger<DocumentoExpedienteService> _logger;

        // Configuración de validación de archivos
        private const long MaxTamañoBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] ExtensionesPermitidas = [".pdf", ".jpg", ".jpeg", ".png"];
        private const string CarpetaDocumentos = "documentos-expedientes";

        public DocumentoExpedienteService(
            IDocumentoExpedienteRepository documentoRepository,
            IExpedienteRepository expedienteRepository,
            IActaRetiroRepository actaRetiroRepository,
            ILocalFileStorageService fileStorage,
            ILogger<DocumentoExpedienteService> logger)
        {
            _documentoRepository = documentoRepository;
            _expedienteRepository = expedienteRepository;
            _actaRetiroRepository = actaRetiroRepository;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ===================================================================
        // CONSULTAS
        // ===================================================================

        /// <inheritdoc/>
        public async Task<List<DocumentoExpedienteDTO>> GetByExpedienteIdAsync(int expedienteId)
        {
            var documentos = await _documentoRepository.GetByExpedienteIdAsync(expedienteId);
            return documentos.Select(MapToDTO).ToList();
        }

        /// <inheritdoc/>
        public async Task<DocumentoExpedienteDTO?> GetByIdAsync(int documentoId)
        {
            var documento = await _documentoRepository.GetByIdAsync(documentoId);
            return documento is null ? null : MapToDTO(documento);
        }

        /// <inheritdoc/>
        public async Task<ResumenDocumentosDTO> GetResumenAsync(int expedienteId)
        {
            var documentos = await _documentoRepository.GetByExpedienteIdAsync(expedienteId);
            var acta = await _actaRetiroRepository.GetByExpedienteIdAsync(expedienteId);
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId)
                ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado.");

            // TipoSalida efectivo: del acta si existe, del preliminar si no
            var tipoSalidaEfectivo = acta is not null
                ? acta.TipoSalida
                : expediente.TipoSalidaPreliminar;

            var resumen = new ResumenDocumentosDTO
            {
                ExpedienteID = expedienteId,
                TipoSalida = tipoSalidaEfectivo,
                Documentos = documentos.Select(MapToDTO).ToList()
            };

            resumen.DNIFamiliar = BuildEstadoItem(documentos, TipoDocumentoExpediente.DNI_Familiar);
            resumen.DNIFallecido = BuildEstadoItem(documentos, TipoDocumentoExpediente.DNI_Fallecido);
            resumen.CertificadoDefuncion = BuildEstadoItem(documentos, TipoDocumentoExpediente.CertificadoDefuncion);
            resumen.OficioLegal = BuildEstadoItem(documentos, TipoDocumentoExpediente.OficioLegal);

            resumen.DocumentacionCompleta = tipoSalidaEfectivo is not null
                ? EsDocumentacionCompleta(documentos, tipoSalidaEfectivo.Value)
                : EsDocumentacionCompletaSinActa(documentos);

            return resumen;
        }

        // ===================================================================
        // SUBIDA DE DOCUMENTOS
        // ===================================================================

        /// <inheritdoc/>
        public async Task<DocumentoExpedienteDTO> SubirDocumentoAsync(SubirDocumentoDTO dto, IFormFile archivo)
        {
            // Validar expediente existe
            var expediente = await _expedienteRepository.GetByIdAsync(dto.ExpedienteID)
                ?? throw new KeyNotFoundException($"Expediente con ID {dto.ExpedienteID} no encontrado.");

            // Validar archivo
            ValidarArchivo(archivo);

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            // Generar nombre único para evitar colisiones
            var nombreArchivo = GenerarNombreArchivo(
                expediente.CodigoExpediente,
                dto.TipoDocumento,
                extension);

            // Guardar en filesystem
            var rutaRelativa = await _fileStorage.GuardarArchivoAsync(
                archivo,
                CarpetaDocumentos,
                nombreArchivo);

            // Crear entidad
            var documento = new DocumentoExpediente
            {
                ExpedienteID = dto.ExpedienteID,
                TipoDocumento = dto.TipoDocumento,
                Estado = EstadoDocumentoExpediente.PendienteVerificacion,
                RutaArchivo = rutaRelativa,
                NombreArchivo = archivo.FileName,
                ExtensionArchivo = extension,
                TamañoBytes = archivo.Length,
                UsuarioSubioID = dto.UsuarioSubioID,
                FechaHoraSubida = DateTime.Now,
                Observaciones = dto.Observaciones
            };

            var creado = await _documentoRepository.CreateAsync(documento);

            _logger.LogInformation(
                "Documento subido - ExpedienteID: {ExpedienteID}, Tipo: {Tipo}, Usuario: {UsuarioID}",
                dto.ExpedienteID, dto.TipoDocumento, dto.UsuarioSubioID);

            return MapToDTO(creado);
        }

        // ===================================================================
        // VERIFICACIÓN / RECHAZO
        // ===================================================================

        /// <inheritdoc/>
        public async Task<DocumentoExpedienteDTO> VerificarDocumentoAsync(VerificarDocumentoDTO dto)
        {
            var documento = await _documentoRepository.GetByIdAsync(dto.DocumentoExpedienteID)
                ?? throw new KeyNotFoundException($"Documento con ID {dto.DocumentoExpedienteID} no encontrado.");

            if (documento.Estado == EstadoDocumentoExpediente.Verificado)
                throw new InvalidOperationException("El documento ya fue verificado anteriormente.");

            documento.MarcarVerificado(dto.UsuarioVerificoID, dto.Observaciones);

            var actualizado = await _documentoRepository.UpdateAsync(documento);

            // Recalcular DocumentacionCompleta del expediente
            await VerificarDocumentacionCompletaAsync(documento.ExpedienteID);

            _logger.LogInformation(
                "Documento verificado - DocumentoID: {ID}, ExpedienteID: {ExpedienteID}, Usuario: {UsuarioID}",
                dto.DocumentoExpedienteID, documento.ExpedienteID, dto.UsuarioVerificoID);

            return MapToDTO(actualizado);
        }

        /// <inheritdoc/>
        public async Task<DocumentoExpedienteDTO> RechazarDocumentoAsync(RechazarDocumentoDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo))
                throw new ArgumentException("El motivo del rechazo es obligatorio.");

            var documento = await _documentoRepository.GetByIdAsync(dto.DocumentoExpedienteID)
                ?? throw new KeyNotFoundException($"Documento con ID {dto.DocumentoExpedienteID} no encontrado.");

            if (documento.Estado == EstadoDocumentoExpediente.Verificado)
                throw new InvalidOperationException(
                    "No se puede rechazar un documento ya verificado. Contacte al supervisor.");

            documento.MarcarRechazado(dto.UsuarioVerificoID, dto.Motivo);

            var actualizado = await _documentoRepository.UpdateAsync(documento);

            // Recalcular — puede que DocumentacionCompleta pase a false
            await VerificarDocumentacionCompletaAsync(documento.ExpedienteID);

            _logger.LogInformation(
                "Documento rechazado - DocumentoID: {ID}, Motivo: {Motivo}, Usuario: {UsuarioID}",
                dto.DocumentoExpedienteID, dto.Motivo, dto.UsuarioVerificoID);

            return MapToDTO(actualizado);
        }

        // ===================================================================
        // ELIMINACIÓN
        // ===================================================================

        /// <inheritdoc/>
        public async Task EliminarDocumentoAsync(int documentoId, int usuarioId)
        {
            var documento = await _documentoRepository.GetByIdAsync(documentoId)
                ?? throw new KeyNotFoundException($"Documento con ID {documentoId} no encontrado.");

            if (documento.Estado == EstadoDocumentoExpediente.Verificado)
                throw new InvalidOperationException(
                    "No se puede eliminar un documento verificado. Rechácelo primero si necesita reemplazarlo.");

            // Eliminar archivo físico
            await _fileStorage.EliminarArchivoAsync(documento.RutaArchivo);

            // Eliminar registro
            await _documentoRepository.DeleteAsync(documentoId);

            // Recalcular DocumentacionCompleta
            await VerificarDocumentacionCompletaAsync(documento.ExpedienteID);

            _logger.LogInformation(
                "Documento eliminado - DocumentoID: {ID}, ExpedienteID: {ExpedienteID}, Usuario: {UsuarioID}",
                documentoId, documento.ExpedienteID, usuarioId);
        }

        // ===================================================================
        // VERIFICACIÓN DE DOCUMENTACIÓN COMPLETA
        // ===================================================================
        /// <inheritdoc/>
        public async Task<bool> VerificarDocumentacionCompletaAsync(int expedienteId)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(expedienteId)
                ?? throw new KeyNotFoundException($"Expediente con ID {expedienteId} no encontrado.");

            var acta = await _actaRetiroRepository.GetByExpedienteIdAsync(expedienteId);
            var documentos = await _documentoRepository.GetByExpedienteIdAsync(expedienteId);

            // TipoSalida efectivo: del acta si existe, del preliminar si no
            var tipoSalidaEfectivo = acta is not null
                ? acta.TipoSalida
                : expediente.TipoSalidaPreliminar;

            bool completo = tipoSalidaEfectivo is not null
                ? EsDocumentacionCompleta(documentos, tipoSalidaEfectivo.Value)
                : false; // Sin tipo definido aún → incompleto

            if (expediente.DocumentacionCompleta != completo)
            {
                expediente.DocumentacionCompleta = completo;
                expediente.FechaValidacionAdmision = completo ? DateTime.Now : null;
                expediente.FechaModificacion = DateTime.Now;
                await _expedienteRepository.UpdateAsync(expediente);

                _logger.LogInformation(
                    "DocumentacionCompleta actualizada - ExpedienteID: {ID}, Completo: {Completo}",
                    expedienteId, completo);
            }

            return completo;
        }

        // ===================================================================
        // DESCARGA
        // ===================================================================

        /// <inheritdoc/>
        public async Task<(Stream FileStream, string ContentType, string FileName)> DescargarDocumentoAsync(
            int documentoId)
        {
            var documento = await _documentoRepository.GetByIdAsync(documentoId)
                ?? throw new KeyNotFoundException($"Documento con ID {documentoId} no encontrado.");

            var (stream, contentType) = await _fileStorage.ObtenerArchivoAsync(documento.RutaArchivo);

            return (stream, contentType, documento.NombreArchivo);
        }

        // ===================================================================
        // MÉTODOS PRIVADOS
        // ===================================================================

        /// <summary>
        /// Evalúa si los documentos verificados cubren los requeridos según TipoSalida.
        /// - Familiar       → DNI_Familiar + DNI_Fallecido + CertificadoDefuncion
        /// - AutoridadLegal → OficioLegal únicamente
        /// </summary>
        private static bool EsDocumentacionCompleta(
            List<DocumentoExpediente> documentos,
            TipoSalida tipoSalida)
        {
            bool TieneVerificado(TipoDocumentoExpediente tipo) =>
                documentos.Any(d => d.TipoDocumento == tipo
                                 && d.Estado == EstadoDocumentoExpediente.Verificado);

            return tipoSalida switch
            {
                TipoSalida.Familiar =>
                    TieneVerificado(TipoDocumentoExpediente.DNI_Familiar) &&
                    TieneVerificado(TipoDocumentoExpediente.DNI_Fallecido) &&
                    TieneVerificado(TipoDocumentoExpediente.CertificadoDefuncion),

                TipoSalida.AutoridadLegal =>
                    TieneVerificado(TipoDocumentoExpediente.OficioLegal),

                _ => false
            };
        }
        private static bool EsDocumentacionCompletaSinActa(List<DocumentoExpediente> documentos)
        {
            bool TieneVerificado(TipoDocumentoExpediente tipo) =>
                documentos.Any(d => d.TipoDocumento == tipo
                                 && d.Estado == EstadoDocumentoExpediente.Verificado);

            // Si tiene oficio legal verificado → caso AutoridadLegal
            if (TieneVerificado(TipoDocumentoExpediente.OficioLegal))
                return true;

            // Caso Familiar: los 3 obligatorios verificados
            return TieneVerificado(TipoDocumentoExpediente.DNI_Familiar)
                && TieneVerificado(TipoDocumentoExpediente.DNI_Fallecido)
                && TieneVerificado(TipoDocumentoExpediente.CertificadoDefuncion);
        }
        /// <summary>
        /// Construye el estado visual de un tipo de documento para el ResumenDTO.
        /// Toma el documento más reciente del tipo especificado.
        /// </summary>
        private static EstadoDocumentoItem BuildEstadoItem(
            List<DocumentoExpediente> documentos,
            TipoDocumentoExpediente tipo)
        {
            var doc = documentos
                .Where(d => d.TipoDocumento == tipo)
                .OrderByDescending(d => d.FechaHoraSubida)
                .FirstOrDefault();

            if (doc is null)
                return new EstadoDocumentoItem { Subido = false };

            return new EstadoDocumentoItem
            {
                Subido = true,
                Verificado = doc.Estado == EstadoDocumentoExpediente.Verificado,
                Rechazado = doc.Estado == EstadoDocumentoExpediente.Rechazado,
                DocumentoID = doc.DocumentoExpedienteID,
                NombreArchivo = doc.NombreArchivo,
                Observaciones = doc.Observaciones
            };
        }

        /// <summary>
        /// Genera nombre único de archivo para evitar colisiones en filesystem.
        /// Formato: {CodigoExpediente}_{TipoDocumento}_{Timestamp}{Extension}
        /// Ejemplo: SGM-2025-00001_DNI_Familiar_20250115143022.pdf
        /// </summary>
        private static string GenerarNombreArchivo(
            string codigoExpediente,
            TipoDocumentoExpediente tipo,
            string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var tipoStr = tipo.ToString().Replace("_", "");
            var codigoLimpio = codigoExpediente.Replace("-", "");
            return $"{codigoLimpio}_{tipoStr}_{timestamp}{extension}";
        }

        /// <summary>
        /// Valida formato y tamaño del archivo antes de guardar.
        /// </summary>
        private static void ValidarArchivo(IFormFile archivo)
        {
            if (archivo is null || archivo.Length == 0)
                throw new ArgumentException("El archivo está vacío o no fue enviado.");

            if (archivo.Length > MaxTamañoBytes)
                throw new ArgumentException(
                    $"El archivo supera el tamaño máximo permitido de 5 MB. " +
                    $"Tamaño recibido: {archivo.Length / 1024 / 1024:0.##} MB");

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                throw new ArgumentException(
                    $"Formato de archivo no permitido: '{extension}'. " +
                    $"Formatos aceptados: {string.Join(", ", ExtensionesPermitidas)}");
        }

        /// <summary>
        /// Mapea entidad DocumentoExpediente a DocumentoExpedienteDTO.
        /// </summary>
        private static DocumentoExpedienteDTO MapToDTO(DocumentoExpediente doc)
        {
            return new DocumentoExpedienteDTO
            {
                DocumentoExpedienteID = doc.DocumentoExpedienteID,
                ExpedienteID = doc.ExpedienteID,
                TipoDocumento = doc.TipoDocumento,
                TipoDocumentoDescripcion = doc.TipoDocumento switch
                {
                    TipoDocumentoExpediente.DNI_Familiar => "DNI del Familiar",
                    TipoDocumentoExpediente.DNI_Fallecido => "DNI del Fallecido",
                    TipoDocumentoExpediente.CertificadoDefuncion => "Certificado de Defunción (SINADEF)",
                    TipoDocumentoExpediente.OficioLegal => "Oficio Legal (PNP/Fiscal)",
                    TipoDocumentoExpediente.ActaLevantamiento => "Acta de Levantamiento",
                    TipoDocumentoExpediente.Otro => "Documento Adicional",
                    _ => doc.TipoDocumento.ToString()
                },
                Estado = doc.Estado,
                EstadoDescripcion = doc.Estado switch
                {
                    EstadoDocumentoExpediente.PendienteVerificacion => "Pendiente de Verificación",
                    EstadoDocumentoExpediente.Verificado => "Verificado",
                    EstadoDocumentoExpediente.Rechazado => "Rechazado",
                    _ => doc.Estado.ToString()
                },
                NombreArchivo = doc.NombreArchivo,
                ExtensionArchivo = doc.ExtensionArchivo,
                TamanioLegible = doc.ObtenerTamañoLegible(),
                UsuarioSubioNombre = doc.UsuarioSubio?.NombreCompleto ?? string.Empty,
                FechaHoraSubida = doc.FechaHoraSubida,
                UsuarioVerificoNombre = doc.UsuarioVerifico?.NombreCompleto,
                FechaHoraVerificacion = doc.FechaHoraVerificacion,
                Observaciones = doc.Observaciones
            };
        }
    }
}