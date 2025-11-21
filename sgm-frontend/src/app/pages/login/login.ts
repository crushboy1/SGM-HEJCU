import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, LoginRequest } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})

export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  credentials: LoginRequest = { username: '', password: '' };
  isLoading = false;
  errorMessage = '';

  onLogin() {
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.credentials).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          // ¡Login Exitoso!
          alert(`Bienvenido ${res.nombreCompleto} (${res.rol})`);
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage = res.errorMessage || 'Credenciales inválidas';
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        this.errorMessage = 'Error de conexión con el servidor.';
      }
    });
  }
}
