import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styles: [`
    .login-container {
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
    }
    .login-card {
      width: 100%;
      max-width: 400px;
      padding: 40px;
      border-radius: 24px;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
    }
    .login-header {
      text-align: center;
      margin-bottom: 32px;
    }
    .login-header h1 {
      font-size: 1.875rem;
      color: #1e293b;
      margin-bottom: 8px;
    }
    .login-header p {
      color: #64748b;
      font-size: 0.875rem;
    }
    .form-group {
      margin-bottom: 20px;
    }
    .form-group label {
      display: block;
      font-size: 0.875rem;
      font-weight: 600;
      margin-bottom: 6px;
      color: #475569;
    }
    .form-group input {
      width: 100%;
      padding: 12px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      font-size: 1rem;
      transition: all 0.2s;
    }
    .form-group input:focus {
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
      outline: none;
    }
    .w-full { width: 100%; }
    .error-msg {
      color: #ef4444;
      font-size: 0.875rem;
      margin-bottom: 16px;
      text-align: center;
    }
    .role-hints {
      margin-top: 32px;
      padding-top: 24px;
      border-top: 1px solid rgba(0,0,0,0.05);
      text-align: center;
    }
    .role-hints p {
      font-size: 0.75rem;
      color: #94a3b8;
      margin-bottom: 8px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .hint-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
    }
    .hint-grid span {
      font-size: 0.75rem;
      background: rgba(0,0,0,0.05);
      padding: 4px 8px;
      border-radius: 6px;
      color: #64748b;
    }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  credentials = { email: '', password: '' };
  loading = false;
  error = '';

  onLogin() {
    this.loading = true;
    this.error = '';
    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        if (err.status === 0) {
          this.error = 'Cannot connect to the gateway. Please ensure backend services are running.';
        } else {
          this.error = 'Invalid email or password';
        }
        this.loading = false;
      }
    });
  }
}
