class VerificacionRequestModel {
  final String codigoExpedienteBrazalete;
  final String hcBrazalete;
  final String tipoDocumentoBrazalete;
  final String numeroDocumentoBrazalete;
  final String nombreCompletoBrazalete;
  final String servicioBrazalete;
  final bool brazaletePresente;
  final String observaciones;

  const VerificacionRequestModel({
    required this.codigoExpedienteBrazalete,
    required this.hcBrazalete,
    required this.tipoDocumentoBrazalete,
    required this.numeroDocumentoBrazalete,
    required this.nombreCompletoBrazalete,
    required this.servicioBrazalete,
    this.brazaletePresente = true,
    this.observaciones = 'Ingreso verificado por Vigilante Mortuorio.',
  });

  Map<String, dynamic> toJson() => {
    'codigoExpedienteBrazalete': codigoExpedienteBrazalete,
    'hCBrazalete': hcBrazalete,
    'tipoDocumentoBrazalete': tipoDocumentoBrazalete,
    'numeroDocumentoBrazalete': numeroDocumentoBrazalete,
    'nombreCompletoBrazalete': nombreCompletoBrazalete,
    'servicioBrazalete': servicioBrazalete,
    'brazaletePresente': brazaletePresente,
    'observaciones': observaciones,
  };
}

class VerificacionResultadoModel {
  final int verificacionID;
  final bool aprobada;
  final String mensajeResultado;
  final String estadoExpedienteNuevo;
  final String? motivoRechazo;

  const VerificacionResultadoModel({
    required this.verificacionID,
    required this.aprobada,
    required this.mensajeResultado,
    required this.estadoExpedienteNuevo,
    this.motivoRechazo,
  });

  factory VerificacionResultadoModel.fromJson(Map<String, dynamic> json) {
    return VerificacionResultadoModel(
      verificacionID: json['verificacionID'] as int? ?? 0,
      aprobada: json['aprobada'] as bool? ?? false,
      mensajeResultado: json['mensajeResultado'] as String? ?? '',
      estadoExpedienteNuevo: json['estadoExpedienteNuevo'] as String? ?? '',
      motivoRechazo: json['motivoRechazo'] as String?,
    );
  }
}