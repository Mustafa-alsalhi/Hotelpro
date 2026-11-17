import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { UsersRolesService } from '../../../core/services/users-roles.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoleDto } from '../../../core/models/role.model';
import { UserDto } from '../../../core/models/user.model';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { UserFormDialogComponent } from './user-form-dialog.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressBarModule,
    HasPermissionDirective,
    EmptyStateComponent
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('users.title') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.t('users.subtitle') }}</p>
      </div>
      <button
        *appHasPermission="'Users.Create'"
        mat-flat-button
        class="hp-btn-primary"
        (click)="openForm()">
        <mat-icon>person_add</mat-icon>
        <span>{{ i18n.t('users.create') }}</span>
      </button>
    </div>

    <div class="hp-card" style="padding:16px;">
      <div style="display:flex;flex-wrap:wrap;gap:12px;margin-bottom:12px;">
        <mat-form-field appearance="outline" style="flex:1;min-width:220px;">
          <mat-label>{{ i18n.t('common.search') }}</mat-label>
          <input matInput [(ngModel)]="search" (keyup.enter)="loadUsers(1)" />
          <mat-icon matSuffix style="cursor:pointer;" (click)="loadUsers(1)">search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" style="min-width:180px;">
          <mat-label>{{ i18n.t('users.roles') }}</mat-label>
          <mat-select [(ngModel)]="selectedRole" (selectionChange)="loadUsers(1)">
            <mat-option value="">All</mat-option>
            @for (r of roles(); track r.id) {
              <mat-option [value]="r.name">{{ r.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @if (loading()) {
        <mat-progress-bar mode="indeterminate"></mat-progress-bar>
      }

      @if (!loading() && users().length === 0) {
        <app-empty-state icon="group_off"></app-empty-state>
      } @else {
        <div style="overflow-x:auto;">
          <table mat-table [dataSource]="users()" style="width:100%;">
            <ng-container matColumnDef="fullName">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('users.fullName') }}</th>
              <td mat-cell *matCellDef="let u">
                <div style="font-weight:600;color:#0f2540;">{{ u.fullName }}</div>
                <div style="font-size:0.8rem;color:#64748b;">{{ u.email }}</div>
              </td>
            </ng-container>

            <ng-container matColumnDef="roles">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('users.roles') }}</th>
              <td mat-cell *matCellDef="let u">
                @for (r of u.roles; track r) {
                  <span class="hp-badge hp-badge-gold" style="margin-inline-end:4px;">{{ r }}</span>
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.status') }}</th>
              <td mat-cell *matCellDef="let u">
                <span class="hp-badge" [class.hp-badge-success]="u.isActive" [class.hp-badge-danger]="!u.isActive">
                  {{ u.isActive ? i18n.t('common.active') : i18n.t('common.inactive') }}
                </span>
              </td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
              <td mat-cell *matCellDef="let u">
                <button *appHasPermission="'Users.Edit'" mat-icon-button (click)="openForm(u)">
                  <mat-icon>edit</mat-icon>
                </button>
                <button *appHasPermission="'Users.ManageUsers'" mat-icon-button (click)="toggleActive(u)">
                  <mat-icon [style.color]="u.isActive ? '#b91c1c' : '#15803d'">
                    {{ u.isActive ? 'block' : 'check_circle' }}
                  </mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        </div>

        <mat-paginator
          [length]="totalCount()"
          [pageSize]="pageSize"
          [pageIndex]="pageNumber - 1"
          [pageSizeOptions]="[10, 15, 25]"
          (page)="onPage($event)">
        </mat-paginator>
      }
    </div>
  `
})
export class UsersListComponent implements OnInit {
  private readonly service = inject(UsersRolesService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly displayedColumns = ['fullName', 'roles', 'status', 'actions'];
  readonly users = signal<UserDto[]>([]);
  readonly roles = signal<RoleDto[]>([]);
  readonly loading = signal(false);
  readonly totalCount = signal(0);

  pageNumber = 1;
  pageSize = 15;
  search = '';
  selectedRole = '';

  ngOnInit(): void {
    this.service.getRoles().subscribe((r) => this.roles.set(r));
    this.loadUsers();
  }

  loadUsers(page = this.pageNumber): void {
    this.pageNumber = page;
    this.loading.set(true);
    this.service.getUsers(this.pageNumber, this.pageSize, this.search, null, this.selectedRole).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load users');
      }
    });
  }

  onPage(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.loadUsers(event.pageIndex + 1);
  }

  openForm(user?: UserDto): void {
    const ref = this.dialog.open(UserFormDialogComponent, {
      data: { user, roles: this.roles() },
      width: '440px'
    });
    ref.afterClosed().subscribe((saved) => {
      if (saved) this.loadUsers();
    });
  }

  toggleActive(user: UserDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: user.fullName,
        message: this.i18n.t('users.toggleConfirm'),
        danger: user.isActive
      }
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.service.toggleUserActive(user.id).subscribe({
        next: () => {
          this.toast.success('Updated');
          this.loadUsers();
        },
        error: () => this.toast.error('Failed to update user')
      });
    });
  }
}

