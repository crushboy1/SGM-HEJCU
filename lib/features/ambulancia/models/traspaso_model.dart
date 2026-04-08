class TraspasoResponseModel {
  final int transferenciaID;
  final int expedienteID;
  final String codigoExpediente;
  final String nombreCompleto;
  final String estadoAnterior;
  final String estadoNuevo;

  const TraspasoResponseModel({
    required this.transferenciaID,
    required this.expedienteID,
    required this.codigoExpediente,
    required this.nombreCompleto,
    required this.estadoAnterior,
    required this.estadoNuevo,
  });

  factory TraspasoResponseModel.fromJson(Map<String, dynamic> json) {
    return TraspasoResponseModel(
      transferenciaID: json['transferenciaID'] as int? ?? 0,
      expedienteID: json['expedienteID'] as int? ?? 0,
      codigoExpediente: json['codigoExpediente'] as String? ?? '',
      nombreCompleto: json['nombreCompleto'] as String? ?? '',
      estadoAnterior: json['estadoAnterior'] as String? ?? '',
      estadoNuevo: json['estadoNuevo'] as String? ?? '',
    );
  }
}