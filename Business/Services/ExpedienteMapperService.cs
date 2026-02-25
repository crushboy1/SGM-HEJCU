using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using System;
using System.Linq;

namespace SisMortuorio.Business.Services
{
    /// <summary>
    /// Implementación del servicio de mapeo para Expedientes.
    /// </summary>
    public class ExpedienteMapperService : IExpedienteMapperService
    {
        /// <summary>
        /// Lógica centralizada para mapear Expediente -> ExpedienteDTO.
        /// </summary>
        public ExpedienteDTO? MapToExpedienteDTO(Expediente expediente)
        {
            if (expediente == null)
                return null;

            // Lógica de cálculo de edad
            var edad = DateTime.Now.Year - expediente.FechaNacimiento.Year;
            if (DateTime.Now < expediente.FechaNacimiento.AddYears(edad)) edad--;

            // Mapeo
            return new ExpedienteDTO
            {
                ExpedienteID = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                TipoExpediente = expediente.TipoExpediente,
                HC = expediente.HC,
                TipoDocumento = expediente.TipoDocumento.ToString(),
                NumeroDocumento = expediente.NumeroDocumento,
                NombreCompleto = expediente.NombreCompleto,
                FechaNacimiento = expediente.FechaNacimiento,
                Edad = edad,
                Sexo = expediente.Sexo,
                TipoSeguro = expediente.TipoSeguro,
                ServicioFallecimiento = expediente.ServicioFallecimiento,
                NumeroCama = expediente.NumeroCama,
                BandejaActualID = expediente.BandejaActualID,
                CodigoBandeja = expediente.BandejaActual?.Codigo,
                FechaHoraFallecimiento = expediente.FechaHoraFallecimiento,
                MedicoCertificaNombre = expediente.MedicoCertificaNombre,
                MedicoCMP = expediente.MedicoCMP,
                MedicoRNE = expediente.MedicoRNE,
                NumeroCertificadoSINADEF = expediente.NumeroCertificadoSINADEF,
                DiagnosticoFinal = expediente.DiagnosticoFinal,
                TipoSalidaPreliminar = expediente.TipoSalidaPreliminar?.ToString(),
                EstadoActual = expediente.EstadoActual.ToString(),
                CodigoQR = expediente.CodigoQR,
                FechaGeneracionQR = expediente.FechaGeneracionQR,
                UsuarioCreador = expediente.UsuarioCreador?.NombreCompleto ?? "Usuario no disponible",
                FechaCreacion = expediente.FechaCreacion,
                FechaModificacion = expediente.FechaModificacion,
                Pertenencias = expediente.Pertenencias?.Select(p => new PertenenciaDTO
                {
                    PertenenciaID = p.PertenenciaID,
                    Descripcion = p.Descripcion,
                    Estado = p.Estado,
                    Observaciones = p.Observaciones
                }).ToList()
            };
        }
    }
}