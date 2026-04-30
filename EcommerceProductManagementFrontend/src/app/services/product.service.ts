import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  stock: number;
  availableQuantity: number;
  categoryId: number;
  status: any;
  media?: { mediaUrl: string; mediaType: string }[];
  tags?: string[];
  variants?: ProductVariant[];
}

export interface Category {
  id: number;
  name: string;
}

export interface ProductVariant {
  id: number;
  productId: number;
  sku: string;
  price: number;
  stock: number;
  attributes: string;
}

export interface ProductVariantCreateRequest {
  productId: number;
  sku: string;
  price: number;
  stock: number;
  attributes?: string;
  imageUrl?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7000/gateway/api/products';
  private variantsApiUrl = 'http://localhost:7000/gateway/api/variants';

  getProducts() {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getCategories() {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`);
  }

  createCategory(name: string) {
    return this.http.post<Category>(`${this.apiUrl}/categories`, { name });
  }

  getProduct(id: number) {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  searchProducts(query: string) {
    return this.http.get<Product[]>(`${this.apiUrl}/search`, { params: { query: query } });
  }

  createProduct(product: any) {
    return this.http.post(this.apiUrl, product);
  }

  updateProduct(id: number, product: any) {
    return this.http.put(`${this.apiUrl}/${id}`, product);
  }

  deleteProduct(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getVariantsByProduct(productId: number) {
    return this.http.get<ProductVariant[]>(`${this.variantsApiUrl}/product/${productId}`);
  }

  createVariant(payload: ProductVariantCreateRequest) {
    return this.http.post(`${this.variantsApiUrl}`, payload);
  }
}
