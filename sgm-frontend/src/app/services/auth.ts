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
          // Guardamos datos en localStorage
          localStorage.setItem(this.tokenKey, response.token);
          localStorage.setItem(this.userKey, response.username);
          localStorage.setItem(this.roleKey, response.rol);
          localStorage.setItem(this.nameKey, response.nombreCompleto);
        }
      })
    );
  }

  /**
   * Logout del usuario
   */
  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.nameKey);
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
