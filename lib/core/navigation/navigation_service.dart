import 'package:flutter/material.dart';
import '../constants/routes.dart';

class NavigationService {
  static final GlobalKey<NavigatorState> navigatorKey =
      GlobalKey<NavigatorState>();

  static bool _navegandoALogin = false;

  static void irLogin() {
    if (_navegandoALogin) return;
    _navegandoALogin = true;
    navigatorKey.currentState?.pushNamedAndRemoveUntil(
      Routes.login,
      (_) => false,
    );
  }

  static void reset() {
    _navegandoALogin = false;
  }
}