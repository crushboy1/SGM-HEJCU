using Microsoft.Extensions.Logging;

namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula consultas a SIGEM (Sistema de Gestión de Emergencias y Medicina).
    /// TODO producción: reemplazar con consultas reales a BD SIGEM.
    ///
    /// Reglas de los datos mock:
    /// - NumeroCama: solo dígitos, max 4 chars (Ej: "201", "308", "412")
    /// - MedicoCMP:  4–6 dígitos numéricos
    /// - MedicoRNE:  exactamente 5 dígitos numéricos (o null)
    ///
    /// Casos de prueba cubiertos:
    /// HC 123456  → Interno, adulto mayor, SIS, insuficiencia cardíaca
    /// HC 234567  → Interno, mujer, EsSalud, sepsis
    /// HC 345678  → UCI urgente (8h), paro cardiorrespiratorio
    /// HC 456789  → Reciente (2h), no urgente, neumonía
    /// HC 567890  → SOAT urgente (6h), TCE severo
    /// HC 600001  → NN, sin episodio (prueba modo manual)
    /// HC 700002  → CausaViolenta = true, SOAT, traumatismo múltiple
    /// HC 800003  → Sin episodio SIGEM (prueba advertencia manual)
    /// HC 111222  → Sin CMP (prueba advertencia)
    /// HC 222333  → Con RNE, IRC terminal
    /// </summary>
    public class SigemService : ISigemService
    {
        private readonly ILogger<SigemService> _logger;

        public SigemService(ILogger<SigemService> logger)
        {
            _logger = logger;
        }

        public async Task<EpisodioSigem?> GetUltimoEpisodioByHCAsync(string hc)
        {
            _logger.LogInformation("Consultando último episodio en SIGEM. HC: {HC}", hc);

            await Task.Delay(100);

            var episodio = GetEpisodiosSimulados()
                .FirstOrDefault(e => e.HC == hc);

            if (episodio == null)
            {
                _logger.LogWarning("No se encontró episodio en SIGEM. HC: {HC}", hc);
            }
            else
            {
                _logger.LogInformation(
                    "Episodio encontrado. HC: {HC}, Diagnóstico: {Dx}, Fecha: {Fecha}",
                    hc,
                    episodio.DiagnosticoFinal,
                    episodio.FechaHoraFallecimiento
                );
            }

            return episodio;
        }
        //Remplazar GetEpisodiosSimulados() por las consultas reales a la BD
        private List<EpisodioSigem> GetEpisodiosSimulados()
        {
            // Fecha base fija — evita inconsistencias
            var ahora = new DateTime(2026, 3, 22, 10, 0, 0);

            return new List<EpisodioSigem>
            {
                new EpisodioSigem
                {
                    HC = "123456",
                    ServicioFallecimiento = "Medicina Interna",
                    NumeroCama = "201",
                    FechaHoraFallecimiento = ahora.AddDays(-2).AddHours(-5),
                    DiagnosticoFinal = "I50.0 - Insuficiencia cardíaca congestiva",
                    CodigoCIE10 = "I50.0",
                    MedicoCertificaNombre = "Dr. Roberto Sánchez Vargas",
                    MedicoCMP = "45678",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "234567",
                    ServicioFallecimiento = "Cirugía General",
                    NumeroCama = "305",
                    FechaHoraFallecimiento = ahora.AddDays(-1).AddHours(-12),
                    DiagnosticoFinal = "A41.9 - Sepsis no especificada",
                    CodigoCIE10 = "A41.9",
                    MedicoCertificaNombre = "Dra. Patricia Mendoza López",
                    MedicoCMP = "56789",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "345678",
                    ServicioFallecimiento = "UCI",
                    NumeroCama = "408",
                    FechaHoraFallecimiento = ahora.AddHours(-8),
                    DiagnosticoFinal = "I46.9 - Paro cardiorrespiratorio",
                    CodigoCIE10 = "I46.9",
                    MedicoCertificaNombre = "Dr. Carlos Fernández Ruiz",
                    MedicoCMP = "34567",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "456789",
                    ServicioFallecimiento = "Medicina Interna",
                    NumeroCama = "105",
                    FechaHoraFallecimiento = ahora.AddHours(-2),
                    DiagnosticoFinal = "J15.9 - Neumonía bacteriana no especificada",
                    CodigoCIE10 = "J15.9",
                    MedicoCertificaNombre = "Dr. Luis Torres Mendoza",
                    MedicoCMP = "67890",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "567890",
                    ServicioFallecimiento = "Emergencia",
                    NumeroCama = "512",
                    FechaHoraFallecimiento = ahora.AddHours(-6),
                    DiagnosticoFinal = "S06.9 - Traumatismo craneoencefálico severo",
                    CodigoCIE10 = "S06.9",
                    MedicoCertificaNombre = "Dra. Ana Ramírez Castro",
                    MedicoCMP = "78901",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "700002",
                    ServicioFallecimiento = "Trauma Shock",
                    NumeroCama = "102",
                    FechaHoraFallecimiento = ahora.AddHours(-5),
                    DiagnosticoFinal = "T07 - Traumatismos múltiples no especificados",
                    CodigoCIE10 = "T07",
                    MedicoCertificaNombre = "Dr. Miguel Ángel Soto Pérez",
                    MedicoCMP = "89012",
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "111222",
                    ServicioFallecimiento = "Cirugía General",
                    NumeroCama = "204",
                    FechaHoraFallecimiento = ahora.AddDays(-1).AddHours(-6),
                    DiagnosticoFinal = "C34.9 - Tumor maligno de bronquios y pulmón",
                    CodigoCIE10 = "C34.9",
                    MedicoCertificaNombre = "Dr. Jorge Vásquez Huamán",
                    MedicoCMP = null,
                    MedicoRNE = null
                },
                new EpisodioSigem
                {
                    HC = "222333",
                    ServicioFallecimiento = "UCI",
                    NumeroCama = "403",
                    FechaHoraFallecimiento = ahora.AddHours(-15),
                    DiagnosticoFinal = "N18.0 - Insuficiencia renal crónica terminal",
                    CodigoCIE10 = "N18.0",
                    MedicoCertificaNombre = "Dr. José Miguel González Ríos",
                    MedicoCMP = null,
                    MedicoRNE = "12345"
                }
                // HC 600001 y 800003 → sin episodio
            };
        }
    }
}