import 'package:flutter/material.dart';
import '../../../core/constants/routes.dart';
import '../../../features/auth/services/auth_service.dart';
import '../../../shared/theme/app_theme.dart';
import '../models/expediente_vigilante_model.dart';
import '../services/expediente_vigilante_service.dart';
import '../../../core/models/usuario_model.dart';
class VigilanteHomeScreen extends StatefulWidget {
  const VigilanteHomeScreen({super.key});

  @override
  State<VigilanteHomeScreen> createState() =>
      _VigilanteHomeScreenState();
}

class _VigilanteHomeScreenState extends State<VigilanteHomeScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  // ── Estado Salidas ───────────────────────────────────────────────
  List<ExpedienteVigilanteModel> _todos = [];
  List<ExpedienteVigilanteModel> _filtrados = [];
  bool _isLoading = false;
  String? _error;
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _tabController.addListener(() {
      if (_tabController.index == 1 && _todos.isEmpty) {
        _cargarExpedientes();
      }
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _cargarExpedientes() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final lista =
          await ExpedienteVigilanteService.getPendientesRetiro();
      if (mounted) {
        setState(() {
          _todos = lista;
          _filtrados = lista;
          _searchCtrl.clear();
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error =
            e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _filtrar(String texto) {
    final t = texto.trim();
    if (t.isEmpty) {
      setState(() => _filtrados = _todos);
      return;
    }
    final esNumero = RegExp(r'^\d+$').hasMatch(t);
    setState(() {
      _filtrados = _todos.where((e) {
        if (esNumero) {
          return e.hc == t || e.numeroDocumento == t;
        }
        return e.nombreCompleto
            .toLowerCase()
            .contains(t.toLowerCase());
      }).toList();
    });
  }

  Future<void> _logout() async {
    await AuthService.logout();
    if (mounted) {
      Navigator.pushReplacementNamed(context, Routes.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    final usuario = AuthService.usuarioActual;

    return Scaffold(
      backgroundColor: AppTheme.bgGray,
      body: Column(
        children: [
          // ── Header + TabBar ──────────────────────────────────
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
              ),
            ),
            child: SafeArea(
              bottom: false,
              child: Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(
                        20, 12, 12, 8),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color:
                                Colors.white.withValues(alpha: 0.2),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Icon(
                            Icons.security_rounded,
                            color: Colors.white,
                            size: 24,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment:
                                CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Control Mortuorio',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              Text(
                                usuario?.nombre ?? '',
                                style: TextStyle(
                                  color: Colors.white
                                      .withValues(alpha: 0.75),
                                  fontSize: 12,
                                ),
                              ),
                              Text(
                                usuario?.rolLabel ?? '',
                                style: TextStyle(
                                  color: Colors.white.withValues(alpha: 0.55),
                                  fontSize: 12,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.logout_rounded,
                              color: Colors.white),
                          onPressed: _logout,
                        ),
                      ],
                    ),
                  ),
                  TabBar(
                    controller: _tabController,
                    indicatorColor: Colors.white,
                    indicatorWeight: 3,
                    labelColor: Colors.white,
                    unselectedLabelColor:
                        Colors.white.withValues(alpha: 0.55),
                    labelStyle: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 13),
                    tabs: [
                      Tab(
                        icon: const Icon(
                            Icons.qr_code_scanner_rounded,
                            size: 20),
                        text: 'Verificar',
                      ),
                      Tab(
                        icon: Stack(
                          clipBehavior: Clip.none,
                          children: [
                            const Icon(
                                Icons.exit_to_app_rounded,
                                size: 20),
                            if (_todos.isNotEmpty)
                              Positioned(
                                top: -4,
                                right: -8,
                                child: Container(
                                  padding: const EdgeInsets.all(3),
                                  decoration: const BoxDecoration(
                                    color: AppTheme.red,
                                    shape: BoxShape.circle,
                                  ),
                                  child: Text(
                                    '${_todos.length}',
                                    style: const TextStyle(
                                      color: Colors.white,
                                      fontSize: 9,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                ),
                              ),
                          ],
                        ),
                        text: 'Salidas',
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),

          // ── Contenido ────────────────────────────────────────
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _buildTabVerificar(),
                _buildTabSalidas(),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ================================================================
  // TAB 1 — VERIFICAR
  // ================================================================
  Widget _buildTabVerificar() {
    return RefreshIndicator(
      color: AppTheme.cyan,
      onRefresh: () async {},
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            const SizedBox(height: 8),

            // CTA card-action
            InkWell(
              onTap: () => Navigator.pushNamed(
                      context, Routes.verificacion)
                  .then((_) => setState(() {})),
              borderRadius: BorderRadius.circular(20),
              child: Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
                  ),
                  borderRadius: BorderRadius.circular(20),
                  boxShadow: [
                    BoxShadow(
                      color:
                          AppTheme.cyan.withValues(alpha: 0.35),
                      blurRadius: 16,
                      offset: const Offset(0, 6),
                    ),
                  ],
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: const Icon(
                        Icons.qr_code_scanner_rounded,
                        color: Colors.white,
                        size: 36,
                      ),
                    ),
                    const SizedBox(width: 20),
                    const Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Verificar Ingreso QR',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            'Escanee el QR del brazalete para autorizar ingreso',
                            style: TextStyle(
                              color: Colors.white70,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right_rounded,
                        color: Colors.white70, size: 28),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 24),

            // Info card
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFEFF6FF),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                    color: const Color(0xFFBFDBFE)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.info_outline_rounded,
                      color: Color(0xFF3B82F6), size: 18),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Flujo de verificacion',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF1D4ED8),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '1. Escanee el QR del brazalete\n'
                          '2. Verifique los datos del fallecido\n'
                          '3. Autorice el ingreso al mortuorio',
                          style: TextStyle(
                            fontSize: 12,
                            color:
                                const Color(0xFF1D4ED8)
                                    .withValues(alpha: 0.8),
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ================================================================
  // TAB 2 — SALIDAS
  // ================================================================
  Widget _buildTabSalidas() {
    return RefreshIndicator(
      color: AppTheme.cyan,
      onRefresh: _cargarExpedientes,
      child: CustomScrollView(
        slivers: [
          // SearchBar + contador
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
              child: Column(
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _searchCtrl,
                          onChanged: _filtrar,
                          decoration: InputDecoration(
                            hintText: 'HC, DNI o Nombre...',
                            prefixIcon: const Icon(
                                Icons.search_rounded,
                                color: AppTheme.cyan),
                            suffixIcon: _searchCtrl.text.isNotEmpty
                                ? IconButton(
                                    icon: const Icon(Icons.close_rounded),
                                    onPressed: () {
                                      _searchCtrl.clear();
                                      _filtrar('');
                                    },
                                  )
                                : null,
                            contentPadding: const EdgeInsets.symmetric(
                                vertical: 12, horizontal: 16),
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 10),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                              color: const Color(0xFFE5E7EB)),
                        ),
                        child: Text(
                          '${_filtrados.length} pendiente${_filtrados.length != 1 ? 's' : ''}',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppTheme.textGray,
                          ),
                        ),
                      ),
                    ],
                  ),
                  if (_searchCtrl.text.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Row(
                        children: [
                          const Icon(Icons.filter_list_rounded,
                              size: 14, color: AppTheme.cyan),
                          const SizedBox(width: 4),
                          Text(
                            'Filtro activo: "${_searchCtrl.text}"',
                            style: const TextStyle(
                                fontSize: 11,
                                color: AppTheme.cyan,
                                fontWeight: FontWeight.w600),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ),

          // Estados
          if (_isLoading)
            const SliverFillRemaining(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    CircularProgressIndicator(color: AppTheme.cyan),
                    SizedBox(height: 12),
                    Text('Cargando expedientes...',
                        style:
                            TextStyle(color: AppTheme.textGray)),
                  ],
                ),
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
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                            color: AppTheme.textGray)),
                    const SizedBox(height: 16),
                    ElevatedButton.icon(
                      onPressed: _cargarExpedientes,
                      icon: const Icon(Icons.refresh_rounded),
                      label: const Text('Reintentar'),
                    ),
                  ],
                ),
              ),
            )
          else if (_todos.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.inbox_rounded,
                        size: 64, color: Colors.grey.shade300),
                    const SizedBox(height: 12),
                    const Text('Sin expedientes pendientes',
                        style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textDark)),
                    const SizedBox(height: 6),
                    Text(
                      'Deslice hacia abajo para recargar',
                      style: TextStyle(color: AppTheme.textGray),
                    ),
                  ],
                ),
              ),
            )
          else if (_filtrados.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.search_off_rounded,
                        size: 56, color: Colors.grey.shade300),
                    const SizedBox(height: 12),
                    const Text('Sin coincidencias',
                        style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textDark)),
                    const SizedBox(height: 6),
                    TextButton.icon(
                      onPressed: () {
                        _searchCtrl.clear();
                        _filtrar('');
                      },
                      icon: const Icon(Icons.close_rounded,
                          size: 16),
                      label: const Text('Limpiar filtro'),
                    ),
                  ],
                ),
              ),
            )
          else
            SliverPadding(
              padding:
                  const EdgeInsets.fromLTRB(16, 0, 16, 32),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (ctx, i) => Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: _SalidaCard(
                      expediente: _filtrados[i],
                      onRegistrarSalida: () =>
                          _irASalida(_filtrados[i]),
                    ),
                  ),
                  childCount: _filtrados.length,
                ),
              ),
            ),
        ],
      ),
    );
  }

  void _irASalida(ExpedienteVigilanteModel exp) {
    Navigator.pushNamed(
      context,
      Routes.salida,
      arguments: exp,
    ).then((_) => _cargarExpedientes());
  }
}

