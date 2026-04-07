import 'package:flutter/material.dart';

class AppTheme {
  // Colores base — alineados con Tailwind del frontend Angular
  static const Color cyan = Color(0xFF0891B2);
  static const Color cyanDark = Color(0xFF0E7490);
  static const Color cyanLight = Color(0xFFECFEFF);
  static const Color red = Color(0xFFDC3545);
  static const Color green = Color(0xFF28A745);
  static const Color orange = Color(0xFFFD7E14);
  static const Color bgGray = Color(0xFFE8EEF3);
  static const Color textDark = Color(0xFF1F2937);
  static const Color textGray = Color(0xFF6B7280);

  static ThemeData get theme {
    const colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: cyan,
      onPrimary: Colors.white,
      secondary: cyanDark,
      onSecondary: Colors.white,
      error: red,
      onError: Colors.white,
      surface: Colors.white,
      onSurface: textDark,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: bgGray,
      fontFamily: 'Roboto',

      textTheme: const TextTheme(
        titleLarge: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: textDark,
        ),
        bodyMedium: TextStyle(fontSize: 14, color: textDark),
        labelLarge: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
      ),

      appBarTheme: const AppBarTheme(
        backgroundColor: cyan,
        foregroundColor: Colors.white,
        elevation: 0,
        centerTitle: false,
      ),

      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ButtonStyle(
          minimumSize: WidgetStateProperty.all(const Size.fromHeight(52)),
          shape: WidgetStateProperty.all(
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
          textStyle: WidgetStateProperty.all(
            const TextStyle(fontWeight: FontWeight.w600, letterSpacing: 0.5),
          ),
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return Colors.grey.shade300;
            }
            return cyan;
          }),
          foregroundColor: WidgetStateProperty.all(Colors.white),
        ),
      ),

      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFFD1D5DB)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFFD1D5DB)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: cyan, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: red),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: red, width: 2),
        ),
        labelStyle: const TextStyle(color: textGray),
      ),

      cardTheme: CardThemeData(
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        color: Colors.white,
      ),
    );
  }
}
