using Microsoft.Extensions.Logging;

namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Servicio simulado para integración con SIGEM 
    /// (Sistema de Gestión de Emergencias y Medicina)
    /// En producción, esto se conectaría a la API/BD real de SIGEM
    /// </summary>
    public class SigemService : ISigemService
    {
        private readonly ILogger<SigemService> _logger;

        public SigemService(ILogger<SigemService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Consulta datos del último episodio médico de un paciente por HC
        /// Incluye: diagnóstico final, médico certificante, fecha/hora fallecimiento
        /// En producción: SELECT * FROM Episodios WHERE HC = @hc ORDER BY Fecha DESC
        /// </summary>
        public async Task<EpisodioSigem?> GetUltimoEpisodioByHCAsync(string hc)
        {
            _logger.LogInformation("Consultando último episodio en SIGEM. HC: {HC}", hc);

            // Simulación de delay de red
            await Task.Delay(600);

            // Base de datos simulada
            var episodiosSimulados = GetEpisodiosSimulados();

            var episodio = episodiosSimulados.FirstOrDefault(e => e.HC == hc);

            if (episodio == null)
            {
                _logger.LogWarning("No se encontró episodio médico en SIGEM. HC: {HC}", hc);
                return null;
            }

            _logger.LogInformation(
                "Episodio encontrado en SIGEM. HC: {HC}, Diagnóstico: {Diagnostico}, Fecha: {Fecha}",
                hc, episodio.DiagnosticoFinal, episodio.FechaHoraFallecimiento);

            return episodio;
        }

        /// <summary>
        /// Base de datos simulada de episodios médicos
        /// En producción: esto vendría de la BD real de SIGEM
        /// Incluye diagnósticos CIE-10 reales
        /// </summary>
        private List<EpisodioSigem> GetEpisodiosSimulados()
        {
            return new List<EpisodioSigem>
            {
                // ═══════════════════════════════════════════════════════════
                // PACIENTE 1: HC 123456 - Insuficiencia cardíaca
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "123456",
                    ServicioFallecimiento = "Medicina Interna",
                    NumeroCama = "M-201",
                    FechaHoraFallecimiento = DateTime.Now.AddDays(-2).AddHours(-5),
                    DiagnosticoFinal = "Insuficiencia cardíaca congestiva",
                    CodigoCIE10 = "I50.0",
                    MedicoCertificaNombre = "Dr. Roberto Sánchez Vargas",
                    MedicoCMP = "45678",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 2: HC 12345679 - Sepsis
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "12345679",
                    ServicioFallecimiento = "Cirugía General",
                    NumeroCama = "C-305",
                    FechaHoraFallecimiento = DateTime.Now.AddDays(-1).AddHours(-12),
                    DiagnosticoFinal = "Sepsis no especificada",
                    CodigoCIE10 = "A41.9",
                    MedicoCertificaNombre = "Dra. Patricia Mendoza López",
                    MedicoCMP = "56789",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 3: HC 789456 - Paro cardiorrespiratorio
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "789456",
                    ServicioFallecimiento = "UCI - Unidad de Cuidados Intensivos",
                    NumeroCama = "UCI-08",
                    FechaHoraFallecimiento = DateTime.Now.AddHours(-8),
                    DiagnosticoFinal = "Paro cardiorrespiratorio",
                    CodigoCIE10 = "I46.9",
                    MedicoCertificaNombre = "Dr. Carlos Fernández Ruiz",
                    MedicoCMP = "34567",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 4: HC 456789 - Neumonía
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "456789",
                    ServicioFallecimiento = "Medicina Interna",
                    NumeroCama = "M-105",
                    FechaHoraFallecimiento = DateTime.Now.AddDays(-3).AddHours(-2),
                    DiagnosticoFinal = "Neumonía bacteriana no especificada",
                    CodigoCIE10 = "J15.9",
                    MedicoCertificaNombre = "Dr. Luis Torres Mendoza",
                    MedicoCMP = "67890",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 5: HC 654321 - Traumatismo craneoencefálico
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "654321",
                    ServicioFallecimiento = "Emergencia",
                    NumeroCama = "E-12",
                    FechaHoraFallecimiento = DateTime.Now.AddHours(-3),
                    DiagnosticoFinal = "Traumatismo craneoencefálico severo",
                    CodigoCIE10 = "S06.9",
                    MedicoCertificaNombre = "Dra. Ana Ramírez Castro",
                    MedicoCMP = "78901",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 6: HC 111222 - Cáncer de pulmón
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "111222",
                    ServicioFallecimiento = "Cirugía de Tórax",
                    NumeroCama = "CT-204",
                    FechaHoraFallecimiento = DateTime.Now.AddDays(-1).AddHours(-6),
                    DiagnosticoFinal = "Tumor maligno de los bronquios y del pulmón",
                    CodigoCIE10 = "C34.9",
                    MedicoCertificaNombre = "Dr. Miguel Ángel Soto Pérez",
                    MedicoCMP = "89012",
                    MedicoRNE = null
                },

                // ═══════════════════════════════════════════════════════════
                // PACIENTE 7: HC 333444 - Insuficiencia renal (Médico extranjero)
                // ═══════════════════════════════════════════════════════════
                new EpisodioSigem
                {
                    HC = "333444",
                    ServicioFallecimiento = "UCI - Unidad de Cuidados Intensivos",
                    NumeroCama = "UCI-03",
                    FechaHoraFallecimiento = DateTime.Now.AddHours(-15),
                    DiagnosticoFinal = "Insuficiencia renal crónica terminal",
                    CodigoCIE10 = "N18.0",
                    MedicoCertificaNombre = "Dr. José Miguel González",
                    MedicoCMP = null,
                    MedicoRNE = "RNE-12345"  // Médico extranjero
                }
            };
        }
    }

    /// <summary>
    /// Modelo de episodio médico según estructura de SIGEM
    /// Representa el último episodio de atención del paciente fallecido
    /// </summary>
    public class EpisodioSigem
    {
        /// <summary>
        /// Historia Clínica del paciente
        /// </summary>
        public string HC { get; set; } = string.Empty;

        /// <summary>
        /// Servicio hospitalario donde ocurrió el fallecimiento
        /// </summary>
        public string ServicioFallecimiento { get; set; } = string.Empty;

        /// <summary>
        /// Número de cama o ubicación específica
        /// </summary>
        public string NumeroCama { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora exacta del fallecimiento
        /// </summary>
        public DateTime FechaHoraFallecimiento { get; set; }

        /// <summary>
        /// Diagnóstico final del paciente (texto descriptivo)
        /// </summary>
        public string DiagnosticoFinal { get; set; } = string.Empty;

        /// <summary>
        /// Código CIE-10 del diagnóstico
        /// </summary>
        public string CodigoCIE10 { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del médico que certifica el fallecimiento
        /// </summary>
        public string MedicoCertificaNombre { get; set; } = string.Empty;

        /// <summary>
        /// Colegio Médico del Perú (CMP) del médico certificante
        /// </summary>
        public string? MedicoCMP { get; set; }

        /// <summary>
        /// Registro Nacional de Extranjería (RNE) - para médicos extranjeros
        /// </summary>
        public string? MedicoRNE { get; set; }
    }
}