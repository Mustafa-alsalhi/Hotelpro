import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { OperationsService } from '../../core/services/operations.service';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { HousekeepingStatus, HousekeepingTaskDto, TaskPriority } from '../../core/models/housekeeping.model';
import { RoomDto } from '../../core/models/room.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-housekeeping',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.housekeeping') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'مهام تنظيف الغرف، الفحص، والإبلاغ عن التلف' : 'Manage room cleaning tasks, supervisor inspection, and readiness' }}</p>
      </div>
    </div>

    <div class="hp-card" style="padding:20px;margin-bottom:20px;">
      <form [formGroup]="form" (ngSubmit)="createTask()" style="display:flex;flex-wrap:wrap;gap:12px;align-items:center;">
        <mat-form-field appearance="outline" style="min-width:160px;">
          <mat-label>Room</mat-label>
          <mat-select formControlName="roomId">
            @for (r of rooms(); track r.id) { <mat-option [value]="r.id">#{{ r.roomNumber }} (Floor {{ r.floor }})</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="min-width:150px;">
          <mat-label>Task Type</mat-label>
          <mat-select formControlName="taskType">
            <mat-option [value]="1">Checkout Cleaning</mat-option>
            <mat-option [value]="2">Daily Cleaning</mat-option>
            <mat-option [value]="3">Deep Cleaning</mat-option>
            <mat-option [value]="4">Inspection</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="min-width:130px;">
          <mat-label>Priority</mat-label>
          <mat-select formControlName="priority">
            <mat-option [value]="1">Low</mat-option>
            <mat-option [value]="2">Normal</mat-option>
            <mat-option [value]="3">High</mat-option>
            <mat-option [value]="4">Urgent</mat-option>
          </mat-select>
        </mat-form-field>
        <button *appHasPermission="'Housekeeping.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid">
          <mat-icon>add_task</mat-icon>
          <span>Create Task</span>
        </button>
      </form>
    </div>

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      <table mat-table [dataSource]="tasks()" style="width:100%;">
        <ng-container matColumnDef="room"><th mat-header-cell *matHeaderCellDef>Room</th><td mat-cell *matCellDef="let t" style="font-weight:700;">#{{ t.roomNumber }} (Floor {{ t.floor }})</td></ng-container>
        <ng-container matColumnDef="scheduled"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let t">{{ t.scheduledFor }}</td></ng-container>
        <ng-container matColumnDef="notes"><th mat-header-cell *matHeaderCellDef>Notes</th><td mat-cell *matCellDef="let t">{{ t.notes || '-' }}</td></ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let t">
            <mat-select *appHasPermission="'Housekeeping.Edit'" [value]="t.status" (selectionChange)="updateStatus(t, $event.value)" style="width:140px;">
              <mat-option [value]="1">Pending</mat-option>
              <mat-option [value]="2">Assigned</mat-option>
              <mat-option [value]="3">In Progress</mat-option>
              <mat-option [value]="4">Completed</mat-option>
              <mat-option [value]="5">Inspected (Ready)</mat-option>
            </mat-select>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="['room','scheduled','notes','status']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['room','scheduled','notes','status'];"></tr>
      </table>
    </div>
  `
})
export class HousekeepingComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ops = inject(OperationsService);
  private readonly roomService = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly tasks = signal<HousekeepingTaskDto[]>([]);
  readonly rooms = signal<RoomDto[]>([]);

  readonly form = this.fb.nonNullable.group({
    roomId: ['', Validators.required],
    taskType: [2, Validators.required],
    priority: [2 as TaskPriority, Validators.required],
    scheduledFor: [new Date().toISOString().slice(0, 10), Validators.required],
    notes: ['']
  });

  ngOnInit(): void {
    this.roomService.getRooms(1, 100).subscribe((r) => this.rooms.set(r.items));
    this.load();
  }

  load(): void {
    this.ops.getHousekeepingTasks().subscribe((res) => this.tasks.set(res.items));
  }

  createTask(): void {
    if (this.form.invalid) return;
    this.ops.createHousekeepingTask(this.form.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Task created');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  updateStatus(t: HousekeepingTaskDto, status: HousekeepingStatus): void {
    this.ops.updateHousekeepingTask(t.id, {
      status,
      assignedToUserId: t.assignedToUserId,
      damageReported: t.damageReported,
      notes: t.notes
    }).subscribe({
      next: () => {
        this.toast.success('Task updated');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }
}

