import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent],
  templateUrl: './app.component.html',
  styles: [`
    main {
      min-height: 100vh;
      transition: padding 0.3s ease;
    }
    .has-nav {
      padding-bottom: 80px;
    }
  `]
})
export class AppComponent {
  authService = inject(AuthService);
  themeService = inject(ThemeService);
}
