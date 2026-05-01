import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { CatalogComponent } from './components/catalog/catalog.component';
import { ProductFormComponent } from './components/product-form/product-form.component';
import { UserManagementComponent } from './components/user-management/user-management.component';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { dashboardGuard } from './guards/dashboard.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [dashboardGuard] },
  { path: 'catalog', component: CatalogComponent, canActivate: [authGuard] },
  { path: 'product/add', component: ProductFormComponent, canActivate: [dashboardGuard] },
  { path: 'product/edit/:id', component: ProductFormComponent, canActivate: [authGuard] },
  { path: 'users', component: UserManagementComponent, canActivate: [adminGuard] },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];
