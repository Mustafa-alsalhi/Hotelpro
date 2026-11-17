import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslationService } from '../../../core/i18n/translation.service';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div style="padding: 8px 4px;">
      <h2 mat-dialog-title style="display:flex;align-items:center;gap:8px;margin:0;">
        <mat-icon [style.color]="data.danger ? '#b91c1c' : '#c9a227'">
          {{ data.danger ? 'warning' : 'help_outline' }}
        </mat-icon>
        <span>{{ data.title }}</span>
      </h2>
      <mat-dialog-content style="padding-top:10px;color:#475569;">
        <p style="margin:0;">{{ data.message }}</p>
      </mat-dialog-content>
      <mat-dialog-actions align="end" style="gap:8px;margin-top:12px;">
        <button mat-stroked-button (click)="dialogRef.close(false)">
          {{ data.cancelText || i18n.t('common.cancel') }}
        </button>
        <button
          mat-flat-button
          [class.hp-btn-primary]="!data.danger"
          [style.background-color]="data.danger ? '#b91c1c' : ''"
          [style.color]="'#fff'"
          (click)="dialogRef.close(true)">
          {{ data.confirmText || i18n.t('common.confirm') }}
        </button>
      </mat-dialog-actions>
    </div>
  `
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  readonly i18n = inject(TranslationService);
}

