import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { DocumentType, GuestDto } from '../../core/models/guest.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-guests-list',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    HasPermissionDirective,
    EmptyStateComponent
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.guests') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'ملفات النزلاء، الوثائق، والملاحظات' : 'Guest profiles, identity documents, and notes' }}</p>
      </div>
      <button *appHasPermission="'Guests.Create'" mat-flat-button class="hp-btn-primary" (click)="openNew()">
        <mat-icon>person_add</mat-icon>
        <span>{{ i18n.lang() === 'ar' ? 'نزيل جديد' : 'New Guest' }}</span>
      </button>
    </div>

    <div class="hp-card" style="padding:16px;margin-bottom:16px;">
      <div style="display:flex;gap:12px;flex-wrap:wrap;align-items:center;">
        <mat-form-field appearance="outline" style="flex:1;min-width:240px;">
          <mat-label>{{ i18n.lang() === 'ar' ? 'بحث بالاسم أو الهاتف أو رقم الوثيقة...' : 'Search by name, phone, or document...' }}</mat-label>
          <input matInput [(ngModel)]="search" (keyup.enter)="loadGuests(1)" />
          <mat-icon matSuffix style="cursor:pointer;" (click)="loadGuests(1)">search</mat-icon>
        </mat-form-field>
        <mat-checkbox [(ngModel)]="vipOnly" (change)="loadGuests(1)">VIP Only</mat-checkbox>
      </div>
    </div>

    @if (showForm()) {
      <div class="hp-card" style="padding:20px;margin-bottom:18px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ editingId() ? i18n.t('common.edit') : i18n.t('common.add') }}</h3>
        <form [formGroup]="form" (ngSubmit)="save()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:12px;">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('users.fullName') }}</mat-label>
            <input matInput formControlName="fullName" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('users.phone') }}</mat-label>
            <input matInput formControlName="phone" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('auth.email') }}</mat-label>
            <input matInput formControlName="email" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الجنسية' : 'Nationality' }}</mat-label>
            <input matInput formControlName="nationality" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'نوع الوثيقة' : 'Document Type' }}</mat-label>
            <mat-select formControlName="documentType">
              <mat-option [value]="1">National ID</mat-option>
              <mat-option [value]="2">Passport</mat-option>
              <mat-option [value]="3">Driver License</mat-option>
              <mat-option [value]="4">Other</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'رقم الوثيقة' : 'Document Number' }}</mat-label>
            <input matInput formControlName="documentNumber" />
          </mat-form-field>
          <mat-form-field appearance="outline" style="grid-column:1/-1;">
            <mat-label>{{ i18n.lang() === 'ar' ? 'ملاحظات' : 'Notes' }}</mat-label>
            <input matInput formControlName="notes" />
          </mat-form-field>
          <div style="grid-column:1/-1;display:flex;justify-content:space-between;align-items:center;">
            <mat-checkbox formControlName="isVip">VIP Guest</mat-checkbox>
            <div style="display:flex;gap:8px;">
              <button mat-stroked-button type="button" (click)="showForm.set(false)">{{ i18n.t('common.cancel') }}</button>
              <button mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid || saving()">
                {{ i18n.t('common.save') }}
              </button>
            </div>
          </div>
        </form>
      </div>
    }

    @if (loading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      @if (!loading() && guests().length === 0) {
        <app-empty-state icon="person_off"></app-empty-state>
      } @else {
        <table mat-table [dataSource]="guests()" style="width:100%;">
          <ng-container matColumnDef="fullName">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('users.fullName') }}</th>
            <td mat-cell *matCellDef="let g">
              <div style="font-weight:700;color:#0f2540;">
                {{ g.fullName }}
                @if (g.isVip) {
                  <span class="hp-badge hp-badge-gold" style="margin-inline-start:6px;">VIP</span>
                }
              </div>
              <div style="font-size:0.78rem;color:#64748b;">{{ g.nationality || '-' }}</div>
            </td>
          </ng-container>
          <ng-container matColumnDef="phone">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('users.phone') }}</th>
            <td mat-cell *matCellDef="let g">
              <div>{{ g.phone }}</div>
              <div style="font-size:0.78rem;color:#64748b;">{{ g.email || '-' }}</div>
            </td>
          </ng-container>
          <ng-container matColumnDef="document">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'الوثيقة' : 'Document' }}</th>
            <td mat-cell *matCellDef="let g">
              {{ g.documents?.[0]?.documentNumber || '-' }}
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
            <td mat-cell *matCellDef="let g">
              <button *appHasPermission="'Guests.Edit'" mat-icon-button (click)="edit(g)"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="['fullName','phone','document','actions']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['fullName','phone','document','actions'];"></tr>
        </table>
        <mat-paginator [length]="totalCount()" [pageSize]="pageSize" [pageIndex]="pageNumber - 1" [pageSizeOptions]="[10, 15, 25]" (page)="onPage($event)"></mat-paginator>
      }
    </div>
  `
})
export class GuestsListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly guests = signal<GuestDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);

  pageNumber = 1;
  pageSize = 15;
  search = '';
  vipOnly = false;

  readonly form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    phone: ['', Validators.required],
    email: [''],
    nationality: [''],
    documentType: [1 as DocumentType],
    documentNumber: [''],
    notes: [''],
    isVip: [false]
  });

  ngOnInit(): void {
    this.loadGuests();
  }

  loadGuests(page = this.pageNumber): void {
    this.pageNumber = page;
    this.loading.set(true);
    this.service.getGuests(this.pageNumber, this.pageSize, this.search, this.vipOnly ? true : null).subscribe({
      next: (res) => {
        this.guests.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load guests');
      }
    });
  }

  onPage(e: PageEvent): void {
    this.pageSize = e.pageSize;
    this.loadGuests(e.pageIndex + 1);
  }

  openNew(): void {
    this.editingId.set(null);
    this.form.reset({ fullName: '', phone: '', email: '', nationality: '', documentType: 1, documentNumber: '', notes: '', isVip: false });
    this.showForm.set(true);
  }

  edit(g: GuestDto): void {
    this.editingId.set(g.id);
    const doc = g.documents?.[0];
    this.form.patchValue({
      fullName: g.fullName,
      phone: g.phone,
      email: g.email ?? '',
      nationality: g.nationality ?? '',
      documentType: doc?.documentType ?? 1,
      documentNumber: doc?.documentNumber ?? '',
      notes: g.notes ?? '',
      isVip: g.isVip
    });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    const payload = {
      fullName: v.fullName,
      phone: v.phone,
      email: v.email || null,
      nationality: v.nationality || null,
      notes: v.notes || null,
      isVip: v.isVip,
      documents: v.documentNumber
        ? [{ documentType: v.documentType, documentNumber: v.documentNumber }]
        : []
    };

    const id = this.editingId();
    const obs = id ? this.service.updateGuest(id, payload) : this.service.createGuest(payload);
    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.toast.success('Guest saved');
        this.loadGuests();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Failed to save guest');
      }
    });
  }
}

