class VerificacionErrorInfo {
  final String title;
  final String text;
  final VerificacionErrorTipo tipo;

  const VerificacionErrorInfo({
    required this.title,
    required this.text,
    required this.tipo,
  });
}

enum VerificacionErrorTipo { info, warning, error }

class VerificacionErrorMapper {
  static VerificacionErrorInfo resolver(String backendMsg) {
    if (backendMsg.contains('PendienteAsignacionBandeja')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.info,
        title: 'Ingreso ya registrado',
        text: 'Este expediente ya fue ingresado al mortuorio y esta pendiente de asignacion de bandeja.',
      );
    }
    if (backendMsg.contains('EnBandeja')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.info,
        title: 'Expediente en bandeja',
        text: 'Este expediente ya tiene una bandeja asignada dentro del mortuorio.',
      );
    }
    if (backendMsg.contains('PendienteRetiro')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.info,
        title: 'Pendiente de retiro',
        text: 'Este expediente ya esta autorizado para retiro. No requiere nuevo ingreso.',
      );
    }
    if (backendMsg.contains('Retirado')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.info,
        title: 'Expediente retirado',
        text: 'Este cuerpo ya fue retirado del mortuorio. El expediente esta cerrado.',
      );
    }
    if (backendMsg.contains('VerificacionRechazada')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.warning,
        title: 'Verificacion rechazada previamente',
        text: 'Este expediente tiene una verificacion rechazada. Contacte a Enfermeria.',
      );
    }
    if (backendMsg.contains('custodia') || backendMsg.contains('Ambulancia')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.warning,
        title: 'Sin custodia registrada',
        text: 'El expediente no registra entrega por parte del Tecnico de Ambulancia.',
      );
    }
    if (backendMsg.contains('QR') || backendMsg.contains('codigo')) {
      return const VerificacionErrorInfo(
        tipo: VerificacionErrorTipo.error,
        title: 'Codigo no valido',
        text: 'El codigo escaneado no corresponde a ningun expediente activo.',
      );
    }
    return VerificacionErrorInfo(
      tipo: VerificacionErrorTipo.error,
      title: 'Error al procesar',
      text: backendMsg.isNotEmpty
          ? backendMsg
          : 'No se pudo procesar el ingreso. Intente nuevamente.',
    );
  }
}