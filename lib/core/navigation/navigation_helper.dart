import 'package:flutter/material.dart';
import '../constants/routes.dart';
import '../models/usuario_model.dart';

class NavigationHelper {
  static void navegarSegunRol(BuildContext context, UsuarioModel usuario) {
    if (usuario.esAmbulancia) {
      Navigator.pushReplacementNamed(context, Routes.ambulancia);
    } else if (usuario.esVigilante) {
      Navigator.pushReplacementNamed(context, Routes.vigilante);
    } else {
      Navigator.pushReplacementNamed(context, Routes.login);
    }
  }
}