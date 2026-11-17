import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { OperationsService } from '../../core/services/operations.service';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { MaintenanceRequestDto, MaintenanceStatus } from '../../core/models/maintenance.model';
import { RoomDto } from '../../core/models/room.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.maintenance') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة طلبات الصيانة والأعطال وإعادة الغرف للخدمة' : 'Manage maintenance requests, repairs, costs, and room restoration' }}</p>
      </div>
    </div>

    <div class="hp-card" style="padding:20px;margin-bottom:20px;">
      <form [formGroup]="form" (ngSubmit)="createRequest()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:12px;align-items:center;">
        <mat-form-field appearance="outline">
          <mat-label>Room (Optional)</mat-label>
          <mat-select formControlName="roomId">
            <mat-option [value]="null">General / Facility</mat-option>
            @for (r of rooms(); track r.id) { <mat-option [value]="r.id">#{{ r.roomNumber }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Issue Type</mat-label>
          <input matInput formControlName="issueType" placeholder="AC, Plumbing, Electrical..." />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Priority</mat-label>
          <mat-select formControlName="priority">
            <mat-option [value]="1">Low</mat-option>
            <mat-option [value]="2">Normal</mat-option>
            <mat-option [value]="3">High</mat-option>
            <mat-option [value]="4">Critical</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="grid-column:1/-1;">
          <mat-label>Description</mat-label>
          <input matInput formControlName="description" />
        </mat-form-field>
        <mat-checkbox formControlName="takeRoomOutOfService">Take room out of service</mat-checkbox>
        <div style="display:flex;justify-content:flex-end;">
          <button *appHasPermission="'Maintenance.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid">
            <mat-icon>build</mat-icon>
            <span>Open Request</span>
          </button>
        </div>
      </form>
    </div>

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      <table mat-table [dataSource]="requests()" style="width:100%;">
        <ng-container matColumnDef="number"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let m" style="font-weight:700;">{{ m.requestNumber }}</td></ng-container>
        <ng-container matColumnDef="room"><th mat-header-cell *matHeaderCellDef>Room</th><td mat-cell *matCellDef="let m">{{ m.roomNumber ? '#' + m.roomNumber : 'Facility' }}</td></ng-container>
        <ng-container matColumnDef="issue"><th mat-header-cell *matHeaderCellDef>Issue</th><td mat-cell *matCellDef="let m">{{ m.issueType }} — {{ m.description }}</td></ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let m">
            <mat-select *appHasPermission="'Maintenance.Edit'" [value]="m.status" (selectionChange)="updateStatus(m, $event.value)" style="width:150px;">
              <mat-option [value]="1">Open</mat-option>
              <mat-option [value]="2">Assigned</mat-option>
              <mat-option [value]="3">In Progress</mat-option>
              <mat-option [value]="4">Waiting Parts</mat-option>
              <mat-option [value]="5">Completed</mat-option>
              <mat-option [value]="6">Closed (Restore Room)</mat-option>
            </mat-select>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="['number','room','issue','status']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['number','room','issue','status'];"></tr>
      </table>
    </div>
  `
})
export class MaintenanceComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ops = inject(OperationsService);
  private readonly roomService = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly requests = signal<MaintenanceRequestDto[]>([]);
  readonly rooms = signal<RoomDto[]>([]);

  readonly form = this.fb.group({
    roomId: [null as string | null],
    issueType: ['', Validators.required],
    description: ['', Validators.required],
    priority: [2, Validators.required],
    takeRoomOutOfService: [false]
  });

  ngOnInit(): void {
    this.roomService.getRooms(1, 100).subscribe((r) => this.rooms.set(r.items));
    this.load();
  }

  load(): void {
    this.ops.getMaintenanceRequests().subscribe((res) => this.requests.set(res.items));
  }

  createRequest(): void {
    if (this.form.invalid) return;
    this.ops.createMaintenanceRequest(this.form.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Maintenance request opened');
        this.form.reset({ roomId: null, issueType: '', description: '', priority: 2, takeRoomOutOfService: false });
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  updateStatus(m: MaintenanceRequestDto, status: MaintenanceStatus): void {
    this.ops.updateMaintenanceRequest(m.id, {
      status,
      assignedTechnicianUserId: m.assignedTechnicianUserId,
      sparePartsUsed: m.sparePartsUsed,
      cost: m.cost,
      restoreRoomToAvailable: status === 6
    }).subscribe({
      next: () => {
        this.toast.success('Request updated');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }
}

