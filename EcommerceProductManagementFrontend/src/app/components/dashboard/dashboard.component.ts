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
  styles: [`
    .page-header {
      margin-bottom: 40px;
    }
    .page-header h1 {
      font-size: 2.25rem;
      margin-bottom: 8px;
    }
    .page-header p {
      color: #64748b;
    }
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 24px;
      margin-bottom: 48px;
    }
    .kpi-card {
      display: flex;
      align-items: center;
      gap: 20px;
    }
    .kpi-icon {
      width: 56px;
      height: 56px;
      border-radius: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .kpi-icon.pending { background: #fff7ed; color: #f97316; }
    .kpi-icon.approved { background: #f0fdf4; color: #22c55e; }
    .kpi-icon.rejected { background: #fef2f2; color: #ef4444; }
    .kpi-icon.value { background: #eef2ff; color: #6366f1; }
    
    .kpi-info h3 {
      font-size: 1.5rem;
      color: #1e293b;
    }
    .kpi-info p {
      font-size: 0.875rem;
      color: #64748b;
      font-weight: 500;
    }

    .section-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 24px;
    }
    .btn-text {
      background: transparent;
      color: #6366f1;
      font-size: 0.875rem;
    }

    .table-container {
      padding: 0;
      overflow: hidden;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th {
      text-align: left;
      padding: 16px 24px;
      background: #f8fafc;
      font-size: 0.75rem;
      text-transform: uppercase;
      color: #64748b;
      font-weight: 700;
      border-bottom: 1px solid #e2e8f0;
    }
    td {
      padding: 16px 24px;
      border-bottom: 1px solid #f1f5f9;
      font-size: 0.875rem;
    }
    
    .promo-banner {
      background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
      color: white;
      text-align: center;
      padding: 64px;
    }
    .promo-banner h2 { font-size: 2.5rem; margin-bottom: 16px; }
    .promo-banner p { font-size: 1.125rem; margin-bottom: 32px; opacity: 0.9; }
    .promo-banner .btn-primary { background: white; color: #6366f1; }
  `]
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
