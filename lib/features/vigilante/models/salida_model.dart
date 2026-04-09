class DatosPreLlenadoModel {
  final int expedienteID;
  final String codigoExpediente;
  final String nombrePaciente;
  final String hc;
  final String servicio;
  final String? bandejaAsignada;
  final String tipoSalida; // 'Familiar' | 'AutoridadLegal'
  final bool puedeRegistrarSalida;
  final String? mensajeBloqueo;
  final bool deudaSangreOK;
  final String? deudaSangreMensaje;
  final bool deudaEconomicaOK;
  final String? deudaEconomicaMensaje;
  final bool tieneActaFirmada;

  // Responsable readonly
  final String? responsableNombreCompleto;
  final String? responsableTipoDocumento;
  final String? responsableNumeroDocumento;
  final String? responsableParentesco;
  final String? responsableTelefono;

  // Autoridad legal readonly
  final String? tipoAutoridad;
  final String? autoridadInstitucion;
  final String? autoridadCargo;
  final String? numeroOficioPolicial;

  // Campos editables prellenados
  final String? destino;

  const DatosPreLlenadoModel({
    required this.expedienteID,
    required this.codigoExpediente,
    required this.nombrePaciente,
    required this.hc,
    required this.servicio,
    this.bandejaAsignada,
    required this.tipoSalida,
    required this.puedeRegistrarSalida,
    this.mensajeBloqueo,
    required this.deudaSangreOK,
    this.deudaSangreMensaje,
    required this.deudaEconomicaOK,
    this.deudaEconomicaMensaje,
    required this.tieneActaFirmada,
    this.responsableNombreCompleto,
    this.responsableTipoDocumento,
    this.responsableNumeroDocumento,
    this.responsableParentesco,
    this.responsableTelefono,
    this.tipoAutoridad,
    this.autoridadInstitucion,
    this.autoridadCargo,
    this.numeroOficioPolicial,
    this.destino,
  });

  bool get esFamiliar => tipoSalida == 'Familiar';
  bool get esAutoridadLegal => tipoSalida == 'AutoridadLegal';

  factory DatosPreLlenadoModel.fromJson(Map<String, dynamic> json) {
    return DatosPreLlenadoModel(
      expedienteID: json['expedienteID'] as int? ?? 0,
      codigoExpediente: json['codigoExpediente'] as String? ?? '',
      nombrePaciente: json['nombrePaciente'] as String? ?? '',
      hc: json['hC'] as String? ?? json['hc'] as String? ?? '',
      servicio: json['servicio'] as String? ?? '',
      bandejaAsignada: json['bandejaAsignada'] as String?,
      tipoSalida: (json['tipoSalida'] as String?) ?? 'Familiar',
      puedeRegistrarSalida: json['puedeRegistrarSalida'] as bool? ?? false,
      mensajeBloqueo: json['mensajeBloqueo'] as String?,
      deudaSangreOK: json['deudaSangreOK'] as bool? ?? false,
      deudaSangreMensaje: json['deudaSangreMensaje'] as String?,
      deudaEconomicaOK: json['deudaEconomicaOK'] as bool? ?? false,
      deudaEconomicaMensaje: json['deudaEconomicaMensaje'] as String?,
      tieneActaFirmada: json['tieneActaFirmada'] as bool? ?? false,
      responsableNombreCompleto:
          json['responsableNombreCompleto'] as String?,
      responsableTipoDocumento:
          json['responsableTipoDocumento'] as String?,
      responsableNumeroDocumento:
          json['responsableNumeroDocumento'] as String?,
      responsableParentesco: json['responsableParentesco'] as String?,
      responsableTelefono: json['responsableTelefono'] as String?,
      tipoAutoridad: json['tipoAutoridad'] as String?,
      autoridadInstitucion: json['autoridadInstitucion'] as String?,
      autoridadCargo: json['autoridadCargo'] as String?,
      numeroOficioPolicial: json['numeroOficioPolicial'] as String?,
      destino: json['destino'] as String?,
    );
  }
}

class RegistrarSalidaModel {
  final int expedienteID;
  final String? nombreFuneraria;
  final String? funerariaRUC;
  final String? funerariaTelefono;
  final String? conductorFuneraria;
  final String? dniConductor;
  final String? ayudanteFuneraria;
  final String? dniAyudante;
  final String? placaVehiculo;
  final String? destino;
  final String? observaciones;

  const RegistrarSalidaModel({
    required this.expedienteID,
    this.nombreFuneraria,
    this.funerariaRUC,
    this.funerariaTelefono,
    this.conductorFuneraria,
    this.dniConductor,
    this.ayudanteFuneraria,
    this.dniAyudante,
    this.placaVehiculo,
    this.destino,
    this.observaciones,
  });

  Map<String, dynamic> toJson() => {
    'expedienteID': expedienteID,
    if (nombreFuneraria?.isNotEmpty == true)
      'nombreFuneraria': nombreFuneraria,
    if (funerariaRUC?.isNotEmpty == true) 'funerariaRUC': funerariaRUC,
    if (funerariaTelefono?.isNotEmpty == true)
      'funerariaTelefono': funerariaTelefono,
    if (conductorFuneraria?.isNotEmpty == true)
      'conductorFuneraria': conductorFuneraria,
    if (dniConductor?.isNotEmpty == true) 'dNIConductor': dniConductor,
    if (ayudanteFuneraria?.isNotEmpty == true)
      'ayudanteFuneraria': ayudanteFuneraria,
    if (dniAyudante?.isNotEmpty == true) 'dNIAyudante': dniAyudante,
    if (placaVehiculo?.isNotEmpty == true) 'placaVehiculo': placaVehiculo,
    if (destino?.isNotEmpty == true) 'destino': destino,
    if (observaciones?.isNotEmpty == true) 'observaciones': observaciones,
  };
}