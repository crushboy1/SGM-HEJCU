namespace SisMortuorio.Business.DTOs.Verificacion
{
    /// <summary>
    /// DTO simplificado para mostrar el historial de intentos de verificación.
    /// </summary>
    public class VerificacionHistorialDTO
    {
        public int VerificacionID { get; set; }
        public DateTime FechaHora { get; set; }
        public string VigilanteNombre { get; set; } = string.Empty;
        public string TecnicoAmbulanciaNombre { get; set; } = string.Empty;
        public bool Aprobada { get; set; }
        public string? MotivoRechazo { get; set; }
    }
}