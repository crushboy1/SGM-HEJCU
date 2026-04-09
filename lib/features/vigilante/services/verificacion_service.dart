import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../models/verificacion_model.dart';

class VerificacionService {
  static Future<VerificacionResultadoModel> verificarIngreso(
      VerificacionRequestModel request) async {
    final response = await ApiClient.post(
      ApiConstants.verificacionIngreso,
      request.toJson(),
    );
    if (response.statusCode == 200) {
      return VerificacionResultadoModel.fromJson(
          jsonDecode(response.body) as Map<String, dynamic>);
    }
    final error = jsonDecode(response.body);
    throw Exception(
        error['message'] ?? 'Error al verificar ingreso (${response.statusCode})');
  }
}