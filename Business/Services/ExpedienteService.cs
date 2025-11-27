using Microsoft.AspNetCore.SignalR; // ⭐ SignalR
using SisMortuorio.Business.DTOs;
using SisMortuorio.Business.DTOs.Notificacion; // ⭐ DTO Notificacion
using SisMortuorio.Business.Hubs; // ⭐ Hubs
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SisMortuorio.Business.Services
{
    public class ExpedienteService : IExpedienteService
    {
        private readonly IExpedienteRepository _expedienteRepository;
        private readonly IExpedienteMapperService _mapper;
        private readonly IHubContext<SgmHub, ISgmClient> _hubContext; // 1. Inyectar Hub

        public ExpedienteService(
            IExpedienteRepository expedienteRepository,
            IExpedienteMapperService mapper,
            IHubContext<SgmHub, ISgmClient> hubContext) //  2. Recibir en constructor
        {
            _expedienteRepository = expedienteRepository;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task<ExpedienteDTO?> GetByIdAsync(int id)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;
            return _mapper.MapToExpedienteDTO(expediente);
        }

        public async Task<List<ExpedienteDTO>> GetAllAsync()
        {
            var expedientes = await _expedienteRepository.GetAllAsync();
            return expedientes.Select(_mapper.MapToExpedienteDTO).Where(dto => dto != null).Select(dto => dto!).ToList();
        }

        public async Task<List<ExpedienteDTO>> GetByFiltrosAsync(
            string? hc, string? dni, string? servicio, DateTime? fd, DateTime? fh, EstadoExpediente? estado)
        {
            var expedientes = await _expedienteRepository.GetByFiltrosAsync(hc, dni, servicio, fd, fh, estado);
            return expedientes.Select(_mapper.MapToExpedienteDTO).Where(dto => dto != null).Select(dto => dto!).ToList();
        }

        public async Task<ExpedienteDTO> CreateAsync(CreateExpedienteDTO dto, int usuarioCreadorId)
        {
            // Validaciones
            if (await _expedienteRepository.ExistsHCAsync(dto.HC))
                throw new InvalidOperationException($"Ya existe un expediente con la HC {dto.HC}");

            if (!string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF) &&
                await _expedienteRepository.ExistsCertificadoSINADEFAsync(dto.NumeroCertificadoSINADEF))
                throw new InvalidOperationException($"El certificado SINADEF {dto.NumeroCertificadoSINADEF} ya está registrado");

            if (dto.FechaHoraFallecimiento > DateTime.Now)
                throw new InvalidOperationException("La fecha de fallecimiento no puede ser futura");

            if (dto.FechaHoraFallecimiento < dto.FechaNacimiento)
                throw new InvalidOperationException("La fecha de fallecimiento debe ser posterior a la fecha de nacimiento");

            // Generar código
            var año = DateTime.Now.Year;
            var codigoExpediente = await GenerarCodigoUnicoAsync(año);

            var expediente = new Expediente
            {
                CodigoExpediente = codigoExpediente,
                TipoExpediente = dto.TipoExpediente,
                HC = dto.HC,
                TipoDocumento = (TipoDocumentoIdentidad)dto.TipoDocumento, // Asegúrate que el DTO tenga el tipo correcto (int)
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
                EstadoActual = EstadoExpediente.EnPiso,
                UsuarioCreadorID = usuarioCreadorId,
                FechaCreacion = DateTime.Now
            };

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

            // ============================================================
            //  3. ENVIAR NOTIFICACIÓN (SIGNALR)
            // ============================================================
            try
            {
                // Notificar a AMBULANCIA y ADMINISTRADOR
                var notificacion = new NotificacionDTO
                {
                    Titulo = "Nuevo Fallecido en Piso",
                    Mensaje = $"Se requiere traslado para {expedienteCreado.NombreCompleto} en {expedienteCreado.ServicioFallecimiento}.",
                    Tipo = "info",
                    CodigoExpediente = expedienteCreado.CodigoExpediente,
                    ExpedienteId = expedienteCreado.ExpedienteID,
                    RolesDestino = "Ambulancia,Administrador",
                    RequiereAccion = true,
                    AccionSugerida = "Realizar Recojo",
                    UrlNavegacion = "/mis-tareas" // Ruta de la app móvil
                };

                await _hubContext.Clients.Groups(["Ambulancia", "Administrador"])
                    .RecibirNotificacion(notificacion);
            }
            catch (Exception ex)
            {
                // Loggear error pero NO detener la creación
                // _logger.LogError(ex, "Error enviando notificación SignalR");
                Console.WriteLine($"Error SignalR: {ex.Message}");
            }
            // ============================================================

            return _mapper.MapToExpedienteDTO(expedienteCreado)!;
        }

        public async Task<ExpedienteDTO?> UpdateAsync(int id, UpdateExpedienteDTO dto)
        {
            var expediente = await _expedienteRepository.GetByIdAsync(id);
            if (expediente == null) return null;

            if (!string.IsNullOrEmpty(dto.NumeroCama))
                expediente.NumeroCama = dto.NumeroCama;

            if (!string.IsNullOrEmpty(dto.DiagnosticoFinal))
                expediente.DiagnosticoFinal = dto.DiagnosticoFinal;

            if (!string.IsNullOrEmpty(dto.MedicoRNE))
                expediente.MedicoRNE = dto.MedicoRNE;

            if (!string.IsNullOrEmpty(dto.NumeroCertificadoSINADEF))
            {
                if (expediente.NumeroCertificadoSINADEF != dto.NumeroCertificadoSINADEF &&
                    await _expedienteRepository.ExistsCertificadoSINADEFAsync(dto.NumeroCertificadoSINADEF))
                {
                    throw new InvalidOperationException($"El certificado SINADEF {dto.NumeroCertificadoSINADEF} ya está registrado");
                }
                expediente.NumeroCertificadoSINADEF = dto.NumeroCertificadoSINADEF;
            }

            await _expedienteRepository.UpdateAsync(expediente);

            return _mapper.MapToExpedienteDTO(expediente);
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

        private async Task<string> GenerarCodigoUnicoAsync(int año)
        {
            var ultimoExpediente = await _expedienteRepository.GetUltimoExpedienteDelAñoAsync(año);
            int siguienteNumero = 1;

            if (ultimoExpediente != null)
            {
                var partes = ultimoExpediente.CodigoExpediente.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out int numeroActual))
                {
                    siguienteNumero = numeroActual + 1;
                }
            }

            var codigoPropuesto = $"SGM-{año}-{siguienteNumero:D5}";
            var existe = await _expedienteRepository.GetByCodigoAsync(codigoPropuesto);

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