enum UserRole {
  ambulancia,
  vigilante,
  supervisor,
  desconocido,
}

class UsuarioModel {
  final int id;
  final String nombre;
  final String username;
  final UserRole rol;
  final String token;

  const UsuarioModel({
    required this.id,
    required this.nombre,
    required this.username,
    required this.rol,
    required this.token,
  });

  factory UsuarioModel.fromJson(Map<String, dynamic> json, String token) {
  return UsuarioModel(
    id: json['id'] as int? ?? 0,
    nombre: json['nombreCompleto'] as String? ?? '',
    username: json['username'] as String? ?? '',
    rol: _mapRol(json['rol'] as String?),
    token: token,
  );
}

  static UserRole _mapRol(String? rol) {
  switch (rol) {
    case 'Ambulancia':
    case 'ambulancia':
      return UserRole.ambulancia;
    case 'VigilanciaMortuorio':
    case 'vigilante':
      return UserRole.vigilante;
    case 'VigilanteSupervisor':
    case 'supervisor':
      return UserRole.supervisor;
    default:
      return UserRole.desconocido;
  }
  
}

  Map<String, dynamic> toStorageJson() => {
    'id': id,
    'nombreCompleto': nombre,
    'username': username,
    'rol': rol.name,
  };

  bool get esAmbulancia => rol == UserRole.ambulancia;
  bool get esVigilante =>
      rol == UserRole.vigilante || rol == UserRole.supervisor;
}