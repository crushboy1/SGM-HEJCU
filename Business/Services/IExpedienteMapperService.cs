using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Servicio de utilidad con la única responsabilidad de mapear
    /// la entidad Expediente a sus diferentes DTOs.
    /// Sigue los principios SRP y DRY.
    /// </summary>
    public interface IExpedienteMapperService
    {
        /// <summary>
        /// Mapea una entidad Expediente a un ExpedienteDTO.
        /// </summary>
        /// <param name="expediente">La entidad de la base de datos.</param>
        /// <returns>Un ExpedienteDTO poblado.</returns>
        ExpedienteDTO? MapToExpedienteDTO(Expediente expediente);
    }
}