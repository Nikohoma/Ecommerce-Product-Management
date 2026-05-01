import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  productDescription?: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  imageUrl?: string;
}

export interface Cart {
  id: number;
  userId: string;
  items: CartItem[] | { "$values": CartItem[] };
  totalPrice: number;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7000/api/cart'; // Based on Ocelot config

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl);
  }

  addItem(productId: number, quantity: number): Observable<CartItem> {
    return this.http.post<CartItem>(`${this.apiUrl}/items`, { productId, quantity });
  }

  updateQuantity(itemId: number, quantity: number): Observable<CartItem> {
    return this.http.put<CartItem>(`${this.apiUrl}/items/${itemId}`, { quantity });
  }

  removeItem(itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/items/${itemId}`);
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/items`);
  }

  checkout(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/checkout`, {});
  }
}
