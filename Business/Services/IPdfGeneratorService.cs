using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    public interface IPdfGeneratorService
    {
        /// <summary>
        /// Genera el PDF del acta de compromiso de reposición de sangre.
        /// Retorna el archivo en bytes listo para descargar.
        /// </summary>
        byte[] GenerarCompromisoSangre(GenerarCompromisoDTO datos);

        /// <summary>
        /// ⭐ NUEVO: Genera el PDF del Acta de Retiro Tripartita.
        /// Retorna el archivo en bytes listo para descargar.
        /// </summary>
        byte[] GenerarActaRetiro(ActaRetiro acta);
    }
}