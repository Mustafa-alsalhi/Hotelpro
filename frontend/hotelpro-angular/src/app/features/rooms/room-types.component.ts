import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { AmenityDto, RoomTypeDto } from '../../core/models/room-type.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-room-types',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatProgressBarModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.lang() === 'ar' ? 'أنواع الغرف' : 'Room Types' }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة فئات الغرف والأسعار الأساسية والمرافق' : 'Manage room categories, base rates, and amenities' }}</p>
      </div>
      <button *appHasPermission="'Rooms.Create'" mat-flat-button class="hp-btn-primary" (click)="startNew()">
        <mat-icon>add</mat-icon>
        <span>{{ i18n.lang() === 'ar' ? 'نوع غرفة جديد' : 'New Room Type' }}</span>
      </button>
    </div>

    @if (loading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    <div style="display:grid;grid-template-columns:1fr 380px;gap:20px;align-items:start;">
      <div class="hp-card" style="padding:16px;overflow-x:auto;">
        <table mat-table [dataSource]="roomTypes()" style="width:100%;">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'النوع' : 'Type' }}</th>
            <td mat-cell *matCellDef="let rt">
              <div style="font-weight:700;color:#0f2540;">{{ rt.name }}</div>
              <div style="font-size:0.78rem;color:#64748b;">{{ rt.code }} • {{ rt.areaSqm }} m²</div>
            </td>
          </ng-container>

          <ng-container matColumnDef="capacity">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'السعة' : 'Capacity' }}</th>
            <td mat-cell *matCellDef="let rt">
              {{ rt.maxAdults }}A / {{ rt.maxChildren }}C • {{ rt.bedCount }} Beds
            </td>
          </ng-container>

          <ng-container matColumnDef="basePrice">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'السعر الأساسي' : 'Base Price' }}</th>
            <td mat-cell *matCellDef="let rt" style="font-weight:700;color:#0f2540;">
              {{ rt.basePrice }}
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
            <td mat-cell *matCellDef="let rt">
              <button *appHasPermission="'Rooms.Edit'" mat-icon-button (click)="edit(rt)">
                <mat-icon>edit</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="['name', 'capacity', 'basePrice', 'actions']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['name', 'capacity', 'basePrice', 'actions'];"></tr>
        </table>
      </div>

      <div class="hp-card" style="padding:20px;">
        <h3 style="margin-top:0;color:#0f2540;">
          {{ editingId() ? i18n.t('common.edit') : i18n.t('common.add') }}
        </h3>
        <form [formGroup]="form" (ngSubmit)="save()" style="display:flex;flex-direction:column;gap:8px;">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الاسم' : 'Name' }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الرمز' : 'Code' }}</mat-label>
            <input matInput formControlName="code" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'السعر الأساسي' : 'Base Price' }}</mat-label>
            <input matInput type="number" formControlName="basePrice" />
          </mat-form-field>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
            <mat-form-field appearance="outline">
              <mat-label>Max Adults</mat-label>
              <input matInput type="number" formControlName="maxAdults" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Max Children</mat-label>
              <input matInput type="number" formControlName="maxChildren" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Beds</mat-label>
              <input matInput type="number" formControlName="bedCount" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Area (m²)</mat-label>
              <input matInput type="number" formControlName="areaSqm" />
            </mat-form-field>
          </div>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'المرافق' : 'Amenities' }}</mat-label>
            <mat-select formControlName="amenityIds" multiple>
              @for (a of amenities(); track a.id) {
                <mat-option [value]="a.id">{{ a.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('roles.description') }}</mat-label>
            <textarea matInput rows="2" formControlName="description"></textarea>
          </mat-form-field>
          <mat-checkbox formControlName="isActive">{{ i18n.t('common.active') }}</mat-checkbox>
          <button mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid || saving()">
            {{ i18n.t('common.save') }}
          </button>
        </form>
      </div>
    </div>
  `
})
export class RoomTypesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly amenities = signal<AmenityDto[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    description: [''],
    basePrice: [300, [Validators.required, Validators.min(0)]],
    maxAdults: [2, [Validators.required, Validators.min(1)]],
    maxChildren: [1, [Validators.required, Validators.min(0)]],
    bedCount: [1, [Validators.required, Validators.min(1)]],
    areaSqm: [25, [Validators.required, Validators.min(1)]],
    isActive: [true],
    amenityIds: [<string[]>[]]
  });

  ngOnInit(): void {
    this.service.getAmenities().subscribe((a) => this.amenities.set(a));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getRoomTypes(true).subscribe({
      next: (list) => {
        this.roomTypes.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  startNew(): void {
    this.editingId.set(null);
    this.form.reset({
      name: '', code: '', description: '', basePrice: 300,
      maxAdults: 2, maxChildren: 1, bedCount: 1, areaSqm: 25, isActive: true, amenityIds: []
    });
  }

  edit(rt: RoomTypeDto): void {
    this.editingId.set(rt.id);
    this.form.patchValue({
      ...rt,
      amenityIds: rt.amenities.map((a) => a.id)
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const id = this.editingId();
    const obs = id
      ? this.service.updateRoomType(id, this.form.getRawValue())
      : this.service.createRoomType(this.form.getRawValue());

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Saved');
        this.startNew();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Error saving room type');
      }
    });
  }
}

