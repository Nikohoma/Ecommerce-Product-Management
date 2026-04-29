import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService, Product } from '../../services/product.service';
import { AuthService } from '../../services/auth.service';
import { WorkflowService } from '../../services/workflow.service';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './catalog.component.html',
  styles: [`
    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      margin-bottom: 32px;
    }
    .search-bar input {
      padding: 10px 20px;
      border-radius: 99px;
      border: 1px solid #e2e8f0;
      width: 300px;
      outline: none;
    }
    .search-bar input:focus { border-color: #6366f1; }

    .product-card {
      padding: 0;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }
    .product-image {
      position: relative;
      height: 200px;
      background: #f1f5f9;
    }
    .product-image img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    .status-chip {
      position: absolute;
      top: 12px;
      right: 12px;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 0.7rem;
      font-weight: 800;
      text-transform: uppercase;
      background: white;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    }
    .status-approved, .status-active { color: #166534; background: #dcfce7; }
    .status-draft { color: #475569; background: #f1f5f9; }
    .status-submitted { color: #854d0e; background: #fef9c3; }
    .status-rejected { color: #991b1b; background: #fee2e2; }

    .product-body {
      padding: 20px;
      display: flex;
      flex-direction: column;
      flex: 1;
    }
    .product-main {
      margin-bottom: 16px;
      flex: 1;
    }
    .product-main h3 { font-size: 1.125rem; margin-bottom: 8px; }
    .description {
      font-size: 0.875rem;
      color: #64748b;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .product-meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }
    .price { font-size: 1.25rem; font-weight: 800; color: #1e293b; }
    .stock { font-size: 0.75rem; color: #64748b; font-weight: 600; }
    .stock.low { color: #ef4444; }

    .tags {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-bottom: 20px;
    }
    .tag {
      font-size: 0.7rem;
      color: #6366f1;
      background: #eef2ff;
      padding: 2px 8px;
      border-radius: 4px;
      font-weight: 600;
    }

    .product-actions {
      border-top: 1px solid #f1f5f9;
      padding-top: 16px;
    }
    .action-group {
      display: flex;
      gap: 8px;
    }
    .btn-sm {
      padding: 6px 12px;
      font-size: 0.8125rem;
      flex: 1;
    }
    .btn-outline {
      background: white;
      border: 1px solid #e2e8f0;
      color: #475569;
    }
    .btn-outline:hover { background: #f8fafc; border-color: #cbd5e1; }
    .btn-success { background: #22c55e; color: white; }
    .btn-danger { background: #ef4444; color: white; }
    .w-full { width: 100%; }
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 999;
    }
    .modal-card {
      width: 100%;
      max-width: 380px;
      background: #fff;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      padding: 20px;
      box-shadow: 0 12px 24px rgba(0, 0, 0, 0.12);
    }
    .modal-card h3 {
      margin-bottom: 12px;
      font-size: 1rem;
      color: #1e293b;
    }
    .modal-card select {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      margin-bottom: 16px;
    }
    .modal-actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    }
  `]
})
export class CatalogComponent implements OnInit {
  private router = inject(Router);

  productService = inject(ProductService);
  authService = inject(AuthService);
  workflowService = inject(WorkflowService);

  products: Product[] = [];
  visibleProducts: Product[] = [];
  role = '';
  searchQuery = '';
  showStatusModal = false;
  selectedProductId: number | null = null;
  selectedStatusAction = 'submit';

  ngOnInit() {
    this.role = this.authService.getRole();
    this.loadProducts();
  }

  private applyVisibilityRules(products: Product[]) {
    if (this.role === 'Customer') {
      this.visibleProducts = products.filter(p => this.getStatusName((p as any).status).toLowerCase() === 'active');
    } else {
      this.visibleProducts = products;
    }
  }

  loadProducts() {
    this.productService.getProducts().subscribe(data => {
      this.products = data;
      this.applyVisibilityRules(data);
    });
  }

  onSearch() {
    if (this.searchQuery) {
      this.productService.searchProducts(this.searchQuery).subscribe(data => {
        this.products = data;
        this.applyVisibilityRules(data);
      });
    } else {
      this.loadProducts();
    }
  }

  // Workflow actions for admin
  onWorkflow(productId: number, action: string) {
    let obs;
    if (action === 'submit') obs = this.workflowService.submit(productId);
    else if (action === 'approve') obs = this.workflowService.approve(productId);
    else obs = this.workflowService.reject(productId);
    obs.subscribe(() => this.loadProducts());
  }

  openStatusModal(productId: number) {
    this.selectedProductId = productId;
    this.selectedStatusAction = 'submit';
    this.showStatusModal = true;
  }

  closeStatusModal() {
    this.showStatusModal = false;
    this.selectedProductId = null;
  }

  applyStatusChange() {
    if (this.selectedProductId == null) return;
    this.onWorkflow(this.selectedProductId, this.selectedStatusAction);
    this.closeStatusModal();
  }

  // Navigate to edit product page
  editProduct(productId: number) {
    this.router.navigate(['/product/edit', productId]);
  }

  getStatusName(status: any): string {
    if (typeof status === 'number') {
      const statuses = ['Draft', 'Submitted', 'Approved', 'Rejected', 'Active', 'Inactive'];
      return statuses[status] || 'Unknown';
    }
    return status || 'Unknown';
  }

  isActionDisabled(status: any, action: string): boolean {
    const name = this.getStatusName(status).toLowerCase();
    if (action === 'submit') return name === 'submitted' || name === 'approved' || name === 'active';
    if (action === 'approve') return name === 'approved' || name === 'active';
    if (action === 'reject') return name === 'rejected';
    return false;
  }
}
