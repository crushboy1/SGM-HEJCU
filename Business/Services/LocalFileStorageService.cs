namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación de almacenamiento de archivos en filesystem local.
    /// Guarda archivos en wwwroot/documentos-legales/{carpeta}/{nombreArchivo}
    /// </summary>
    public class LocalFileStorageService : ILocalFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _baseDirectory;

        private static readonly Dictionary<string, string> _contentTypes = new()
        {
            { ".pdf", "application/pdf" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
        };

        public LocalFileStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalFileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
            _baseDirectory = Path.Combine(_environment.WebRootPath, "documentos-legales");

            // Crear directorio base si no existe
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
                _logger.LogInformation("Directorio base creado: {BaseDirectory}", _baseDirectory);
            }
        }

        public async Task<string> GuardarArchivoAsync(IFormFile file, string carpeta, string nombreArchivo)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo está vacío", nameof(file));

            ValidarNombreArchivo(nombreArchivo);
            ValidarNombreCarpeta(carpeta);

            var directorioDestino = Path.Combine(_baseDirectory, carpeta);
            if (!Directory.Exists(directorioDestino))
            {
                Directory.CreateDirectory(directorioDestino);
                _logger.LogInformation("Directorio creado: {Directorio}", directorioDestino);
            }

            var rutaCompleta = Path.Combine(directorioDestino, nombreArchivo);
            var rutaRelativa = Path.Combine(carpeta, nombreArchivo);

            try
            {
                using var stream = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await file.CopyToAsync(stream);

                _logger.LogInformation(
                    "Archivo guardado exitosamente - Ruta: {RutaRelativa}, Tamaño: {Tamaño} bytes",
                    rutaRelativa, file.Length
                );

                return rutaRelativa.Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar archivo: {NombreArchivo} en {Carpeta}", nombreArchivo, carpeta);
                throw new InvalidOperationException($"Error al guardar el archivo: {ex.Message}", ex);
            }
        }

        public Task<(Stream stream, string contentType)> ObtenerArchivoAsync(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
                throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(rutaRelativa));

            var rutaNormalizada = rutaRelativa.Replace("/", "\\");
            var rutaCompleta = Path.Combine(_baseDirectory, rutaNormalizada);

            ValidarRutaSegura(rutaCompleta);

            if (!File.Exists(rutaCompleta))
            {
                _logger.LogWarning("Archivo no encontrado: {RutaRelativa}", rutaRelativa);
                throw new FileNotFoundException($"El archivo no existe: {rutaRelativa}");
            }

            try
            {
                var stream = new FileStream(rutaCompleta, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var extension = Path.GetExtension(rutaCompleta).ToLowerInvariant();
                var contentType = _contentTypes.GetValueOrDefault(extension, "application/octet-stream");

                _logger.LogDebug("Archivo obtenido: {RutaRelativa}, ContentType: {ContentType}", rutaRelativa, contentType);

                return Task.FromResult<(Stream, string)>((stream, contentType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener archivo: {RutaRelativa}", rutaRelativa);
                throw new InvalidOperationException($"Error al leer el archivo: {ex.Message}", ex);
            }
        }

        public Task EliminarArchivoAsync(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
                throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(rutaRelativa));

            var rutaNormalizada = rutaRelativa.Replace("/", "\\");
            var rutaCompleta = Path.Combine(_baseDirectory, rutaNormalizada);

            ValidarRutaSegura(rutaCompleta);

            if (!File.Exists(rutaCompleta))
            {
                _logger.LogWarning("Intento de eliminar archivo inexistente: {RutaRelativa}", rutaRelativa);
                return Task.CompletedTask;
            }

            try
            {
                File.Delete(rutaCompleta);
                _logger.LogInformation("Archivo eliminado: {RutaRelativa}", rutaRelativa);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo: {RutaRelativa}", rutaRelativa);
                throw new InvalidOperationException($"Error al eliminar el archivo: {ex.Message}", ex);
            }
        }

        public Task<bool> ExisteArchivoAsync(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
                return Task.FromResult(false);

            var rutaNormalizada = rutaRelativa.Replace("/", "\\");
            var rutaCompleta = Path.Combine(_baseDirectory, rutaNormalizada);

            try
            {
                ValidarRutaSegura(rutaCompleta);
                return Task.FromResult(File.Exists(rutaCompleta));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<long?> ObtenerTamañoArchivoAsync(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
                return Task.FromResult<long?>(null);

            var rutaNormalizada = rutaRelativa.Replace("/", "\\");
            var rutaCompleta = Path.Combine(_baseDirectory, rutaNormalizada);

            try
            {
                ValidarRutaSegura(rutaCompleta);

                if (!File.Exists(rutaCompleta))
                    return Task.FromResult<long?>(null);

                var fileInfo = new FileInfo(rutaCompleta);
                return Task.FromResult<long?>(fileInfo.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tamaño de archivo: {RutaRelativa}", rutaRelativa);
                return Task.FromResult<long?>(null);
            }
        }

        private void ValidarNombreArchivo(string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("El nombre del archivo no puede estar vacío", nameof(nombreArchivo));

            var caracteresInvalidos = Path.GetInvalidFileNameChars();
            if (nombreArchivo.Any(c => caracteresInvalidos.Contains(c)))
                throw new ArgumentException($"El nombre del archivo contiene caracteres inválidos: {nombreArchivo}", nameof(nombreArchivo));

            if (nombreArchivo.Contains(".."))
                throw new ArgumentException("El nombre del archivo no puede contener '..'", nameof(nombreArchivo));
        }

        private void ValidarNombreCarpeta(string carpeta)
        {
            if (string.IsNullOrWhiteSpace(carpeta))
                throw new ArgumentException("El nombre de la carpeta no puede estar vacío", nameof(carpeta));

            var caracteresInvalidos = Path.GetInvalidPathChars();
            if (carpeta.Any(c => caracteresInvalidos.Contains(c)))
                throw new ArgumentException($"El nombre de la carpeta contiene caracteres inválidos: {carpeta}", nameof(carpeta));

            if (carpeta.Contains(".."))
                throw new ArgumentException("El nombre de la carpeta no puede contener '..'", nameof(carpeta));
        }

        private void ValidarRutaSegura(string rutaCompleta)
        {
            var rutaNormalizadaBase = Path.GetFullPath(_baseDirectory);
            var rutaNormalizadaArchivo = Path.GetFullPath(rutaCompleta);

            if (!rutaNormalizadaArchivo.StartsWith(rutaNormalizadaBase, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Intento de acceso fuera del directorio permitido - Base: {Base}, Solicitado: {Solicitado}",
                    rutaNormalizadaBase, rutaNormalizadaArchivo
                );
                throw new UnauthorizedAccessException("Acceso denegado: ruta fuera del directorio permitido");
            }
        }
    }
}