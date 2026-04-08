import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';

class BandejaDisponibleModel {
  final int bandejaID;
  final String codigo;
  final String estado;
  final String? nombrePaciente;
  final String? codigoExpediente;
  final String? tiempoOcupada;
  final bool tieneAlerta;
  final String? motivoMantenimiento;

  const BandejaDisponibleModel({
    required this.bandejaID,
    required this.codigo,
    required this.estado,
    this.nombrePaciente,
    this.codigoExpediente,
    this.tiempoOcupada,
    this.tieneAlerta = false,
    this.motivoMantenimiento,
  });

  factory BandejaDisponibleModel.fromJson(Map<String, dynamic> json) {
    return BandejaDisponibleModel(
      bandejaID: json['bandejaID'] as int? ?? 0,
      codigo: json['codigo'] as String? ?? '',
      estado: '',
    );
  }
  factory BandejaDisponibleModel.fromJsonCompleto(
      Map<String, dynamic> json) {
    return BandejaDisponibleModel(
      bandejaID: json['bandejaID'] as int? ?? 0,
      codigo: json['codigo'] as String? ?? '',
      estado: json['estado'] as String? ?? '',
      nombrePaciente: json['nombrePaciente'] as String?,
      codigoExpediente: json['codigoExpediente'] as String?,
      tiempoOcupada: json['tiempoOcupada'] as String?,
      tieneAlerta: json['tieneAlerta'] as bool? ?? false,
      motivoMantenimiento: json['motivoMantenimiento'] as String?,
    );
  }
}

class BandejaService {
  static Future<List<BandejaDisponibleModel>> getDisponibles() async {
    final response = await ApiClient.get(ApiConstants.bandejasDisponibles);
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list
          .map((e) => BandejaDisponibleModel.fromJson(
              e as Map<String, dynamic>))
          .toList();
    }
    throw Exception(
        'Error al obtener bandejas (${response.statusCode})');
  }

  static Future<void> asignarBandeja({
    required int expedienteID,
    required int bandejaID,
  }) async {
    final response = await ApiClient.post(
      ApiConstants.bandejasAsignar,
      {'expedienteID': expedienteID, 'bandejaID': bandejaID},
    );
    if (response.statusCode != 200) {
      final error = jsonDecode(response.body);
      throw Exception(
          error['message'] ?? 'Error al asignar bandeja (${response.statusCode})');
    }
  }
}