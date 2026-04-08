class ApiConstants {
  static const bool isProd = false;

  static const String _devBaseUrl = 'http://10.0.2.2:5041';
  static const String _prodBaseUrl = 'ip:puerto';

  static String get baseUrl => isProd ? _prodBaseUrl : _devBaseUrl;

  static String url(String endpoint) => '$baseUrl$endpoint';

  static const Map<String, String> defaultHeaders = {
    'Content-Type': 'application/json',
  };

  /// AUTH
  static const String login = '/api/Auth/login';

  /// EXPEDIENTES
  static const String pendientesRecojo = '/api/Expedientes/pendientes-recojo';

  /// QR
  static const String consultarQR = '/api/QR/consultar';

  /// CUSTODIA
  static const String traspasos = '/api/Custodia/traspasos';

  /// BANDEJAS
  static const String bandejasDisponibles = '/api/Bandejas/disponibles';
  static const String bandejasAsignar = '/api/Bandejas/asignar';
  static const String bandejasDashboard = '/api/Bandejas/dashboard';
  /// VERIFICACION
  static const String verificacionIngreso = '/api/verificacion/ingreso';

  /// SALIDAS
  static const String salidasPrellenar = '/api/salidas/prellenar';
  static const String salidasRegistrar = '/api/salidas/registrar';
}
