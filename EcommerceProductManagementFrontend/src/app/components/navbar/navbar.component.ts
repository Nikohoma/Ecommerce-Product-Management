import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
  // styles: [`

  // `]
})
export class NavbarComponent {
  authService = inject(AuthService);
  themeService = inject(ThemeService);

  isDarkMode$ = this.themeService.darkMode$;

  toggleTheme(): void {
    this.themeService.toggleDarkMode();
  }
}
