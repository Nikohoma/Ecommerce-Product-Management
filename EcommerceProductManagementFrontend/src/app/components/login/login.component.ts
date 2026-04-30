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
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
    }
    .login-card {
      width: 100%;
      max-width: 420px;
      padding: 36px;
      border-radius: 24px;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
    }
    .tab-bar {
      display: flex;
      gap: 8px;
      margin-bottom: 24px;
    }
    .tab-button {
      flex: 1;
      padding: 12px 0;
      border-radius: 14px;
      background: rgba(255,255,255,0.6);
      border: 1px solid transparent;
      color: #475569;
      font-weight: 700;
      transition: all 0.2s ease;
    }
    .tab-button.active {
      background: #ffffff;
      border-color: #6366f1;
      color: #1e293b;
      box-shadow: 0 10px 25px -15px rgba(0, 0, 0, 0.25);
    }
    .login-header {
      text-align: center;
      margin-bottom: 24px;
    }
    .login-header h1 {
      font-size: 1.75rem;
      color: #1e293b;
      margin-bottom: 8px;
    }
    .login-header p {
      color: #64748b;
      font-size: 0.95rem;
      margin: 0;
    }
    .form-group {
      margin-bottom: 18px;
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
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12);
      outline: none;
    }
    .button-row {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }
    .text-button {
      background: transparent;
      color: #6366f1;
      font-weight: 700;
      border: none;
      cursor: pointer;
      padding: 0;
    }
    .button-secondary {
      background: #f1f5f9;
      color: #1e293b;
      padding: 10px 16px;
      border-radius: 12px;
      border: 1px solid transparent;
      min-width: 140px;
      transition: background 0.2s;
    }
    .button-secondary:hover {
      background: #e2e8f0;
    }
    .w-full { width: 100%; }
    .error-msg {
      color: #ef4444;
      font-size: 0.925rem;
      margin-bottom: 16px;
      text-align: center;
    }
    .success-msg {
      color: #15803d;
      font-size: 0.925rem;
      margin-bottom: 16px;
      text-align: center;
    }
    .hint-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
      margin-top: 8px;
      font-size: 0.9rem;
    }
    .hint-row span {
      color: #64748b;
    }
    .hint-row button {
      color: #6366f1;
      background: transparent;
      border: none;
      cursor: pointer;
      font-weight: 700;
    }
    .hint-row button:hover {
      text-decoration: underline;
    }
    .otp-note {
      background: #eff6ff;
      color: #1d4ed8;
      border: 1px solid #bfdbfe;
      padding: 12px 14px;
      border-radius: 12px;
      margin-top: 12px;
      font-size: 0.9rem;
    }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  viewMode: 'login' | 'register' | 'forgot' = 'login';

  credentials = { email: '', password: '' };
  registerData = { name: '', email: '', password: '', otp: '' };
  resetData = { email: '', otp: '', newPassword: '' };
  loading = false;
  error = '';
  success = '';
  registerOtpSent = false;
  resetOtpSent = false;

  setMode(mode: 'login' | 'register' | 'forgot') {
    this.viewMode = mode;
    this.error = '';
    this.success = '';
    this.loading = false;
    this.registerOtpSent = false;
    this.resetOtpSent = false;
    if (mode !== 'register') {
      this.registerData = { name: '', email: '', password: '', otp: '' };
    }
    if (mode !== 'forgot') {
      this.resetData = { email: '', otp: '', newPassword: '' };
    }
  }

  onLogin() {
    this.loading = true;
    this.error = '';
    this.success = '';
    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.error = err.status === 0
          ? 'Cannot connect to the gateway. Please ensure backend services are running.'
          : this.getErrorMessage(err, 'Invalid email or password');
        this.loading = false;
      }
    });
  }

  private getErrorMessage(err: any, fallback: string) {
    if (!err) return fallback;
    if (typeof err === 'string') return err;
    if (typeof err.error === 'string') return err.error;
    if (err.error && typeof err.error.message === 'string') return err.error.message;
    if (err.message) return err.message;
    if (typeof err.error === 'object') return JSON.stringify(err.error);
    return fallback;
  }

  sendRegisterOtp() {
    if (!this.registerData.email) {
      this.error = 'Please enter your email to receive the OTP.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.success = '';

    this.authService.registerSendOtp(this.registerData.email).subscribe({
      next: (res: any) => {
        this.registerOtpSent = true;
        this.success = typeof res === 'string' ? res : res?.message || 'OTP sent to your email. Enter it below to complete registration.';
        this.loading = false;
      },
      error: (err) => {
        this.error = this.getErrorMessage(err, 'Unable to send OTP for registration.');
        this.loading = false;
      }
    });
  }

  onRegister() {
    if (!this.registerData.email || !this.registerData.name || !this.registerData.password || !this.registerData.otp) {
      this.error = 'Please complete all registration fields, including OTP.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.success = '';

    this.authService.registerCustomerVerify(this.registerData).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.error = this.getErrorMessage(err, 'Registration failed. Check your OTP and details.');
        this.loading = false;
      }
    });
  }

  sendResetOtp() {
    if (!this.resetData.email) {
      this.error = 'Please enter your email to receive the reset OTP.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.success = '';

    this.authService.sendResetPasswordOtp(this.resetData.email).subscribe({
      next: (res: any) => {
        this.resetOtpSent = true;
        this.success = typeof res === 'string' ? res : res?.message || 'OTP sent to your email. Use it to reset your password.';
        this.loading = false;
      },
      error: (err) => {
        this.error = this.getErrorMessage(err, 'Unable to send password reset OTP.');
        this.loading = false;
      }
    });
  }

  onResetPassword() {
    if (!this.resetData.email || !this.resetData.otp || !this.resetData.newPassword) {
      this.error = 'Please enter email, OTP, and your new password.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.success = '';

    this.authService.resetPassword({
      email: this.resetData.email,
      otp: this.resetData.otp,
      newPassword: this.resetData.newPassword
    }).subscribe({
      next: () => {
        this.success = 'Password reset successful. Please sign in with your new password.';
        this.loading = false;
        this.resetOtpSent = false;
        this.resetData.otp = '';
        this.resetData.newPassword = '';
        this.viewMode = 'login';
      },
      error: (err) => {
        this.error = this.getErrorMessage(err, 'Password reset failed. Please check your OTP and try again.');
        this.loading = false;
      }
    });
  }
}
