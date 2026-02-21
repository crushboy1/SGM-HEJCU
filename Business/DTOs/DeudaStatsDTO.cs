namespace SisMortuorio.Business.DTOs
{
    /// <summary>
    /// Estadísticas combinadas de deudas para el Dashboard
    /// </summary>
    public class DeudaStatsDTO
    {
        // Deuda de Sangre
        public int SangrePendientes { get; set; }
        public int SangreAnuladas { get; set; }

        // Deuda Económica
        public int EconomicasPendientes { get; set; }
        public int EconomicasExoneradas { get; set; }
        public decimal MontoTotalPendiente { get; set; }
        public decimal MontoTotalExonerado { get; set; }
    }
}