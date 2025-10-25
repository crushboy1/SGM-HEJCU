namespace SisMortuorio.Data.ExternalSystems
{
    /// <summary>
    /// Simula consultas a la BD de SIGEM
    /// TODO: En producción, reemplazar con consultas reales a BD de SIGEM
    /// </summary>
    public class SigemService : ISigemService
    {
        private readonly List<DiagnosticoSigem> _diagnosticosSimulados = new()
        {
            new DiagnosticoSigem
            {
                HC = "123456",
                NumeroCuenta = "CTA-2025-001",
                DiagnosticoFinal = "Shock séptico post-operatorio",
                FechaHoraFallecimiento = DateTime.Now.AddHours(-2),
                MedicoCMP = "45678",
                MedicoNombre = "Dr. Bruno Rodríguez",
                ServicioOrigen = "Cirugía",
                NumeroCama = "305-A"
            }
        };

        public async Task<DiagnosticoSigem?> GetDiagnosticoByHCAsync(string hc)
        {
            await Task.Delay(100);

            return _diagnosticosSimulados.FirstOrDefault(d => d.HC == hc);
        }
    }
}