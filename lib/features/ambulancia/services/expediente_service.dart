import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../models/expediente_pendiente_model.dart';

class ExpedienteAmbulanciaService {
  static Future<List<ExpedientePendienteModel>> getPendientesRecojo() async {
    final response = await ApiClient.get(ApiConstants.pendientesRecojo);
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list
          .map((e) => ExpedientePendienteModel.fromJson(
              e as Map<String, dynamic>))
          .toList();
    }
    throw Exception('Error al obtener pendientes (${response.statusCode})');
  }
}