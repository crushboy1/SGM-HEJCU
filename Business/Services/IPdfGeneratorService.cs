using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.DTOs.Reportes;
using SisMortuorio.Business.DTOs.Salida;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    public interface IPdfGeneratorService
    {
        /// <summary>Genera PDF del compromiso de reposición de sangre.</summary>
        byte[] GenerarCompromisoSangre(GenerarCompromisoDTO datos);

        /// <summary>Genera PDF del Acta de Retiro Tripartita.</summary>
        byte[] GenerarActaRetiro(ActaRetiro acta);

        /// <summary>Genera PDF del cuaderno digital de permanencia (VigSup).</summary>
        byte[] GenerarReportePermanencia(
            List<PermanenciaItemDTO> datos,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor);

        /// <summary>Genera PDF del reporte de salidas del mortuorio.</summary>
        byte[] GenerarReporteSalidas(
            List<SalidaDTO> datos,
            EstadisticasSalidaDTO estadisticas,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor);

        /// <summary>Genera PDF del reporte de actas de retiro (Admisión).</summary>
        byte[] GenerarReporteActas(
            List<ActaReportesItemDTO> datos,
            ActaEstadisticasDTO estadisticas,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor);

        /// <summary>
        /// Genera PDF del reporte consolidado de deudas.
        /// Solo para JefeGuardia y Administrador — incluye montos.
        /// </summary>
        byte[] GenerarReporteDeudas(
            DeudaConsolidadaDTO datos,
            DateTime fechaInicio,
            DateTime fechaFin,
            string generadoPor);
    }
}