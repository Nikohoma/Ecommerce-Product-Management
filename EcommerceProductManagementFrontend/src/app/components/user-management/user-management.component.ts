import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  private authService = inject(AuthService);

  activeTab: 'list' | 'create' = 'list';
  users: any[] = [];
  loading = false;
  successMsg = '';
  errorMsg = '';

  newUser = {
    name: '',
    email: '',
    password: '',
    role: 'Customer'
  };

  // currentUserEmail = '';
  currentUserEmail = this.authService.getEmail();
  currentUserName = this.authService.getName();
  currentData = this.authService.getUserFromStorage();

  ngOnInit() {
    this.loadUsers();
  }

  isCurrentUser(email: string): boolean {
    if (!email || !this.currentUserEmail) return false;
    return this.currentUserEmail.toLowerCase() === email.toLowerCase();
  }

  setTab(tab: 'list' | 'create') {
    this.activeTab = tab;
    if (tab === 'list') {
      this.loadUsers();
    } else {
      this.successMsg = '';
      this.errorMsg = '';
    }
  }

  loadUsers() {
    this.loading = true;
    this.authService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading users', err);
        this.loading = false;
      }
    });
  }

  onCreateUser(event: Event) {
    event.preventDefault();
    this.loading = true;
    this.successMsg = '';
    this.errorMsg = '';

    this.authService.associateSignup(this.newUser).subscribe({
      next: (res) => {
        this.successMsg = 'User created successfully!';
        this.newUser = { name: '', email: '', password: '', role: 'Customer' };
        this.loading = false;
        setTimeout(() => {
          this.setTab('list');
        }, 1500);
      },
      error: (err) => {
        this.errorMsg = err.error || 'Failed to create user. Make sure the email is unique and roles are valid.';
        this.loading = false;
      }
    });
  }

  showEditModal = false;
  selectedUserForEdit: any = null;
  editForm = {
    role: '',
    isActive: true
  };

  openEditModal(user: any) {
    this.selectedUserForEdit = user;
    this.editForm = {
      role: user.role,
      isActive: user.isActive
    };
    this.showEditModal = true;
  }

  closeEditModal() {
    this.showEditModal = false;
    this.selectedUserForEdit = null;
  }

  saveUserChanges() {
    if (!this.selectedUserForEdit) return;

    this.loading = true;
    this.authService.updateUser({
      Email: this.selectedUserForEdit.email,
      Role: this.editForm.role,
      IsActive: this.editForm.isActive
    }).subscribe({
      next: () => {
        alert('User updated successfully!');
        this.loadUsers();
        this.closeEditModal();
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to update user', err);
        const msg = err.error?.message || err.error || 'Gateway error (check PUT permission)';
        alert(`Failed to update user: ${msg}`);
        this.loading = false;
      }
    });
  }
}
