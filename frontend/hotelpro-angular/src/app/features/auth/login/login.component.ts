import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="hp-login-page" [dir]="i18n.isRtl() ? 'rtl' : 'ltr'">
      <div class="hp-login-card hp-card">
        <div class="hp-login-header">
          <div class="hp-login-logo">H</div>
          <h1>HotelPro</h1>
          <p>{{ i18n.t('auth.loginSubtitle') }}</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="hp-login-form">
          <mat-form-field appearance="outline" class="hp-full">
            <mat-label>{{ i18n.t('auth.email') }}</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            <mat-icon matPrefix>email</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="hp-full">
            <mat-label>{{ i18n.t('auth.password') }}</mat-label>
            <input matInput [type]="hidePassword() ? 'password' : 'text'" formControlName="password" autocomplete="current-password" />
            <mat-icon matPrefix>lock</mat-icon>
            <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
              <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
          </mat-form-field>

          <button
            mat-flat-button
            type="submit"
            class="hp-btn-primary hp-login-submit"
            [disabled]="form.invalid || loading()">
            @if (loading()) {
              <mat-spinner diameter="20" style="display:inline-block;margin-inline-end:8px;"></mat-spinner>
              <span>{{ i18n.t('auth.signingIn') }}</span>
            } @else {
              <span>{{ i18n.t('auth.login') }}</span>
            }
          </button>
        </form>

        <div class="hp-login-footer">
          <button mat-button type="button" (click)="i18n.toggleLanguage()">
            <mat-icon>translate</mat-icon>
            <span>{{ i18n.lang() === 'ar' ? 'English' : 'العربية' }}</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .hp-login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #09182b 0%, #0f2540 55%, #1a365d 100%);
      padding: 20px;
    }
    .hp-login-card {
      width: 100%;
      max-width: 420px;
      padding: 36px 32px 24px;
    }
    .hp-login-header { text-align: center; margin-bottom: 24px; }
    .hp-login-logo {
      width: 56px;
      height: 56px;
      margin: 0 auto 12px;
      border-radius: 14px;
      background: linear-gradient(135deg, #c9a227, #e4c766);
      color: #09182b;
      font-weight: 800;
      font-size: 1.7rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .hp-login-header h1 { margin: 0; color: #0f2540; font-size: 1.6rem; font-weight: 700; }
    .hp-login-header p { margin: 6px 0 0; color: #64748b; font-size: 0.9rem; }
    .hp-login-form { display: flex; flex-direction: column; gap: 10px; }
    .hp-full { width: 100%; }
    .hp-login-submit { height: 46px; font-size: 1rem; margin-top: 6px; }
    .hp-login-footer { text-align: center; margin-top: 16px; }
  `]
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly loading = signal(false);
  readonly hidePassword = signal(true);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.message || this.i18n.t('auth.invalidCredentials'));
      }
    });
  }
}

