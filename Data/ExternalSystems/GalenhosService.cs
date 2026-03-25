using Microsoft.Extensions.Logging;

namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula consultas a la BD de Galenhos.
    /// TODO producción: reemplazar con consultas reales a BD Galenhos.
    ///
    /// Reglas de los datos mock:
    /// - HC:              6 dígitos numéricos
    /// - NumeroDocumento: DNI = 8 dígitos numéricos / CE o Pasaporte = alfanumérico max 12
    /// - FuenteFinanciamiento: exactamente como el enum del backend
    /// </summary>
    public class GalenhosService : IGalenhosService
    {
        private readonly ILogger<GalenhosService> _logger;

        public GalenhosService(ILogger<GalenhosService> logger)
        {
            _logger = logger;
        }

        private readonly List<PacienteGalenhos> _pacientesSimulados = new()
        {
            // ── Caso 1: Interno, adulto mayor, SIS ───────────────────────────
            new PacienteGalenhos
            {
                HC = "123456",
                NumeroCuenta = "CTA-2026-001",
                TipoDocumentoID = 1,
                NumeroDocumento = "12345678",
                ApellidoPaterno = "García",
                ApellidoMaterno = "López",
                Nombres = "Juan Carlos",
                FechaNacimiento = new DateTime(1950, 5, 15),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },

            // ── Caso 2: Interno, mujer adulta, EsSalud ───────────────────────
            new PacienteGalenhos
            {
                HC = "234567",
                NumeroCuenta = "CTA-2026-002",
                TipoDocumentoID = 1,
                NumeroDocumento = "87654321",
                ApellidoPaterno = "Fernández",
                ApellidoMaterno = "Rodríguez",
                Nombres = "Ana María",
                FechaNacimiento = new DateTime(1965, 10, 22),
                Sexo = "F",
                FuenteFinanciamiento = "EsSalud"
            },

            // ── Caso 3: UCI, urgente (fallecio hace 8h) ──────────────────────
            new PacienteGalenhos
            {
                HC = "345678",
                NumeroCuenta = "CTA-2026-003",
                TipoDocumentoID = 1,
                NumeroDocumento = "98765432",
                ApellidoPaterno = "Martínez",
                ApellidoMaterno = "Silva",
                Nombres = "Roberto Carlos",
                FechaNacimiento = new DateTime(1960, 8, 20),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },

            // ── Caso 4: Reciente (<4h), NO urgente ──────────────────────────
            new PacienteGalenhos
            {
                HC = "456789",
                NumeroCuenta = "CTA-2026-004",
                TipoDocumentoID = 1,
                NumeroDocumento = "11223344",
                ApellidoPaterno = "Ramírez",
                ApellidoMaterno = "Castro",
                Nombres = "María Elena",
                FechaNacimiento = new DateTime(1945, 3, 10),
                Sexo = "F",
                FuenteFinanciamiento = "SIS"
            },

            // ── Caso 5: SOAT, urgente (6h), TCE ─────────────────────────────
            new PacienteGalenhos
            {
                HC = "567890",
                NumeroCuenta = "CTA-2026-005",
                TipoDocumentoID = 1,
                NumeroDocumento = "55667788",
                ApellidoPaterno = "Torres",
                ApellidoMaterno = "Mendoza",
                Nombres = "Carlos Alberto",
                FechaNacimiento = new DateTime(1970, 12, 5),
                Sexo = "M",
                FuenteFinanciamiento = "SOAT"
            },

            // ── Caso 6: NN (no identificado) ────────────────────────────────
            new PacienteGalenhos
            {
                HC = "600001",
                NumeroCuenta = "CTA-2026-NN-001",
                TipoDocumentoID = 5,
                NumeroDocumento = "00000000",
                ApellidoPaterno = "NN",
                ApellidoMaterno = "NN",
                Nombres = "No Identificado",
                FechaNacimiento = new DateTime(1980, 1, 1),
                Sexo = "M",
                FuenteFinanciamiento = "PendientePago"
            },

            // ── Caso 7: CausaViolenta (accidente), SOAT ──────────────────────
            // Prueba bloqueo de card Familiar en gestion-documentos
            new PacienteGalenhos
            {
                HC = "700002",
                NumeroCuenta = "CTA-2026-006",
                TipoDocumentoID = 1,
                NumeroDocumento = "99887766",
                ApellidoPaterno = "Villanueva",
                ApellidoMaterno = "Paredes",
                Nombres = "Luis Enrique",
                FechaNacimiento = new DateTime(1975, 11, 30),
                Sexo = "M",
                FuenteFinanciamiento = "SOAT"
            },

            // ── Caso 8: Sin episodio SIGEM (modo manual parcial) ─────────────
            // Prueba advertencia y campos médicos editables
            new PacienteGalenhos
            {
                HC = "800003",
                NumeroCuenta = "CTA-2026-007",
                TipoDocumentoID = 2, // Pasaporte
                NumeroDocumento = "P12345678",
                ApellidoPaterno = "Chuquipiondo",
                ApellidoMaterno = "Ikari",
                Nombres = "Diego Armando",
                FechaNacimiento = new DateTime(1990, 8, 20),
                Sexo = "M",
                FuenteFinanciamiento = "Particular"
            },

            // ── Caso 9: Cancer, sin CMP ──────────────────────────────────────
            // Prueba advertencia sin CMP médico
            new PacienteGalenhos
            {
                HC = "111222",
                NumeroCuenta = "CTA-2026-008",
                TipoDocumentoID = 1,
                NumeroDocumento = "44332211",
                ApellidoPaterno = "Quispe",
                ApellidoMaterno = "Huanca",
                Nombres = "Pedro Aurelio",
                FechaNacimiento = new DateTime(1958, 4, 12),
                Sexo = "M",
                FuenteFinanciamiento = "SIS"
            },

            // ── Caso 10: IRC terminal, médico con RNE ────────────────────────
            new PacienteGalenhos
            {
                HC = "222333",
                NumeroCuenta = "CTA-2026-009",
                TipoDocumentoID = 1,
                NumeroDocumento = "33221100",
                ApellidoPaterno = "Gonzáles",
                ApellidoMaterno = "Ríos",
                Nombres = "José Miguel",
                FechaNacimiento = new DateTime(1950, 7, 18),
                Sexo = "M",
                FuenteFinanciamiento = "EsSalud"
            }
        };

        public async Task<PacienteGalenhos?> GetPacienteByHCAsync(string hc)
        {
            _logger.LogInformation("Consultando paciente en Galenhos. HC: {HC}", hc);
            await Task.Delay(100);
            var paciente = _pacientesSimulados.FirstOrDefault(p => p.HC == hc);
            if (paciente == null)
                _logger.LogWarning("Paciente no encontrado en Galenhos. HC: {HC}", hc);
            else
                _logger.LogInformation("Paciente encontrado: {Nombre}",
                    $"{paciente.ApellidoPaterno} {paciente.ApellidoMaterno}, {paciente.Nombres}");
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
                "Cirugía General", "Medicina Interna",
                "UCI - Unidad de Cuidados Intensivos",
                "UCINT - UCI Intermedia",
                "UVE1 - Unidad de Vigilancia 1",
                "UVE2 - Unidad de Vigilancia 2",
                "Emergencia", "Trauma Shock"
            };
        }

        public async Task<List<PacienteGalenhos>> GetPacientesByFiltroSimulado()
        {
            _logger.LogInformation("Simulando consulta de todos los pacientes en Galenhos");
            await Task.Delay(50);
            return _pacientesSimulados;
        }
    }
}