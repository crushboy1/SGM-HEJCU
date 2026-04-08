import 'dart:convert';
import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_constants.dart';
import '../../../core/models/usuario_model.dart';
import '../../../core/network/api_client.dart';
import '../../../core/storage/secure_storage_service.dart';

class AuthService {
  static UsuarioModel? _usuarioActual;

  static UsuarioModel? get usuarioActual => _usuarioActual;

  static Future<UsuarioModel> login(String username, String password) async {
    final response = await ApiClient.post(
      ApiConstants.login,
      {'username': username, 'password': password},
      requiresAuth: false,
    );

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      final token = json['token'] as String;
      final usuario = UsuarioModel.fromJson(json, token);
      _usuarioActual = usuario;
      await _guardarSesion(usuario);
      return usuario;
      
    } else if (response.statusCode == 401) {
      throw Exception('Credenciales incorrectas');
    } else {
      throw Exception('Error del servidor (${response.statusCode})');
    }
    
  }

  static Future<void> logout() async {
    _usuarioActual = null;
    await SecureStorageService.delete(StorageKeys.token);
    await SecureStorageService.delete(StorageKeys.user);
    await SecureStorageService.delete(StorageKeys.sessionTime);
  }

  static Future<UsuarioModel?> cargarSesion() async {
    final token = await SecureStorageService.read(StorageKeys.token);
    final userJson = await SecureStorageService.read(StorageKeys.user);
    final sessionTime = await SecureStorageService.read(StorageKeys.sessionTime);

    if (token == null || userJson == null) return null;

    // Verificar expiración de sesión
    if (sessionTime != null) {
      final diff = DateTime.now().difference(DateTime.parse(sessionTime));
      if (diff.inMinutes > SessionConfig.timeoutMinutes) {
        await logout();
        return null;
      }
    }
    
    final usuario = UsuarioModel.fromJson(
      jsonDecode(userJson) as Map<String, dynamic>,
      token,
    );
    _usuarioActual = usuario;
    return usuario;
  }
  
  static Future<void> _guardarSesion(UsuarioModel usuario) async {
    await SecureStorageService.write(StorageKeys.token, usuario.token);
    await SecureStorageService.write(
      StorageKeys.user,
      jsonEncode(usuario.toStorageJson()),
    );
    await SecureStorageService.write(
      StorageKeys.sessionTime,
      DateTime.now().toIso8601String(),
    );
  }
  
  // Token vacío → headers vacíos (no manda "Bearer ")
  static Map<String, String> get authHeaders {
    if (_usuarioActual?.token == null) return {};
    return {
      'Authorization': 'Bearer ${_usuarioActual!.token}',
    };
  }
}