class ActaRetiroModel {
  final int actaRetiroID;
  final int expedienteID;
  final String codigoExpediente;
  final String tipoSalida;
  final bool estaCompleta;
  final bool tienePDFFirmado;
  final bool bypassDeudaAutorizado;

  // Datos fallecido
  final String nombrePaciente;
  final String hc;
  final String servicio;

  // Familiar
  final String? familiarNombreCompleto;
  final String? familiarTipoDocumento;
  final String? familiarNumeroDocumento;
  final String? familiarParentesco;
  final String? familiarTelefono;

  // Autoridad Legal
  final String? autoridadNombreCompleto;
  final String? autoridadTipoDocumento;
  final String? autoridadNumeroDocumento;
  final String? autoridadCargo;
  final String? autoridadInstitucion;
  final String? autoridadTelefono;
  final String? numeroOficioPolicial;
  final String? tipoAutoridad;

  // Destino
  final String? destino;

  const ActaRetiroModel({
    required this.actaRetiroID,
    required this.expedienteID,
    required this.codigoExpediente,
    required this.tipoSalida,
    required this.estaCompleta,
    required this.tienePDFFirmado,
    required this.bypassDeudaAutorizado,
    required this.nombrePaciente,
    required this.hc,
    required this.servicio,
    this.familiarNombreCompleto,
    this.familiarTipoDocumento,
    this.familiarNumeroDocumento,
    this.familiarParentesco,
    this.familiarTelefono,
    this.autoridadNombreCompleto,
    this.autoridadTipoDocumento,
    this.autoridadNumeroDocumento,
    this.autoridadCargo,
    this.autoridadInstitucion,
    this.autoridadTelefono,
    this.numeroOficioPolicial,
    this.tipoAutoridad,
    this.destino,
  });

  // ── Getters de dominio ───────────────────────────────────────────
  bool get esFamiliar => tipoSalida == 'Familiar';
  bool get esAutoridadLegal => tipoSalida == 'AutoridadLegal';
  bool get puedeRegistrarSalida => estaCompleta && tienePDFFirmado;

  String get responsableNombre => esFamiliar
      ? (familiarNombreCompleto ?? '—')
      : (autoridadNombreCompleto ?? '—');

  factory ActaRetiroModel.fromJson(Map<String, dynamic> json) {
    return ActaRetiroModel(
      actaRetiroID: json['actaRetiroID'] as int? ?? 0,
      expedienteID: json['expedienteID'] as int? ?? 0,
      codigoExpediente: json['codigoExpediente'] as String? ?? '',
      tipoSalida: json['tipoSalida'] as String? ?? 'Familiar',
      estaCompleta: json['estaCompleta'] as bool? ?? false,
      tienePDFFirmado: json['tienePDFFirmado'] as bool? ?? false,
      bypassDeudaAutorizado:
          json['bypassDeudaAutorizado'] as bool? ?? false,
      nombrePaciente:
          json['nombreCompletoFallecido'] as String? ?? '',
      hc: json['historiaClinica'] as String? ?? '',
      servicio: json['servicioFallecimiento'] as String? ?? '',
      familiarNombreCompleto:
          json['familiarNombreCompleto'] as String?,
      familiarTipoDocumento:
          json['familiarTipoDocumento'] as String?,
      familiarNumeroDocumento:
          json['familiarNumeroDocumento'] as String?,
      familiarParentesco: json['familiarParentesco'] as String?,
      familiarTelefono: json['familiarTelefono'] as String?,
      autoridadNombreCompleto:
          json['autoridadNombreCompleto'] as String?,
      autoridadTipoDocumento:
          json['autoridadTipoDocumento'] as String?,
      autoridadNumeroDocumento:
          json['autoridadNumeroDocumento'] as String?,
      autoridadCargo: json['autoridadCargo'] as String?,
      autoridadInstitucion: json['autoridadInstitucion'] as String?,
      autoridadTelefono: json['autoridadTelefono'] as String?,
      numeroOficioPolicial: json['numeroOficioPolicial'] as String?,
      tipoAutoridad: json['tipoAutoridad'] as String?,
      destino: json['destino'] as String?,
    );
  }
}