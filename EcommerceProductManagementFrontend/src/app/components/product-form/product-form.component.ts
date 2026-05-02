import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { ProductService, Category } from '../../services/product.service';

interface VariantFormModel {
  sku: string;
  price: number;
  stock: number;
  attributes: string;
  imageUrl: string;
}

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './product-form.component.html',
  styles: [`
    .form-card {
      max-width: 800px;
      margin: 0 auto;
      padding: 32px;
    }
    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
      margin-bottom: 24px;
    }
    .form-group {
      margin-bottom: 20px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    label {
      font-weight: 600;
      font-size: 0.875rem;
      color: #334155;
    }
    input, select, textarea {
      padding: 10px 14px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s;
    }
    input:focus, select:focus, textarea:focus {
      outline: none;
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
    }
    input.invalid {
      border-color: #ef4444;
    }
    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      margin-top: 32px;
      padding-top: 24px;
      border-top: 1px solid #f1f5f9;
    }
    .error-message {
      margin-top: 16px;
      color: #ef4444;
      font-size: 0.875rem;
      text-align: center;
    }
    .split-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
    }
    .inline-actions {
      display: flex;
      justify-content: flex-end;
      margin-top: 8px;
    }
    .images-list, .variants-list {
      display: grid;
      gap: 12px;
      margin-top: 12px;
    }
    .image-chip, .variant-card {
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      padding: 10px;
      background: #f8fafc;
    }
    .image-chip {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
    }
    .image-preview, .variant-preview {
      width: 80px;
      height: 80px;
      border-radius: 8px;
      object-fit: cover;
      background: #e2e8f0;
      border: 1px solid #cbd5e1;
      flex-shrink: 0;
    }
    .image-url {
      font-size: 0.8rem;
      color: #475569;
      word-break: break-all;
      flex: 1;
    }
    .btn-danger-outline {
      border: 1px solid #ef4444;
      color: #dc2626;
      background: #fff;
    }
    .btn-danger-outline:hover {
      background: #fef2f2;
    }
    .sub-header {
      margin-top: 20px;
      margin-bottom: 6px;
      font-size: 0.95rem;
      color: #334155;
      font-weight: 700;
    }
    .category-row {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 10px;
      align-items: center;
    }
    .inline-category {
      display: grid;
      grid-template-columns: 1fr auto auto;
      gap: 8px;
      margin-top: 10px;
    }
    .inline-note {
      color: #64748b;
      font-size: 0.8rem;
    }
      .submit-wrapper {
  position: relative;
  display: inline-block;
}

.floating-chip {
  position: absolute;
  bottom: 110%;
  left: 50%;
  transform: translateX(-50%);
  
  background: #1f2937;
  color: #fff;
  padding: 6px 12px;
  border-radius: 16px;
  font-size: 12px;
  white-space: nowrap;

  opacity: 0;
  pointer-events: none;
  transition: 0.2s ease;
}

/* show on hover */
.submit-wrapper:hover .floating-chip {
  opacity: 1;
}
  `]
})
export class ProductFormComponent implements OnInit {
  private productService = inject(ProductService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  product = {
    name: '',
    description: '',
    price: 0,
    stock: 0,
    categoryId: 1,
    tags: [] as string[],
    mediaUrls: [] as string[],
    variants: [] as VariantFormModel[]
  };

  tagsString = '';
  mediaUrlInput = '';
  loading = false;
  error = '';
  editMode = false;
  productId?: number;
  categories: Category[] = [];
  newCategoryName = '';
  addingCategory = false;
  showAddCategory = false;

  onTagsChange(value: string) {
    this.tagsString = value;
    this.product.tags = value.split(',').map(t => t.trim()).filter(t => t !== '');
  }

  ngOnInit() {
    this.loadCategories();
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editMode = true;
      this.productId = +idParam;
      this.productService.getProduct(this.productId).subscribe(p => {
        // Populate with existing product data
        this.product = {
          name: p.name,
          description: p.description,
          price: p.price,
          stock: p.availableQuantity ?? 0,
          categoryId: p.categoryId,
          tags: p.tags ?? [],
          mediaUrls: p.media?.map(m => m.mediaUrl) ?? [],
          variants: (p.variants ?? []).map(v => this.mapVariantFromApi(v))
        };
        this.tagsString = this.product.tags.join(', ');
      }, err => {
        this.error = 'Failed to load product for editing.';
      });
    }
  }

