using SisMortuorio.Data.Entities;

namespace SisMortuorio.Business.Services;

/// <summary>
/// Contrato para notificaciones SignalR relacionadas con Actas de Retiro.
/// 
/// Centraliza el envío de notificaciones hacia roles específicos
/// cuando ocurren eventos críticos en el flujo de retiro.
/// </summary>
public interface INotificacionActaRetiroService
{
    /// <summary>
    /// Notifica que un expediente está listo para retiro físico.
    /// Se dispara cuando Admisión sube el PDF firmado del Acta de Retiro,
    /// lo que dispara la transición EnBandeja → PendienteRetiro.
    /// 
    /// Destinatarios: VigilanciaMortuorio, VigilanteSupervisor
    /// Acción esperada: Badge +1 en bandeja de tareas, toast de alerta,
    ///                  tabla de retiros pendientes actualiza sin F5.
    /// </summary>
    /// <param name="expediente">Expediente que pasó a PendienteRetiro</param>
    /// <param name="acta">ActaRetiro con datos del responsable y tipo de salida</param>
    Task NotificarExpedienteListoParaRetiroAsync(Expediente expediente, ActaRetiro acta);
}