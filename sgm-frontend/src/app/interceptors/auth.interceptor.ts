import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // 1. Obtener el token que guardamos en el Login
  const token = localStorage.getItem('sgm_token');

  // 2. Si existe token, clonamos la petición y le inyectamos el Header
  if (token) {
    const authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    // Pasamos la petición "modificada" (con el token)
    return next(authReq);
  }

  // 3. Si no hay token (ej. al hacer Login), pasa la petición original
  return next(req);
};
