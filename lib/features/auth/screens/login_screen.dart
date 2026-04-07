import 'package:flutter/material.dart';
import '../../../core/navigation/navigation_helper.dart';
import '../../../core/constants/app_constants.dart';
import '../../../shared/theme/app_theme.dart';
import '../../../shared/widgets/sgm_button.dart';
import '../services/auth_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _isLoading = false;
  bool _obscurePassword = true;
  String? _errorMsg;

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _isLoading = true;
      _errorMsg = null;
    });
    try {
      final usuario = await AuthService.login(
        _usernameCtrl.text.trim(),
        _passwordCtrl.text.trim(),
      );
      if (mounted) NavigationHelper.navegarSegunRol(context, usuario);
    } catch (e) {
      setState(() {
        _errorMsg = e is Exception
            ? e.toString().replaceFirst('Exception: ', '')
            : 'Error inesperado';
      });
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    final isTablet = width >= Breakpoints.mobile;

    return Scaffold(
      body: isTablet ? _buildTablet() : _buildMobile(),
    );
  }

  // ================================================================
  // LAYOUT MÓVIL
  // ================================================================
  Widget _buildMobile() {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
        ),
      ),
      child: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final keyboardVisible =
                MediaQuery.of(context).viewInsets.bottom > 100;
            return SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  minHeight: constraints.maxHeight,
                ),
                child: IntrinsicHeight(
                  child: Column(
                    children: [
                      SizedBox(height: keyboardVisible ? 20 : 48),

                      // Logo
                      AnimatedContainer(
                        duration: const Duration(milliseconds: 200),
                        width: keyboardVisible ? 64 : 100,
                        height: keyboardVisible ? 64 : 100,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          shape: BoxShape.circle,
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.2),
                              blurRadius: 20,
                              offset: const Offset(0, 8),
                            ),
                          ],
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Image.asset(
                            'assets/images/logo.png',
                            fit: BoxFit.contain,
                          ),
                        ),
                      ),

                      SizedBox(height: keyboardVisible ? 12 : 24),

                      // Título
                      if (!keyboardVisible) ...[
                        const Text(
                          'SGM — HEJCU',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 22,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 1,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Sistema de Gestión Mortuoria',
                          style: TextStyle(
                            color: Colors.white.withValues(alpha: 0.75),
                            fontSize: 13,
                          ),
                        ),
                        const SizedBox(height: 32),
                      ] else
                        const SizedBox(height: 8),

                      // Card form
                      Card(
                        elevation: 8,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(24),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(28),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Bienvenido',
                                style: TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF1F2937),
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                'Ingrese sus credenciales para continuar',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: AppTheme.textGray,
                                ),
                              ),
                              const SizedBox(height: 24),
                              _buildForm(),
                            ],
                          ),
                        ),
                      ),

                      const Spacer(),
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        child: Text(
                          '© 2025 Hospital de Emergencias José Casimiro Ulloa',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: Colors.white.withValues(alpha: 0.5),
                            fontSize: 11,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }

  // ================================================================
  // LAYOUT TABLET — 2 columnas
  // ================================================================
  Widget _buildTablet() {
    return Row(
      children: [
        // Columna izquierda — branding
        Expanded(
          child: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
              ),
            ),
            child: SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(48),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Logo
                    Container(
                      width: 120,
                      height: 120,
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
                        padding: const EdgeInsets.all(16),
                        child: Image.asset(
                          'assets/images/logo.png',
                          fit: BoxFit.contain,
                        ),
                      ),
                    ),
                    const SizedBox(height: 40),
                    const Text(
                      'Sistema de Gestión\nMortuoria',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 32,
                        fontWeight: FontWeight.bold,
                        height: 1.2,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Text(
                      'Hospital de Emergencias\nJosé Casimiro Ulloa',
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.8),
                        fontSize: 16,
                        height: 1.5,
                      ),
                    ),
                    const SizedBox(height: 48),
                    // Features
                    _buildFeature(
                      Icons.access_time_rounded,
                      'Gestión en Tiempo Real',
                    ),
                    const SizedBox(height: 20),
                    _buildFeature(
                      Icons.qr_code_scanner_rounded,
                      'Trazabilidad con QR',
                    ),
                    const SizedBox(height: 20),
                    _buildFeature(
                      Icons.notifications_active_rounded,
                      'Notificaciones Automáticas',
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),

        // Columna derecha — form
        Expanded(
          child: Container(
            color: Colors.white,
            child: SafeArea(
              child: Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 64,
                    vertical: 32,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Bienvenido',
                        style: TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF1F2937),
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Ingrese sus credenciales para continuar',
                        style: TextStyle(
                          fontSize: 14,
                          color: AppTheme.textGray,
                        ),
                      ),
                      const SizedBox(height: 40),
                      _buildForm(),
                      const SizedBox(height: 32),
                      Center(
                        child: Text(
                          '© 2025 Hospital de Emergencias José Casimiro Ulloa\nSistema de Gestión Mortuoria v1.0',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 11,
                            color: AppTheme.textGray,
                            height: 1.6,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }

  // ================================================================
  // FORM — compartido móvil y tablet
  // ================================================================
  Widget _buildForm() {
    return Form(
      key: _formKey,
      child: Column(
        children: [
          // Usuario
          TextFormField(
            controller: _usernameCtrl,
            autofocus: true,
            decoration: const InputDecoration(
              labelText: 'Usuario',
              prefixIcon: Icon(Icons.person_outline_rounded),
            ),
            keyboardType: TextInputType.text,
            textInputAction: TextInputAction.next,
            validator: (v) =>
                v == null || v.trim().isEmpty ? 'Ingrese su usuario' : null,
          ),
          const SizedBox(height: 16),

          // Contraseña
          TextFormField(
            controller: _passwordCtrl,
            decoration: InputDecoration(
              labelText: 'Contraseña',
              prefixIcon: const Icon(Icons.lock_outline_rounded),
              suffixIcon: IconButton(
                icon: Icon(
                  _obscurePassword
                      ? Icons.visibility_off_outlined
                      : Icons.visibility_outlined,
                ),
                onPressed: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
              ),
            ),
            obscureText: _obscurePassword,
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _login(),
            validator: (v) =>
                v == null || v.trim().isEmpty ? 'Ingrese su contraseña' : null,
          ),
          const SizedBox(height: 20),

          // Error
          if (_errorMsg != null) ...[
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppTheme.red.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(10),
                border: Border(
                  left: BorderSide(color: AppTheme.red, width: 4),
                ),
              ),
              child: Row(
                children: [
                  Icon(Icons.error_outline_rounded,
                      color: AppTheme.red, size: 18),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      _errorMsg!,
                      style: TextStyle(color: AppTheme.red, fontSize: 13),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
          ],

          SgmButton(
            label: 'INGRESAR AL SISTEMA',
            icon: Icons.login_rounded,
            isLoading: _isLoading,
            onPressed: _isLoading ? null : _login,
          ),
        ],
      ),
    );
  }

  // ================================================================
  // FEATURE ITEM — solo tablet
  // ================================================================
  Widget _buildFeature(IconData icon, String texto) {
    return Row(
      children: [
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: .15),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, color: Colors.white, size: 22),
        ),
        const SizedBox(width: 16),
        Text(
          texto,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 15,
            fontWeight: FontWeight.w500,
          ),
        ),
      ],
    );
  }
}