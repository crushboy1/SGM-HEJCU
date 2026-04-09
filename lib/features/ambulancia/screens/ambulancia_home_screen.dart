import 'package:flutter/material.dart';
import '../../../core/constants/routes.dart';
import '../../../features/auth/services/auth_service.dart';
import '../../../shared/theme/app_theme.dart';
import 'qr_scan_screen.dart';
import '../models/expediente_pendiente_model.dart';
import '../services/expediente_service.dart';

class AmbulanciaHomeScreen extends StatefulWidget {
  const AmbulanciaHomeScreen({super.key});

  @override
  State<AmbulanciaHomeScreen> createState() => _AmbulanciaHomeScreenState();
}

class _AmbulanciaHomeScreenState extends State<AmbulanciaHomeScreen> {
  List<ExpedientePendienteModel> _tareas = [];
  bool _isLoading = true;
  String? _error;
  String _activeTab = 'pendientes'; // 'pendientes' | 'custodia'

  // KPIs
  int get _kpiPendientes => _tareas
      .where(
        (t) =>
            t.estadoActual == 'PendienteDeRecojo' || t.estadoActual == 'EnPiso',
      )
      .length;

  int get _kpiEnCustodia => _tareas
      .where(
        (t) =>
            t.estadoActual == 'EnTrasladoMortuorio' ||
            t.estadoActual == 'PendienteAsignacionBandeja',
      )
      .length;

  List<ExpedientePendienteModel> get _tareasFiltradas {
    if (_activeTab == 'pendientes') {
      return _tareas
          .where(
            (t) =>
                t.estadoActual == 'PendienteDeRecojo' ||
                t.estadoActual == 'EnPiso',
          )
          .toList();
    }
    return _tareas
        .where(
          (t) =>
              t.estadoActual == 'EnTrasladoMortuorio' ||
              t.estadoActual == 'PendienteAsignacionBandeja',
        )
        .toList();
  }

  @override
  void initState() {
    super.initState();
    _cargarDatos();
  }

  Future<void> _cargarDatos() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final tareas = await ExpedienteAmbulanciaService.getPendientesRecojo();
      if (mounted) setState(() => _tareas = tareas);
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _logout() async {
    await AuthService.logout();
    if (mounted) Navigator.pushReplacementNamed(context, Routes.login);
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = MediaQuery.of(context).size.width >= 600;
    final usuario = AuthService.usuarioActual;

    return Scaffold(
      backgroundColor: AppTheme.bgGray,
      body: RefreshIndicator(
        color: AppTheme.cyan,
        onRefresh: _cargarDatos,
        child: CustomScrollView(
          slivers: [
            // ── AppBar ──────────────────────────────────────────
            SliverAppBar(
              pinned: true,
              expandedHeight: 80,
              backgroundColor: AppTheme.cyan,
              flexibleSpace: FlexibleSpaceBar(
                background: Container(
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
                    ),
                  ),
                  padding: const EdgeInsets.fromLTRB(20, 48, 20, 12),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: const Icon(
                          Icons.local_shipping_rounded,
                          color: Colors.white,
                          size: 26,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Mis Traslados',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            Text(
                              '${usuario?.rolLabel ?? ''} • ${usuario?.nombre ?? ''}',
                              style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.8),
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(
                          Icons.logout_rounded,
                          color: Colors.white,
                        ),
                        onPressed: _logout,
                        tooltip: 'Cerrar sesión',
                      ),
                    ],
                  ),
                ),
              ),
            ),

