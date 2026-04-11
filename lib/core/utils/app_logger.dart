import 'package:flutter/foundation.dart';

class AppLogger {
  static void info(String message) =>
      debugPrint('[SGM INFO] $message');

  static void error(String message) =>
      debugPrint('[SGM ERROR] $message');

  static void qr(String message) =>
      debugPrint('[SGM QR] $message');
}