// ================================================================
// CARD DE SALIDA
// ================================================================
class _SalidaCard extends StatelessWidget {
  final ExpedienteVigilanteModel expediente;
  final VoidCallback onRegistrarSalida;

  const _SalidaCard({
    required this.expediente,
    required this.onRegistrarSalida,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
            color: AppTheme.green.withValues(alpha: 0.3)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: 16, vertical: 10),
            decoration: BoxDecoration(
              color: AppTheme.green.withValues(alpha: 0.06),
              borderRadius:
                  const BorderRadius.vertical(top: Radius.circular(16)),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppTheme.green,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    expediente.codigoExpediente,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const Spacer(),
                if (expediente.codigoBandeja != null)
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppTheme.red.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.archive_rounded,
                            size: 12, color: AppTheme.red),
                        const SizedBox(width: 4),
                        Text(
                          expediente.codigoBandeja!,
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.red,
                          ),
                        ),
                      ],
                    ),
                  ),
                const SizedBox(width: 8),
                Row(
                  children: [
                    const Icon(Icons.access_time_rounded,
                        size: 13, color: AppTheme.textGray),
                    const SizedBox(width: 4),
                    Text(
                      expediente.tiempoEnMortuorio,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppTheme.textGray,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Body
          Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Nombre
                Text(
                  expediente.nombreCompleto,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textDark,
                  ),
                ),
                const SizedBox(height: 6),

                // HC + Servicio
                Row(
                  children: [
                    const Icon(Icons.tag_rounded,
                        size: 13, color: AppTheme.textGray),
                    const SizedBox(width: 4),
                    Text(
                      'HC: ${expediente.hc}',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppTheme.textGray,
                        fontFamily: 'monospace',
                      ),
                    ),
                    const SizedBox(width: 12),
                    const Icon(Icons.local_hospital_rounded,
                        size: 13, color: AppTheme.textGray),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        expediente.servicioFallecimiento,
                        style: const TextStyle(
                            fontSize: 12,
                            color: AppTheme.textGray),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Botón
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: onRegistrarSalida,
                    icon: const Icon(
                        Icons.check_circle_outline_rounded,
                        size: 18),
                    label: const Text('REGISTRAR SALIDA'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppTheme.green,
                      padding:
                          const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),

          // Barra inferior
          Container(
            height: 4,
            decoration: BoxDecoration(
              color: AppTheme.green,
              borderRadius: const BorderRadius.vertical(
                  bottom: Radius.circular(16)),
            ),
          ),
        ],
      ),
    );
  }
}