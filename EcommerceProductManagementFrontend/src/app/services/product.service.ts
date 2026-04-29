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
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7000/gateway/api/products';

  getProducts() {
    return this.http.get<Product[]>(this.apiUrl);
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
}
