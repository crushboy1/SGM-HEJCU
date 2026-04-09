import 'package:flutter/material.dart';
import '../../../shared/theme/app_theme.dart';
import '../models/expediente_vigilante_model.dart';
import '../models/acta_retiro_model.dart';
import '../models/salida_model.dart';
import '../services/salida_service.dart';

class SalidaScreen extends StatefulWidget {
  const SalidaScreen({super.key});

  @override
  State<SalidaScreen> createState() => _SalidaScreenState();
}

class _SalidaScreenState extends State<SalidaScreen> {
  late ExpedienteVigilanteModel _expediente;
  ActaRetiroModel? _datos;
  bool _cargando = true;
  bool _enviando = false;
  bool _inicializado = false;
  String? _errorCarga;

  // Campos editables
  final _funerariaCtrl = TextEditingController();
  final _rucCtrl = TextEditingController();
  final _telFunerariaCtrl = TextEditingController();
  final _conductorCtrl = TextEditingController();
  final _dniConductorCtrl = TextEditingController();
  final _ayudanteCtrl = TextEditingController();
  final _dniAyudanteCtrl = TextEditingController();
  final _placaCtrl = TextEditingController();
  final _destinoCtrl = TextEditingController();
  final _obsCtrl = TextEditingController();

  // Validacion reactiva
  bool get _funerariaIngresada => _funerariaCtrl.text.trim().isNotEmpty;
  bool get _conductorRequerido =>
      _funerariaIngresada && _conductorCtrl.text.trim().isEmpty;
  bool get _placaRequerida =>
      _funerariaIngresada && _placaCtrl.text.trim().isEmpty;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_inicializado) return;
    _inicializado = true;
    _expediente = ModalRoute.of(context)!.settings.arguments
        as ExpedienteVigilanteModel;
    _cargarActa();
  }

  @override
  void dispose() {
    _funerariaCtrl.dispose();
    _rucCtrl.dispose();
    _telFunerariaCtrl.dispose();
    _conductorCtrl.dispose();
    _dniConductorCtrl.dispose();
    _ayudanteCtrl.dispose();
    _dniAyudanteCtrl.dispose();
    _placaCtrl.dispose();
    _destinoCtrl.dispose();
    _obsCtrl.dispose();
    super.dispose();
  }

  Future<void> _cargarActa() async {
    setState(() {
      _cargando = true;
      _errorCarga = null;
    });
    try {
      final acta =
          await SalidaService.getActaRetiro(_expediente.expedienteID);
      if (mounted) {
        setState(() {
          _datos = acta;
          if (acta.destino?.isNotEmpty == true) {
            _destinoCtrl.text = acta.destino!;
          }
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() =>
            _errorCarga = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  // ── Validacion ───────────────────────────────────────────────────
  String? _validar() {
    if (_datos == null) return 'No se cargaron los datos del acta.';
    if (!_datos!.puedeRegistrarSalida) {
      return _datos!.tienePDFFirmado
          ? 'El acta no esta completa. Contacte a Admision.'
          : 'El acta no tiene PDF firmado. Contacte a Admision.';
    }
    if (_datos!.esFamiliar && _funerariaIngresada) {
      if (_conductorCtrl.text.trim().isEmpty) {
        return 'Si registra funeraria, ingrese el nombre del conductor.';
      }
      if (_placaCtrl.text.trim().isEmpty) {
        return 'Si registra funeraria, ingrese la placa del vehiculo.';
      }
    }
    return null;
  }

  void _confirmarSalida() {
    final error = _validar();
    if (error != null) {
      _mostrarSnackbar(error, isError: true);
      return;
    }
    _mostrarBottomSheetConfirmacion();
  }

  // ── Bottom sheet confirmacion ────────────────────────────────────
  void _mostrarBottomSheetConfirmacion() {
    final datos = _datos!;
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      isDismissible: !_enviando,
      builder: (ctx) => Container(
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius:
              BorderRadius.vertical(top: Radius.circular(24)),
        ),
        padding: EdgeInsets.fromLTRB(
            24, 16, 24, MediaQuery.of(ctx).padding.bottom + 24),
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
            const Text('Confirmar Entrega',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textDark)),
            const SizedBox(height: 16),

            // Resumen
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFF0FFF4),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                    color: AppTheme.green.withValues(alpha: 0.3)),
              ),
              child: Column(
                children: [
                  _ResumenRow(
                      icon: Icons.person_rounded,
                      label: 'Paciente',
                      value: datos.nombrePaciente,
                      bold: true),
                  const Divider(height: 12),
                  _ResumenRow(
                      icon: Icons.qr_code_rounded,
                      label: 'Expediente',
                      value: datos.codigoExpediente,
                      mono: true),
                  const Divider(height: 12),
                  _ResumenRow(
                      icon: datos.esFamiliar
                          ? Icons.family_restroom_rounded
                          : Icons.shield_rounded,
                      label: 'Tipo salida',
                      value: datos.esFamiliar
                          ? 'Entrega a Familiar'
                          : 'Autoridad Legal'),
                  const Divider(height: 12),
                  _ResumenRow(
                      icon: Icons.person_outline_rounded,
                      label: 'Responsable',
                      value: datos.responsableNombre),
                  if (_placaCtrl.text.trim().isNotEmpty) ...[
                    const Divider(height: 12),
                    _ResumenRow(
                        icon: Icons.directions_car_rounded,
                        label: 'Placa',
                        value: _placaCtrl.text.trim().toUpperCase(),
                        mono: true),
                  ],
                  if (_funerariaCtrl.text.trim().isNotEmpty) ...[
                    const Divider(height: 12),
                    _ResumenRow(
                        icon: Icons.local_shipping_rounded,
                        label: 'Funeraria',
                        value: _funerariaCtrl.text.trim()),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 12),

            // Aviso irreversible
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFFEF2F2),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                    color: AppTheme.red.withValues(alpha: 0.3)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.warning_amber_rounded,
                      color: AppTheme.red, size: 18),
                  const SizedBox(width: 10),
                  const Expanded(
                    child: Text(
                      'Esta accion no se puede deshacer. La bandeja quedara libre y el expediente se cerrara.',
                      style:
                          TextStyle(fontSize: 12, color: AppTheme.red),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed:
                        _enviando ? null : () => Navigator.pop(ctx),
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
                    onPressed: _enviando
                        ? null
                        : () {
                            Navigator.pop(ctx);
                            _procesarSalida();
                          },
                    icon: const Icon(Icons.check_rounded),
                    label: const Text('SI, CONFIRMAR'),
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
      ),
    );
  }

  // ── Procesar salida ──────────────────────────────────────────────
  Future<void> _procesarSalida() async {
    setState(() => _enviando = true);
    try {
      final model = RegistrarSalidaModel(
        expedienteID: _expediente.expedienteID,
        nombreFuneraria: _funerariaCtrl.text.trim().isEmpty
            ? null
            : _funerariaCtrl.text.trim(),
        funerariaRUC: _rucCtrl.text.trim().isEmpty
            ? null
            : _rucCtrl.text.trim(),
        funerariaTelefono: _telFunerariaCtrl.text.trim().isEmpty
            ? null
            : _telFunerariaCtrl.text.trim(),
        conductorFuneraria: _conductorCtrl.text.trim().isEmpty
            ? null
            : _conductorCtrl.text.trim(),
        dniConductor: _dniConductorCtrl.text.trim().isEmpty
            ? null
            : _dniConductorCtrl.text.trim(),
        ayudanteFuneraria: _ayudanteCtrl.text.trim().isEmpty
            ? null
            : _ayudanteCtrl.text.trim(),
        dniAyudante: _dniAyudanteCtrl.text.trim().isEmpty
            ? null
            : _dniAyudanteCtrl.text.trim(),
        placaVehiculo: _placaCtrl.text.trim().isEmpty
            ? null
            : _placaCtrl.text.trim().toUpperCase(),
        destino: _destinoCtrl.text.trim().isEmpty
            ? null
            : _destinoCtrl.text.trim(),
        observaciones: _obsCtrl.text.trim().isEmpty
            ? null
            : _obsCtrl.text.trim(),
      );
      await SalidaService.registrarSalida(model);
      if (!mounted) return;
      await _mostrarExito();
    } catch (e) {
      if (!mounted) return;
      _mostrarSnackbar(
          e.toString().replaceFirst('Exception: ', ''),
          isError: true);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  Future<void> _mostrarExito() async {
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
            const Text('Entrega Confirmada',
                style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.textDark)),
            const SizedBox(height: 8),
            Text(
              'El cuerpo fue retirado y la bandeja ${_expediente.codigoBandeja ?? ''} quedo liberada.',
              textAlign: TextAlign.center,
              style: TextStyle(
                  fontSize: 13, color: AppTheme.textGray),
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
              child: const Text('VOLVER A SALIDAS'),
            ),
          ),
        ],
      ),
    );
  }

  void _mostrarSnackbar(String msg, {bool isError = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg),
        backgroundColor: isError ? AppTheme.red : AppTheme.green,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  // ================================================================
  // BUILD
  // ================================================================

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.bgGray,
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Confirmar Entrega',
                style: TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.bold)),
            Text(
              _expediente.nombreCompleto,
              style: const TextStyle(
                  color: Colors.white70, fontSize: 11),
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_rounded,
              color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
        flexibleSpace: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              colors: [Color(0xFF0891B2), Color(0xFF0C4A6E)],
            ),
          ),
        ),
      ),
      body: _cargando
          ? const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  CircularProgressIndicator(color: AppTheme.cyan),
                  SizedBox(height: 12),
                  Text('Cargando datos del acta...',
                      style:
                          TextStyle(color: AppTheme.textGray)),
                ],
              ),
            )
          : _errorCarga != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.wifi_off_rounded,
                            size: 48, color: AppTheme.textGray),
                        const SizedBox(height: 12),
                        Text(_errorCarga!,
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                                color: AppTheme.textGray)),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: _cargarActa,
                          icon: const Icon(Icons.refresh_rounded),
                          label: const Text('Reintentar'),
                        ),
                      ],
                    ),
                  ),
                )
              : _buildFormulario(),
      bottomNavigationBar:
          (!_cargando && _errorCarga == null)
              ? _buildStickyButtons()
              : null,
    );
  }

  // ================================================================
  // FORMULARIO
  // ================================================================

  Widget _buildFormulario() {
    final datos = _datos!;
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildSeccionExpediente(),
          const SizedBox(height: 12),
          _buildSeccionDocumentacion(datos),
          const SizedBox(height: 12),
          _buildSeccionTipoSalida(datos),
          const SizedBox(height: 12),
          if (datos.esFamiliar)
            _buildSeccionFamiliarReadonly(datos)
          else
            _buildSeccionAutoridadReadonly(datos),
          const SizedBox(height: 12),
          if (datos.esFamiliar) ...[
            _buildSeccionFuneraria(),
            const SizedBox(height: 12),
          ],
          if (datos.esAutoridadLegal) ...[
            _buildSeccionPlacaAutoridad(),
            const SizedBox(height: 12),
          ],
          if (datos.destino?.isNotEmpty == true) ...[
            _buildSeccionDestino(datos.destino!),
            const SizedBox(height: 12),
          ],
          _buildSeccionObservaciones(datos),
          const SizedBox(height: 80),
        ],
      ),
    );
  }

  // ── Sección 1: Info expediente ───────────────────────────────────
  Widget _buildSeccionExpediente() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            AppTheme.cyan.withValues(alpha: 0.08),
            AppTheme.cyan.withValues(alpha: 0.03),
          ],
        ),
        borderRadius: BorderRadius.circular(16),
        border:
            Border.all(color: AppTheme.cyan.withValues(alpha: 0.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            _expediente.nombreCompleto,
            style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.bold,
                color: AppTheme.textDark),
          ),
          const SizedBox(height: 8),
          Column(
  children: [
    Row(
      children: [
        Expanded(
          child: _Chip(
              icon: Icons.tag_rounded,
              label: 'HC: ${_expediente.hc}',
              mono: true,
              color: AppTheme.cyan),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: _Chip(
              icon: Icons.local_hospital_rounded,
              label: _expediente.servicioFallecimiento,
              color: AppTheme.cyan),
        ),
      ],
    ),
    if (_expediente.codigoBandeja != null) ...[
      const SizedBox(height: 6),
      Row(
        children: [
          Expanded(
            child: _Chip(
                icon: Icons.archive_rounded,
                label: _expediente.codigoBandeja!,
                color: AppTheme.red),
          ),
        ],
      ),
    ],
  ],
),
        ],
      ),
    );
  }

  // ── Sección 2: Documentacion del acta ───────────────────────────
  Widget _buildSeccionDocumentacion(ActaRetiroModel datos) {
    if (datos.puedeRegistrarSalida) {
      return Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: const Color(0xFFF0FFF4),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
              color: AppTheme.green.withValues(alpha: 0.3)),
        ),
        child: Row(
          children: [
            const Icon(Icons.verified_rounded,
                color: AppTheme.green, size: 20),
            const SizedBox(width: 10),
            const Expanded(
              child: Text(
                'Documentacion completa — listo para retiro',
                style: TextStyle(
                    fontSize: 13,
                    color: AppTheme.green,
                    fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      );
    }
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(14),
        border:
            Border.all(color: AppTheme.red.withValues(alpha: 0.4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.block_rounded,
                  color: AppTheme.red, size: 18),
              SizedBox(width: 8),
              Text('NO SE PUEDE REGISTRAR SALIDA',
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.red,
                      letterSpacing: 0.3)),
            ],
          ),
          if (!datos.tienePDFFirmado) ...[
            const SizedBox(height: 8),
            const _DeudaItem(
              icono: Icons.picture_as_pdf_rounded,
              titulo: 'PDF del acta sin firmar',
              mensaje:
                  'Contacte a Admision para subir el PDF firmado.',
            ),
          ],
          if (!datos.estaCompleta) ...[
            const SizedBox(height: 8),
            const _DeudaItem(
              icono: Icons.assignment_late_rounded,
              titulo: 'Acta incompleta',
              mensaje: 'Faltan firmas o documentos requeridos.',
            ),
          ],
        ],
      ),
    );
  }

  // ── Sección 3: Tipo salida ───────────────────────────────────────
  Widget _buildSeccionTipoSalida(ActaRetiroModel datos) {
    final esFamiliar = datos.esFamiliar;
    final color = esFamiliar ? AppTheme.cyan : AppTheme.orange;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(
              esFamiliar
                  ? Icons.family_restroom_rounded
                  : Icons.shield_rounded,
              color: color,
              size: 20,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Tipo de Salida',
                    style: TextStyle(
                        fontSize: 10,
                        color: AppTheme.textGray,
                        fontWeight: FontWeight.w600)),
                Text(
                  esFamiliar
                      ? 'Entrega a Familiar'
                      : 'Autoridad Legal (Fiscalia / PNP)',
                  style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: color),
                ),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: const Color(0xFFFEF3C7),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Text('Definido por Admision',
                style: TextStyle(
                    fontSize: 9,
                    color: Color(0xFF92400E),
                    fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }

  // ── Sección 4a: Familiar readonly ───────────────────────────────
  Widget _buildSeccionFamiliarReadonly(ActaRetiroModel datos) {
    return _SeccionReadonly(
      titulo: 'Datos del Familiar',
      icono: Icons.person_rounded,
      color: AppTheme.cyan,
      campos: [
        _CampoReadonly(
            label: 'Nombre Completo',
            value: datos.familiarNombreCompleto ?? '—',
            fullWidth: true),
        _CampoReadonly(
            label: 'Tipo Documento',
            value: datos.familiarTipoDocumento ?? '—',
            mono: true),
        _CampoReadonly(
            label: 'N° Documento',
            value: datos.familiarNumeroDocumento ?? '—',
            mono: true),
        _CampoReadonly(
            label: 'Parentesco',
            value: datos.familiarParentesco ?? '—'),
        _CampoReadonly(
            label: 'Telefono',
            value: datos.familiarTelefono ?? '—',
            mono: true),
      ],
    );
  }

  // ── Sección 4b: Autoridad readonly ──────────────────────────────
  Widget _buildSeccionAutoridadReadonly(ActaRetiroModel datos) {
    return _SeccionReadonly(
      titulo: 'Datos de la Autoridad Legal',
      icono: Icons.shield_rounded,
      color: AppTheme.orange,
      bgColor: const Color(0xFFFFFBEB),
      borderColor: const Color(0xFFFDE68A),
      campos: [
        _CampoReadonly(
            label: 'Nombre Completo',
            value: datos.autoridadNombreCompleto ?? '—',
            fullWidth: true),
        _CampoReadonly(
            label: 'Tipo Documento',
            value: datos.autoridadTipoDocumento ?? '—',
            mono: true),
        _CampoReadonly(
            label: 'N° Documento',
            value: datos.autoridadNumeroDocumento ?? '—',
            mono: true),
        _CampoReadonly(
            label: 'Grado / Cargo',
            value: datos.autoridadCargo ?? '—'),
        _CampoReadonly(
            label: 'Comisaria / Institucion',
            value: datos.autoridadInstitucion ?? '—'),
        _CampoReadonly(
            label: 'N° Oficio',
            value: datos.numeroOficioPolicial ?? '—',
            mono: true),
        _CampoReadonly(
            label: 'Telefono',
            value: datos.autoridadTelefono ?? '—',
            mono: true),
      ],
    );
  }

  // ── Sección 5: Funeraria (solo Familiar) ────────────────────────
  Widget _buildSeccionFuneraria() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: const Color(0xFFF3E8FF),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(Icons.local_shipping_rounded,
                    color: Color(0xFF7C3AED), size: 18),
              ),
              const SizedBox(width: 10),
              const Text('Datos de Funeraria y Vehiculo',
                  style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.textDark)),
              const SizedBox(width: 6),
              Text('(opcional)',
                  style:
                      TextStyle(fontSize: 11, color: AppTheme.textGray)),
            ],
          ),
          const SizedBox(height: 14),
          _buildInput(
            label: 'Nombre de Funeraria',
            ctrl: _funerariaCtrl,
            hint: 'Ej: Funeraria San Martin',
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _buildInput(
                  label: _funerariaIngresada
                      ? 'Conductor *'
                      : 'Conductor',
                  ctrl: _conductorCtrl,
                  hint: 'Nombre del conductor',
                  errorText:
                      _conductorRequerido ? 'Requerido' : null,
                  onChanged: (_) => setState(() {}),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _buildInput(
                  label: 'DNI Conductor',
                  ctrl: _dniConductorCtrl,
                  hint: '00000000',
                  numeric: true,
                  maxLength: 8,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _buildInput(
            label: _funerariaIngresada
                ? 'Placa del Vehiculo Funerario *'
                : 'Placa del Vehiculo Funerario',
            ctrl: _placaCtrl,
            hint: 'Ej: A1F-819',
            upperCase: true,
            errorText: _placaRequerida ? 'Requerida' : null,
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _buildInput(
                  label: 'Ayudante',
                  ctrl: _ayudanteCtrl,
                  hint: 'Nombre del ayudante',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _buildInput(
                  label: 'DNI Ayudante',
                  ctrl: _dniAyudanteCtrl,
                  hint: '00000000',
                  numeric: true,
                  maxLength: 8,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ── Sección 6: Placa autoridad ───────────────────────────────────
  Widget _buildSeccionPlacaAutoridad() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: AppTheme.orange.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(Icons.local_police_rounded,
                    color: AppTheme.orange, size: 18),
              ),
              const SizedBox(width: 10),
              const Text('Vehiculo Oficial',
                  style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.textDark)),
              const SizedBox(width: 6),
              Text('(opcional)',
                  style:
                      TextStyle(fontSize: 11, color: AppTheme.textGray)),
            ],
          ),
          const SizedBox(height: 14),
          _buildInput(
            label: 'Placa del Patrullero / Vehiculo Oficial',
            ctrl: _placaCtrl,
            hint: 'Ej: BWR-763',
            upperCase: true,
          ),
        ],
      ),
    );
  }

  // ── Destino readonly ─────────────────────────────────────────────
  Widget _buildSeccionDestino(String destino) {
    return Container(
      padding:
          const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Row(
        children: [
          const Icon(Icons.place_rounded,
              color: AppTheme.textGray, size: 18),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('DESTINO FINAL',
                    style: TextStyle(
                        fontSize: 9,
                        color: AppTheme.textGray,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.5)),
                const SizedBox(height: 2),
                Text(destino,
                    style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.textDark)),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: const Color(0xFFFEF3C7),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Text('Del acta',
                style: TextStyle(
                    fontSize: 9,
                    color: Color(0xFF92400E),
                    fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }

  // ── Sección 7: Observaciones ─────────────────────────────────────
  Widget _buildSeccionObservaciones(ActaRetiroModel datos) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Observaciones',
              style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textDark)),
          const SizedBox(height: 8),
          TextField(
            controller: _obsCtrl,
            maxLines: 3,
            maxLength: 1000,
            decoration: InputDecoration(
              hintText:
                  'Registre incidencias, discrepancias o informacion adicional...',
              hintStyle:
                  TextStyle(fontSize: 13, color: AppTheme.textGray),
            ),
          ),
          if (datos.esAutoridadLegal)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Row(
                children: [
                  const Icon(Icons.info_outline_rounded,
                      size: 13, color: AppTheme.orange),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      'Si hay discrepancia con el documento fisico, registrela aqui.',
                      style: TextStyle(
                          fontSize: 11, color: AppTheme.orange),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  // ── Botones sticky ────────────────────────────────────────────────
  Widget _buildStickyButtons() {
    final puede = _datos?.puedeRegistrarSalida ?? false;
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
              onPressed:
                  (_enviando || !puede) ? null : _confirmarSalida,
              icon: _enviando
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                          color: Colors.white, strokeWidth: 2))
                  : const Icon(Icons.check_circle_rounded),
              label: Text(_enviando
                  ? 'PROCESANDO...'
                  : !puede
                      ? 'DOCUMENTACION INCOMPLETA'
                      : 'CONFIRMAR ENTREGA'),
              style: ElevatedButton.styleFrom(
                backgroundColor:
                    puede ? AppTheme.green : Colors.grey.shade400,
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed:
                  _enviando ? null : () => Navigator.pop(context),
              icon: const Icon(Icons.arrow_back_rounded, size: 18),
              label: const Text('CANCELAR'),
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

  // ── Helper input ──────────────────────────────────────────────────
  Widget _buildInput({
    required String label,
    required TextEditingController ctrl,
    String? hint,
    bool numeric = false,
    bool upperCase = false,
    int? maxLength,
    String? errorText,
    void Function(String)? onChanged,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppTheme.textDark)),
        const SizedBox(height: 4),
        TextField(
          controller: ctrl,
          keyboardType:
              numeric ? TextInputType.number : TextInputType.text,
          textCapitalization: upperCase
              ? TextCapitalization.characters
              : TextCapitalization.none,
          maxLength: maxLength,
          onChanged: onChanged,
          style: TextStyle(
              fontFamily: (numeric || upperCase) ? 'monospace' : null,
              fontSize: 14),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle:
                TextStyle(fontSize: 13, color: AppTheme.textGray),
            errorText: errorText,
            counterText: '',
          ),
        ),
      ],
    );
  }
}

