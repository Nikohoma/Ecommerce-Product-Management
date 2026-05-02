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
  recentPage = 1;
  recentPageSize = 10;
  recentTotalCount = 0;
  recentTotalPages = 0;

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
    this.reportingService.getRecentReportsPaged(this.recentPage, this.recentPageSize).subscribe(res => {
      this.recentReports = res.items || [];
      this.recentTotalCount = res.totalCount || 0;
      this.recentTotalPages = Math.max(1, Math.ceil(this.recentTotalCount / this.recentPageSize));
    });
  }

  changeRecentPage(nextPage: number) {
    if (nextPage < 1 || (this.recentTotalPages && nextPage > this.recentTotalPages)) return;
    this.recentPage = nextPage;
    this.loadRecent();
  }

  getActivityLabel(item: any): string {
    const type = (item?.activityType || '').toString().toLowerCase();
    if (type === 'productcreated') return 'Product Created';
    if (type === 'priceupdated') return 'Price Updated';
    if (type === 'stockupdated') return 'Stock Updated';
    if (type === 'mediauploaded') return 'Media Uploaded';
    return 'Activity';
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
