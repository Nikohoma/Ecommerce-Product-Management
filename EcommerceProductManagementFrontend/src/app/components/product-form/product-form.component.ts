import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { ProductService } from '../../services/product.service';

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
    mediaUrls: [] as string[]
  };

  tagsString = '';
  singleMediaUrl = '';
  loading = false;
  error = '';
  editMode = false;
  productId?: number;

  onTagsChange(value: string) {
    this.tagsString = value;
    this.product.tags = value.split(',').map(t => t.trim()).filter(t => t !== '');
  }

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editMode = true;
      this.productId = +idParam;
      this.productService.getProduct(this.productId).subscribe(p => {
        // Populate form fields with existing product data
        this.product = {
          name: p.name,
          description: p.description,
          price: p.price,
          stock: p.availableQuantity ?? 0,
          categoryId: p.categoryId,
          tags: p.tags ?? [],
          mediaUrls: p.media?.map(m => m.mediaUrl) ?? []
        };
        this.tagsString = this.product.tags.join(', ');
        this.singleMediaUrl = this.product.mediaUrls[0] || '';
      }, err => {
        this.error = 'Failed to load product for editing.';
      });
    }
  }

  onSubmit() {
    this.loading = true;
    this.error = '';

    // Ensure mediaUrls contains the entered image URL
    if (this.singleMediaUrl) {
      this.product.mediaUrls = [this.singleMediaUrl];
    } else {
      this.product.mediaUrls = ['https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&q=80'];
    }

    if (this.editMode && this.productId != null) {
      this.productService.updateProduct(this.productId, this.product).subscribe({
        next: () => this.router.navigate(['/catalog']),
        error: err => {
          console.error('Update product error:', err);
          this.error = 'Failed to update product. ' + (err.error?.message || 'Please check your inputs.');
          this.loading = false;
        }
      });
    } else {
      this.productService.createProduct(this.product).subscribe({
        next: () => this.router.navigate(['/catalog']),
        error: err => {
          console.error('Create product error:', err);
          this.error = 'Failed to create product. ' + (err.error?.message || 'Please check your inputs.');
          this.loading = false;
        }
      });
    }
  }
}
