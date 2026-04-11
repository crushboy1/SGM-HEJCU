import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../navigation/navigation_service.dart';
import '../utils/app_logger.dart';
import '../../features/auth/services/auth_service.dart';

class ApiClient {
  static const _timeout = Duration(seconds: 10);

  static Future<http.Response> post(
    String endpoint,
    dynamic body, {
    bool requiresAuth = true,
  }) async {
    AppLogger.info('POST $endpoint');
    try {
      final response = await http
          .post(
            Uri.parse(ApiConstants.url(endpoint)),
            headers: {
              ...ApiConstants.defaultHeaders,
              if (requiresAuth) ...AuthService.authHeaders,
            },
            body: jsonEncode(body),
          )
          .timeout(_timeout);
      _handle401(response);
      return response;
    } on SocketException {
      throw Exception('Error de conexion. Verifique su red.');
    } on TimeoutException {
      throw Exception('Tiempo de espera agotado. Intente nuevamente.');
    } catch (e) {
      final msg = e.toString();
      if (msg.contains('http') ||
          msg.contains('://') ||
          msg.contains('Exception: ')) {
        if (e is Exception) rethrow;
      }
      throw Exception('Error inesperado. Intente nuevamente.');
    }
  }

  static Future<http.Response> get(
    String endpoint, {
    bool requiresAuth = true,
  }) async {
    AppLogger.info('GET $endpoint');
    try {
      final response = await http
          .get(
            Uri.parse(ApiConstants.url(endpoint)),
            headers: {
              ...ApiConstants.defaultHeaders,
              if (requiresAuth) ...AuthService.authHeaders,
            },
          )
          .timeout(_timeout);
      _handle401(response);
      return response;
    } on SocketException {
      throw Exception('Error de conexion. Verifique su red.');
    } on TimeoutException {
      throw Exception('Tiempo de espera agotado. Intente nuevamente.');
    } catch (e) {
      final msg = e.toString();
      if (msg.contains('http') ||
          msg.contains('://') ||
          msg.contains('Exception: ')) {
        if (e is Exception) rethrow;
      }
      throw Exception('Error inesperado. Intente nuevamente.');
    }
  }

  // ── Interceptor 401 — con flag anti-loop ─────────────────────────
  static void _handle401(http.Response response) {
    if (response.statusCode == 401) {
      AppLogger.error('401 — sesion expirada, redirigiendo a login');
      AuthService.logout();
      NavigationService.irLogin();
      throw Exception('Sesion expirada. Inicie sesion nuevamente.');
    }
  }
}
