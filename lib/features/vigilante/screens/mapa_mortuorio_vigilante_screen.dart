import 'package:flutter/material.dart';
import '../../../core/services/bandeja_service.dart';
import '../../../shared/theme/app_theme.dart';

class MapaMortuorioVigilanteScreen extends StatefulWidget {
  const MapaMortuorioVigilanteScreen({super.key});

  @override
  State<MapaMortuorioVigilanteScreen> createState() =>
      _MapaMortuorioVigilanteScreenState();
}

class _MapaMortuorioVigilanteScreenState
    extends State<MapaMortuorioVigilanteScreen> {
  List<BandejaDisponibleModel> _bandejas = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _cargarBandejas();
  }

  Future<void> _cargarBandejas() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final bandejas = await BandejaService.getDashboard();
      if (mounted) setState(() => _bandejas = bandejas);
    } catch (e) {
      if (mounted) {
        setState(() =>
            _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  // ── KPIs calculados ──────────────────────────────────────────────
  int get _total => _bandejas.length;
  int get _ocupadas =>
      _bandejas.where((b) => b.estado == 'Ocupada').length;
  int get _disponibles =>
      _bandejas.where((b) => b.estado == 'Disponible').length;
  int get _alertas =>
      _bandejas.where((b) => b.tieneAlerta).length;
  int get _mantenimiento =>
      _bandejas.where((b) => b.estado == 'Mantenimiento').length;

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      color: AppTheme.cyan,
      onRefresh: _cargarBandejas,
      child: _isLoading
          ? const Center(
              child: CircularProgressIndicator(color: AppTheme.cyan))
          : _error != null
              ? _buildError()
              : CustomScrollView(
                  slivers: [
                    SliverToBoxAdapter(child: _buildKPIs()),
                    SliverToBoxAdapter(child: _buildLeyenda()),
                    if (_alertas > 0)
                      SliverToBoxAdapter(child: _buildAlertaBanner()),
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                      sliver: _buildGrid(),
                    ),
                    const SliverToBoxAdapter(
                        child: SizedBox(height: 24)),
                  ],
                ),
    );
  }

  // ── Error ────────────────────────────────────────────────────────
  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.wifi_off_rounded,
                size: 48, color: AppTheme.textGray),
            const SizedBox(height: 12),
            Text(_error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppTheme.textGray)),
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: _cargarBandejas,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Reintentar'),
              style: ElevatedButton.styleFrom(
                minimumSize: Size.zero,
                padding: const EdgeInsets.symmetric(
                    horizontal: 24, vertical: 12),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── KPIs — scrollable horizontal ─────────────────────────────────
  Widget _buildKPIs() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Row(
        children: [
          _KpiCard(
            label: 'Total',
            valor: '$_total',
            icono: Icons.grid_view_rounded,
            color: AppTheme.cyan,
          ),
          const SizedBox(width: 8),
          _KpiCard(
            label: 'Ocupadas',
            valor: '$_ocupadas',
            icono: Icons.person_rounded,
            color: _ocupadas >= 6 ? AppTheme.red : AppTheme.orange,
          ),
          const SizedBox(width: 8),
          _KpiCard(
            label: 'Libres',
            valor: '$_disponibles',
            icono: Icons.check_circle_outline_rounded,
            color: AppTheme.green,
          ),
          const SizedBox(width: 8),
          _KpiCard(
            label: 'Alertas',
            valor: '$_alertas',
            icono: Icons.warning_amber_rounded,
            color: _alertas > 0 ? AppTheme.red : AppTheme.textGray,
          ),
          const SizedBox(width: 8),
          _KpiCard(
            label: 'Mantenim.',
            valor: '$_mantenimiento',
            icono: Icons.build_rounded,
            color: AppTheme.orange,
          ),
        ],
      ),
    );
  }

  // ── Banner alerta ────────────────────────────────────────────────
  Widget _buildAlertaBanner() {
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.red.withValues(alpha: 0.4)),
      ),
      child: Row(
        children: [
          const Icon(Icons.warning_amber_rounded,
              color: AppTheme.red, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              'Hay $_alertas ${_alertas == 1 ? 'cuerpo' : 'cuerpos'} '
              'con mas de 24h en mortuorio. Coordine con Admision.',
              style: const TextStyle(
                  fontSize: 13,
                  color: AppTheme.red,
                  fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }

  // ── Grid bandejas ────────────────────────────────────────────────
  Widget _buildGrid() {
    return SliverGrid(
      delegate: SliverChildBuilderDelegate(
        (ctx, i) => _BandejaCard(bandeja: _bandejas[i]),
        childCount: _bandejas.length,
      ),
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount:
            MediaQuery.of(context).size.width < 600 ? 2 : 4,
        crossAxisSpacing: 10,
        mainAxisSpacing: 10,
        childAspectRatio: 1.1,
      ),
    );
  }

  // ── Leyenda — una sola fila sin titulo ───────────────────────────
  Widget _buildLeyenda() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Container(
        padding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFE5E7EB)),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            _LeyendaItem(color: AppTheme.green, label: 'Disponible'),
            _LeyendaItem(color: AppTheme.red, label: 'Ocupada'),
            _LeyendaItem(color: AppTheme.orange, label: 'Mantenim.'),
            _LeyendaItem(
                color: AppTheme.red, label: '>24h', esBadge: true),
          ],
        ),
      ),
    );
  }
}