  loadCategories() {
    this.productService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories || [];
        if (!this.categories.some(c => c.id === this.product.categoryId) && this.categories.length) {
          this.product.categoryId = this.categories[0].id;
        }
      }
    });
  }


  addCategory() {
    const categoryName = this.newCategoryName.trim();
    if (!categoryName || this.addingCategory) return;

    this.addingCategory = true;

    this.productService.createCategory(categoryName).subscribe({
      next: (created) => {
        this.newCategoryName = '';
        this.product.categoryId = created.id;
        this.loadCategories();

        this.showAddCategory = false;
        this.addingCategory = false;
      },
      error: (err) => {
        this.error = err?.error || err?.error?.message || 'Failed to create category.';
        this.addingCategory = false;
      }
    });
  }

  addMediaUrl() {
    const trimmed = this.mediaUrlInput.trim();
    if (!trimmed) return;
    if (!this.product.mediaUrls.includes(trimmed)) {
      this.product.mediaUrls.push(trimmed);
    }
    this.mediaUrlInput = '';
  }

  removeMediaUrl(url: string) {
    this.product.mediaUrls = this.product.mediaUrls.filter(u => u !== url);
  }

  addVariant() {
    this.product.variants.push({
      sku: '',
      price: 0,
      stock: 0,
      attributes: '',
      imageUrl: ''
    });
  }

  removeVariant(index: number) {
    this.product.variants.splice(index, 1);
  }

  onSubmit() {
    this.loading = true;
    this.error = '';
    if (this.mediaUrlInput.trim()) {
      this.addMediaUrl();
    }
    if (!this.product.mediaUrls.length) {
      this.product.mediaUrls = ['https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&q=80'];
    }

    const payload = {
      ...this.product,
      variants: this.product.variants
        .filter(v => v.sku.trim() !== '')
        .map(v => ({
          productId: this.productId ?? 0,
          sku: v.sku.trim(),
          price: Number(v.price) || 0,
          stock: Number(v.stock) || 0,
          attributes: v.attributes?.trim() || '',
          imageUrl: v.imageUrl?.trim() || ''
        }))
    };

    if (this.editMode && this.productId != null) {
      this.productService.updateProduct(this.productId, payload).subscribe({
        next: () => this.router.navigate(['/catalog']),
        error: err => {
          console.error('Update product error:', err);
          this.error = 'Failed to update product. ' + (err.error?.message || 'Please check your inputs.');
          this.loading = false;
        }
      });
    } else {
      this.productService.createProduct(payload).subscribe({
        next: () => this.router.navigate(['/catalog']),
        error: err => {
          console.error('Create product error:', err);
          this.error = 'Failed to create product. ' + (err.error?.message || 'Please check your inputs.');
          this.loading = false;
        }
      });
    }
  }

  private mapVariantFromApi(variant: any): VariantFormModel {
    let attributes = '';
    let imageUrl = '';

    if (typeof variant?.attributes === 'string' && variant.attributes.trim()) {
      try {
        const parsed = JSON.parse(variant.attributes);
        if (parsed && typeof parsed === 'object') {
          attributes = parsed.details || '';
          imageUrl = parsed.imageUrl || '';
        } else {
          attributes = variant.attributes;
        }
      } catch {
        attributes = variant.attributes;
      }
    }

    return {
      sku: variant?.sku || '',
      price: Number(variant?.price) || 0,
      stock: Number(variant?.stock) || 0,
      attributes,
      imageUrl
    };
  }
}
