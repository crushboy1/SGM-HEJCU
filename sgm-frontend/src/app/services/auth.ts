import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

// ===================================================================
// INTERFACES
// ===================================================================
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token: string;
  username: string;
  nombreCompleto: string;
  rol: string;
  userId?: number;
  errorMessage?: string;
}

export interface CurrentUser {
  username: string;
  nombreCompleto: string;
  rol: string;
}

// ===================================================================
// SERVICE
// ===================================================================
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7153/api';

  private tokenKey = 'sgm_token';
  private userKey = 'sgm_user';
  private roleKey = 'sgm_role';
  private nameKey = 'sgm_name';

  /**
   * Login del usuario
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/login`, credentials).pipe(
      tap(response => {
        if (response.success && response.token) {
          // Guardar datos básicos
          localStorage.setItem(this.tokenKey, response.token);
          localStorage.setItem(this.userKey, response.username);
          localStorage.setItem(this.roleKey, response.rol);
          localStorage.setItem(this.nameKey, response.nombreCompleto);

          // Extraer UserID del JWT (siempre)
          this.extraerUserIdDelToken(response.token);
        }
      })
    );
  }
  /**
 * Extrae el UserID del token JWT y lo guarda en localStorage
 */
  private extraerUserIdDelToken(token: string): void {
    try {
      const parts = token.split('.');

      if (parts.length !== 3) {
        console.error('[AuthService] Token JWT inválido');
        return;
      }

      const payload = JSON.parse(atob(parts[1]));

      // Buscar UserID en múltiples claims posibles
      const userId =
        payload.nameid ||
        payload.sub ||
        payload.userId ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

      if (userId) {
        localStorage.setItem('sgm_user_id', userId.toString());
      } else {
        console.error('[AuthService] No se pudo extraer UserID del token');
      }
    } catch (error) {
      console.error('[AuthService] Error al decodificar token JWT:', error);
    }
  }
  /**
   * Logout del usuario
   */
  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.nameKey);
    localStorage.removeItem('sgm_user_id');
  }

  /**
   * Obtener el token JWT
   */
  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  /**
   * Verificar si el usuario está autenticado
   */
  isLoggedIn(): boolean {
    const token = this.getToken();
    return !!token;
  }

  /**
   * Obtener datos del usuario actual
   */
  getCurrentUser(): CurrentUser | null {
    const username = localStorage.getItem(this.userKey);
    const nombreCompleto = localStorage.getItem(this.nameKey);
    const rol = localStorage.getItem(this.roleKey);

    if (!username || !nombreCompleto || !rol) {
      return null;
    }

    return {
      username,
      nombreCompleto,
      rol
    };
  }
  getUserId(): number {
    const userIdStr = localStorage.getItem('sgm_user_id');

    if (!userIdStr) {
      console.error('[AuthService] No se encontró sgm_user_id en localStorage');
      throw new Error('Usuario no autenticado o UserID no disponible');
    }

    const userId = parseInt(userIdStr, 10);

    if (isNaN(userId) || userId <= 0) {
      console.error('[AuthService] UserID inválido:', userIdStr);
      throw new Error('UserID inválido en localStorage');
    }

    return userId;
  }
  /**
   * Obtener nombre del usuario
   */
  getUserName(): string {
    return localStorage.getItem(this.nameKey) || 'Usuario';
  }

  /**
   * Obtener rol del usuario
   */
  getUserRole(): string {
    return localStorage.getItem(this.roleKey) || 'Sin Rol';
  }
}