            // ── KPI Tabs ────────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                child: Row(
                  children: [
                    Expanded(
                      child: _KpiTab(
                        label: 'Pendientes',
                        count: _kpiPendientes,
                        isActive: _activeTab == 'pendientes',
                        activeColor: AppTheme.cyan,
                        icon: Icons.notifications_active_rounded,
                        onTap: () => setState(() => _activeTab = 'pendientes'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _KpiTab(
                        label: 'En Tránsito',
                        count: _kpiEnCustodia,
                        isActive: _activeTab == 'custodia',
                        activeColor: AppTheme.green,
                        icon: Icons.directions_run_rounded,
                        onTap: () => setState(() => _activeTab = 'custodia'),
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // ── Lista ────────────────────────────────────────────
            if (_isLoading)
              const SliverFillRemaining(child: _LoadingState())
            else if (_error != null)
              SliverFillRemaining(
                child: _ErrorState(error: _error!, onRetry: _cargarDatos),
              )
            else if (_tareasFiltradas.isEmpty)
              SliverFillRemaining(
                child: _EmptyState(isTransito: _activeTab == 'custodia'),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
                sliver: isTablet
                    ? SliverGrid(
                        delegate: SliverChildBuilderDelegate(
                          (ctx, i) => _TareaCard(
                            tarea: _tareasFiltradas[i],
                            isTransito: _activeTab == 'custodia',
                            onAsignarBandeja: () =>
                                _irAMapaBandejas(_tareasFiltradas[i]),
                          ),
                          childCount: _tareasFiltradas.length,
                        ),
                        gridDelegate:
                            const SliverGridDelegateWithFixedCrossAxisCount(
                              crossAxisCount: 2,
                              crossAxisSpacing: 12,
                              mainAxisSpacing: 12,
                              childAspectRatio: 0.85,
                            ),
                      )
                    : SliverList(
                        delegate: SliverChildBuilderDelegate(
                          (ctx, i) => Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: _TareaCard(
                              tarea: _tareasFiltradas[i],
                              isTransito: _activeTab == 'custodia',
                              onAsignarBandeja: () =>
                                  _irAMapaBandejas(_tareasFiltradas[i]),
                            ),
                          ),
                          childCount: _tareasFiltradas.length,
                        ),
                      ),
              ),
          ],
        ),
      ),

      // ── FAB — solo tab pendientes con items ───────────────────
      floatingActionButton:
          _activeTab == 'pendientes' && _tareasFiltradas.isNotEmpty
          ? _SgmFab(onPressed: _irAEscanearQR)
          : null,
    );
  }

  void _irAEscanearQR() {
  Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => const QrScanScreen(mostrarInputManual: true),
    ),
  ).then((_) => _cargarDatos());
}

  void _irAMapaBandejas(ExpedientePendienteModel tarea) {
    if (!tarea.puedeAsignarBandeja) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Esperando verificación del Vigilante Mortuorio'),
          backgroundColor: AppTheme.cyan,
          behavior: SnackBarBehavior.floating,
        ),
      );
      return;
    }
    Navigator.pushNamed(
      context,
      Routes.mapaBandejas,
      arguments: tarea,
    ).then((_) => _cargarDatos());
  }
}

// ================================================================
// WIDGETS INTERNOS
// ================================================================

class _KpiTab extends StatelessWidget {
  final String label;
  final int count;
  final bool isActive;
  final Color activeColor;
  final IconData icon;
  final VoidCallback onTap;

