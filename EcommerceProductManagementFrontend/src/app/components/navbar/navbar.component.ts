import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styles: [`
    .navbar {
      position: sticky;
      top: 0;
      z-index: 100;
      height: 72px;
      margin-bottom: 32px;
      border-bottom: 1px solid rgba(0,0,0,0.05);
    }
    .nav-content {
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 12px;
      font-weight: 800;
      font-size: 1.25rem;
      color: #1e293b;
      cursor: pointer;
    }
    .logo {
      width: 36px;
      height: 36px;
      background: #6366f1;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 10px;
      font-size: 1.5rem;
    }
    .nav-links {
      display: flex;
      list-style: none;
      gap: 32px;
    }
    .nav-links a {
      text-decoration: none;
      color: #64748b;
      font-weight: 600;
      font-size: 0.9375rem;
      transition: color 0.2s;
    }
    .nav-links a:hover, .nav-links a.active {
      color: #6366f1;
    }
    .user-actions {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .user-info {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
    }
    .role-badge {
      font-size: 0.625rem;
      font-weight: 800;
      text-transform: uppercase;
      background: #e2e8f0;
      padding: 2px 6px;
      border-radius: 4px;
      color: #475569;
    }
    .user-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: #1e293b;
    }
    .btn-logout {
      background: transparent;
      color: #94a3b8;
      padding: 8px;
      border-radius: 10px;
    }
    .btn-logout:hover {
      background: #fee2e2;
      color: #ef4444;
    }
  `]
})
export class NavbarComponent {
  authService = inject(AuthService);
}
