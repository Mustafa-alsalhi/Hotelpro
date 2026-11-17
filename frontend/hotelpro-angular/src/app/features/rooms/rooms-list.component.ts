import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { ROOM_STATUS_LABELS, RoomDto, RoomStatus } from '../../core/models/room.model';
import { RoomTypeDto } from '../../core/models/room-type.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-rooms-list',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatButtonToggleModule,
    MatProgressBarModule,
    HasPermissionDirective,
    EmptyStateComponent
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.rooms') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة الغرف، الحالات، الطوابق، والمخطط البصري' : 'Manage rooms, statuses, floors, and visual floor map' }}</p>
      </div>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <mat-button-toggle-group [(ngModel)]="viewMode">
          <mat-button-toggle value="table"><mat-icon>table_rows</mat-icon></mat-button-toggle>
          <mat-button-toggle value="cards"><mat-icon>grid_view</mat-icon></mat-button-toggle>
          <mat-button-toggle value="map"><mat-icon>apartment</mat-icon></mat-button-toggle>
        </mat-button-toggle-group>
        <button *appHasPermission="'Rooms.Create'" mat-flat-button class="hp-btn-primary" (click)="openNewRoom()">
          <mat-icon>add</mat-icon>
          <span>{{ i18n.lang() === 'ar' ? 'غرفة جديدة' : 'New Room' }}</span>
        </button>
      </div>
    </div>

    <div class="hp-card" style="padding:16px;margin-bottom:18px;">
      <div style="display:flex;flex-wrap:wrap;gap:12px;">
        <mat-form-field appearance="outline" style="flex:1;min-width:200px;">
          <mat-label>{{ i18n.t('common.search') }}</mat-label>
          <input matInput [(ngModel)]="search" (keyup.enter)="loadRooms(1)" />
          <mat-icon matSuffix style="cursor:pointer;" (click)="loadRooms(1)">search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" style="min-width:160px;">
          <mat-label>{{ i18n.lang() === 'ar' ? 'نوع الغرفة' : 'Room Type' }}</mat-label>
          <mat-select [(ngModel)]="selectedRoomTypeId" (selectionChange)="loadRooms(1)">
            <mat-option value="">All</mat-option>
            @for (rt of roomTypes(); track rt.id) {
              <mat-option [value]="rt.id">{{ rt.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="min-width:150px;">
          <mat-label>{{ i18n.t('common.status') }}</mat-label>
          <mat-select [(ngModel)]="selectedStatus" (selectionChange)="loadRooms(1)">
            <mat-option [value]="null">All</mat-option>
            @for (s of statusOptions; track s.value) {
              <mat-option [value]="s.value">{{ i18n.lang() === 'ar' ? s.ar : s.en }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width:120px;">
          <mat-label>{{ i18n.lang() === 'ar' ? 'الطابق' : 'Floor' }}</mat-label>
          <input matInput type="number" [(ngModel)]="selectedFloor" (change)="loadRooms(1)" />
        </mat-form-field>
      </div>
    </div>

    @if (showForm()) {
      <div class="hp-card" style="padding:20px;margin-bottom:18px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ editingRoomId() ? i18n.t('common.edit') : i18n.t('common.add') }}</h3>
        <form [formGroup]="form" (ngSubmit)="saveRoom()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:12px;">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'رقم الغرفة' : 'Room Number' }}</mat-label>
            <input matInput formControlName="roomNumber" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'نوع الغرفة' : 'Room Type' }}</mat-label>
            <mat-select formControlName="roomTypeId">
              @for (rt of roomTypes(); track rt.id) {
                <mat-option [value]="rt.id">{{ rt.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الطابق' : 'Floor' }}</mat-label>
            <input matInput type="number" formControlName="floor" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'السعة القصوى' : 'Max Capacity' }}</mat-label>
            <input matInput type="number" formControlName="maxCapacity" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'عدد الأسرة' : 'Beds' }}</mat-label>
            <input matInput type="number" formControlName="bedCount" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'تجاوز السعر (اختياري)' : 'Price Override' }}</mat-label>
            <input matInput type="number" formControlName="basePriceOverride" />
          </mat-form-field>
          <mat-form-field appearance="outline" style="grid-column:1/-1;">
            <mat-label>{{ i18n.t('roles.description') }}</mat-label>
            <input matInput formControlName="description" />
          </mat-form-field>
          <div style="grid-column:1/-1;display:flex;gap:8px;justify-content:flex-end;">
            <button mat-stroked-button type="button" (click)="showForm.set(false)">{{ i18n.t('common.cancel') }}</button>
            <button mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid || saving()">
              {{ i18n.t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    }

    @if (loading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    @if (!loading() && rooms().length === 0) {
      <div class="hp-card"><app-empty-state icon="meeting_room"></app-empty-state></div>
    } @else {
      @if (viewMode === 'table') {
        <div class="hp-card" style="padding:16px;overflow-x:auto;">
          <table mat-table [dataSource]="rooms()" style="width:100%;">
            <ng-container matColumnDef="roomNumber">
              <th mat-header-cell *matHeaderCellDef>#</th>
              <td mat-cell *matCellDef="let r" style="font-weight:700;color:#0f2540;">{{ r.roomNumber }}</td>
            </ng-container>
            <ng-container matColumnDef="type">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'النوع' : 'Type' }}</th>
              <td mat-cell *matCellDef="let r">{{ r.roomTypeName }}</td>
            </ng-container>
            <ng-container matColumnDef="floor">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'الطابق' : 'Floor' }}</th>
              <td mat-cell *matCellDef="let r">{{ r.floor }}</td>
            </ng-container>
            <ng-container matColumnDef="price">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'السعر' : 'Rate' }}</th>
              <td mat-cell *matCellDef="let r">{{ r.effectivePrice }}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.status') }}</th>
              <td mat-cell *matCellDef="let r">
                <span class="hp-badge" [class]="statusClass(r.status)">{{ statusLabel(r.status) }}</span>
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
              <td mat-cell *matCellDef="let r">
                <button *appHasPermission="'Rooms.Edit'" mat-icon-button (click)="editRoom(r)"><mat-icon>edit</mat-icon></button>
                @if (r.status !== 3 && r.status !== 2) {
                  <mat-select
                    *appHasPermission="'Rooms.Edit'"
                    [value]="r.status"
                    (selectionChange)="changeStatus(r, $event.value)"
                    style="width:120px;display:inline-block;font-size:0.8rem;">
                    <mat-option [value]="1">{{ statusLabel(1) }}</mat-option>
                    <mat-option [value]="4">{{ statusLabel(4) }}</mat-option>
                    <mat-option [value]="5">{{ statusLabel(5) }}</mat-option>
                    <mat-option [value]="6">{{ statusLabel(6) }}</mat-option>
                  </mat-select>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['roomNumber','type','floor','price','status','actions']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['roomNumber','type','floor','price','status','actions'];"></tr>
          </table>
          <mat-paginator [length]="totalCount()" [pageSize]="pageSize" [pageIndex]="pageNumber - 1" [pageSizeOptions]="[15, 25, 50]" (page)="onPage($event)"></mat-paginator>
        </div>
      } @else if (viewMode === 'cards') {
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:16px;">
          @for (r of rooms(); track r.id) {
            <div class="hp-card" style="padding:16px;">
              <div style="display:flex;justify-content:space-between;align-items:center;">
                <span style="font-size:1.25rem;font-weight:800;color:#0f2540;">{{ r.roomNumber }}</span>
                <span class="hp-badge" [class]="statusClass(r.status)">{{ statusLabel(r.status) }}</span>
              </div>
              <div style="color:#64748b;font-size:0.88rem;margin-top:6px;">{{ r.roomTypeName }} • Floor {{ r.floor }}</div>
              <div style="font-weight:700;color:#c9a227;margin-top:10px;">{{ r.effectivePrice }} / night</div>
            </div>
          }
        </div>
      } @else {
        @for (grp of roomsByFloor(); track grp.floor) {
          <div class="hp-card" style="padding:16px;margin-bottom:14px;">
            <h3 style="margin:0 0 12px;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'الطابق ' + grp.floor : 'Floor ' + grp.floor }}</h3>
            <div style="display:flex;flex-wrap:wrap;gap:10px;">
              @for (r of grp.rooms; track r.id) {
                <div class="hp-map-tile" [class]="statusClass(r.status)" (click)="editRoom(r)">
                  <div style="font-weight:800;font-size:1.05rem;">{{ r.roomNumber }}</div>
                  <div style="font-size:0.74rem;">{{ r.roomTypeName }}</div>
                  <div style="font-size:0.72rem;margin-top:4px;">{{ statusLabel(r.status) }}</div>
                </div>
              }
            </div>
          </div>
        }
      }
    }
  `,
  styles: [`
    .hp-status-available { background: #dcfce7; color: #15803d; }
    .hp-status-reserved { background: #dbeafe; color: #1d4ed8; }
    .hp-status-occupied { background: #fee2e2; color: #b91c1c; }
    .hp-status-cleaning { background: #fef3c7; color: #b45309; }
    .hp-status-maintenance { background: #f3e8ff; color: #7e22ce; }
    .hp-status-oos { background: #e2e8f0; color: #475569; }
    .hp-map-tile {
      width: 120px;
      padding: 12px;
      border-radius: 10px;
      cursor: pointer;
      border: 1px solid rgba(0,0,0,0.08);
      text-align: center;
    }
  `]
})
export class RoomsListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly rooms = signal<RoomDto[]>([]);
  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly editingRoomId = signal<string | null>(null);

  viewMode: 'table' | 'cards' | 'map' = 'table';
  pageNumber = 1;
  pageSize = 25;
  search = '';
  selectedRoomTypeId = '';
  selectedStatus: RoomStatus | null = null;
  selectedFloor: number | null = null;

  readonly statusOptions = Object.entries(ROOM_STATUS_LABELS).map(([k, v]) => ({
    value: Number(k) as RoomStatus,
    en: v.en,
    ar: v.ar
  }));

  readonly roomsByFloor = computed(() => {
    const map = new Map<number, RoomDto[]>();
    for (const r of this.rooms()) {
      const arr = map.get(r.floor) ?? [];
      arr.push(r);
      map.set(r.floor, arr);
    }
    return Array.from(map.entries())
      .sort((a, b) => a[0] - b[0])
      .map(([floor, rooms]) => ({ floor, rooms }));
  });

  readonly form = this.fb.group({
    roomNumber: ['', Validators.required],
    roomTypeId: ['', Validators.required],
    floor: [1, [Validators.required, Validators.min(0)]],
    maxCapacity: [2, [Validators.required, Validators.min(1)]],
    bedCount: [1, [Validators.required, Validators.min(1)]],
    basePriceOverride: [null as number | null],
    description: ['']
  });

  ngOnInit(): void {
    this.service.getRoomTypes(true).subscribe((rt) => this.roomTypes.set(rt));
    this.loadRooms();
  }

  loadRooms(page = this.pageNumber): void {
    this.pageNumber = page;
    this.loading.set(true);
    this.service
      .getRooms(this.pageNumber, this.pageSize, this.search, this.selectedFloor, this.selectedRoomTypeId, this.selectedStatus)
      .subscribe({
        next: (res) => {
          this.rooms.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Failed to load rooms');
        }
      });
  }

  onPage(e: PageEvent): void {
    this.pageSize = e.pageSize;
    this.loadRooms(e.pageIndex + 1);
  }

  openNewRoom(): void {
    this.editingRoomId.set(null);
    this.form.reset({
      roomNumber: '',
      roomTypeId: this.roomTypes()[0]?.id ?? '',
      floor: 1,
      maxCapacity: 2,
      bedCount: 1,
      basePriceOverride: null,
      description: ''
    });
    this.showForm.set(true);
  }

  editRoom(r: RoomDto): void {
    this.editingRoomId.set(r.id);
    this.form.patchValue({
      roomNumber: r.roomNumber,
      roomTypeId: r.roomTypeId,
      floor: r.floor,
      maxCapacity: r.maxCapacity,
      bedCount: r.bedCount,
      basePriceOverride: r.basePriceOverride ?? null,
      description: r.description
    });
    this.showForm.set(true);
  }

  saveRoom(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    const payload = { ...v, imageUrls: [], amenityIds: [] };
    const id = this.editingRoomId();
    const obs = id ? this.service.updateRoom(id, payload) : this.service.createRoom(payload);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.toast.success('Room saved');
        this.loadRooms();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Failed to save room');
      }
    });
  }

  changeStatus(r: RoomDto, status: RoomStatus): void {
    this.service.changeRoomStatus(r.id, status).subscribe({
      next: () => {
        this.toast.success('Status updated');
        this.loadRooms();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Cannot change status')
    });
  }

  statusLabel(status: number): string {
    const item = ROOM_STATUS_LABELS[status];
    return item ? (this.i18n.lang() === 'ar' ? item.ar : item.en) : '';
  }

  statusClass(status: number): string {
    return ROOM_STATUS_LABELS[status]?.css ?? '';
  }
}