  const _KpiTab({
    required this.label,
    required this.count,
    required this.isActive,
    required this.activeColor,
    required this.icon,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: isActive ? Colors.white : const Color(0xFFF3F4F6),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isActive ? activeColor : Colors.transparent,
            width: 2,
          ),
          boxShadow: isActive
              ? [
                  BoxShadow(
                    color: activeColor.withValues(alpha: 0.15),
                    blurRadius: 12,
                    offset: const Offset(0, 4),
                  ),
                ]
              : [],
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: isActive
                    ? activeColor.withValues(alpha: 0.12)
                    : Colors.grey.shade200,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(
                icon,
                color: isActive ? activeColor : Colors.grey,
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '$count',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: isActive ? activeColor : AppTheme.textGray,
                    ),
                  ),
                  Text(
                    label,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                      color: isActive ? activeColor : AppTheme.textGray,
                    ),
                  ),
                ],
              ),
            ),
            if (count > 0 && !isActive)
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: activeColor,
                  shape: BoxShape.circle,
                ),
                child: Text(
                  '$count',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _TareaCard extends StatelessWidget {
  final ExpedientePendienteModel tarea;
  final bool isTransito;
  final VoidCallback onAsignarBandeja;

  const _TareaCard({
    required this.tarea,
    required this.isTransito,
    required this.onAsignarBandeja,
  });

  @override
  Widget build(BuildContext context) {
    final color = isTransito ? AppTheme.green : AppTheme.cyan;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withValues(alpha: 0.3)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.08),
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(16),
              ),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: color,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    tarea.codigoExpediente,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const Spacer(),
                Icon(Icons.access_time_rounded, size: 14, color: color),
                const SizedBox(width: 4),
                Text(
                  tarea.tiempoFormateado,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: color,
                  ),
                ),
                if (tarea.esUrgente) ...[
                  const SizedBox(width: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 6,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: AppTheme.red,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: const Text(
                      'URGENTE',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 9,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),

          // Body
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Nombre paciente + HC
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: color.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(Icons.person_rounded, color: color, size: 20),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            tarea.nombreCompleto,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: AppTheme.textDark,
                            ),
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 3),
                          Row(
                            children: [
                              Icon(
                                Icons.badge_outlined,
                                size: 12,
                                color: AppTheme.textGray,
                              ),
                              const SizedBox(width: 4),
                              Text(
                                'HC: ${tarea.hc.isNotEmpty ? tarea.hc : 'N/A'}',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: AppTheme.textGray,
                                  fontFamily: 'monospace',
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Origen / Destino
                Row(
                  children: [
                    Expanded(
                      child: _InfoChip(
                        label: 'Origen',
                        value: tarea.servicioFallecimiento,
                        color: color,
                      ),
                    ),
                    const SizedBox(width: 8),
                    const Expanded(
                      child: _InfoChip(
                        label: 'Destino',
                        value: 'Mortuorio',
                        color: AppTheme.textGray,
                      ),
                    ),
                  ],
                ),

                // Botón solo en tab custodia
                if (isTransito) ...[
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: tarea.puedeAsignarBandeja
                          ? onAsignarBandeja
                          : null,
                      icon: const Icon(Icons.grid_view_rounded, size: 18),
                      label: const Text('ASIGNAR BANDEJA'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: tarea.puedeAsignarBandeja
                            ? AppTheme.green
                            : Colors.grey.shade300,
                        foregroundColor: tarea.puedeAsignarBandeja
                            ? Colors.white
                            : Colors.grey,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                    ),
                  ),
                  if (!tarea.puedeAsignarBandeja)
                    Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.access_time_rounded,
                            size: 12,
                            color: AppTheme.textGray,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            'Esperando verificación del Vigilante',
                            style: TextStyle(
                              fontSize: 11,
                              color: AppTheme.textGray,
                            ),
                          ),
                        ],
                      ),
                    ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoChip extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _InfoChip({
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF9FAFB),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: const TextStyle(
              fontSize: 10,
              color: AppTheme.textGray,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppTheme.textDark,
            ),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

class _SgmFab extends StatelessWidget {
  final VoidCallback onPressed;

  const _SgmFab({required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return FloatingActionButton.extended(
      onPressed: onPressed,
      backgroundColor: AppTheme.cyan,
      foregroundColor: Colors.white,
      elevation: 6,
      icon: const Icon(Icons.qr_code_scanner_rounded),
      label: const Text(
        'ESCANEAR QR',
        style: TextStyle(fontWeight: FontWeight.bold),
      ),
    );
  }
}

class _LoadingState extends StatelessWidget {
  const _LoadingState();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          CircularProgressIndicator(color: AppTheme.cyan),
          SizedBox(height: 16),
          Text(
            'Cargando traslados...',
            style: TextStyle(color: AppTheme.textGray),
          ),
        ],
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  final String error;
  final VoidCallback onRetry;

  const _ErrorState({required this.error, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.wifi_off_rounded,
              size: 64,
              color: AppTheme.textGray,
            ),
            const SizedBox(height: 16),
            const Text(
              'Sin conexión',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppTheme.textDark,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              error,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppTheme.textGray),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Reintentar'),
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  final bool isTransito;

  const _EmptyState({required this.isTransito});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            isTransito
                ? Icons.check_circle_outline_rounded
                : Icons.inbox_rounded,
            size: 72,
            color: Colors.grey.shade300,
          ),
          const SizedBox(height: 16),
          Text(
            isTransito ? 'Sin traslados en curso' : 'Sin traslados pendientes',
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppTheme.textDark,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            isTransito
                ? 'Los traslados aceptados aparecerán aquí'
                : 'Las nuevas solicitudes aparecerán aquí',
            style: const TextStyle(color: AppTheme.textGray),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
