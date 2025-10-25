using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    public class ExpedienteService : IExpedienteService
    {
        private readonly IExpedienteRepository _expedienteRepository;

        public ExpedienteService(IExpedienteRepository expedienteRepository)
        {
            _expedienteRepository = expedienteRepository;
        }

        public async Task<ExpedienteDTO?> GetByIdAsync(int id)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;

            return MapToDTO(expediente);
        }

        public async Task<List<ExpedienteDTO>> GetAllAsync()
        {
            var expedientes = await _expedienteRepository.GetAllAsync();
            return expedientes.Select(MapToDTO).ToList();
        }

        public async Task<List<ExpedienteDTO>> GetByFiltrosAsync(
            string? hc,
            string? dni,
            string? servicio,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            string? estado)
        {
            var expedientes = await _expedienteRepository.GetByFiltrosAsync(hc, dni, servicio, fechaDesde, fechaHasta, estado);
            return expedientes.Select(MapToDTO).ToList();
        }

        public async Task<ExpedienteDTO> CreateAsync(CreateExpedienteDTO dto, int usuarioCreadorId)
        {
            // Validaciones
            if (await _expedienteRepository.ExistsHCAsync(dto.HC))
                throw new InvalidOperationException($"Ya existe un expediente con la HC {dto.HC}");

            if (!string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF) &&
                await _expedienteRepository.ExistsCertificadoSINADEFAsync(dto.NumeroCertificadoSINADEF))
                throw new InvalidOperationException($"El certificado SINADEF {dto.NumeroCertificadoSINADEF} ya está registrado");

            // Validar fechas
            if (dto.FechaHoraFallecimiento > DateTime.Now)
                throw new InvalidOperationException("La fecha de fallecimiento no puede ser futura");

            if (dto.FechaHoraFallecimiento < dto.FechaNacimiento)
                throw new InvalidOperationException("La fecha de fallecimiento debe ser posterior a la fecha de nacimiento");

            // Generar código de expediente
            var año = DateTime.Now.Year;
            var count = await _expedienteRepository.GetCountByServicioAsync(dto.ServicioFallecimiento);
            var codigoExpediente = $"SGM-{año}-{(count + 1):D5}";

            var expediente = new Expediente
            {
                CodigoExpediente = codigoExpediente,
                TipoExpediente = dto.TipoExpediente,
                HC = dto.HC,
                TipoDocumento = dto.TipoDocumento,  
                NumeroDocumento = dto.NumeroDocumento,
                ApellidoPaterno = dto.ApellidoPaterno,
                ApellidoMaterno = dto.ApellidoMaterno,
                Nombres = dto.Nombres,
                NombreCompleto = $"{dto.ApellidoPaterno} {dto.ApellidoMaterno}, {dto.Nombres}",
                FechaNacimiento = dto.FechaNacimiento,
                Sexo = dto.Sexo,
                TipoSeguro = dto.TipoSeguro,
                ServicioFallecimiento = dto.ServicioFallecimiento,
                NumeroCama = dto.NumeroCama,
                FechaHoraFallecimiento = dto.FechaHoraFallecimiento,
                MedicoCertificaNombre = dto.MedicoCertificaNombre,
                MedicoCMP = dto.MedicoCMP,
                MedicoRNE = dto.MedicoRNE,
                NumeroCertificadoSINADEF = dto.NumeroCertificadoSINADEF,
                CausaMuerte = dto.CausaMuerte,
                EstadoActual = "En Piso",
                UsuarioCreadorID = usuarioCreadorId,
                FechaCreacion = DateTime.Now
            };

            // Agregar pertenencias si existen
            if (dto.Pertenencias != null && dto.Pertenencias.Any())
            {
                expediente.Pertenencias = dto.Pertenencias.Select(p => new Pertenencia
                {
                    Descripcion = p.Descripcion,
                    Estado = "ConCuerpo",
                    Observaciones = p.Observaciones,
                    FechaRegistro = DateTime.Now
                }).ToList();
            }

            var expedienteCreado = await _expedienteRepository.CreateAsync(expediente);

            return MapToDTO(expedienteCreado);
        }

        public async Task<ExpedienteDTO?> UpdateAsync(int id, UpdateExpedienteDTO dto)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;

            // Actualizar solo campos permitidos
            if (!string.IsNullOrEmpty(dto.NumeroCama))
                expediente.NumeroCama = dto.NumeroCama;

            if (!string.IsNullOrEmpty(dto.CausaMuerte))
                expediente.CausaMuerte = dto.CausaMuerte;

            if (!string.IsNullOrEmpty(dto.MedicoRNE))
                expediente.MedicoRNE = dto.MedicoRNE;

            if (!string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF))
            {
                if (await _expedienteRepository.ExistsCertificadoSINADEFAsync(dto.NumeroCertificadoSINADEF))
                    throw new InvalidOperationException($"El certificado SINADEF {dto.NumeroCertificadoSINADEF} ya está registrado");

                expediente.NumeroCertificadoSINADEF = dto.NumeroCertificadoSINADEF;
            }

            await _expedienteRepository.UpdateAsync(expediente);

            return MapToDTO(expediente);
        }

        public async Task<bool> ValidarHCUnicoAsync(string hc)
        {
            return !await _expedienteRepository.ExistsHCAsync(hc);
        }

        public async Task<bool> ValidarCertificadoSINADEFUnicoAsync(string certificado)
        {
            return !await _expedienteRepository.ExistsCertificadoSINADEFAsync(certificado);
        }

        private ExpedienteDTO MapToDTO(Expediente expediente)
        {
            var edad = DateTime.Now.Year - expediente.FechaNacimiento.Year;
            if (DateTime.Now < expediente.FechaNacimiento.AddYears(edad)) edad--;

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
                FechaHoraFallecimiento = expediente.FechaHoraFallecimiento,
                MedicoCertificaNombre = expediente.MedicoCertificaNombre,
                MedicoCMP = expediente.MedicoCMP,
                MedicoRNE = expediente.MedicoRNE,
                NumeroCertificadoSINADEF = expediente.NumeroCertificadoSINADEF,
                CausaMuerte = expediente.CausaMuerte,
                EstadoActual = expediente.EstadoActual,
                CodigoQR = expediente.CodigoQR,
                FechaGeneracionQR = expediente.FechaGeneracionQR,
                UsuarioCreador = expediente.UsuarioCreador?.NombreCompleto ?? "",
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