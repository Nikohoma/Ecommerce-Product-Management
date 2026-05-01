import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = 'http://localhost:7000/gateway/api/auth';

  private userSubject = new BehaviorSubject<any>(null);
  user$ = this.userSubject.asObservable();

  constructor() {
    const token = localStorage.getItem('token');
    if (token) {
      this.decodeToken(token);
    }
  }

  getUserFromStorage() {
    const data = localStorage.getItem('user');
    return data ? JSON.parse(data) : null;
  }

  login(credentials: any) {
    const payload = {
      email: credentials.email,
      password: credentials.password
    };
    return this.http.post(`${this.apiUrl}/login`, payload).pipe(
      tap((res: any) => {
        const token = res.accessToken || res.token;
        if (token) {
          localStorage.setItem('token', token);
          this.decodeToken(token);
        }
      })
    );
  }

  registerSendOtp(email: string) {
    return this.http.post(`${this.apiUrl}/register/send-otp`, { email }, { responseType: 'text' as 'json' });
  }

  registerVerify(data: { name: string; email: string; password: string; otp: string }) {
    return this.http.post(`${this.apiUrl}/register/verify`, data).pipe(
      tap((res: any) => {
        const token = res.accessToken || res.token;
        if (token) {
          localStorage.setItem('token', token);
          this.decodeToken(token);
        }
      })
    );
  }

  registerCustomerVerify(data: { name: string; email: string; password: string; otp: string }) {
    return this.http.post(`${this.apiUrl}/register/customer/verify`, data).pipe(
      catchError((err) => {
        // Backward compatibility: if backend isn't restarted with the new route,
        // fall back to existing register verify endpoint.
        if (err?.status === 404) {
          return this.http.post(`${this.apiUrl}/register/verify`, data);
        }
        return throwError(() => err);
      }),
      tap((res: any) => {
        const token = res.accessToken || res.token;
        if (token) {
          localStorage.setItem('token', token);
          this.decodeToken(token);
        }
      })
    );
  }

  sendResetPasswordOtp(email: string) {
    return this.http.post(`${this.apiUrl}/password/reset/send-otp`, { email }, { responseType: 'text' as 'json' });
  }

  resetPassword(data: { email: string; otp: string; newPassword: string }) {
    return this.http.post(`${this.apiUrl}/password/reset/verify`, data, { responseType: 'text' as 'json' });
  }


  associateSignup(data: any) {
    return this.http.post(`${this.apiUrl}/associateSignup`, data, { responseType: 'text' as 'json' });
  }

  getUsers() {
    return this.http.get<any[]>(`${this.apiUrl}/users`);
  }

  updateUser(data: { Email: string; Role: string; IsActive: boolean }) {
    return this.http.put(`${this.apiUrl}/users/update`, data, { responseType: 'text' as 'json' });
  }

  logout() {
    localStorage.removeItem('token');
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  private decodeToken(token: string) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // Mapping common claim names
      const user = {
        id: payload.nameid || payload.sub,
        name: payload.unique_name || payload.name,
        email: payload.sub || payload.unique_name || payload.email,
        role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
        token: token
      };
      this.userSubject.next(user);
    } catch (e) {
      this.logout();
    }
  }

  get token() {
    return localStorage.getItem('token');
  }

  isLoggedIn() {
    return !!this.userSubject.value;
  }

  getRole() {
    return this.userSubject.value?.role;
  }

  getEmail() {
    return this.userSubject.value?.email;
  }

  getName() {
    return this.userSubject.value?.name;
  }
}
