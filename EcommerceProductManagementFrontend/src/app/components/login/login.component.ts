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
  styleUrls: ['./login.component.css'],
  // styles: [`

  // `]
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
    
    // Check for Angular parsing errors (often happens when server returns text but client expects JSON)
    if (err.message && err.message.includes('Http failure during parsing')) {
      return fallback; // Return the meaningful fallback instead of the technical parsing error
    }

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
