import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../../ambulancia/screens/qr_scan_screen.dart';
import '../../../shared/theme/app_theme.dart';
import '../models/verificacion_model.dart';
import '../services/verificacion_error_mapper.dart';
import '../services/verificacion_service.dart';

class VerificacionScreen extends StatefulWidget {
  const VerificacionScreen({super.key});

  @override
  State<VerificacionScreen> createState() => _VerificacionScreenState();
}

class _VerificacionScreenState extends State<VerificacionScreen> {
  final _inputCtrl = TextEditingController();
  final _focusNode = FocusNode();

  String _paso = 'escanear';
  bool _isLoading = false;
  Map<String, dynamic>? _datosExpediente;

  @override
  void dispose() {
    _inputCtrl.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  // ── Getters de dominio ───────────────────────────────────────────
  bool get _esNN => _datosExpediente?['esNN'] == true;
  bool get _esCausaViolenta =>
      _datosExpediente?['causaViolentaODudosa'] == true;

  String get _documentoDisplay {
    if (_esNN) return 'No Identificado (NN)';
    final tipo = _datosExpediente?['tipoDocumento'] as String? ?? '';
    final num = _datosExpediente?['numeroDocumento'] as String? ?? '';
    if (tipo.isNotEmpty && num.isNotEmpty) return '$tipo: $num';
    return num.isNotEmpty ? num : '—';
  }

  // ── Abrir camara principal ───────────────────────────────────────
  void _abrirCamara() {
  Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => const QrScanScreen(mostrarInputManual: false),
    ),
  ).then((codigo) {
    if (codigo != null && codigo is String && codigo.trim().isNotEmpty) {
      _inputCtrl.text = codigo.trim();
      _buscarQR(codigo.trim());
    }
  });
}

  // ── PASO 1: Buscar expediente por QR ────────────────────────────
  // Solo se llama al presionar el botón o al retornar de la cámara
  // NO se llama con cada caracter del input
  Future<void> _buscarQR([String? codigoOverride]) async {
    final codigo = (codigoOverride ?? _inputCtrl.text).trim();
    if (codigo.isEmpty) return;
    FocusScope.of(context).unfocus();
    setState(() => _isLoading = true);

    try {
      final response = await ApiClient.get(
        '${ApiConstants.consultarQR}/$codigo',
      );
      if (!mounted) return;

      if (response.statusCode == 200) {
        final json =
            jsonDecode(response.body) as Map<String, dynamic>;
        setState(() {
          _datosExpediente = json;
          _paso = 'validar';
        });
      } else {
        final error = jsonDecode(response.body);
        final msg = error['message'] as String? ??
            'No se pudo consultar el expediente (${response.statusCode})';
        _mostrarError(msg);
      }
    } catch (e) {
      _mostrarError('Error de conexion. Verifique su red.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  // ── PASO 2: Confirmar verificacion ──────────────────────────────
  Future<void> _confirmarIngreso() async {
    if (_datosExpediente == null) return;
    setState(() => _isLoading = true);

    final request = VerificacionRequestModel(
      codigoExpedienteBrazalete:
          _datosExpediente!['codigoExpediente'] as String? ?? '',
      hcBrazalete: _datosExpediente!['hC'] as String? ??
          _datosExpediente!['hc'] as String? ?? '',
      tipoDocumentoBrazalete:
          _datosExpediente!['tipoDocumento'] as String? ?? '',
      numeroDocumentoBrazalete:
          _datosExpediente!['numeroDocumento'] as String? ?? '',
      nombreCompletoBrazalete:
          _datosExpediente!['nombreCompleto'] as String? ?? '',
      servicioBrazalete:
          _datosExpediente!['servicioFallecimiento'] as String? ?? '',
    );

    try {
      final resultado =
          await VerificacionService.verificarIngreso(request);
      if (!mounted) return;

      if (resultado.aprobada) {
        await _mostrarExito(resultado);
      } else {
        _mostrarResultadoRechazo(resultado);
      }
    } catch (e) {
      if (!mounted) return;
      final msg = e.toString().replaceFirst('Exception: ', '');
      final info = VerificacionErrorMapper.resolver(msg);
      _mostrarErrorInfo(info);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _mostrarExito(
      VerificacionResultadoModel resultado) async {
    final codigo =
        _datosExpediente?['codigoExpediente'] ?? '';
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: Color(0xFFDCFCE7),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.check_rounded,
                  color: AppTheme.green, size: 48),
            ),
            const SizedBox(height: 16),
            const Text('Ingreso Registrado',
                style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textDark)),
            const SizedBox(height: 8),
            Text(
              'El expediente $codigo ingreso al mortuorio.',
              textAlign: TextAlign.center,
              style: TextStyle(
                  fontSize: 13, color: AppTheme.textGray),
            ),
            const SizedBox(height: 4),
            Text(
              'Ahora debe asignarse a una bandeja.',
              textAlign: TextAlign.center,
              style: TextStyle(
                  fontSize: 12, color: AppTheme.textGray),
            ),
          ],
        ),
        actions: [
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: () {
                Navigator.of(ctx).pop();
                Navigator.of(context).pop(true);
              },
              child: const Text('ENTENDIDO'),
            ),
          ),
        ],
      ),
    );
  }

  void _mostrarResultadoRechazo(
      VerificacionResultadoModel resultado) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
            resultado.motivoRechazo ?? resultado.mensajeResultado),
        backgroundColor: AppTheme.orange,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10)),
        duration: const Duration(seconds: 5),
      ),
    );
  }

  void _mostrarErrorInfo(VerificacionErrorInfo info) {
    final color = info.tipo == VerificacionErrorTipo.error
        ? AppTheme.red
        : info.tipo == VerificacionErrorTipo.warning
            ? AppTheme.orange
            : AppTheme.cyan;

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            Icon(
              info.tipo == VerificacionErrorTipo.error
                  ? Icons.error_outline_rounded
                  : info.tipo == VerificacionErrorTipo.warning
                      ? Icons.warning_amber_rounded
                      : Icons.info_outline_rounded,
              color: color,
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Text(info.title,
                  style: const TextStyle(fontSize: 16)),
            ),
          ],
        ),
        content: Text(info.text,
            style: const TextStyle(
                fontSize: 14, color: AppTheme.textGray)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('ACEPTAR'),
          ),
        ],
      ),
    );
  }

  void _mostrarError(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(mensaje),
        backgroundColor: AppTheme.red,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  void _cancelar() {
    setState(() {
      _paso = 'escanear';
      _datosExpediente = null;
      _inputCtrl.clear();
    });
    Future.delayed(const Duration(milliseconds: 100),
        () => _focusNode.requestFocus());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.bgGray,
      body: Column(
        children: [
          _buildHeader(),
          Expanded(
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 250),
              transitionBuilder: (child, animation) =>
                  FadeTransition(opacity: animation, child: child),
              child: _paso == 'escanear'
                  ? _buildPaso1()
                  : _buildPaso2(),
            ),
          ),
        ],
      ),
      bottomNavigationBar:
          _paso == 'validar' ? _buildStickyButtons() : null,
    );
  }

  // ================================================================
  // HEADER con step indicator
  // ================================================================
  Widget _buildHeader() {
    return Container(
      padding: EdgeInsets.fromLTRB(
          20, MediaQuery.of(context).padding.top + 12, 20, 16),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
        ),
      ),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back_ios_rounded,
                color: Colors.white),
            onPressed: () => Navigator.pop(context),
            padding: EdgeInsets.zero,
          ),
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.2),
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(Icons.qr_code_scanner_rounded,
                color: Colors.white, size: 22),
          ),
          const SizedBox(width: 12),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Verificar Ingreso QR',
                    style: TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                        fontWeight: FontWeight.bold)),
                Text('Vigilancia Mortuorio',
                    style:
                        TextStyle(color: Colors.white70, fontSize: 11)),
              ],
            ),
          ),
          Row(
            children: [
              _StepCircle(
                  numero: '1',
                  activo: _paso == 'escanear',
                  completado: _paso == 'validar'),
              AnimatedContainer(
                duration: const Duration(milliseconds: 300),
                width: 24,
                height: 2,
                color: _paso == 'validar'
                    ? Colors.white
                    : Colors.white.withValues(alpha: 0.3),
              ),
              _StepCircle(
                  numero: '2',
                  activo: _paso == 'validar',
                  completado: false),
            ],
          ),
        ],
      ),
    );
  }

  // ================================================================
  // PASO 1 — ESCANEAR QR
  // ================================================================
  Widget _buildPaso1() {
    return SingleChildScrollView(
      key: const ValueKey('paso1'),
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          const SizedBox(height: 16),

          // ── Boton principal: ícono QR grande abre camara ─────
          GestureDetector(
            onTap: _isLoading ? null : _abrirCamara,
            child: Container(
              padding: const EdgeInsets.all(28),
              decoration: BoxDecoration(
                color: AppTheme.cyan.withValues(alpha: 0.08),
                shape: BoxShape.circle,
                border: Border.all(
                    color: AppTheme.cyan.withValues(alpha: 0.25),
                    width: 2),
              ),
              child: Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: AppTheme.cyan.withValues(alpha: 0.12),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.qr_code_scanner_rounded,
                  color: AppTheme.cyan,
                  size: 56,
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),

          const Text('Escanear Brazalete',
              style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.textDark)),
          const SizedBox(height: 6),
          Text(
            'Toque el ícono para abrir la camara\no ingrese el codigo manualmente',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 13, color: AppTheme.textGray),
          ),
          const SizedBox(height: 28),

          // ── Separador ───────────────────────────────────────
          Row(
            children: [
              Expanded(
                  child: Divider(
                      color: AppTheme.textGray.withValues(alpha: 0.2))),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Text('Ingresar manualmente',
                    style: TextStyle(
                        fontSize: 11, color: AppTheme.textGray)),
              ),
              Expanded(
                  child: Divider(
                      color: AppTheme.textGray.withValues(alpha: 0.2))),
            ],
          ),
          const SizedBox(height: 16),

          // ── Input manual — sin debounce, solo submit/boton ──
          TextField(
            controller: _inputCtrl,
            focusNode: _focusNode,
            textCapitalization: TextCapitalization.characters,
            style:
                const TextStyle(fontFamily: 'monospace', fontSize: 15),
            decoration: InputDecoration(
              hintText: 'Ingrese codigo SGM (ej: SGM-2026-00001)',
              hintStyle: TextStyle(
                  fontFamily: 'sans-serif',
                  fontSize: 13,
                  color: AppTheme.textGray),
              prefixIcon:
                  const Icon(Icons.edit_rounded, color: AppTheme.cyan),
            ),
            // Sin onChanged — no dispara busqueda por caracter
            onSubmitted: (_) => _buscarQR(),
          ),
          const SizedBox(height: 12),

          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: _isLoading ? null : _buscarQR,
              icon: _isLoading
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                          color: Colors.white, strokeWidth: 2))
                  : const Icon(Icons.search_rounded),
              label: Text(
                  _isLoading ? 'BUSCANDO...' : 'BUSCAR EXPEDIENTE'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),
          const SizedBox(height: 16),

          // ── Tip info ─────────────────────────────────────────
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFFEFF6FF),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFFBFDBFE)),
            ),
            child: Row(
              children: [
                const Icon(Icons.info_outline_rounded,
                    color: Color(0xFF3B82F6), size: 16),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    'Si usa lector fisico QR, enfoque el campo y escanee — el codigo se ingresara automaticamente.',
                    style: TextStyle(
                        fontSize: 12,
                        color: const Color(0xFF1D4ED8)),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ================================================================
  // PASO 2 — VALIDAR Y CONFIRMAR
  // ================================================================
  Widget _buildPaso2() {
    if (_datosExpediente == null) return const SizedBox.shrink();
    final exp = _datosExpediente!;
    final estado = exp['estadoActual'] as String? ?? '';

    return SingleChildScrollView(
      key: const ValueKey('paso2'),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (_esNN) ...[
            _AlertaBanner(
              icon: Icons.warning_amber_rounded,
              titulo: 'Paciente No Identificado (NN)',
              mensaje:
                  'Verifique que la etiqueta interna de la bolsa coincida con este expediente.',
              color: AppTheme.orange,
              bgColor: const Color(0xFFFFF7ED),
            ),
            const SizedBox(height: 8),
          ],
          if (_esCausaViolenta) ...[
            _AlertaBanner(
              icon: Icons.shield_rounded,
              titulo: 'Causa Violenta o Dudosa',
              mensaje: 'Requiere intervencion policial para el retiro.',
              color: AppTheme.red,
              bgColor: const Color(0xFFFEF2F2),
            ),
            const SizedBox(height: 8),
          ],

          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                  color: AppTheme.cyan.withValues(alpha: 0.3)),
              boxShadow: [
                BoxShadow(
                  color: AppTheme.cyan.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Column(
              children: [
                // Header cyan
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppTheme.cyan.withValues(alpha: 0.06),
                    borderRadius: const BorderRadius.vertical(
                        top: Radius.circular(16)),
                    border: Border(
                        bottom: BorderSide(
                            color:
                                AppTheme.cyan.withValues(alpha: 0.15))),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('NOMBRES',
                                style: TextStyle(
                                    fontSize: 10,
                                    color: AppTheme.textGray,
                                    fontWeight: FontWeight.bold,
                                    letterSpacing: 0.5)),
                            const SizedBox(height: 2),
                            Text(
                              exp['nombreCompleto'] as String? ?? '',
                              style: const TextStyle(
                                  fontSize: 17,
                                  fontWeight: FontWeight.bold,
                                  color: AppTheme.textDark),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text('CODIGO',
                              style: TextStyle(
                                  fontSize: 10,
                                  color: AppTheme.textGray,
                                  fontWeight: FontWeight.bold,
                                  letterSpacing: 0.5)),
                          const SizedBox(height: 2),
                          Text(
                            exp['codigoExpediente'] as String? ?? '',
                            style: const TextStyle(
                                fontSize: 14,
                                fontFamily: 'monospace',
                                fontWeight: FontWeight.bold,
                                color: AppTheme.cyan),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // Grid datos
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: _DatoItem(
                              label: 'Historia Clinica',
                              value: exp['hC'] as String? ??
                                  exp['hc'] as String? ?? '—',
                              mono: true,
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: _DatoItem(
                              label: 'Documento',
                              value: _documentoDisplay,
                              mono: true,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: _DatoItem(
                              label: 'Servicio de Origen',
                              value: exp['servicioFallecimiento']
                                      as String? ??
                                  '—',
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: _DatoItem(
                              label: 'Estado',
                              value: estado,
                              isEstado: true,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // Sello verificacion
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 10),
                  decoration: BoxDecoration(
                    color: AppTheme.green.withValues(alpha: 0.05),
                    borderRadius: const BorderRadius.vertical(
                        bottom: Radius.circular(16)),
                    border: Border(
                        top: BorderSide(
                            color:
                                AppTheme.green.withValues(alpha: 0.15))),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.verified_rounded,
                          color: AppTheme.green, size: 14),
                      const SizedBox(width: 6),
                      Text(
                        'Datos verificados contra la base de datos del SGM',
                        style: TextStyle(
                            fontSize: 11,
                            color:
                                AppTheme.green.withValues(alpha: 0.8),
                            fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ================================================================
  // BOTONES STICKY
  // ================================================================
  Widget _buildStickyButtons() {
    return Container(
      padding: EdgeInsets.fromLTRB(
          16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.08),
            blurRadius: 16,
            offset: const Offset(0, -4),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: _isLoading ? null : _confirmarIngreso,
              icon: _isLoading
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                          color: Colors.white, strokeWidth: 2))
                  : const Icon(Icons.check_circle_rounded),
              label: Text(
                  _isLoading ? 'PROCESANDO...' : 'AUTORIZAR INGRESO'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.green,
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: _isLoading ? null : _cancelar,
              icon:
                  const Icon(Icons.arrow_back_rounded, size: 18),
              label: const Text('CANCELAR Y VOLVER A ESCANEAR'),
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 12),
                foregroundColor: AppTheme.textGray,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ================================================================
// WIDGETS INTERNOS
// ================================================================

class _StepCircle extends StatelessWidget {
  final String numero;
  final bool activo;
  final bool completado;

  const _StepCircle({
    required this.numero,
    required this.activo,
    required this.completado,
  });

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 300),
      width: 28,
      height: 28,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: activo
            ? Colors.white
            : completado
                ? const Color(0xFF4ADE80)
                : Colors.white.withValues(alpha: 0.2),
        border: Border.all(
          color: activo
              ? Colors.white
              : completado
                  ? const Color(0xFF4ADE80)
                  : Colors.white.withValues(alpha: 0.4),
          width: 2,
        ),
      ),
      child: Center(
        child: completado
            ? const Icon(Icons.check_rounded,
                color: Colors.white, size: 14)
            : Text(
                numero,
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: activo
                      ? AppTheme.cyan
                      : Colors.white.withValues(alpha: 0.6),
                ),
              ),
      ),
    );
  }
}

class _AlertaBanner extends StatelessWidget {
  final IconData icon;
  final String titulo;
  final String mensaje;
  final Color color;
  final Color bgColor;

  const _AlertaBanner({
    required this.icon,
    required this.titulo,
    required this.mensaje,
    required this.color,
    required this.bgColor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.4)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(titulo,
                    style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: color)),
                const SizedBox(height: 2),
                Text(mensaje,
                    style: TextStyle(
                        fontSize: 12,
                        color: color.withValues(alpha: 0.8))),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _DatoItem extends StatelessWidget {
  final String label;
  final String value;
  final bool mono;
  final bool isEstado;

  const _DatoItem({
    required this.label,
    required this.value,
    this.mono = false,
    this.isEstado = false,
  });

  Color _estadoColor(String estado) {
    switch (estado) {
      case 'EnTrasladoMortuorio':
        return AppTheme.orange;
      case 'PendienteAsignacionBandeja':
        return AppTheme.cyan;
      case 'EnBandeja':
        return AppTheme.green;
      default:
        return AppTheme.textGray;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: const TextStyle(
                fontSize: 10,
                color: AppTheme.textGray,
                fontWeight: FontWeight.w500)),
        const SizedBox(height: 3),
        if (isEstado)
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: _estadoColor(value).withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              value,
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: _estadoColor(value)),
            ),
          )
        else
          Text(
            value,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppTheme.textDark,
              fontFamily: mono ? 'monospace' : null,
            ),
          ),
      ],
    );
  }
}