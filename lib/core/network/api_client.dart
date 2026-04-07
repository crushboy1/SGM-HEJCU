import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../../features/auth/services/auth_service.dart';

class ApiClient {
  static Future<http.Response> post(
    String endpoint,
    dynamic body, {
    bool requiresAuth = true,
  }) async {
    try {
      return await http.post(
        Uri.parse(ApiConstants.url(endpoint)),
        headers: {
          ...ApiConstants.defaultHeaders,
          if (requiresAuth) ...AuthService.authHeaders,
        },
        body: jsonEncode(body),
      );
    } catch (_) {
      throw Exception('Error de conexión. Verifique su red.');
    }
  }

  static Future<http.Response> get(
    String endpoint, {
    bool requiresAuth = true,
  }) async {
    try {
      return await http.get(
        Uri.parse(ApiConstants.url(endpoint)),
        headers: {
          ...ApiConstants.defaultHeaders,
          if (requiresAuth) ...AuthService.authHeaders,
        },
      );
    } catch (_) {
      throw Exception('Error de conexión. Verifique su red.');
    }
  }
}