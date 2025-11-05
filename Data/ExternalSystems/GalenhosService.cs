using Microsoft.Extensions.Logging;

namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula consultas a la BD de Galenhos
    /// TODO: En producción, reemplazar con consultas reales a BD de Galenhos
    /// </summary>
    public class GalenhosService : IGalenhosService
    {
        private readonly ILogger<GalenhosService> _logger;

        public GalenhosService(ILogger<GalenhosService> logger)
        {
            _logger = logger;
        }

        // Base de datos simulada
        private readonly List<PacienteGalenhos> _pacientesSimulados = new()
        {
            new PacienteGalenhos
            {
                HC = "123456",
                NumeroCuenta = "CTA-2025-001",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "12345678",
                ApellidoPaterno = "García",
                ApellidoMaterno = "López",
                Nombres = "Juan Carlos",
                FechaNacimiento = new DateTime(1950, 5, 15),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },
            new PacienteGalenhos
            {
                HC = "12345679",
                NumeroCuenta = "CTA-2025-002",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "87654321",
                ApellidoPaterno = "Fernández",
                ApellidoMaterno = "Rodríguez",
                Nombres = "Ana María",
                FechaNacimiento = new DateTime(1965, 10, 22),
                Sexo = "F",
                FuenteFinanciamiento = "EsSalud"
            },
            new PacienteGalenhos
            {
                HC = "789456",
                NumeroCuenta = "CTA-2025-003",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "98765432",
                ApellidoPaterno = "Martínez",
                ApellidoMaterno = "Silva",
                Nombres = "Roberto Carlos",
                FechaNacimiento = new DateTime(1960, 8, 20),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },
            new PacienteGalenhos
            {
                HC = "456789",
                NumeroCuenta = "CTA-2025-004",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "11223344",
                ApellidoPaterno = "Ramírez",
                ApellidoMaterno = "Castro",
                Nombres = "María Elena",
                FechaNacimiento = new DateTime(1945, 3, 10),
                Sexo = "F",
                FuenteFinanciamiento = "SIS"
            },
            new PacienteGalenhos
            {
                HC = "654321",
                NumeroCuenta = "CTA-2025-005",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "55667788",
                ApellidoPaterno = "Torres",
                ApellidoMaterno = "Mendoza",
                Nombres = "Carlos Alberto",
                FechaNacimiento = new DateTime(1970, 12, 5),
                Sexo = "M",
                FuenteFinanciamiento = "PARTICULAR"
            },
            new PacienteGalenhos
            {
                HC = "NN-24102025-001",
                NumeroCuenta = "CTA-2025-NN-001",
                TipoDocumentoID = 5, // NN
                NumeroDocumento = "00000000",
                ApellidoPaterno = "NN",
                ApellidoMaterno = "NN",
                Nombres = "NN",
                FechaNacimiento = new DateTime(1980, 1, 1), // Estimado
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },
            new PacienteGalenhos
            {
                HC = "789012",
                NumeroCuenta = "CTA-2025-006",
                TipoDocumentoID = 2, // Pasaporte
                NumeroDocumento = "P123456789",
                ApellidoPaterno = "Chuquipiondo",
                ApellidoMaterno = "Ikari",
                Nombres = "Diego",
                FechaNacimiento = new DateTime(1990, 8, 20),
                Sexo = "M",
                FuenteFinanciamiento = "PARTICULAR"
            }
        };

        public async Task<PacienteGalenhos?> GetPacienteByHCAsync(string hc)
        {
            _logger.LogInformation("Consultando paciente en Galenhos. HC: {HC}", hc);

            // Simular latencia de BD
            await Task.Delay(100);

            var paciente = _pacientesSimulados.FirstOrDefault(p => p.HC == hc);

            if (paciente == null)
            {
                _logger.LogWarning("Paciente no encontrado en Galenhos. HC: {HC}", hc);
            }
            else
            {
                _logger.LogInformation("Paciente encontrado: {Nombre}",
                    $"{paciente.ApellidoPaterno} {paciente.ApellidoMaterno}, {paciente.Nombres}");
            }

            return paciente;
        }

        public async Task<PacienteGalenhos?> GetPacienteByDocumentoAsync(string numeroDocumento)
        {
            _logger.LogInformation("Consultando paciente por documento. Doc: {Doc}", numeroDocumento);

            await Task.Delay(100);

            return _pacientesSimulados.FirstOrDefault(p => p.NumeroDocumento == numeroDocumento);
        }

        public async Task<List<string>> GetServiciosActivos()
        {
            await Task.Delay(50);
            return new List<string>
            {
                "Cirugía General",
                "Medicina Interna",
                "UCI - Unidad de Cuidados Intensivos",
                "UCINT - UCI Intermedia",
                "UVE1 - Unidad de Vigilancia 1",
                "UVE2 - Unidad de Vigilancia 2",
                "Emergencia",
                "Trauma Shock"
            };
        }
    }
}