// ================================================================
// WIDGETS INTERNOS
// ================================================================

class _SeccionReadonly extends StatelessWidget {
  final String titulo;
  final IconData icono;
  final Color color;
  final Color bgColor;
  final Color borderColor;
  final List<_CampoReadonly> campos;

  const _SeccionReadonly({
    required this.titulo,
    required this.icono,
    required this.color,
    required this.campos,
    this.bgColor = Colors.white,
    this.borderColor = const Color(0xFFE5E7EB),
  });

  @override
  Widget build(BuildContext context) {
    final fullWidth = campos.where((c) => c.fullWidth).toList();
    final grid = campos.where((c) => !c.fullWidth).toList();
    final pairs = <List<_CampoReadonly>>[];
    for (var i = 0; i < grid.length; i += 2) {
      pairs.add(
          grid.sublist(i, i + 2 > grid.length ? grid.length : i + 2));
    }

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: borderColor),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icono, color: color, size: 18),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(titulo,
                    style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.textDark)),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                    horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEF3C7),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Text('Solo lectura',
                    style: TextStyle(
                        fontSize: 9,
                        color: Color(0xFF92400E),
                        fontWeight: FontWeight.w600)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...fullWidth.map((c) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: c,
              )),
          ...pairs.map((par) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: par[0]),
                    if (par.length > 1) ...[
                      const SizedBox(width: 12),
                      Expanded(child: par[1]),
                    ],
                  ],
                ),
              )),
        ],
      ),
    );
  }
}

