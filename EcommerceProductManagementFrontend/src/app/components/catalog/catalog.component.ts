import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService, Product, ProductVariant } from '../../services/product.service';
import { AuthService } from '../../services/auth.service';
import { WorkflowService } from '../../services/workflow.service';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { CartService, CartItem } from '../../services/cart.service';


@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.css']

})
export class CatalogComponent implements OnInit {
  private router = inject(Router);

  productService = inject(ProductService);
  authService = inject(AuthService);
  workflowService = inject(WorkflowService);
  cartService = inject(CartService);


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
  selectedImageIndex = 0;

  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalProducts = 0;
  totalPages = 0;
  selectedStatus = '';

  // Cart tracking
  cartQuantities: { [productId: number]: { quantity: number, itemId?: number } } = {};


  // Logistics (Price/Stock)
  showPriceModal = false;
  showStockModal = false;
  newPrice = 0;
  newStock = 0;
  selectedProductForLogistics: Product | null = null;

  ngOnInit() {
    this.role = this.authService.getRole();
    this.loadProducts();
    if (this.role === 'Customer') {
      this.loadCart();
    }
  }

  loadCart() {
    this.cartService.getCart().subscribe({
      next: (cart) => {
        const items = cart.items && (cart.items as any).$values ? (cart.items as any).$values : (cart.items as CartItem[]);
        this.cartQuantities = {};
        items.forEach((item: CartItem) => {
          this.cartQuantities[item.productId] = { quantity: item.quantity, itemId: item.id };
        });
      },
      error: (err) => console.error('Error loading cart', err)
    });
  }

  addToCart(product: Product) {
    this.cartService.addItem(product.id, 1).subscribe({
      next: (item) => {
        this.cartQuantities[product.id] = { quantity: item.quantity, itemId: item.id };
      },
      error: (err) => alert('Failed to add to cart: ' + err.error)
    });
  }

  updateCartQuantity(productId: number, delta: number) {
    const itemInfo = this.cartQuantities[productId];
    if (!itemInfo || !itemInfo.itemId) return;

    const newQty = itemInfo.quantity + delta;
    if (newQty <= 0) {
      this.cartService.removeItem(itemInfo.itemId).subscribe({
        next: () => {
          delete this.cartQuantities[productId];
        },
        error: (err) => console.error(err)
      });
    } else {
      this.cartService.updateQuantity(itemInfo.itemId, newQty).subscribe({
        next: (item) => {
          this.cartQuantities[productId].quantity = item.quantity;
        },
        error: (err) => console.error(err)
      });
    }
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
    else if (action === 'reject') obs = this.workflowService.reject(productId);
    else obs = this.workflowService.setStatus(productId, action);

    obs.subscribe({
      next: () => {
        let statusText = '';
        if (action === 'submit') statusText = 'Submitted';
        else if (action === 'approve') statusText = 'Approved/Active';
        else if (action === 'reject') statusText = 'Rejected';
        else statusText = action;

        alert(`Product status has been updated to ${statusText} successfully.`);
        this.loadProducts();
      },
      error: (err) => {
        console.error('Workflow error:', err);
        alert('Failed to update product status. ' + this.getBackendErrorMessage(err));
      }
    });
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

  selectedProductForStatus: Product | null = null;
  openStatusModal(product: Product) {
    this.selectedProductId = product.id;
    this.selectedProductForStatus = product;
    this.selectedStatusAction = 'submit';
    this.showStatusModal = true;
  }

  closeStatusModal() {
    this.showStatusModal = false;
    this.selectedProductId = null;
    this.selectedProductForStatus = null;
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
    this.selectedImageIndex = 0;
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
    this.selectedImageIndex = 0;
  }

  getSelectedProductImages(): { mediaUrl: string; mediaType: string }[] {
    const media = (this.selectedProductForDetails as any)?.media;
    return Array.isArray(media) ? media : (media?.$values && Array.isArray(media.$values) ? media.$values : []);
  }

  getActiveImageUrl(): string {
    const images = this.getSelectedProductImages();
    if (!images.length) return '';
    const idx = Math.min(Math.max(this.selectedImageIndex, 0), images.length - 1);
    return images[idx]?.mediaUrl || '';
  }

  prevImage() {
    const images = this.getSelectedProductImages();
    if (images.length <= 1) return;
    this.selectedImageIndex = (this.selectedImageIndex - 1 + images.length) % images.length;
  }

  nextImage() {
    const images = this.getSelectedProductImages();
    if (images.length <= 1) return;
    this.selectedImageIndex = (this.selectedImageIndex + 1) % images.length;
  }

  setImageIndex(index: number) {
    const images = this.getSelectedProductImages();
    if (index < 0 || index >= images.length) return;
    this.selectedImageIndex = index;
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

    // ProductCreateDto with MediaUrls (strings) and Stock
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
        alert('Media uploaded successfully! The product has been submitted for review.');
        this.loadProducts();
        this.closeUploadModal();
      },
      error: (err) => {
        console.error('Error uploading media', err);
        alert('Failed to upload media. Please check if the product is in a status that allows updates.');
      }
    });
  }

  removeMedia(url: string) {
    if (!this.selectedProductForMedia) return;
    if (!confirm('Are you sure you want to delete this media?')) return;

    const currentMedia = (this.selectedProductForMedia.media || []).map((m: any) => m.mediaUrl);
    const updatedMedia = currentMedia.filter(m => m !== url);

    const payload: any = {
      name: this.selectedProductForMedia.name,
      description: this.selectedProductForMedia.description,
      price: this.selectedProductForMedia.price,
      stock: this.selectedProductForMedia.availableQuantity,
      categoryId: this.selectedProductForMedia.categoryId,
      tags: this.selectedProductForMedia.tags || [],
      mediaUrls: updatedMedia,
      variants: (this.selectedProductForMedia.variants || []).map((v: any) => ({
        productId: v.productId,
        sku: v.sku,
        price: v.price,
        stock: v.stock,
        attributes: v.attributes
      }))
    };

    this.productService.updateProduct(this.selectedProductForMedia.id, payload).subscribe({
      next: () => {
        alert('Media removed successfully!');
        // Update local state to reflect change without full reload if possible, 
        // but loadProducts is safer.
        if (this.selectedProductForMedia) {
          this.selectedProductForMedia.media = this.selectedProductForMedia.media?.filter((m: any) => m.mediaUrl !== url);
        }
        this.loadProducts();
      },
      error: (err) => {
        console.error('Error removing media', err);
        alert('Failed to remove media.');
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
    if (action === 'reject') return false;
    return false;
  }
}
