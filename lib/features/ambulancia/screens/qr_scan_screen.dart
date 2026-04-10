import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../../../shared/theme/app_theme.dart';
import '../../../features/auth/services/auth_service.dart';
import '../../../core/models/usuario_model.dart';
import '../services/custodia_service.dart';

class QrScanScreen extends StatefulWidget {
  /// Si es false, oculta el botón de ingreso manual.
  /// Ambulancia: true (default). Vigilante: false.
  final bool mostrarInputManual;
  
  const QrScanScreen({
    super.key,
    required this.mostrarInputManual,
});

  @override
  State<QrScanScreen> createState() => _QrScanScreenState();
}

class _QrScanScreenState extends State<QrScanScreen>
    with WidgetsBindingObserver {
  final MobileScannerController _scannerCtrl = MobileScannerController();

  bool _procesando = false;
  bool _escaneado = false;
  String? _ultimoCodigo;
  bool get _puedeIngresoManual => AuthService.usuarioActual?.rol == UserRole.ambulancia;

  // Estados válidos para aceptar custodia
  static const _estadosValidos = {'PendienteDeRecojo', 'EnPiso'};

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _scannerCtrl.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _scannerCtrl.start();
    } else if (state == AppLifecycleState.paused) {
      _scannerCtrl.stop();
    }
  }

  // ── Reset centralizado ───────────────────────────────────────────
  Future<void> _resetScanner() async {
    if (!mounted) return;
    setState(() {
      _escaneado = false;
      _procesando = false;
      _ultimoCodigo = null;
    });
    await _scannerCtrl.start();
  }

  // ── Llamada API separada ─────────────────────────────────────────
  Future<Map<String, dynamic>> _consultarExpediente(
      String codigoQR) async {
    final response = await ApiClient.get(
      '${ApiConstants.custodiaConsultarPrevio}/$codigoQR',
    );
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    if (response.statusCode == 404) {
      throw Exception('QR no encontrado. Verifique el brazalete.');
    }
    throw Exception(
        'Error al consultar el expediente (${response.statusCode})');
  }

  // ── Mapeo de errores amigable ────────────────────────────────────
  String _mapError(dynamic e) {
    final msg = e.toString().replaceFirst('Exception: ', '');
    if (msg.toLowerCase().contains('socket') ||
        msg.toLowerCase().contains('connection')) {
      return 'Sin conexion. Verifique su red.';
    }
    return msg;
  }

  // ── Deteccion QR ─────────────────────────────────────────────────
  Future<void> _onQRDetectado(String codigoQR) async {
    if (_procesando || _escaneado || _ultimoCodigo == codigoQR) return;

    _ultimoCodigo = codigoQR;
    setState(() {
      _procesando = true;
      _escaneado = true;
    });
    await _scannerCtrl.stop();
    HapticFeedback.mediumImpact();

    try {
      final expediente = await _consultarExpediente(codigoQR);
      if (!mounted) return;
      await _mostrarConfirmacion(codigoQR, expediente);
    } catch (e) {
      _mostrarError(_mapError(e));
    } finally {
      if (mounted) setState(() => _procesando = false);
    }
  }

  // ── Confirmacion ─────────────────────────────────────────────────
  Future<void> _mostrarConfirmacion(
    String codigoQR,
    Map<String, dynamic> expediente,
  ) async {
    final nombreCompleto =
        expediente['nombreCompleto'] as String? ?? '';
    final hc = expediente['hC'] as String? ??
        expediente['hc'] as String? ?? '';
    final codigo = expediente['codigoExpediente'] as String? ?? '';
    final servicio =
        expediente['servicioFallecimiento'] as String? ?? '';
    final estado = expediente['estadoActual'] as String? ?? '';

    if (!_estadosValidos.contains(estado)) {
      _mostrarError(
          'Este expediente no esta disponible para recojo.\nEstado actual: $estado');
      return;
    }

    final confirmado = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _ConfirmacionSheet(
        codigoQR: codigoQR,
        nombreCompleto: nombreCompleto,
        hc: hc,
        codigoExpediente: codigo,
        servicio: servicio,
      ),
    );

    if (confirmado == true && mounted) {
      await _procesarTraspaso(codigoQR, nombreCompleto);
    } else {
      await _resetScanner();
    }
  }

  // ── Traspaso ─────────────────────────────────────────────────────
  Future<void> _procesarTraspaso(
      String codigoQR, String nombreCompleto) async {
    setState(() => _procesando = true);
    try {
      await CustodiaService.realizarTraspaso(codigoQR: codigoQR);
      if (!mounted) return;
      await _mostrarExito(nombreCompleto);
    } catch (e) {
      if (!mounted) return;
      _mostrarError(_mapError(e));
      await _resetScanner();
    }
  }

  // ── Exito ────────────────────────────────────────────────────────
  Future<void> _mostrarExito(String nombreCompleto) async {
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
            const Text('Custodia Aceptada',
                style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textDark)),
            const SizedBox(height: 8),
            Text('Has recibido el cuerpo de:',
                style:
                    TextStyle(color: AppTheme.textGray, fontSize: 13)),
            const SizedBox(height: 4),
            Text(
              nombreCompleto,
              textAlign: TextAlign.center,
              style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.textDark),
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
              child: const Text('CONTINUAR'),
            ),
          ),
        ],
      ),
    );
  }

  // ── Input manual (solo Ambulancia) ───────────────────────────────
  void _mostrarInputManual() {
    final ctrl = TextEditingController();
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(
            bottom: MediaQuery.of(ctx).viewInsets.bottom),
        child: Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius:
                BorderRadius.vertical(top: Radius.circular(24)),
          ),
          padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Colors.grey.shade300,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 20),
              const Text('Ingreso manual',
                  style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.textDark)),
              const SizedBox(height: 4),
              Text('Ingrese el codigo del expediente SGM',
                  style: TextStyle(
                      fontSize: 13, color: AppTheme.textGray)),
              const SizedBox(height: 16),
              TextField(
                controller: ctrl,
                autofocus: true,
                textCapitalization: TextCapitalization.characters,
                decoration: InputDecoration(
                  hintText: 'Ej: SGM-2026-00001',
                  prefixIcon:
                      const Icon(Icons.qr_code_rounded),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                onSubmitted: (v) {
                  if (v.trim().isNotEmpty) {
                    Navigator.pop(ctx);
                    _onQRDetectado(v.trim());
                  }
                },
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: () {
                    if (ctrl.text.trim().isNotEmpty) {
                      Navigator.pop(ctx);
                      _onQRDetectado(ctrl.text.trim());
                    }
                  },
                  icon: const Icon(Icons.search_rounded),
                  label: const Text('BUSCAR EXPEDIENTE'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Error ────────────────────────────────────────────────────────
  void _mostrarError(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            const Icon(Icons.error_outline_rounded,
                color: Colors.white, size: 18),
            const SizedBox(width: 8),
            Expanded(child: Text(mensaje)),
          ],
        ),
        backgroundColor: AppTheme.red,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10)),
        duration: const Duration(seconds: 4),
      ),
    );
    _resetScanner();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          // ── Camara ─────────────────────────────────────────
          MobileScanner(
            controller: _scannerCtrl,
            onDetect: (capture) {
              if (capture.barcodes.isEmpty) return;
              final barcode = capture.barcodes.first;
              if (barcode.rawValue != null) {
                _onQRDetectado(barcode.rawValue!);
              }
            },
          ),

          // ── Overlay visor QR ────────────────────────────────
          const CustomPaint(
            painter: _QrOverlayPainter(),
            child: SizedBox.expand(),
          ),

          // ── Header ─────────────────────────────────────────
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: Container(
              padding: EdgeInsets.fromLTRB(
                8,
                MediaQuery.of(context).padding.top + 8,
                8,
                16,
              ),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Colors.black.withValues(alpha: 0.7),
                    Colors.transparent,
                  ],
                ),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios_rounded,
                        color: Colors.white),
                    onPressed: () => Navigator.pop(context),
                  ),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Text('Escanear QR',
                            style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.bold)),
                        Text('Apunte al brazalete del fallecido',
                            style: TextStyle(
                                color: Colors.white70,
                                fontSize: 12)),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: ValueListenableBuilder(
                      valueListenable: _scannerCtrl,
                      builder: (ctx, state, _) => Icon(
                        state.torchState == TorchState.on
                            ? Icons.flash_on_rounded
                            : Icons.flash_off_rounded,
                        color: Colors.white,
                      ),
                    ),
                    onPressed: () => _scannerCtrl.toggleTorch(),
                  ),
                ],
              ),
            ),
          ),

          // ── Instruccion inferior ────────────────────────────
          Positioned(
            bottom: 0,
            left: 0,
            right: 0,
            child: Container(
              padding: EdgeInsets.fromLTRB(
                24,
                24,
                24,
                MediaQuery.of(context).padding.bottom + 24,
              ),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.bottomCenter,
                  end: Alignment.topCenter,
                  colors: [
                    Colors.black.withValues(alpha: 0.7),
                    Colors.transparent,
                  ],
                ),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (_procesando)
                    const Column(
                      children: [
                        CircularProgressIndicator(
                            color: Colors.white),
                        SizedBox(height: 12),
                        Text('Procesando...',
                            style: TextStyle(
                                color: Colors.white,
                                fontWeight: FontWeight.bold)),
                      ],
                    )
                  else
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 20, vertical: 10),
                      decoration: BoxDecoration(
                        color:
                            Colors.white.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(
                            color: Colors.white
                                .withValues(alpha: 0.3)),
                      ),
                      child: const Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.qr_code_scanner_rounded,
                              color: Colors.white, size: 18),
                          SizedBox(width: 8),
                          Text('Coloque el QR dentro del recuadro',
                              style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 13)),
                        ],
                      ),
                    ),

                  // Solo Ambulancia ve el ingreso manual
                  if (widget.mostrarInputManual && _puedeIngresoManual) ...[
                    const SizedBox(height: 16),
                    TextButton.icon(
                      onPressed: _mostrarInputManual,
                      icon: const Icon(Icons.keyboard_rounded,
                          color: Colors.white70, size: 18),
                      label: const Text('Ingresar codigo manualmente',
                          style: TextStyle(color: Colors.white70, fontSize: 13)),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ================================================================
// BOTTOM SHEET DE CONFIRMACIÓN
// ================================================================

class _ConfirmacionSheet extends StatelessWidget {
  final String codigoQR;
  final String nombreCompleto;
  final String hc;
  final String codigoExpediente;
  final String servicio;

  const _ConfirmacionSheet({
    required this.codigoQR,
    required this.nombreCompleto,
    required this.hc,
    required this.codigoExpediente,
    required this.servicio,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: EdgeInsets.fromLTRB(
          24, 16, 24, MediaQuery.of(context).padding.bottom + 24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: Colors.grey.shade300,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 20),
          const Text('Confirmar Custodia',
              style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.textDark)),
          const SizedBox(height: 4),
          Text('Verifique los datos antes de aceptar',
              style: TextStyle(
                  fontSize: 13, color: AppTheme.textGray)),
          const SizedBox(height: 20),

          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: const Color(0xFFF0F9FF),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                  color: AppTheme.cyan.withValues(alpha: 0.3)),
            ),
            child: Column(
              children: [
                _DataRow(
                    icon: Icons.person_rounded,
                    label: 'Paciente',
                    value: nombreCompleto,
                    bold: true),
                const Divider(height: 16),
                _DataRow(
                    icon: Icons.badge_outlined,
                    label: 'HC',
                    value: hc,
                    mono: true),
                const Divider(height: 16),
                _DataRow(
                    icon: Icons.qr_code_rounded,
                    label: 'Expediente',
                    value: codigoExpediente,
                    mono: true),
                const Divider(height: 16),
                _DataRow(
                    icon: Icons.local_hospital_rounded,
                    label: 'Servicio',
                    value: servicio),
              ],
            ),
          ),
          const SizedBox(height: 24),

          Row(
            children: [
              Expanded(
                flex: 2,
                child: ElevatedButton.icon(
                  onPressed: () => Navigator.pop(context, true),
                  icon: const Icon(Icons.check_rounded),
                  label: const Text('ACEPTAR CUSTODIA'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.green,
                    padding:
                        const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton(
                  onPressed: () => Navigator.pop(context, false),
                  style: OutlinedButton.styleFrom(
                    padding:
                        const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Text('Cancelar'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _DataRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final bool bold;
  final bool mono;

  const _DataRow({
    required this.icon,
    required this.label,
    required this.value,
    this.bold = false,
    this.mono = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 16, color: AppTheme.cyan),
        const SizedBox(width: 10),
        SizedBox(
          width: 80,
          child: Text(label,
              style: const TextStyle(
                  fontSize: 12, color: AppTheme.textGray)),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 13,
              fontWeight:
                  bold ? FontWeight.bold : FontWeight.w500,
              color: AppTheme.textDark,
              fontFamily: mono ? 'monospace' : null,
            ),
          ),
        ),
      ],
    );
  }
}

// ================================================================
// PAINTER — overlay del visor QR
// ================================================================

class _QrOverlayPainter extends CustomPainter {
  const _QrOverlayPainter();

  @override
  void paint(Canvas canvas, Size size) {
    final paintDark = Paint()
      ..color = Colors.black.withValues(alpha: 0.55);
    const rectSize = 240.0;
    final centerX = size.width / 2;
    final centerY = size.height / 2 - 40;
    final rect = Rect.fromCenter(
      center: Offset(centerX, centerY),
      width: rectSize,
      height: rectSize,
    );

    final path = Path()
      ..addRect(Rect.fromLTWH(0, 0, size.width, size.height))
      ..addRRect(
          RRect.fromRectAndRadius(rect, const Radius.circular(16)))
      ..fillType = PathFillType.evenOdd;
    canvas.drawPath(path, paintDark);

    final paintCorner = Paint()
      ..color = Colors.white
      ..strokeWidth = 3
      ..style = PaintingStyle.stroke;
    const cornerLen = 24.0;
    final r = rect;

    canvas.drawLine(r.topLeft, r.topLeft + const Offset(cornerLen, 0),
        paintCorner);
    canvas.drawLine(r.topLeft, r.topLeft + const Offset(0, cornerLen),
        paintCorner);
    canvas.drawLine(r.topRight,
        r.topRight + const Offset(-cornerLen, 0), paintCorner);
    canvas.drawLine(r.topRight, r.topRight + const Offset(0, cornerLen),
        paintCorner);
    canvas.drawLine(r.bottomLeft,
        r.bottomLeft + const Offset(cornerLen, 0), paintCorner);
    canvas.drawLine(r.bottomLeft,
        r.bottomLeft + const Offset(0, -cornerLen), paintCorner);
    canvas.drawLine(r.bottomRight,
        r.bottomRight + const Offset(-cornerLen, 0), paintCorner);
    canvas.drawLine(r.bottomRight,
        r.bottomRight + const Offset(0, -cornerLen), paintCorner);
  }

  @override
  bool shouldRepaint(_) => false;
}