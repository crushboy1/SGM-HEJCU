import 'package:flutter/material.dart';
import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../../../shared/theme/app_theme.dart';
import '../../../core/services/bandeja_service.dart';
import '../models/expediente_pendiente_model.dart';
import 'dart:convert';

class MapaMortuorioScreen extends StatefulWidget {
  const MapaMortuorioScreen({super.key});

  @override
  State<MapaMortuorioScreen> createState() => _MapaMortuorioScreenState();
}

class _MapaMortuorioScreenState extends State<MapaMortuorioScreen> {
  List<BandejaDisponibleModel> _todasBandejas = [];
  bool _isLoading = true;
  String? _error;
  bool _asignando = false;

  // Datos del expediente recibidos por argumentos
  late ExpedientePendienteModel _tarea;

  // Stats calculadas
  int get _disponibles =>
      _todasBandejas.where((b) => b.estado == 'Disponible').length;
  int get _ocupadas =>
      _todasBandejas.where((b) => b.estado == 'Ocupada').length;
  int get _mantenimiento =>
      _todasBandejas.where((b) => b.estado == 'Mantenimiento').length;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _tarea =
        ModalRoute.of(context)!.settings.arguments as ExpedientePendienteModel;
    _cargarBandejas();
  }

  Future<void> _cargarBandejas() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      // Usamos dashboard para tener estado completo de cada bandeja
      final response = await ApiClient.get(ApiConstants.bandejasDashboard);
      if (response.statusCode == 200) {
        final list = jsonDecode(response.body) as List<dynamic>;
        setState(() {
          _todasBandejas = list
              .map((e) => BandejaDisponibleModel.fromJsonCompleto(
                  e as Map<String, dynamic>))
              .toList();
        });
      } else {
        throw Exception('Error ${response.statusCode}');
      }
    } catch (e) {
      setState(
          () => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _confirmarAsignacion(BandejaDisponibleModel bandeja) async {
    final confirmado = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      isDismissible: !_asignando,
      builder: (ctx) => _ConfirmacionAsignacionSheet(
        tarea: _tarea,
        bandeja: bandeja,
        isLoading: _asignando,
      ),
    );

    if (confirmado == true) {
      await _ejecutarAsignacion(bandeja);
    }
  }

  Future<void> _ejecutarAsignacion(BandejaDisponibleModel bandeja) async {
    setState(() => _asignando = true);
    try {
      await BandejaService.asignarBandeja(
        expedienteID: _tarea.expedienteID,
        bandejaID: bandeja.bandejaID,
      );
      if (!mounted) return;
      await _mostrarExito(bandeja.codigo);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString().replaceFirst('Exception: ', '')),
          backgroundColor: AppTheme.red,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10)),
        ),
      );
    } finally {
      if (mounted) setState(() => _asignando = false);
    }
  }

  Future<void> _mostrarExito(String codigoBandeja) async {
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
            const Text(
              'Bandeja Asignada',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppTheme.textDark,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              _tarea.nombreCompleto,
              textAlign: TextAlign.center,
              style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textDark),
            ),
            const SizedBox(height: 4),
            Text(
              'asignado a bandeja $codigoBandeja',
              textAlign: TextAlign.center,
              style:
                  TextStyle(fontSize: 13, color: AppTheme.textGray),
            ),
          ],
        ),
        actions: [
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: () {
                Navigator.of(ctx).pop();
                // Vuelve 2 niveles: mapa → home con refresh
                Navigator.of(context).pop(true);
              },
              child: const Text('VOLVER A MIS TAREAS'),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    final crossAxisCount = width < 600 ? 2 : 4;

    return Scaffold(
      backgroundColor: AppTheme.bgGray,
      body: RefreshIndicator(
        color: AppTheme.cyan,
        onRefresh: _cargarBandejas,
        child: CustomScrollView(
          slivers: [
            // ── AppBar ────────────────────────────────────────
            SliverAppBar(
              pinned: true,
              backgroundColor: AppTheme.cyan,
              leading: IconButton(
                icon: const Icon(Icons.arrow_back_ios_rounded,
                    color: Colors.white),
                onPressed: () => Navigator.pop(context),
              ),
              title: const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Mapa Mortuorio',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    'Asignación de bandeja',
                    style:
                        TextStyle(color: Colors.white70, fontSize: 12),
                  ),
                ],
              ),
            ),

            // ── Banner asignación ─────────────────────────────
            SliverToBoxAdapter(
              child: Container(
                margin: const EdgeInsets.all(16),
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFFE0F7FA), Color(0xFFB2EBF2)],
                  ),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                      color: AppTheme.cyan.withValues(alpha: 0.4)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Step indicator
                    Row(
                      children: [
                        _StepChip(
                          icon: Icons.check_circle_rounded,
                          label: 'QR Escaneado',
                          done: true,
                        ),
                        const SizedBox(width: 8),
                        Icon(Icons.arrow_forward_rounded,
                            size: 16, color: AppTheme.textGray),
                        const SizedBox(width: 8),
                        _StepChip(
                          icon: Icons.grid_view_rounded,
                          label: 'Seleccionar bandeja',
                          done: false,
                          active: true,
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    // Datos paciente
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: AppTheme.cyan,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Icon(Icons.person_add_rounded,
                              color: Colors.white, size: 22),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment:
                                CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Asignar bandeja para:',
                                style: TextStyle(
                                  fontSize: 11,
                                  color: AppTheme.textGray,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              Text(
                                _tarea.nombreCompleto,
                                style: const TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.bold,
                                  color: AppTheme.textDark,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              Text(
                                _tarea.codigoExpediente,
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: AppTheme.textGray,
                                  fontFamily: 'monospace',
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),

            // ── KPIs ──────────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Row(
                  children: [
                    Expanded(
                      child: _KpiChip(
                        label: 'Disponibles',
                        count: _disponibles,
                        color: AppTheme.green,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _KpiChip(
                        label: 'Ocupadas',
                        count: _ocupadas,
                        color: AppTheme.red,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _KpiChip(
                        label: 'Mantenim.',
                        count: _mantenimiento,
                        color: AppTheme.orange,
                      ),
                    ),
                  ],
                ),
              ),
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 12)),

            // ── Leyenda ───────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 10),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                        color: const Color(0xFFE5E7EB)),
                  ),
                  child: Row(
                    mainAxisAlignment:
                        MainAxisAlignment.spaceAround,
                    children: [
                      _LeyendaItem(
                          color: AppTheme.green,
                          label: 'Disponible'),
                      _LeyendaItem(
                          color: AppTheme.red, label: 'Ocupada'),
                      _LeyendaItem(
                          color: AppTheme.orange,
                          label: 'Mantenimiento'),
                    ],
                  ),
                ),
              ),
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 12)),

            // ── Grid bandejas ─────────────────────────────────
            if (_isLoading)
              const SliverFillRemaining(
                child: Center(
                  child: CircularProgressIndicator(
                      color: AppTheme.cyan),
                ),
              )
            else if (_error != null)
              SliverFillRemaining(
                child: Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.wifi_off_rounded,
                          size: 48, color: AppTheme.textGray),
                      const SizedBox(height: 12),
                      Text(_error!,
                          style: const TextStyle(
                              color: AppTheme.textGray)),
                      const SizedBox(height: 16),
                      ElevatedButton.icon(
                        onPressed: _cargarBandejas,
                        icon: const Icon(Icons.refresh_rounded),
                        label: const Text('Reintentar'),
                      ),
                    ],
                  ),
                ),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 32),
                sliver: SliverGrid(
                  delegate: SliverChildBuilderDelegate(
                    (ctx, i) => _BandejaCard(
                      bandeja: _todasBandejas[i],
                      modoAsignacion: true,
                      onTap: () =>
                          _confirmarAsignacion(_todasBandejas[i]),
                    ),
                    childCount: _todasBandejas.length,
                  ),
                  gridDelegate:
                      SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: crossAxisCount,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: 0.85,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

// ================================================================
// WIDGETS INTERNOS
// ================================================================

class _StepChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool done;
  final bool active;

  const _StepChip({
    required this.icon,
    required this.label,
    required this.done,
    this.active = false,
  });

  @override
  Widget build(BuildContext context) {
    final color = done
        ? AppTheme.green
        : active
            ? AppTheme.cyan
            : AppTheme.textGray;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        const SizedBox(width: 4),
        Text(
          label,
          style: TextStyle(
            fontSize: 11,
            color: color,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

class _KpiChip extends StatelessWidget {
  final String label;
  final int count;
  final Color color;

  const _KpiChip({
    required this.label,
    required this.count,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding:
          const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border(left: BorderSide(color: color, width: 3)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 4,
          ),
        ],
      ),
      child: Row(
        children: [
          Text(
            '$count',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
          const SizedBox(width: 6),
          Expanded(
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 10,
                color: AppTheme.textGray,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _LeyendaItem extends StatelessWidget {
  final Color color;
  final String label;

  const _LeyendaItem({required this.color, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration:
              BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(
          label,
          style: const TextStyle(
              fontSize: 11,
              color: AppTheme.textGray,
              fontWeight: FontWeight.w500),
        ),
      ],
    );
  }
}

class _BandejaCard extends StatelessWidget {
  final BandejaDisponibleModel bandeja;
  final bool modoAsignacion;
  final VoidCallback onTap;

  const _BandejaCard({
    required this.bandeja,
    required this.modoAsignacion,
    required this.onTap,
  });

  bool get _esDisponible => bandeja.estado == 'Disponible';
  bool get _esOcupada => bandeja.estado == 'Ocupada';
  bool get _esMantenimiento => bandeja.estado == 'Mantenimiento';

  Color get _color => _esDisponible
      ? AppTheme.green
      : _esOcupada
          ? AppTheme.red
          : AppTheme.orange;

  @override
  Widget build(BuildContext context) {
    final tapeable = modoAsignacion ? _esDisponible : true;

    return IgnorePointer(
      ignoring: !tapeable,
      child: Stack(
        children: [
          // Card principal
          InkWell(
            onTap: tapeable ? onTap : null,
            borderRadius: BorderRadius.circular(16),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: tapeable && modoAsignacion
                      ? _color
                      : _color.withValues(alpha: 0.3),
                  width: tapeable && modoAsignacion ? 2 : 1,
                ),
                boxShadow: [
                  BoxShadow(
                    color: tapeable
                        ? _color.withValues(alpha: 0.15)
                        : Colors.black.withValues(alpha: 0.03),
                    blurRadius: tapeable ? 10 : 4,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: Column(
                children: [
                  // Body
                  Expanded(
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(
                        crossAxisAlignment:
                            CrossAxisAlignment.start,
                        children: [
                          // Código + estado badge
                          Row(
                            mainAxisAlignment:
                                MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                bandeja.codigo,
                                style: const TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold,
                                  color: AppTheme.textDark,
                                ),
                              ),
                              Container(
                                width: 10,
                                height: 10,
                                decoration: BoxDecoration(
                                  color: _color,
                                  shape: BoxShape.circle,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: _color.withValues(alpha: 0.1),
                              borderRadius:
                                  BorderRadius.circular(6),
                            ),
                            child: Text(
                              bandeja.estado,
                              style: TextStyle(
                                fontSize: 10,
                                fontWeight: FontWeight.bold,
                                color: _color,
                              ),
                            ),
                          ),
                          const Spacer(),

                          // Contenido por estado
                          if (_esDisponible)
                            Center(
                              child: Column(
                                children: [
                                  Icon(Icons.archive_rounded,
                                      color: _color
                                          .withValues(alpha: 0.4),
                                      size: 32),
                                  if (modoAsignacion)
                                    Padding(
                                      padding:
                                          const EdgeInsets.only(
                                              top: 4),
                                      child: Text(
                                        'Toca para asignar',
                                        style: TextStyle(
                                          fontSize: 10,
                                          color: _color,
                                          fontWeight:
                                              FontWeight.w600,
                                        ),
                                        textAlign:
                                            TextAlign.center,
                                      ),
                                    ),
                                ],
                              ),
                            )
                          else if (_esOcupada) ...[
                            if (bandeja.nombrePaciente != null)
                              Text(
                                bandeja.nombrePaciente!,
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  color: AppTheme.textDark,
                                ),
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                              ),
                            if (bandeja.tiempoOcupada != null)
                              Padding(
                                padding:
                                    const EdgeInsets.only(top: 4),
                                child: Row(
                                  children: [
                                    Icon(
                                      Icons.access_time_rounded,
                                      size: 11,
                                      color: bandeja.tieneAlerta
                                          ? AppTheme.red
                                          : AppTheme.textGray,
                                    ),
                                    const SizedBox(width: 3),
                                    Text(
                                      bandeja.tiempoOcupada!,
                                      style: TextStyle(
                                        fontSize: 11,
                                        fontWeight:
                                            FontWeight.bold,
                                        color: bandeja.tieneAlerta
                                            ? AppTheme.red
                                            : AppTheme.textGray,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                          ] else if (_esMantenimiento)
                            Center(
                              child: Column(
                                children: [
                                  Icon(Icons.build_rounded,
                                      color: _color
                                          .withValues(alpha: 0.5),
                                      size: 28),
                                  if (bandeja.motivoMantenimiento !=
                                      null)
                                    Padding(
                                      padding:
                                          const EdgeInsets.only(
                                              top: 4),
                                      child: Text(
                                        bandeja
                                            .motivoMantenimiento!,
                                        style: TextStyle(
                                          fontSize: 9,
                                          color: _color,
                                          fontWeight:
                                              FontWeight.w600,
                                        ),
                                        textAlign:
                                            TextAlign.center,
                                        maxLines: 2,
                                        overflow:
                                            TextOverflow.ellipsis,
                                      ),
                                    ),
                                ],
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),

                  // Barra inferior de color
                  Container(
                    height: 4,
                    decoration: BoxDecoration(
                      color: _color,
                      borderRadius: const BorderRadius.vertical(
                        bottom: Radius.circular(16),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Overlay deshabilitado
          if (modoAsignacion && !_esDisponible)
            Positioned.fill(
              child: Container(
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.6),
                  borderRadius: BorderRadius.circular(16),
                ),
              ),
            ),

          // Badge alerta >24h
          if (bandeja.tieneAlerta)
            Positioned(
              top: 6,
              right: 6,
              child: Container(
                padding: const EdgeInsets.symmetric(
                    horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: AppTheme.red,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: const Text(
                  '>24h',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 9,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

// ================================================================
// BOTTOM SHEET CONFIRMACIÓN ASIGNACIÓN
// ================================================================

class _ConfirmacionAsignacionSheet extends StatelessWidget {
  final ExpedientePendienteModel tarea;
  final BandejaDisponibleModel bandeja;
  final bool isLoading;

  const _ConfirmacionAsignacionSheet({
    required this.tarea,
    required this.bandeja,
    required this.isLoading,
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
          const Text(
            'Confirmar Asignación',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: AppTheme.textDark,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Verifique los datos antes de confirmar',
            style: TextStyle(fontSize: 13, color: AppTheme.textGray),
          ),
          const SizedBox(height: 20),

          // Card datos
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: const Color(0xFFF0FFF4),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                  color: AppTheme.green.withValues(alpha: 0.3)),
            ),
            child: Column(
              children: [
                Row(
                  children: [
                    const Icon(Icons.person_rounded,
                        size: 16, color: AppTheme.green),
                    const SizedBox(width: 10),
                    const SizedBox(
                      width: 80,
                      child: Text('Paciente',
                          style: TextStyle(
                              fontSize: 12,
                              color: AppTheme.textGray)),
                    ),
                    Expanded(
                      child: Text(
                        tarea.nombreCompleto,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.textDark,
                        ),
                      ),
                    ),
                  ],
                ),
                const Divider(height: 16),
                Row(
                  children: [
                    const Icon(Icons.qr_code_rounded,
                        size: 16, color: AppTheme.green),
                    const SizedBox(width: 10),
                    const SizedBox(
                      width: 80,
                      child: Text('Expediente',
                          style: TextStyle(
                              fontSize: 12,
                              color: AppTheme.textGray)),
                    ),
                    Expanded(
                      child: Text(
                        tarea.codigoExpediente,
                        style: const TextStyle(
                          fontSize: 13,
                          fontFamily: 'monospace',
                          fontWeight: FontWeight.w500,
                          color: AppTheme.textDark,
                        ),
                      ),
                    ),
                  ],
                ),
                const Divider(height: 16),
                Row(
                  children: [
                    const Icon(Icons.grid_view_rounded,
                        size: 16, color: AppTheme.green),
                    const SizedBox(width: 10),
                    const SizedBox(
                      width: 80,
                      child: Text('Bandeja',
                          style: TextStyle(
                              fontSize: 12,
                              color: AppTheme.textGray)),
                    ),
                    Text(
                      bandeja.codigo,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.green,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: isLoading
                      ? null
                      : () => Navigator.pop(context, false),
                  style: OutlinedButton.styleFrom(
                    padding:
                        const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Text('Cancelar'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 2,
                child: ElevatedButton.icon(
                  onPressed: isLoading
                      ? null
                      : () => Navigator.pop(context, true),
                  icon: isLoading
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2,
                          ),
                        )
                      : const Icon(Icons.check_rounded),
                  label: Text(
                      isLoading ? 'Asignando...' : 'CONFIRMAR'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.green,
                    padding:
                        const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}