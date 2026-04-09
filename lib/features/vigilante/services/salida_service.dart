import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../models/acta_retiro_model.dart';
import '../models/salida_model.dart';

class SalidaService {
  static Future<ActaRetiroModel> getActaRetiro(int expedienteID) async {
    final response = await ApiClient.get(
        '${ApiConstants.actaRetiroPorExpediente}/$expedienteID');
    if (response.statusCode == 200) {
      return ActaRetiroModel.fromJson(
          jsonDecode(response.body) as Map<String, dynamic>);
    }
    final error = jsonDecode(response.body);
    throw Exception(error['mensaje'] ?? error['message'] ??
        'Error al obtener acta de retiro (${response.statusCode})');
  }

  static Future<void> registrarSalida(
      RegistrarSalidaModel model) async {
    final response = await ApiClient.post(
        ApiConstants.salidasRegistrar, model.toJson());
    if (response.statusCode != 200) {
      final error = jsonDecode(response.body);
      throw Exception(error['message'] ??
          'Error al registrar salida (${response.statusCode})');
    }
  }
}