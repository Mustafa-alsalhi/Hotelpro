import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { RoleDto } from '../../../core/models/role.model';
import { UserDto } from '../../../core/models/user.model';
import { UsersRolesService } from '../../../core/services/users-roles.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslationService } from '../../../core/i18n/translation.service';

export interface UserFormDialogData {
  user?: UserDto;
  roles: RoleDto[];
}

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.user ? i18n.t('common.edit') : i18n.t('users.create') }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:10px;min-width:340px;padding-top:8px;">
        <mat-form-field appearance="outline">
          <mat-label>{{ i18n.t('users.fullName') }}</mat-label>
          <input matInput formControlName="fullName" />
        </mat-form-field>

        @if (!data.user) {
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('auth.email') }}</mat-label>
            <input matInput type="email" formControlName="email" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('auth.password') }}</mat-label>
            <input matInput type="password" formControlName="password" />
          </mat-form-field>
        }

        <mat-form-field appearance="outline">
          <mat-label>{{ i18n.t('users.phone') }}</mat-label>
          <input matInput formControlName="phoneNumber" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>{{ i18n.t('users.language') }}</mat-label>
          <mat-select formControlName="preferredLanguage">
            <mat-option value="ar">العربية</mat-option>
            <mat-option value="en">English</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>{{ i18n.t('users.roles') }}</mat-label>
          <mat-select formControlName="roles" multiple>
            @for (role of data.roles; track role.id) {
              <mat-option [value]="role.name">{{ role.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        @if (data.user) {
          <mat-checkbox formControlName="isActive">{{ i18n.t('common.active') }}</mat-checkbox>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="dialogRef.close(false)">{{ i18n.t('common.cancel') }}</button>
      <button mat-flat-button class="hp-btn-primary" [disabled]="form.invalid || saving()" (click)="save()">
        {{ i18n.t('common.save') }}
      </button>
    </mat-dialog-actions>
  `
})
export class UserFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(UsersRolesService);
  private readonly toast = inject(ToastService);
  readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  readonly data = inject<UserFormDialogData>(MAT_DIALOG_DATA);
  readonly i18n = inject(TranslationService);

  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    phoneNumber: [''],
    preferredLanguage: ['ar', Validators.required],
    roles: [<string[]>[], Validators.required],
    isActive: [true]
  });

  ngOnInit(): void {
    if (this.data.user) {
      this.form.patchValue({
        fullName: this.data.user.fullName,
        email: this.data.user.email,
        phoneNumber: this.data.user.phoneNumber ?? '',
        preferredLanguage: this.data.user.preferredLanguage,
        roles: this.data.user.roles,
        isActive: this.data.user.isActive
      });
      this.form.controls.email.clearValidators();
      this.form.controls.password.clearValidators();
      this.form.controls.email.updateValueAndValidity();
      this.form.controls.password.updateValueAndValidity();
    }
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const v = this.form.getRawValue();

    const obs = this.data.user
      ? this.service.updateUser(this.data.user.id, {
          fullName: v.fullName,
          phoneNumber: v.phoneNumber || undefined,
          preferredLanguage: v.preferredLanguage,
          isActive: v.isActive,
          roles: v.roles
        })
      : this.service.createUser({
          email: v.email,
          fullName: v.fullName,
          password: v.password,
          phoneNumber: v.phoneNumber || undefined,
          preferredLanguage: v.preferredLanguage,
          roles: v.roles
        });

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Saved successfully');
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Operation failed');
      }
    });
  }
}

