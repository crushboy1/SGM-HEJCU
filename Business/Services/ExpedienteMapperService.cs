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
        /// Mapea Expediente -> ExpedienteDTO.
        /// Edad calculada al momento del fallecimiento, no a la fecha actual.
        /// </summary>
        public ExpedienteDTO? MapToExpedienteDTO(Expediente expediente)
        {
            if (expediente == null)
                return null;

            // Edad al momento del fallecimiento
            var fechaRef = expediente.FechaHoraFallecimiento;
            var edad = fechaRef.Year - expediente.FechaNacimiento.Year;
            if (fechaRef < expediente.FechaNacimiento.AddYears(edad)) edad--;

            return new ExpedienteDTO
            {
                ExpedienteID = expediente.ExpedienteID,
                CodigoExpediente = expediente.CodigoExpediente,
                TipoExpediente = expediente.TipoExpediente.ToString(),
                HC = expediente.HC,
                TipoDocumento = expediente.TipoDocumento.ToString(),
                NumeroDocumento = expediente.NumeroDocumento,
                NombreCompleto = expediente.NombreCompleto,
                FechaNacimiento = expediente.FechaNacimiento,
                Edad = edad,
                Sexo = expediente.Sexo,
                FuenteFinanciamiento = expediente.FuenteFinanciamiento.ToString(),
                ServicioFallecimiento = expediente.ServicioFallecimiento,
                NumeroCama = expediente.NumeroCama,
                FechaHoraFallecimiento = expediente.FechaHoraFallecimiento,
                MedicoCertificaNombre = expediente.MedicoCertificaNombre,
                MedicoCMP = expediente.MedicoCMP,
                MedicoRNE = expediente.MedicoRNE,
                MedicoExternoNombre = expediente.MedicoExternoNombre,
                MedicoExternoCMP = expediente.MedicoExternoCMP,
                EsNN = expediente.EsNN,
                CausaViolentaODudosa = expediente.CausaViolentaODudosa,
                Observaciones = expediente.Observaciones,
                DiagnosticoFinal = expediente.DiagnosticoFinal,
                TipoSalidaPreliminar = expediente.TipoSalidaPreliminar?.ToString(),
                DocumentacionCompleta = expediente.DocumentacionCompleta,
                FechaValidacionAdmision = expediente.FechaValidacionAdmision,
                UsuarioAdmisionNombre = expediente.UsuarioAdmision?.NombreCompleto,
                EstadoActual = expediente.EstadoActual.ToString(),
                CodigoQR = expediente.CodigoQR,
                FechaGeneracionQR = expediente.FechaGeneracionQR,
                BandejaActualID = expediente.BandejaActualID,
                CodigoBandeja = expediente.BandejaActual?.Codigo,
                UsuarioCreador = expediente.UsuarioCreador?.NombreCompleto ?? "Usuario no disponible",
                FechaCreacion = expediente.FechaCreacion,
                FechaModificacion = expediente.FechaModificacion,
                BypassDeudaAutorizado = expediente.BypassDeudaAutorizado,
                BypassDeudaJustificacion = expediente.BypassDeudaJustificacion,
                BypassDeudaUsuarioNombre = expediente.BypassDeudaUsuario?.NombreCompleto,
                BypassDeudaFecha = expediente.BypassDeudaFecha,
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