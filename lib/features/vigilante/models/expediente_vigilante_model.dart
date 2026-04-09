class ExpedienteVigilanteModel {
  final int expedienteID;
  final String codigoExpediente;
  final String nombreCompleto;
  final String hc;
  final String numeroDocumento;
  final String servicioFallecimiento;
  final String estadoActual;
  final String? codigoBandeja;
  final DateTime fechaHoraFallecimiento;

  const ExpedienteVigilanteModel({
    required this.expedienteID,
    required this.codigoExpediente,
    required this.nombreCompleto,
    required this.hc,
    required this.numeroDocumento,
    required this.servicioFallecimiento,
    required this.estadoActual,
    this.codigoBandeja,
    required this.fechaHoraFallecimiento,
  });

  factory ExpedienteVigilanteModel.fromJson(
      Map<String, dynamic> json) {
    return ExpedienteVigilanteModel(
      expedienteID: json['expedienteID'] as int? ?? 0,
      codigoExpediente: json['codigoExpediente'] as String? ?? '',
      nombreCompleto: json['nombreCompleto'] as String? ?? '',
      hc: json['hC'] as String? ?? json['hc'] as String? ?? '',
      numeroDocumento: json['numeroDocumento'] as String? ?? '',
      servicioFallecimiento:
          json['servicioFallecimiento'] as String? ?? '',
      estadoActual: json['estadoActual'] as String? ?? '',
      codigoBandeja: json['codigoBandeja'] as String?,
      fechaHoraFallecimiento: DateTime.tryParse(
              json['fechaHoraFallecimiento'] as String? ?? '') ??
          DateTime.now(),
    );
  }

  String get tiempoEnMortuorio {
    final diff = DateTime.now().difference(fechaHoraFallecimiento);
    final totalMin = diff.inMinutes;
    if (totalMin < 0) return 'Reciente';
    final d = totalMin ~/ 1440;
    final h = (totalMin % 1440) ~/ 60;
    final m = totalMin % 60;
    if (d > 0) return '${d}d ${h}h ${m}m';
    if (h > 0) return '${h}h ${m}m';
    return '${m}m';
  }
}