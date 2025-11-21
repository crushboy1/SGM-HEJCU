using SisMortuorio.Business.DTOs;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;
using SisMortuorio.Business.Services; // <-- 1. Asegurarse de que el using del Mapper esté
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SisMortuorio.Business.Services
{
    public class ExpedienteService : IExpedienteService
    {
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly IExpedienteMapperService _mapper; // <-- 2. Inyectar el Mapper

        public ExpedienteService(
            IExpedienteRepository expedienteRepository,
            IExpedienteMapperService mapper) // <-- 3. Recibir el Mapper en el constructor
        {
            _expedienteRepository = expedienteRepository;
            _mapper = mapper; // <-- 4. Asignar el Mapper
        }

        public async Task<ExpedienteDTO?> GetByIdAsync(int id)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;

            return _mapper.MapToExpedienteDTO(expediente); // <-- 5. Usar el Mapper
        }

        public async Task<List<ExpedienteDTO>> GetAllAsync()
        {
            var expedientes = await _expedienteRepository.GetAllAsync();
            return expedientes.Select(_mapper.MapToExpedienteDTO).Where(dto => dto != null).Select(dto => dto!).ToList(); // <-- 5. Usar el Mapper
        }

        public async Task<List<ExpedienteDTO>> GetByFiltrosAsync(
            string? hc,
            string? dni,
            string? servicio,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            EstadoExpediente? estado)
        {
            
            var expedientes = await _expedienteRepository.GetByFiltrosAsync(hc, dni, servicio, fechaDesde, fechaHasta, estado);
            return expedientes.Select(_mapper.MapToExpedienteDTO).Where(dto => dto != null).Select(dto => dto!).ToList(); // <-- 5. Usar el Mapper
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
            var codigoExpediente = await GenerarCodigoUnicoAsync(año);

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
                NumeroCertificadoSINADEF = string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF) ? null : dto.NumeroCertificadoSINADEF,
                DiagnosticoFinal = dto.DiagnosticoFinal,
                EstadoActual = EstadoExpediente.EnPiso, // Estado inicial del enum
                UsuarioCreadorID = usuarioCreadorId,
                FechaCreacion = DateTime.Now
            };

            // Agregar pertenencias si existen
            if (dto.Pertenencias != null && dto.Pertenencias.Any())
            {
                expediente.Pertenencias = dto.Pertenencias.Select(p => new Pertenencia
                {
                    Descripcion = p.Descripcion,
                    Estado = "ConCuerpo", // TODO: Considerar refactorizar a un enum
                    Observaciones = p.Observaciones,
                    FechaRegistro = DateTime.Now
                }).ToList();
            }

            var expedienteCreado = await _expedienteRepository.CreateAsync(expediente);

            return _mapper.MapToExpedienteDTO(expedienteCreado)!; // <-- 5. Usar el Mapper
        }

        public async Task<ExpedienteDTO?> UpdateAsync(int id, UpdateExpedienteDTO dto)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;

            // Actualizar solo campos permitidos
            if (!string.IsNullOrEmpty(dto.NumeroCama))
                expediente.NumeroCama = dto.NumeroCama;

            if (!string.IsNullOrEmpty(dto.DiagnosticoFinal))
                expediente.DiagnosticoFinal = dto.DiagnosticoFinal;

            if (!string.IsNullOrEmpty(dto.MedicoRNE))
                expediente.MedicoRNE = dto.MedicoRNE;

            if (!string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF))
            {
                // Validar unicidad solo si el valor es nuevo
                if (expediente.NumeroCertificadoSINADEF != dto.NumeroCertificadoSINADEF &&
                    await _expedienteRepository.ExistsCertificadoSINADEFAsync(dto.NumeroCertificadoSINADEF))
                {
                    throw new InvalidOperationException($"El certificado SINADEF {dto.NumeroCertificadoSINADEF} ya está registrado");
                }
                expediente.NumeroCertificadoSINADEF = dto.NumeroCertificadoSINADEF;
            }

            await _expedienteRepository.UpdateAsync(expediente);

            return _mapper.MapToExpedienteDTO(expediente); // <-- 5. Usar el Mapper
        }

        public async Task<bool> ValidarHCUnicoAsync(string hc)
        {
            return !await _expedienteRepository.ExistsHCAsync(hc);
        }

        public async Task<bool> ValidarCertificadoSINADEFUnicoAsync(string certificado)
        {
            if (string.IsNullOrEmpty(certificado)) return true;
            return !await _expedienteRepository.ExistsCertificadoSINADEFAsync(certificado);
        }

        // 6. ELIMINAR EL MÉTODO PRIVADO MapToDTO
        // private ExpedienteDTO MapToDTO(Expediente expediente) { ... }

        /// <summary>
        /// Genera un código de expediente único y correlativo por año.
        /// </summary>
        private async Task<string> GenerarCodigoUnicoAsync(int año)
        {
            var ultimoExpediente = await _expedienteRepository.GetUltimoExpedienteDelAñoAsync(año);

            int siguienteNumero = 1;

            if (ultimoExpediente != null)
            {
                // Extraer el número del último código
                var partes = ultimoExpediente.CodigoExpediente.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out int numeroActual))
                {
                    siguienteNumero = numeroActual + 1;
                }
            }

            // Verificar que el código no exista (manejo de concurrencia simple)
            var codigoPropuesto = $"SGM-{año}-{siguienteNumero:D5}";
            var existe = await _expedienteRepository.GetByCodigoAsync(codigoPropuesto);

            // Si ya existe (ej. dos usuarios crean al mismo tiempo), buscar el siguiente disponible
            while (existe != null)
            {
                siguienteNumero++;
                codigoPropuesto = $"SGM-{año}-{siguienteNumero:D5}";
                existe = await _expedienteRepository.GetByCodigoAsync(codigoPropuesto);
            }

            return codigoPropuesto;
        }
    }
}