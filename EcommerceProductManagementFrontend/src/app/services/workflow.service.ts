import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class WorkflowService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7000/gateway/api/workflow';

  setStatus(productId: number, status: string) {
    return this.http.post(this.apiUrl, null, {
      params: { productId, status }
    });
  }

  submit(productId: number) {
    return this.setStatus(productId, 'submit');
  }

  approve(productId: number) {
    return this.setStatus(productId, 'approve');
  }

  reject(productId: number) {
    return this.setStatus(productId, 'reject');
  }
}
