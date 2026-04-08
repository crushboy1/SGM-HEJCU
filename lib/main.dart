import 'package:flutter/material.dart';
import 'core/models/usuario_model.dart';
import 'core/constants/app_constants.dart';
import 'core/constants/routes.dart';
import 'core/navigation/navigation_helper.dart';
import 'features/auth/screens/login_screen.dart';
import 'features/auth/services/auth_service.dart';
import 'features/ambulancia/screens/ambulancia_home_screen.dart';
import 'features/ambulancia/screens/qr_scan_screen.dart';
import 'features/ambulancia/screens/mapa_mortuorio_screen.dart';
import 'features/vigilante/screens/vigilante_home_screen.dart';
import 'shared/theme/app_theme.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const SgmApp());
}

class SgmApp extends StatelessWidget {
  const SgmApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: AppConstants.appName,
      debugShowCheckedModeBanner: false,
      theme: AppTheme.theme,
      home: const SplashScreen(),
      routes: {
        Routes.login: (_) => const LoginScreen(),
        Routes.ambulancia: (_) => const AmbulanciaHomeScreen(),
        Routes.qrScan: (_) => const QrScanScreen(),
        Routes.mapaBandejas: (_) => const MapaMortuorioScreen(),
        Routes.vigilante: (_) => const VigilanteHomeScreen(),
      },
    );
  }
}

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _verificarSesion();
  }

  Future<void> _verificarSesion() async {
    try {
      final results = await Future.wait([
        AuthService.cargarSesion(),
        Future.delayed(const Duration(milliseconds: 1200)),
      ]);

      if (!mounted) return;

      final usuario = results[0] as UsuarioModel?;
      if (usuario != null) {
        NavigationHelper.navegarSegunRol(context, usuario);
      } else {
        Navigator.pushReplacementNamed(context, Routes.login);
      }
    } catch (_) {
      if (!mounted) return;
      Navigator.pushReplacementNamed(context, Routes.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
          ),
        ),
        child: SafeArea(
          child: Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                // Logo
                Container(
                  width: 110,
                  height: 110,
                  decoration: BoxDecoration(
                    color: Colors.white,
                    shape: BoxShape.circle,
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.2),
                        blurRadius: 24,
                        offset: const Offset(0, 8),
                      ),
                    ],
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Image.asset(
                      'assets/images/logo.png',
                      fit: BoxFit.contain,
                    ),
                  ),
                ),
                const SizedBox(height: 28),
                const Text(
                  'SGM — HEJCU',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  'Sistema de Gestión Mortuoria',
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.75),
                    fontSize: 14,
                  ),
                ),
                const SizedBox(height: 56),
                const CircularProgressIndicator(
                  color: Colors.white,
                  strokeWidth: 2.5,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