// ================================================================
// WIDGETS INTERNOS
// ================================================================

class _KpiCard extends StatelessWidget {
  final String label;
  final String valor;
  final IconData icono;
  final Color color;

  const _KpiCard({
    required this.label,
    required this.valor,
    required this.icono,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 80,
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.3)),
        boxShadow: [
          BoxShadow(
            color: color.withValues(alpha: 0.06),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        children: [
          Icon(icono, color: color, size: 20),
          const SizedBox(height: 4),
          Text(valor,
              style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: color)),
          Text(label,
              style: const TextStyle(
                  fontSize: 10,
                  color: AppTheme.textGray,
                  fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

class _BandejaCard extends StatelessWidget {
  final BandejaDisponibleModel bandeja;

  const _BandejaCard({required this.bandeja});

  Color get _colorEstado {
    switch (bandeja.estado) {
      case 'Ocupada':
        return AppTheme.red;
      case 'Disponible':
        return AppTheme.green;
      case 'Mantenimiento':
        return AppTheme.orange;
      default:
        return AppTheme.textGray;
    }
  }

  @override
  Widget build(BuildContext context) {
    final esOcupada = bandeja.estado == 'Ocupada';
    final esMantenimiento = bandeja.estado == 'Mantenimiento';

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
            color: _colorEstado.withValues(alpha: 0.5),
            width: 1.5),
        boxShadow: [
          BoxShadow(
            color: _colorEstado.withValues(alpha: 0.08),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Stack(
        children: [
          // ── Contenido principal ───────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Codigo + badge alerta
                Row(
                  children: [
                    Text(bandeja.codigo,
                        style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textDark,
                            fontFamily: 'monospace')),
                    const Spacer(),
                    if (bandeja.tieneAlerta)
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppTheme.red,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Text('>24h',
                            style: TextStyle(
                                fontSize: 9,
                                color: Colors.white,
                                fontWeight: FontWeight.bold)),
                      ),
                  ],
                ),
                const SizedBox(height: 4),

                // Estado chip
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: _colorEstado.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(bandeja.estado,
                      style: TextStyle(
                          fontSize: 10,
                          color: _colorEstado,
                          fontWeight: FontWeight.bold)),
                ),

                // Icono central — disponible
                if (!esOcupada && !esMantenimiento)
                  Expanded(
                    child: Center(
                      child: Icon(
                        Icons.inbox_outlined,
                        size: 32,
                        color: AppTheme.green.withValues(alpha: 0.3),
                      ),
                    ),
                  ),

                // Nombre + tiempo — ocupada
                if (esOcupada) ...[
                  const SizedBox(height: 6),
                  if (bandeja.nombrePaciente != null)
                    Expanded(
                      child: Text(
                        bandeja.nombrePaciente!,
                        style: const TextStyle(
                            fontSize: 11,
                            color: AppTheme.textDark,
                            fontWeight: FontWeight.w500),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  if (bandeja.tiempoOcupada != null)
                    Row(
                      children: [
                        Icon(Icons.access_time_rounded,
                            size: 11,
                            color: bandeja.tieneAlerta
                                ? AppTheme.red
                                : AppTheme.textGray),
                        const SizedBox(width: 3),
                        Text(
                          bandeja.tiempoOcupada!,
                          style: TextStyle(
                              fontSize: 11,
                              color: bandeja.tieneAlerta
                                  ? AppTheme.red
                                  : AppTheme.textGray,
                              fontWeight: bandeja.tieneAlerta
                                  ? FontWeight.bold
                                  : FontWeight.normal),
                        ),
                      ],
                    ),
                ],

                // Mantenimiento
                if (esMantenimiento) ...[
                  const SizedBox(height: 6),
                  Expanded(
                    child: Center(
                      child: Icon(Icons.build_rounded,
                          size: 28,
                          color:
                              AppTheme.orange.withValues(alpha: 0.4)),
                    ),
                  ),
                  if (bandeja.motivoMantenimiento != null)
                    Text(
                      bandeja.motivoMantenimiento!,
                      style: TextStyle(
                          fontSize: 10,
                          color:
                              AppTheme.orange.withValues(alpha: 0.8)),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                ],
              ],
            ),
          ),

          // ── Punto color esquina superior derecha ──────────
          Positioned(
            top: 8,
            right: 8,
            child: Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(
                color: _colorEstado,
                shape: BoxShape.circle,
              ),
            ),
          ),

          // ── Barra inferior de color ───────────────────────
          Positioned(
            bottom: 0,
            left: 0,
            right: 0,
            child: Container(
              height: 4,
              decoration: BoxDecoration(
                color: _colorEstado,
                borderRadius: const BorderRadius.vertical(
                    bottom: Radius.circular(14)),
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
  final bool esBadge;

  const _LeyendaItem({
    required this.color,
    required this.label,
    this.esBadge = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (esBadge)
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: 4, vertical: 1),
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(4),
            ),
            child: const Text('>24h',
                style: TextStyle(
                    fontSize: 8,
                    color: Colors.white,
                    fontWeight: FontWeight.bold)),
          )
        else
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
        const SizedBox(width: 5),
        Text(label,
            style: const TextStyle(
                fontSize: 11, color: AppTheme.textGray)),
      ],
    );
  }
}