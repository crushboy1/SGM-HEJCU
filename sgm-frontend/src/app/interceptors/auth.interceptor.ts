import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import Swal from 'sweetalert2';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  // 1. Inyectar token en la petición saliente
  const token = localStorage.getItem('sgm_token');
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  // 2. Manejar respuesta — interceptar 401
  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Limpiar sesión
        localStorage.removeItem('sgm_token');
        localStorage.removeItem('sgm_user');
        localStorage.removeItem('sgm_role');
        localStorage.removeItem('sgm_name');
        localStorage.removeItem('sgm_user_id');

        // Mostrar alerta solo si no estamos ya en el login
        // (evita bucles si el propio login devuelve 401)
        if (!router.url.includes('/login')) {
          Swal.fire({
            icon: 'warning',
            title: 'Sesión Expirada',
            html: `
              <p class="text-gray-600">
                Su sesión ha expirado por inactividad.<br>
                Por favor, inicie sesión nuevamente para continuar.
              </p>
            `,
            confirmButtonText: 'Ir al Login',
            confirmButtonColor: '#0891B2',
            allowOutsideClick: false,
            allowEscapeKey: false,
            width: '400px',
            padding: '1.5rem'
          }).then(() => {
            router.navigate(['/login']);
          });
        }
      }

      return throwError(() => error);
    })
  );
};
