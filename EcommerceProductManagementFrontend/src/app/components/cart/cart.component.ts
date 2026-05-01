import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService, Cart, CartItem } from '../../services/cart.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent implements OnInit {
  private cartService = inject(CartService);
  
  cart: Cart | null = null;
  items: CartItem[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit() {
    this.loadCart();
  }

  loadCart() {
    this.loading = true;
    this.error = null;
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        // Handle potential array wrapping if backend configuration varies
        if (cart.items && (cart.items as any).$values) {
          this.items = (cart.items as any).$values;
        } else {
          this.items = cart.items as CartItem[];
        }
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading cart', err);
        this.error = 'Failed to load your cart. Please try again later.';
        this.loading = false;
      }
    });
  }

  updateQuantity(itemId: number, newQuantity: number) {
    if (newQuantity <= 0) {
      this.removeItem(itemId);
      return;
    }

    this.cartService.updateQuantity(itemId, newQuantity).subscribe({
      next: () => this.loadCart(),
      error: (err) => console.error('Error updating quantity', err)
    });
  }





  removeItem(itemId: number) {
    if (confirm('Are you sure you want to remove this item?')) {
      this.cartService.removeItem(itemId).subscribe({
        next: () => this.loadCart(),
        error: (err) => console.error('Error removing item', err)
      });
    }
  }

  clearCart() {
    if (confirm('Are you sure you want to clear your entire cart?')) {
      this.cartService.clearCart().subscribe({
        next: () => this.loadCart(),
        error: (err) => console.error('Error clearing cart', err)
      });
    }
  }

  checkout() {
    this.cartService.checkout().subscribe({
      next: (res) => {
        alert('Order placed successfully! Order ID: ' + res.cartId);
        this.loadCart();
      },
      error: (err) => {
        console.error('Checkout failed', err);
        alert('Checkout failed: ' + (err.error?.message || 'Unexpected error'));
      }
    });
  }
}
