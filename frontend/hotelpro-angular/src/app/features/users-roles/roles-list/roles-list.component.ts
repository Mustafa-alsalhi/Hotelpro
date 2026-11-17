import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { UsersRolesService } from '../../../core/services/users-roles.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ToastService } from '../../../core/services/toast.service';
import { PermissionDto, RoleDto } from '../../../core/models/role.model';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressBarModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('roles.title') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.t('roles.subtitle') }}</p>
      </div>
    </div>

    @if (loading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    <div style="display:grid;grid-template-columns:280px 1fr;gap:20px;align-items:start;">
      <div class="hp-card" style="padding:12px;">
        @for (role of roles(); track role.id) {
          <div
            (click)="selectRole(role)"
            style="padding:12px;border-radius:8px;cursor:pointer;margin-bottom:4px;"
            [style.background]="selectedRole()?.id === role.id ? 'rgba(201,162,39,0.16)' : 'transparent'"
            [style.font-weight]="selectedRole()?.id === role.id ? '700' : '500'">
            <div style="color:#0f2540;">{{ role.name }}</div>
            <div style="font-size:0.78rem;color:#64748b;">{{ role.permissions.length }} {{ i18n.t('roles.permissions') }}</div>
          </div>
        }
      </div>

      @if (selectedRole(); as role) {
        <div class="hp-card" style="padding:20px;">
          <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
            <div>
              <h2 style="margin:0;color:#0f2540;">{{ role.name }}</h2>
              <p style="margin:4px 0 0;color:#64748b;font-size:0.88rem;">{{ role.description }}</p>
            </div>
            <button
              *appHasPermission="'Roles.Edit'"
              mat-flat-button
              class="hp-btn-primary"
              [disabled]="saving()"
              (click)="savePermissions()">
              <mat-icon>save</mat-icon>
              <span>{{ i18n.t('common.save') }}</span>
            </button>
          </div>

          <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:14px;">
            @for (group of groupedPermissions(); track group.module) {
              <div class="hp-card" style="padding:12px;background:#f8fafc;">
                <div style="font-weight:700;color:#0f2540;margin-bottom:8px;">{{ group.module }}</div>
                @for (perm of group.items; track perm.id) {
                  <div>
                    <mat-checkbox
                      [checked]="selectedCodes().has(perm.code)"
                      (change)="toggleCode(perm.code, $event.checked)">
                      {{ perm.action }}
                    </mat-checkbox>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class RolesListComponent implements OnInit {
  private readonly service = inject(UsersRolesService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly roles = signal<RoleDto[]>([]);
  readonly allPermissions = signal<PermissionDto[]>([]);
  readonly selectedRole = signal<RoleDto | null>(null);
  readonly selectedCodes = signal<Set<string>>(new Set());
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly groupedPermissions = computed(() => {
    const map = new Map<string, PermissionDto[]>();
    for (const p of this.allPermissions()) {
      const arr = map.get(p.module) ?? [];
      arr.push(p);
      map.set(p.module, arr);
    }
    return Array.from(map.entries()).map(([module, items]) => ({ module, items }));
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getAllPermissions().subscribe((perms) => this.allPermissions.set(perms));
    this.service.getRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        if (roles.length > 0) this.selectRole(roles[0]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load roles');
      }
    });
  }

  selectRole(role: RoleDto): void {
    this.selectedRole.set(role);
    this.selectedCodes.set(new Set(role.permissions));
  }

  toggleCode(code: string, checked: boolean): void {
    const next = new Set(this.selectedCodes());
    if (checked) next.add(code);
    else next.delete(code);
    this.selectedCodes.set(next);
  }

  savePermissions(): void {
    const role = this.selectedRole();
    if (!role || this.saving()) return;
    this.saving.set(true);
    this.service.updateRolePermissions(role.id, role.description, Array.from(this.selectedCodes())).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.roles.update((list) => list.map((r) => (r.id === updated.id ? updated : r)));
        this.selectRole(updated);
        this.toast.success('Permissions updated');
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to update permissions');
      }
    });
  }
}