class _CampoReadonly extends StatelessWidget {
  final String label;
  final String value;
  final bool mono;
  final bool fullWidth;

  const _CampoReadonly({
    required this.label,
    required this.value,
    this.mono = false,
    this.fullWidth = false,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label.toUpperCase(),
            style: const TextStyle(
                fontSize: 9,
                color: AppTheme.textGray,
                fontWeight: FontWeight.bold,
                letterSpacing: 0.5)),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppTheme.textDark,
              fontFamily: mono ? 'monospace' : null),
          overflow: TextOverflow.ellipsis,
          maxLines: 2,
        ),
      ],
    );
  }
}

class _DeudaItem extends StatelessWidget {
  final IconData icono;
  final String titulo;
  final String? mensaje;

  const _DeudaItem({
    required this.icono,
    required this.titulo,
    this.mensaje,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icono, color: AppTheme.red, size: 16),
        const SizedBox(width: 8),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(titulo,
                  style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.red)),
              if (mensaje != null)
                Text(mensaje!,
                    style: const TextStyle(
                        fontSize: 11, color: AppTheme.red)),
            ],
          ),
        ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool mono;
  final Color color;

  const _Chip({
    required this.icon,
    required this.label,
    this.mono = false,
    this.color = AppTheme.textGray,
  });

  @override
Widget build(BuildContext context) {
  return Container(
    width: double.infinity,
    padding:
        const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
    decoration: BoxDecoration(
      color: color.withValues(alpha: 0.08),
      borderRadius: BorderRadius.circular(8),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 12, color: color),
        const SizedBox(width: 4),
        Flexible(
          child: Text(label,
              style: TextStyle(
                  fontSize: 11,
                  color: color,
                  fontWeight: FontWeight.w600,
                  fontFamily: mono ? 'monospace' : null),
              overflow: TextOverflow.ellipsis),
        ),
      ],
    ),
  );
}
}

class _ResumenRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final bool bold;
  final bool mono;

  const _ResumenRow({
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
        Icon(icon, size: 15, color: AppTheme.green),
        const SizedBox(width: 8),
        SizedBox(
            width: 80,
            child: Text(label,
                style: const TextStyle(
                    fontSize: 12, color: AppTheme.textGray))),
        Expanded(
          child: Text(value,
              style: TextStyle(
                  fontSize: 13,
                  fontWeight:
                      bold ? FontWeight.bold : FontWeight.w500,
                  color: AppTheme.textDark,
                  fontFamily: mono ? 'monospace' : null)),
        ),
      ],
    );
  }
}