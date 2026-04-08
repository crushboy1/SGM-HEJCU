class ExpedientePendienteModel {
  final int expedienteID;
  final String codigoExpediente;
  final String nombreCompleto;
  final String hc;
  final String servicioFallecimiento;
  final String estadoActual;
  final DateTime fechaHoraFallecimiento;
  final bool esUrgente;

  const ExpedientePendienteModel({
    required this.expedienteID,
    required this.codigoExpediente,
    required this.nombreCompleto,
    required this.hc,
    required this.servicioFallecimiento,
    required this.estadoActual,
    required this.fechaHoraFallecimiento,
    required this.esUrgente,
  });

  factory ExpedientePendienteModel.fromJson(Map<String, dynamic> json) {
    final fecha = DateTime.tryParse(
          json['fechaHoraFallecimiento'] as String? ?? '') ??
        DateTime.now();
    final horas =
        DateTime.now().difference(fecha).inMinutes / 60;

    return ExpedientePendienteModel(
      expedienteID: json['expedienteID'] as int,
      codigoExpediente: json['codigoExpediente'] as String? ?? '',
      nombreCompleto: json['nombreCompleto'] as String? ?? '',
      hc: json['hC'] as String? ?? json['hc'] as String? ?? '',
      servicioFallecimiento:
          json['servicioFallecimiento'] as String? ?? 'No especificado',
      estadoActual: json['estadoActual'] as String? ?? '',
      fechaHoraFallecimiento: fecha,
      esUrgente: horas > 4,
    );
  }

  double get horasTranscurridas =>
      DateTime.now().difference(fechaHoraFallecimiento).inMinutes / 60;

  bool get puedeAsignarBandeja => estadoActual == 'PendienteAsignacionBandeja';

  String get tiempoFormateado {
    final totalMin =
        DateTime.now().difference(fechaHoraFallecimiento).inMinutes;
    if (totalMin < 0) return 'Reciente';
    final d = totalMin ~/ 1440;
    final h = (totalMin % 1440) ~/ 60;
    final m = totalMin % 60;
    if (d > 0) return '${d}d ${h}h ${m}m';
    if (h > 0) return '${h}h ${m}m';
    return '${m}m';
  }
}