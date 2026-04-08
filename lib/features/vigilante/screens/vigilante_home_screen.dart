import 'package:flutter/material.dart';
import '../../../shared/theme/app_theme.dart';
import '../../../features/auth/services/auth_service.dart';
import '../../../core/constants/routes.dart';

class VigilanteHomeScreen extends StatelessWidget {
  const VigilanteHomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final usuario = AuthService.usuarioActual;
    return Scaffold(
      appBar: AppBar(
        title: const Text('SGM — Vigilante'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await AuthService.logout();
              if (context.mounted) {
                Navigator.pushReplacementNamed(context, Routes.login);
              }
            },
          ),
        ],
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.security_rounded,
                size: 64, color: AppTheme.cyan),
            const SizedBox(height: 16),
            Text('Bienvenido, ${usuario?.nombre ?? ''}',
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            const Text('Módulo Vigilante — En construcción',
                style: TextStyle(color: AppTheme.textGray)),
          ],
        ),
      ),
    );
  }
}