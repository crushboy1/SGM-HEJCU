using Microsoft.Extensions.Logging;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.ExternalSystems;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Integración con Galenhos y SIGEM.
    /// Combina datos demográficos y médicos para pre-llenar expedientes.
    /// TODO producción: reemplazar MockServices por consultas reales a BD.
    /// </summary>
    public class IntegracionService : IIntegracionService
    {
        private readonly IGalenhosService _galenhosService;
        private readonly ISigemService _sigemService;
        private readonly ILogger<IntegracionService> _logger;
        private readonly IExpedienteRepository _expedienteRepo;

        /// <summary>
        /// HCs que SIGEM marca como causa violenta o dudosa.
        /// TODO producción: este flag debe venir del campo real en SIGEM
        /// (ej. episodio.CausaViolentaODudosa o episodio.TipoMuerte == "Violenta").
        /// Por ahora se define como conjunto local para el mock.
        /// </summary>
        private static readonly HashSet<string> HcsCausaViolenta = new()
        {
            "700002"  // Trauma Shock — traumatismos múltiples
        };

        public IntegracionService(
            IGalenhosService galenhosService,
            ISigemService sigemService,
            ILogger<IntegracionService> logger,
            IExpedienteRepository expedienteRepo)
        {
            _galenhosService = galenhosService;
            _sigemService = sigemService;
            _logger = logger;
            _expedienteRepo = expedienteRepo;
        }

        // ===================================================================
        // CONSULTA POR HC — para pre-llenar el formulario
        // ===================================================================

        /// <summary>
        /// Consulta datos combinados de Galenhos y SIGEM por HC.
        /// Usado para pre-llenar el formulario de creación de expediente.
        /// </summary>
        public async Task<ConsultarPacienteDTO?> ConsultarPacienteByHCAsync(string hc)
        {
            _logger.LogInformation("Consulta integrada iniciada. HC: {HC}", hc);

            var dto = new ConsultarPacienteDTO
            {
                HC = hc,
                Advertencias = new List<string>()
            };

            // ── PASO 1: Galenhos — datos demográficos ──────────────────────
            var paciente = await _galenhosService.GetPacienteByHCAsync(hc);

            if (paciente == null)
            {
                _logger.LogWarning("Paciente no encontrado en Galenhos. HC: {HC}", hc);
                dto.ExisteEnGalenhos = false;
                dto.Advertencias.Add("Paciente no encontrado en Galenhos. Verifique el número de HC.");
                return dto;
            }

            dto.ExisteEnGalenhos = true;
            dto.TipoDocumentoID = paciente.TipoDocumentoID;
            dto.NumeroDocumento = paciente.NumeroDocumento;
            dto.ApellidoPaterno = paciente.ApellidoPaterno;
            dto.ApellidoMaterno = paciente.ApellidoMaterno;
            dto.Nombres = paciente.Nombres;
            dto.FechaNacimiento = paciente.FechaNacimiento;
            dto.Sexo = paciente.Sexo;
            dto.FuenteFinanciamiento = paciente.FuenteFinanciamiento;
            dto.EsNN = paciente.TipoDocumentoID == 5;

            _logger.LogInformation("Datos Galenhos obtenidos. Paciente: {Nombre}",
                $"{paciente.ApellidoPaterno} {paciente.Nombres}");

            // ── PASO 2: SIGEM — episodio médico ────────────────────────────
            var episodio = await _sigemService.GetUltimoEpisodioByHCAsync(hc);
            if (episodio == null)
            {
                _logger.LogWarning("Episodio no encontrado en SIGEM. HC: {HC}", hc);
                dto.ExisteEnSigem = false;
                dto.Edad = CalcularEdad(paciente.FechaNacimiento, DateTime.Now);
                dto.Advertencias.Add("No se encontró registro en SIGEM. Datos médicos deben ingresarse manualmente.");
                return dto;
            }

            dto.ExisteEnSigem = true;
            dto.Edad = CalcularEdad(paciente.FechaNacimiento, episodio.FechaHoraFallecimiento);
            dto.ServicioFallecimiento = episodio.ServicioFallecimiento;
            dto.NumeroCama = episodio.NumeroCama;
            dto.FechaHoraFallecimiento = episodio.FechaHoraFallecimiento;
            dto.DiagnosticoFinal = episodio.DiagnosticoFinal;
            dto.CodigoCIE10 = episodio.CodigoCIE10;
            dto.MedicoCertificaNombre = episodio.MedicoCertificaNombre;
            dto.MedicoCMP = episodio.MedicoCMP;
            dto.MedicoRNE = episodio.MedicoRNE;

            // CausaViolentaODudosa: determinado por el HC en el mock.
            // TODO producción: usar episodio.CausaViolentaODudosa del campo real de SIGEM.
            dto.CausaViolentaODudosa = HcsCausaViolenta.Contains(hc);

            // ── PASO 3: Advertencias adicionales ──────────────────────────
            var horas = (DateTime.Now - episodio.FechaHoraFallecimiento).TotalHours;
            if (horas > 48)
                dto.Advertencias.Add(
                    $"El fallecimiento ocurrió hace {(int)horas} horas. Verificar si ya fue procesado.");

            if (string.IsNullOrEmpty(episodio.MedicoCMP) && string.IsNullOrEmpty(episodio.MedicoRNE))
                dto.Advertencias.Add("No se encontró CMP ni RNE del médico certificante.");

            if (string.IsNullOrEmpty(episodio.CodigoCIE10))
                dto.Advertencias.Add("Falta código CIE-10 del diagnóstico.");

            if (dto.CausaViolentaODudosa)
                dto.Advertencias.Add(
                    "Causa violenta o dudosa. El tipo de salida será Autoridad Legal obligatoriamente.");

            _logger.LogInformation(
                "Consulta integrada completada. HC: {HC}, CausaViolenta: {CV}, Advertencias: {N}",
                hc, dto.CausaViolentaODudosa, dto.Advertencias.Count);
            
            return dto;
        }

        // ===================================================================
        // BANDEJA DE ENTRADA — para enfermería
        // ===================================================================

        /// <summary>
        /// Devuelve pacientes de Galenhos que aún no tienen expediente SGM.
        /// Enriquecido con datos de SIGEM cuando están disponibles.
        /// </summary>
        public async Task<List<BandejaEntradaDTO>> GetPacientesPendientesAsync()
        {
            _logger.LogInformation("Consultando bandeja de entrada");

            var todosMock = await _galenhosService.GetPacientesByFiltroSimulado();
            var expedientesCreados = await _expedienteRepo.GetAllAsync();
            var hcsProcesadas = expedientesCreados.Select(e => e.HC).ToHashSet();

            var pendientes = todosMock
                .Where(p => !hcsProcesadas.Contains(p.HC))
                .ToList();

            var resultado = new List<BandejaEntradaDTO>();

            foreach (var paciente in pendientes)
            {
                var item = new BandejaEntradaDTO
                {
                    HC = paciente.HC,
                    TipoDocumentoID = paciente.TipoDocumentoID,
                    NumeroDocumento = paciente.NumeroDocumento,
                    NombreCompleto = $"{paciente.ApellidoPaterno} {paciente.ApellidoMaterno}, {paciente.Nombres}",
                    Sexo = paciente.Sexo,
                    FuenteFinanciamiento = paciente.FuenteFinanciamiento,
                    EsNN = paciente.TipoDocumentoID == 5,
                    CausaViolentaODudosa = HcsCausaViolenta.Contains(paciente.HC)
                };

                var episodio = await _sigemService.GetUltimoEpisodioByHCAsync(paciente.HC);
                if (episodio != null)
                {
                    item.TieneDatosSigem = true;
                    item.ServicioFallecimiento = episodio.ServicioFallecimiento;
                    item.NumeroCama = episodio.NumeroCama;
                    item.FechaHoraFallecimiento = episodio.FechaHoraFallecimiento;
                    item.DiagnosticoFinal = episodio.DiagnosticoFinal;
                    item.MedicoCertificaNombre = episodio.MedicoCertificaNombre;
                    item.Edad = CalcularEdad(paciente.FechaNacimiento, episodio.FechaHoraFallecimiento);

                }
                else
                {
                    item.TieneDatosSigem = false;
                    item.Edad = CalcularEdad(paciente.FechaNacimiento, DateTime.Now);
                    item.Advertencias.Add("Sin datos en SIGEM — requiere ingreso manual.");
                }

                resultado.Add(item);
            }

            _logger.LogInformation("Bandeja de entrada: {Count} pacientes pendientes.", resultado.Count);

            return resultado;
        }

        // ===================================================================
        // HELPER
        // ===================================================================

        private static int CalcularEdad(DateTime fechaNacimiento, DateTime fechaReferencia)
        {
            var edad = fechaReferencia.Year - fechaNacimiento.Year;
            if (fechaReferencia < fechaNacimiento.AddYears(edad)) edad--;
            return edad;
        }
    }
}