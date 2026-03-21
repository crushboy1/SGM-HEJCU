using System.ComponentModel.DataAnnotations;

namespace SisMortuorio.Business.DTOs.ActaRetiro;

/// <summary>
/// DTO para autorizar excepcionalmente el retiro con deudas pendientes (económica y/o sangre).
/// 
/// CONTEXTO:
/// Cuando un paciente estuvo hospitalizado menos de 24h, generó deuda económica y/o de sangre,
/// y fallece sin familiar presente → PNP llega con oficio → el hospital no puede recuperar
/// esas deudas de todas formas. El bypass permite al JG/Admin desbloquear AMBAS deudas
/// para no interrumpir el retiro legal.
/// 
/// REGLA: Solo aplica para TipoSalida = AutoridadLegal.
/// Para Familiar: el familiar SIEMPRE debe cancelar/exonerar económica y firmar sangre.
/// 
/// FLUJO:
/// 1. Admisionista sube documentos → elige TipoSalida = AutoridadLegal → guarda
/// 2. En tabla general aparece botón "Autorizar Excepción"
///    (visible solo para JefeGuardia y Administrador)
/// 3. JG/Admin hace clic → modal con campo justificación obligatorio
/// 4. Confirma → backend valida rol → setea bypass en expediente
/// 5. Ambos semáforos se ponen verdes → admisionista puede crear el acta
/// 
/// El UsuarioAutorizaID viene del JWT en el controller — NUNCA del frontend.
/// Roles autorizados: JefeGuardia, Administrador.
/// </summary>
public class AutorizarBypassDeudaDTO
{
    /// <summary>
    /// ID del expediente que tiene deuda pendiente.
    /// </summary>
    [Required(ErrorMessage = "El ID del expediente es obligatorio")]
    public int ExpedienteID { get; set; }

    /// <summary>
    /// Justificación obligatoria para el bypass.
    /// Ej: "PNP retira cuerpo sin familiar identificado — caso legal urgente"
    /// Ej: "JG autoriza retiro pendiente de pago por orden fiscal"
    /// </summary>
    [Required(ErrorMessage = "La justificación es obligatoria para autorizar el bypass")]
    [StringLength(500, MinimumLength = 10,
        ErrorMessage = "La justificación debe tener entre 10 y 500 caracteres")]
    public string Justificacion { get; set; } = string.Empty;
}