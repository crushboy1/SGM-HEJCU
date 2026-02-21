import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Verificamos si está logueado (si tiene token)
  if (authService.isLoggedIn()) {
    return true;
  }

  // Opcional: Log para debugging
  console.warn('[AuthGuard] Acceso denegado - Redirigiendo a login');

  // Si no tiene token, redirigir al login
  return router.createUrlTree(['/login']);
};
