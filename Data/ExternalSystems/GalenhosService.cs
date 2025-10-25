namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula consultas a la BD de Galenhos
    /// TODO: En producción, reemplazar con consultas reales a BD de Galenhos
    /// </summary>
    public class GalenhosService : IGalenhosService
    {
        // Por ahora datos en memoria, luego será consulta a BD real
        private readonly List<PacienteGalenhos> _pacientesSimulados = new()
        {
            new PacienteGalenhos
            {
                HC = "123456",
                NumeroCuenta = "CTA-2025-001",
                TipoDocumentoID = 1, // DNI
                NumeroDocumento = "45678901",
                ApellidoPaterno = "Ramos",
                ApellidoMaterno = "López",
                Nombres = "Erick",
                FechaNacimiento = new DateTime(1985, 5, 15),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
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
                NumeroCuenta = "CTA-2025-002",
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
            // Simular latencia de BD
            await Task.Delay(100);

            return _pacientesSimulados.FirstOrDefault(p => p.HC == hc);
        }

        public async Task<PacienteGalenhos?> GetPacienteByDocumentoAsync(string numeroDocumento)
        {
            await Task.Delay(100);

            return _pacientesSimulados.FirstOrDefault(p => p.NumeroDocumento == numeroDocumento);
        }

        public async Task<List<string>> GetServiciosActivos()
        {
            await Task.Delay(50);

            return new List<string>
            {
                "Cirugía",
                "Medicina",
                "UCI",
                "UCINT",
                "UVE1",
                "UVE2",
                "TraumaShock"
            };
        }
    }
}