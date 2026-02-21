using Microsoft.EntityFrameworkCore;
using SisMortuorio.Business.DTOs;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;
using SisMortuorio.Data.Repositories;

namespace SisMortuorio.Business.Services
{
    public class DeudaSangreService : IDeudaSangreService
    {
        private readonly IDeudaSangreRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeudaSangreService> _logger;
        private readonly INotificacionDeudaService _notificacionDeudaService;

        public DeudaSangreService(
            IDeudaSangreRepository repository,
            ApplicationDbContext context,
            ILogger<DeudaSangreService> logger,
            INotificacionDeudaService notificacionDeudaService)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _notificacionDeudaService = notificacionDeudaService;
        }

        public async Task<DeudaSangreDTO> RegistrarDeudaAsync(CreateDeudaSangreDTO dto)
        {
            var expediente = await _context.Expedientes
                .FirstOrDefaultAsync(e => e.ExpedienteID == dto.ExpedienteID && !e.Eliminado)
                ?? throw new KeyNotFoundException($"Expediente {dto.ExpedienteID} no encontrado");

            if (await _context.DeudasSangre.AnyAsync(d => d.ExpedienteID == dto.ExpedienteID))
                throw new InvalidOperationException($"Ya existe una deuda de sangre para el expediente {dto.ExpedienteID}");

            _ = await _context.Users.FindAsync(dto.UsuarioRegistroID)
                ?? throw new KeyNotFoundException($"Usuario {dto.UsuarioRegistroID} no encontrado");

            var deuda = new DeudaSangre
            {
                ExpedienteID = dto.ExpedienteID,
                Estado = dto.CantidadUnidades > 0 ? EstadoDeudaSangre.Pendiente : EstadoDeudaSangre.SinDeuda,
                CantidadUnidades = dto.CantidadUnidades,
                TipoSangre = dto.TipoSangre,
                UsuarioRegistroID = dto.UsuarioRegistroID,
                FechaRegistro = DateTime.Now
            };

            await _repository.CreateAsync(deuda);
      
            _logger.LogInformation(
                "Deuda de sangre registrada - ExpedienteID: {ExpedienteID}, Estado: {Estado}, Unidades: {Unidades}",
                dto.ExpedienteID, deuda.Estado, dto.CantidadUnidades
            );

            if (deuda.Estado == EstadoDeudaSangre.Pendiente)
            {
                await _notificacionDeudaService.NotificarDeudaCreadaAsync(
                    "Sangre",
                    dto.ExpedienteID,
                    expediente.CodigoExpediente,
                    $"{dto.CantidadUnidades} unidades tipo {dto.TipoSangre ?? "N/A"}"
                );
            }

            await RegistrarAuditoriaAsync("DeudaSangre", "Crear", dto.UsuarioRegistroID, dto.ExpedienteID, null, deuda);

            return await MapToDTO(deuda);
        }

        public async Task<DeudaSangreDTO?> ObtenerPorExpedienteAsync(int expedienteId)
        {
            var deuda = await _repository.GetByExpedienteIdAsync(expedienteId);
            return deuda != null ? await MapToDTO(deuda) : null;
        }

        public async Task<DeudaSangreDTO> MarcarSinDeudaAsync(int expedienteId, int usuarioId)
        {
            var deuda = await ObtenerDeudaOThrowAsync(expedienteId);
            var estadoAnterior = deuda.Estado;

            deuda.MarcarSinDeuda(usuarioId);

            await _repository.UpdateAsync(deuda);
        
            _logger.LogInformation(
                "Deuda sangre marcada como Sin Deuda - ExpedienteID: {ExpedienteID}, EstadoAnterior: {EstadoAnterior}",
                expedienteId, estadoAnterior
            );

            await RegistrarAuditoriaAsync("DeudaSangre", "MarcarSinDeuda", usuarioId, expedienteId,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado });

