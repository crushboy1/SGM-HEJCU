import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../models/expediente_vigilante_model.dart';

class ExpedienteVigilanteService {
  static Future<List<ExpedienteVigilanteModel>>
      getPendientesRetiro() async {
    final response = await ApiClient.get(ApiConstants.expedientesGetAll);
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list
          .map((e) => ExpedienteVigilanteModel.fromJson(
              e as Map<String, dynamic>))
          .where((e) => e.estadoActual == 'PendienteRetiro')
          .toList();
    }
    throw Exception(
        'Error al obtener expedientes (${response.statusCode})');
  }
}