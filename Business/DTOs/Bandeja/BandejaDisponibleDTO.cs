namespace SisMortuorio.Business.DTOs.Bandeja
{
    /// <summary>
    /// DTO simplificado para listar bandejas disponibles
    /// (ej. para un dropdown de asignación).
    /// </summary>
    public class BandejaDisponibleDTO
    {
        public int BandejaID { get; set; }
        public string Codigo { get; set; } = string.Empty;
    }
}