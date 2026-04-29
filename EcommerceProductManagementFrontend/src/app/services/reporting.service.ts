import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface DashboardData {
  approved: number;
  rejected: number;
  pending: number;
  totalInventoryValue: number;
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
    return this.http.get<any[]>(`${this.apiUrl}/recent`);
  }
}
