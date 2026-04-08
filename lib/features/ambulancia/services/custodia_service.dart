import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../models/traspaso_model.dart';

class CustodiaService {
  static Future<TraspasoResponseModel> realizarTraspaso({
    required String codigoQR,
    String observaciones = 'Recojo registrado desde app móvil',
  }) async {
    final response = await ApiClient.post(
      ApiConstants.traspasos,
      {'codigoQR': codigoQR, 'observaciones': observaciones},
    );
    if (response.statusCode == 200) {
      return TraspasoResponseModel.fromJson(
          jsonDecode(response.body) as Map<String, dynamic>);
    }
    final error = jsonDecode(response.body);
    throw Exception(
        error['message'] ?? 'Error al realizar traspaso (${response.statusCode})');
  }
}