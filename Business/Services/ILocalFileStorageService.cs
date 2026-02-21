namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Interface para servicio de almacenamiento de archivos en filesystem local.
    /// Usado para guardar documentos legales (PDFs, imágenes) de expedientes.
    /// </summary>
    public interface ILocalFileStorageService
    {
        /// <summary>
        /// Guarda un archivo en el filesystem.
        /// </summary>
        /// <param name="file">Archivo a guardar (IFormFile)</param>
        /// <param name="carpeta">Carpeta destino relativa a wwwroot/documentos-legales/</param>
        /// <param name="nombreArchivo">Nombre final del archivo (con extensión)</param>
        /// <returns>Ruta relativa del archivo guardado (ej: "1/oficio-pnp-001.pdf")</returns>
        Task<string> GuardarArchivoAsync(IFormFile file, string carpeta, string nombreArchivo);

        /// <summary>
        /// Obtiene un archivo del filesystem para descarga/visualización.
        /// </summary>
        /// <param name="rutaRelativa">Ruta relativa del archivo (ej: "1/oficio-pnp-001.pdf")</param>
        /// <returns>Tupla con Stream del archivo y ContentType</returns>
        Task<(Stream stream, string contentType)> ObtenerArchivoAsync(string rutaRelativa);

        /// <summary>
        /// Elimina un archivo del filesystem.
        /// </summary>
        /// <param name="rutaRelativa">Ruta relativa del archivo a eliminar</param>
        Task EliminarArchivoAsync(string rutaRelativa);

        /// <summary>
        /// Verifica si un archivo existe en el filesystem.
        /// </summary>
        /// <param name="rutaRelativa">Ruta relativa del archivo</param>
        /// <returns>True si existe, False si no</returns>
        Task<bool> ExisteArchivoAsync(string rutaRelativa);

        /// <summary>
        /// Obtiene el tamaño de un archivo en bytes.
        /// </summary>
        /// <param name="rutaRelativa">Ruta relativa del archivo</param>
        /// <returns>Tamaño en bytes, null si no existe</returns>
        Task<long?> ObtenerTamañoArchivoAsync(string rutaRelativa);
    }
}