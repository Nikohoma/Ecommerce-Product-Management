import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService, Product, ProductVariant } from '../../services/product.service';
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
    
    .header-actions {
      display: flex;
      gap: 12px;
      align-items: center;
    }
    .status-filter {
      padding: 10px 16px;
      border-radius: 99px;
      border: 1px solid #e2e8f0;
      background: white;
      color: #475569;
      font-size: 0.9rem;
      font-weight: 600;
      outline: none;
      cursor: pointer;
      transition: all 0.2s;
    }
    .status-filter:hover {
      border-color: #cbd5e1;
      background: #f8fafc;
    }
    .status-filter:focus {
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
    }

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
    .details-modal {
      max-width: 640px;
      max-height: 85vh;
      overflow-y: auto;
    }
    .details-description {
      color: #334155;
      margin-bottom: 16px;
      line-height: 1.5;
    }
    .variants-section h4 {
      margin: 0 0 10px;
      color: #1e293b;
      font-size: 0.95rem;
    }
    .variants-list {
      display: grid;
      gap: 10px;
      margin-bottom: 16px;
    }
    .variant-item {
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      padding: 10px 12px;
      background: #f8fafc;
    }
    .variant-row {
      display: flex;
      justify-content: space-between;
      gap: 8px;
      color: #334155;
      font-size: 0.875rem;
    }
    .variant-attr {
      margin-top: 6px;
      color: #64748b;
      font-size: 0.8rem;
      word-break: break-word;
    }
    .variant-preview {
      margin-top: 8px;
      width: 100%;
      max-height: 180px;
      object-fit: contain;
      border-radius: 8px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
    }
    .empty-variants {
      color: #64748b;
      font-size: 0.875rem;
      margin-bottom: 16px;
    }

    /* Pagination Styles */
    .pagination-container {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 16px;
      margin-top: 40px;
      padding: 20px 0;
    }
    .page-info {
      font-size: 0.9rem;
      color: #64748b;
    }
    .pagination-btn {
      padding: 8px 16px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      background: white;
      color: #475569;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }
    .pagination-btn:hover:not(:disabled) {
      border-color: #6366f1;
      color: #6366f1;
      background: #f8fafc;
    }
    .pagination-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .pagination-btn.active {
      background: #6366f1;
      color: white;
      border-color: #6366f1;
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
  showDetailsModal = false;
  detailsLoading = false;
  selectedProductForDetails: Product | null = null;
  selectedProductVariants: ProductVariant[] = [];

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalProducts = 0;
  totalPages = 0;
  selectedStatus = '';

  // Logistics (Price/Stock)
  showPriceModal = false;
  showStockModal = false;
  newPrice = 0;
  newStock = 0;
  selectedProductForLogistics: Product | null = null;

  ngOnInit() {
    this.role = this.authService.getRole();
    this.loadProducts();
  }

  private ensureArray<T>(data: any): T[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.$values && Array.isArray(data.$values)) return data.$values;
    return [data];
  }

  private applyVisibilityRules(products: any) {
    const list = this.ensureArray<Product>(products).map(p => ({
      ...p,
      media: this.ensureArray<{ mediaUrl: string; mediaType: string }>(p.media),
      tags: this.ensureArray<string>(p.tags)
    })) as Product[];

    if (this.role === 'Customer') {
      this.visibleProducts = list.filter(p => this.getStatusName(p.status).toLowerCase() === 'active');
    } else {
      this.visibleProducts = list;
    }
  }

  loadProducts() {
    this.productService.getProducts(this.currentPage, this.pageSize, this.selectedStatus).subscribe(res => {
      // res is now PaginatedResult
      const items = res.items || [];
      this.totalProducts = res.totalCount || 0;
      this.totalPages = Math.ceil(this.totalProducts / this.pageSize);
      
      this.products = this.ensureArray<Product>(items);
      this.applyVisibilityRules(this.products);
    });
  }

  onFilter() {
    this.currentPage = 1;
    this.loadProducts();
  }

  changePage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadProducts();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onSearch() {
    if (this.searchQuery) {
      this.productService.searchProducts(this.searchQuery).subscribe(data => {
        this.products = this.ensureArray<Product>(data);
        this.applyVisibilityRules(this.products);
      });
    } else {
      this.loadProducts();
    }
  }

  // for admin ui
  onWorkflow(productId: number, action: string) {
    let obs;
    if (action === 'submit') obs = this.workflowService.submit(productId);
    else if (action === 'approve') obs = this.workflowService.approve(productId);
    else obs = this.workflowService.reject(productId);
    obs.subscribe(() => this.loadProducts());
  }

  // Logistics Methods
  openPriceModal(product: Product) {
    this.selectedProductForLogistics = product;
    this.newPrice = product.price;
    this.showPriceModal = true;
  }

  openStockModal(product: Product) {
    this.selectedProductForLogistics = product;
    this.newStock = product.availableQuantity;
    this.showStockModal = true;
  }

  applyPriceUpdate() {
    if (!this.selectedProductForLogistics) return;
    this.productService.updatePrice(this.selectedProductForLogistics.id, this.newPrice).subscribe({
      next: () => {
        alert('Price updated successfully!');
        this.loadProducts();
        this.showPriceModal = false;
      },
      error: (err) => alert('Failed to update price. ' + this.getBackendErrorMessage(err))
    });
  }

  applyStockUpdate() {
    if (!this.selectedProductForLogistics) return;
    this.productService.updateStock(this.selectedProductForLogistics.id, this.newStock).subscribe({
      next: () => {
        alert('Stock updated successfully!');
        this.loadProducts();
        this.showStockModal = false;
      },
      error: (err) => alert('Failed to update stock. ' + this.getBackendErrorMessage(err))
    });
  }

  private getBackendErrorMessage(err: any): string {
    if (!err) return 'Unknown error';
    if (typeof err.error === 'string') return err.error;
    if (err.error && err.error.detail) return err.error.detail;
    if (err.error && err.error.message) return err.error.message;
    if (err.message) return err.message;
    return JSON.stringify(err);
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

  openDetailsModal(product: Product) {
    this.selectedProductForDetails = product;
    this.selectedProductVariants = [];
    this.detailsLoading = true;
    this.showDetailsModal = true;

    this.productService.getVariantsByProduct(product.id).subscribe({
      next: (variants) => {
        this.selectedProductVariants = variants || [];
        this.detailsLoading = false;
      },
      error: () => {
        this.selectedProductVariants = [];
        this.detailsLoading = false;
      }
    });
  }

  closeDetailsModal() {
    this.showDetailsModal = false;
    this.selectedProductForDetails = null;
    this.selectedProductVariants = [];
    this.detailsLoading = false;
  }

  getVariantAttributesText(attributes: string): string {
    if (!attributes) return '';
    try {
      const parsed = JSON.parse(attributes);
      if (parsed && typeof parsed === 'object') {
        return parsed.details || '';
      }
    } catch {
      return attributes;
    }
    return '';
  }

  getVariantImageUrl(attributes: string): string {
    if (!attributes) return '';
    try {
      const parsed = JSON.parse(attributes);
      if (parsed && typeof parsed === 'object') {
        return parsed.imageUrl || '';
      }
    } catch {
      return '';
    }
    return '';
  }

  // Navigate to edit product page
  editProduct(productId: number) {
    this.router.navigate(['/product/edit', productId]);
  }

  showUploadModal = false;
  mediaUrl = '';
  mediaType = 'image';
  selectedProductForMedia: Product | null = null;

  onUploadMedia(product: Product) {
    this.selectedProductForMedia = product;
    this.mediaUrl = '';
    this.showUploadModal = true;
  }

  closeUploadModal() {
    this.showUploadModal = false;
    this.selectedProductForMedia = null;
  }

  saveMedia() {
    if (!this.selectedProductForMedia || !this.mediaUrl) return;

    // The backend expects a ProductCreateDto which uses MediaUrls (strings) and Stock
    const payload: any = {
      name: this.selectedProductForMedia.name,
      description: this.selectedProductForMedia.description,
      price: this.selectedProductForMedia.price,
      stock: this.selectedProductForMedia.availableQuantity,
      categoryId: this.selectedProductForMedia.categoryId,
      tags: this.selectedProductForMedia.tags || [],
      mediaUrls: (this.selectedProductForMedia.media || []).map((m: any) => m.mediaUrl),
      variants: (this.selectedProductForMedia.variants || []).map((v: any) => ({
        productId: v.productId,
        sku: v.sku,
        price: v.price,
        stock: v.stock,
        attributes: v.attributes
      }))
    };

    // Add the new media URL
    if (!payload.mediaUrls.includes(this.mediaUrl)) {
      payload.mediaUrls.push(this.mediaUrl);
    }

    this.productService.updateProduct(this.selectedProductForMedia.id, payload).subscribe({
      next: () => {
        alert('Media uploaded successfully! The product has been sent to Draft status for review.');
        this.loadProducts();
        this.closeUploadModal();
      },
      error: (err) => {
        console.error('Error uploading media', err);
        alert('Failed to upload media. Please check if the product is in a status that allows updates.');
      }
    });
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
