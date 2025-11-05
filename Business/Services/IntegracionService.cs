using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.ExternalSystems;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de integración con sistemas externos
    /// Combina datos de Galenhos (HIS) y SIGEM para crear expedientes
    /// </summary>
    public class IntegracionService : IIntegracionService
    {
        private readonly IGalenhosService _galenhosService;
        private readonly ISigemService _sigemService;
        private readonly ILogger<IntegracionService> _logger;

        public IntegracionService(
            IGalenhosService galenhosService,
            ISigemService sigemService,
            ILogger<IntegracionService> logger)
        {
            _galenhosService = galenhosService;
            _sigemService = sigemService;
            _logger = logger;
        }

        /// <summary>
        /// Consulta datos combinados de Galenhos y SIGEM por HC
        /// </summary>
        public async Task<ConsultarPacienteDTO?> ConsultarPacienteByHCAsync(string hc)
        {
            _logger.LogInformation("Iniciando consulta integrada para HC: {HC}", hc);

            var dto = new ConsultarPacienteDTO
            {
                HC = hc,
                Advertencias = new List<string>()
            };

            // ═══════════════════════════════════════════════════════════
            // PASO 1: Consultar datos demográficos en Galenhos
            // ═══════════════════════════════════════════════════════════
            var pacienteGalenhos = await _galenhosService.GetPacienteByHCAsync(hc);

            if (pacienteGalenhos == null)
            {
                _logger.LogWarning("Paciente no encontrado en Galenhos. HC: {HC}", hc);
                dto.ExisteEnGalenhos = false;
                dto.Advertencias.Add("⚠️ Paciente no encontrado en Galenhos (HIS). Verifique el número de HC.");
                return dto;
            }

            // Mapear datos de Galenhos
            dto.ExisteEnGalenhos = true;
            dto.TipoDocumentoID = pacienteGalenhos.TipoDocumentoID; 
            dto.NumeroDocumento = pacienteGalenhos.NumeroDocumento;
            dto.ApellidoPaterno = pacienteGalenhos.ApellidoPaterno;
            dto.ApellidoMaterno = pacienteGalenhos.ApellidoMaterno;
            dto.Nombres = pacienteGalenhos.Nombres;
            dto.FechaNacimiento = pacienteGalenhos.FechaNacimiento;
            dto.Edad = CalcularEdad(pacienteGalenhos.FechaNacimiento);
            dto.Sexo = pacienteGalenhos.Sexo;
            dto.FuenteFinanciamiento = pacienteGalenhos.FuenteFinanciamiento;

            _logger.LogInformation(
                "Datos demográficos obtenidos de Galenhos. Paciente: {Nombres} {ApellidoPaterno}",
                dto.Nombres, dto.ApellidoPaterno);

            // ═══════════════════════════════════════════════════════════
            // PASO 2: Consultar último episodio médico en SIGEM
            // ═══════════════════════════════════════════════════════════
            var episodioSigem = await _sigemService.GetUltimoEpisodioByHCAsync(hc);

            if (episodioSigem == null)
            {
                _logger.LogWarning("No se encontró episodio médico en SIGEM. HC: {HC}", hc);
                dto.ExisteEnSigem = false;
                dto.Advertencias.Add("⚠️ No se encontró registro de fallecimiento en SIGEM. Deberá ingresarse manualmente.");
                return dto;
            }

            // Mapear datos de SIGEM
            dto.ExisteEnSigem = true;
            dto.ServicioFallecimiento = episodioSigem.ServicioFallecimiento;
            dto.NumeroCama = episodioSigem.NumeroCama;
            dto.FechaHoraFallecimiento = episodioSigem.FechaHoraFallecimiento;
            dto.DiagnosticoFinal = episodioSigem.DiagnosticoFinal;
            dto.CodigoCIE10 = episodioSigem.CodigoCIE10;
            dto.MedicoCertificaNombre = episodioSigem.MedicoCertificaNombre;
            dto.MedicoCMP = episodioSigem.MedicoCMP;
            dto.MedicoRNE = episodioSigem.MedicoRNE;

            _logger.LogInformation(
                "Datos médicos obtenidos de SIGEM. Diagnóstico: {Diagnostico}, Servicio: {Servicio}",
                dto.DiagnosticoFinal, dto.ServicioFallecimiento);

            // ═══════════════════════════════════════════════════════════
            // PASO 3: Validaciones y advertencias adicionales
            // ═══════════════════════════════════════════════════════════

            // Validar fecha de fallecimiento reciente (últimas 48 horas)
            var horasDesdefallecimiento = (DateTime.Now - episodioSigem.FechaHoraFallecimiento).TotalHours;
            if (horasDesdefallecimiento > 48)
            {
                dto.Advertencias.Add($"⚠️ El fallecimiento ocurrió hace {(int)horasDesdefallecimiento} horas. Verificar si ya fue procesado.");
            }

            // Validar que tenga médico certificante
            if (string.IsNullOrEmpty(dto.MedicoCMP) && string.IsNullOrEmpty(dto.MedicoRNE))
            {
                dto.Advertencias.Add("⚠️ No se encontró CMP o RNE del médico certificante.");
            }

            // Validar diagnóstico
            if (string.IsNullOrEmpty(dto.CodigoCIE10))
            {
                dto.Advertencias.Add("⚠️ Falta código CIE-10 del diagnóstico.");
            }

            _logger.LogInformation(
                "Consulta integrada completada. HC: {HC}, Advertencias: {NumAdvertencias}",
                hc, dto.Advertencias.Count);

            return dto;
        }

        /// <summary>
        /// Calcula la edad en años a partir de la fecha de nacimiento
        /// </summary>
        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            var edad = hoy.Year - fechaNacimiento.Year;

            if (fechaNacimiento.Date > hoy.AddYears(-edad))
                edad--;

            return edad;
        }
    }
}