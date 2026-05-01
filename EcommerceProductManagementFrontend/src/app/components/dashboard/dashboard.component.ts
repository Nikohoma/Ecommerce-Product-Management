import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportingService, DashboardData } from '../../services/reporting.service';
import { AuthService } from '../../services/auth.service';
import { ProductService, Product } from '../../services/product.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  // styles: [`

  // `]
})
export class DashboardComponent implements OnInit {
  reportingService = inject(ReportingService);
  authService = inject(AuthService);

  dashboardData?: DashboardData;
  recentReports: any[] = [];
  role = '';

  ngOnInit() {
    this.role = this.authService.getRole();
    if (this.role !== 'Customer') {
      this.loadDashboard();
      this.loadRecent();
    }
  }

  loadDashboard() {
    this.reportingService.getDashboard().subscribe(data => this.dashboardData = data);
  }

  loadRecent() {
    this.reportingService.getRecentReports().subscribe(data => this.recentReports = data);
  }

  getStatusClass(status: any) {
    if (typeof status === 'number') {
      const statuses = ['Draft', 'Submitted', 'Approved', 'Rejected', 'Active', 'Inactive'];
      status = statuses[status] || 'Unknown';
    }
    if (!status) return 'info';
    status = status.toString().toLowerCase();
    if (status === 'approved' || status === 'active') return 'success';
    if (status === 'submitted' || status === 'pending') return 'warning';
    if (status === 'rejected') return 'danger';
    return 'info';
  }
}