            return await MapToDTO(deuda);
        }

        public async Task<DeudaSangreDTO> MarcarLiquidadaAsync(int expedienteId, LiquidarDeudaSangreDTO dto)
        {
            var deuda = await ObtenerDeudaOThrowAsync(expedienteId);

            if (deuda.Estado != EstadoDeudaSangre.Pendiente)
                throw new InvalidOperationException($"Solo se pueden liquidar deudas pendientes. Estado actual: {deuda.Estado}");

            var estadoAnterior = deuda.Estado;

            deuda.MarcarLiquidada(
                dto.NombreFamiliarCompromiso,
                dto.DNIFamiliarCompromiso,
                dto.RutaPDFCompromiso,
                dto.UsuarioActualizacionID
            );

            await _repository.UpdateAsync(deuda);
    
            var expediente = deuda.Expediente ?? await _context.Expedientes.FindAsync(expedienteId);

            _logger.LogInformation(
                "Deuda sangre liquidada - ExpedienteID: {ExpedienteID}, Familiar: {Familiar}",
                expedienteId, dto.NombreFamiliarCompromiso
            );

            await _notificacionDeudaService.NotificarDeudaResueltaAsync(
                "Sangre",
                "Liquidada",
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A"
            );

            await VerificarYNotificarDesbloqueoTotalAsync(
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A",
                "Sangre"
            );

            await RegistrarAuditoriaAsync("DeudaSangre", "Liquidar", dto.UsuarioActualizacionID, expedienteId,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado, Familiar = dto.NombreFamiliarCompromiso });

            return await MapToDTO(deuda);
        }

        public async Task<DeudaSangreDTO> AnularDeudaAsync(int expedienteId, AnularDeudaSangreDTO dto)
        {
            var deuda = await ObtenerDeudaOThrowAsync(expedienteId);

            if (deuda.Estado != EstadoDeudaSangre.Pendiente)
                throw new InvalidOperationException($"Solo se pueden anular deudas pendientes. Estado actual: {deuda.Estado}");

            _ = await _context.Users.FindAsync(dto.MedicoAnulaID)
                ?? throw new KeyNotFoundException($"Médico {dto.MedicoAnulaID} no encontrado");

            var estadoAnterior = deuda.Estado;

            deuda.AnularDeuda(dto.MedicoAnulaID, dto.JustificacionAnulacion);

            _context.DeudasSangre.Update(deuda);
            await _context.SaveChangesAsync();

            var expediente = deuda.Expediente ?? await _context.Expedientes.FindAsync(expedienteId);

            _logger.LogWarning(
                "Deuda sangre anulada por médico - ExpedienteID: {ExpedienteID}, MédicoID: {MedicoID}",
                expedienteId, dto.MedicoAnulaID
            );

            await _notificacionDeudaService.NotificarDeudaResueltaAsync(
                "Sangre",
                "Anulada",
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A"
            );

            await VerificarYNotificarDesbloqueoTotalAsync(
                expedienteId,
                expediente?.CodigoExpediente ?? "N/A",
                "Sangre"
            );

            await RegistrarAuditoriaAsync("DeudaSangre", "Anular", dto.MedicoAnulaID, expedienteId,
                new { EstadoAnterior = estadoAnterior },
                new { EstadoNuevo = deuda.Estado, Justificacion = dto.JustificacionAnulacion });

            return await MapToDTO(deuda);
        }

        public async Task<bool> BloqueaRetiroAsync(int expedienteId)
        {
            var deuda = await _context.DeudasSangre
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            return deuda?.BloqueaRetiro() ?? false;
        }

        public async Task<string> ObtenerSemaforoAsync(int expedienteId)
        {
            var deuda = await _context.DeudasSangre
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            return deuda?.ObtenerSemaforo() ?? "SIN REGISTRO";
        }

        public async Task<List<DeudaSangreDTO>> ObtenerDeudasPendientesAsync()
        {
            var deudas = await _repository.GetPendientesAsync();
            var dtos = new List<DeudaSangreDTO>();
            foreach (var deuda in deudas)
            {
                dtos.Add(await MapToDTO(deuda));
            }
            return dtos;
        }

        public async Task<List<HistorialDeudaSangreDTO>> ObtenerHistorialAsync(int expedienteId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ExpedienteID == expedienteId && a.Modulo == "DeudaSangre")
                .OrderBy(a => a.FechaHora)
                .Include(a => a.Usuario)
                .ToListAsync();

            return logs.Select(log => new HistorialDeudaSangreDTO
            {
                FechaHora = log.FechaHora,
                Accion = log.Accion,
                UsuarioNombre = log.Usuario?.NombreCompleto ?? "Sistema",
                Detalle = log.DatosDespues ?? "Sin detalles",
                IPOrigen = log.IPOrigen
            }).ToList();
        }

        private async Task<DeudaSangre> ObtenerDeudaOThrowAsync(int expedienteId)
        {
            return await _context.DeudasSangre
                .Include(d => d.Expediente)
                .FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId)
                ?? throw new KeyNotFoundException($"No existe deuda de sangre para el expediente {expedienteId}");
        }

        private async Task VerificarYNotificarDesbloqueoTotalAsync(int expedienteId, string codigoExpediente, string tipoDeudaResuelto)
        {
            var deudaSangre = await _context.DeudasSangre.FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);
            var deudaEconomica = await _context.DeudasEconomicas.FirstOrDefaultAsync(d => d.ExpedienteID == expedienteId);

            var deudasResueltas = 0;
            var totalDeudas = 0;

            if (deudaSangre != null)
            {
                totalDeudas++;
                if (!deudaSangre.BloqueaRetiro()) deudasResueltas++;
            }

            if (deudaEconomica != null)
            {
                totalDeudas++;
                if (!deudaEconomica.BloqueaRetiro()) deudasResueltas++;
            }

            if (totalDeudas == 0) return;

            if (deudasResueltas == totalDeudas)
            {
                await _notificacionDeudaService.NotificarDesbloqueoTotalAsync(
                    expedienteId,
                    codigoExpediente,
                    deudasResueltas,
                    totalDeudas
                );
            }
            else if (deudasResueltas > 0)
            {
                await _notificacionDeudaService.NotificarDesbloqueoParcialAsync(
                    expedienteId,
                    codigoExpediente,
                    tipoDeudaResuelto,
                    deudasResueltas,
                    totalDeudas
                );
            }
        }

        private async Task<DeudaSangreDTO> MapToDTO(DeudaSangre deuda)
        {
            if (deuda.Expediente == null)
                await _context.Entry(deuda).Reference(d => d.Expediente).LoadAsync();
            if (deuda.UsuarioRegistro == null)
                await _context.Entry(deuda).Reference(d => d.UsuarioRegistro).LoadAsync();
            if (deuda.UsuarioActualizacion == null && deuda.UsuarioActualizacionID.HasValue)
                await _context.Entry(deuda).Reference(d => d.UsuarioActualizacion).LoadAsync();
            if (deuda.MedicoAnula == null && deuda.MedicoAnulaID.HasValue)
                await _context.Entry(deuda).Reference(d => d.MedicoAnula).LoadAsync();

            return new DeudaSangreDTO
            {
                DeudaSangreID = deuda.DeudaSangreID,
                ExpedienteID = deuda.ExpedienteID,
                CodigoExpediente = deuda.Expediente?.CodigoExpediente ?? "N/A",
                Estado = deuda.Estado.ToString(),
                CantidadUnidades = deuda.CantidadUnidades ?? 0,
                TipoSangre = deuda.TipoSangre,
                NombreFamiliarCompromiso = deuda.NombreFamiliarCompromiso,
                DNIFamiliarCompromiso = deuda.DNIFamiliarCompromiso,
                FechaLiquidacion = deuda.FechaLiquidacion,
                RutaPDFCompromiso = deuda.RutaPDFCompromiso,
                AnuladaPorMedico = deuda.AnuladaPorMedico,
                MedicoAnulaID = deuda.MedicoAnulaID,
                FechaAnulacion = deuda.FechaAnulacion,
                JustificacionAnulacion = deuda.JustificacionAnulacion,
                UsuarioRegistroID = deuda.UsuarioRegistroID,
                UsuarioRegistroNombre = deuda.UsuarioRegistro?.NombreCompleto ?? "N/A",
                FechaRegistro = deuda.FechaRegistro,
                BloqueaRetiro = deuda.BloqueaRetiro(),
                Semaforo = deuda.ObtenerSemaforo()
            };
        }

        private async Task RegistrarAuditoriaAsync(string modulo, string accion, int usuarioId, int? expedienteId, object? datosAntes, object? datosDespues)
        {
            try
            {
                var log = AuditLog.CrearLogPersonalizado(modulo, accion, usuarioId, expedienteId, datosAntes, datosDespues, null);
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría");
            }
        }
    }
}