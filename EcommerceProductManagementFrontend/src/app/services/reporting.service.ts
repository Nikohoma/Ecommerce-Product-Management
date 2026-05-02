import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface DashboardData {
  approved: number;
  rejected: number;
  pending: number;
  totalInventoryValue: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class ReportingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7000/gateway/api/reports';

  getDashboard() {
    return this.http.get<DashboardData>(`${this.apiUrl}/dashboard`);
  }

  getApprovalRate() {
    return this.http.get<number>(`${this.apiUrl}/approval-rate`);
  }

  getRecentReports() {
    return this.http.get<PaginatedResult<any>>(`${this.apiUrl}/recent`, {
      params: { page: '1', pageSize: '10' }
    });
  }

  getRecentReportsPaged(page: number, pageSize: number) {
    return this.http.get<PaginatedResult<any>>(`${this.apiUrl}/recent`, {
      params: { page: page.toString(), pageSize: pageSize.toString() }
    });
  }
}
