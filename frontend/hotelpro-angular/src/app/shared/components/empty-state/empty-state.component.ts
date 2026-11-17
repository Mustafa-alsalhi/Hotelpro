import { Component, Input, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslationService } from '../../../core/i18n/translation.service';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div style="text-align:center;padding:48px 16px;color:#64748b;">
      <mat-icon style="font-size:48px;width:48px;height:48px;color:#cbd5e1;margin-bottom:8px;">
        {{ icon }}
      </mat-icon>
      <p style="margin:0;font-size:0.98rem;">{{ message || i18n.t('common.noData') }}</p>
    </div>
  `
})
export class EmptyStateComponent {
  readonly i18n = inject(TranslationService);
  @Input() icon = 'inbox';
  @Input() message = '';
}

