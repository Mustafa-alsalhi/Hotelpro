import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ReservationService } from '../../core/services/reservation.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { BookingCalendarDto, CalendarBookingBlockDto } from '../../core/models/booking-calendar.model';
import { RESERVATION_STATUS_LABELS } from '../../core/models/reservation.model';

@Component({
  selector: 'app-booking-calendar',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.calendar') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'مخطط إشغال الغرف والحجوزات عبر الأيام' : 'Visual room availability and booking timeline' }}</p>
      </div>
      <div style="display:flex;gap:8px;align-items:center;">
        <button mat-stroked-button (click)="shiftDays(-7)"><mat-icon>chevron_left</mat-icon></button>
        <span style="font-weight:700;color:#0f2540;">{{ fromDate() }} → {{ toDate() }}</span>
        <button mat-stroked-button (click)="shiftDays(7)"><mat-icon>chevron_right</mat-icon></button>
      </div>
    </div>

    @if (loading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      <table class="hp-cal-table">
        <thead>
          <tr>
            <th class="hp-cal-room-col">{{ i18n.t('nav.rooms') }}</th>
            @for (day of days(); track day) {
              <th>{{ day.slice(5) }}</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (row of calendar()?.rooms ?? []; track row.roomId) {
            <tr>
              <td class="hp-cal-room-col">
                <div style="font-weight:700;color:#0f2540;">#{{ row.roomNumber }}</div>
                <div style="font-size:0.72rem;color:#64748b;">{{ row.roomTypeName }}</div>
              </td>
              @for (day of days(); track day) {
                @let booking = getBookingForDay(row.bookings, day);
                <td class="hp-cal-cell" [class.hp-cal-booked]="!!booking" [class.hp-cal-maint]="row.currentRoomStatus === 5 || row.currentRoomStatus === 6">
                  @if (booking) {
                    <div class="hp-cal-chip" [title]="booking.reservationNumber + ' - ' + booking.guestName">
                      {{ booking.guestName.split(' ')[0] }}
                    </div>
                  }
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .hp-cal-table { width: 100%; border-collapse: collapse; min-width: 900px; }
    .hp-cal-table th, .hp-cal-table td { border: 1px solid #e2e8f0; padding: 6px; text-align: center; font-size: 0.78rem; }
    .hp-cal-room-col { text-align: start !important; min-width: 130px; background: #f8fafc; position: sticky; left: 0; }
    .hp-cal-cell { height: 44px; min-width: 68px; }
    .hp-cal-booked { background: rgba(15, 37, 64, 0.08); }
    .hp-cal-maint { background: #f3e8ff; }
    .hp-cal-chip {
      background: #0f2540;
      color: #e4c766;
      border-radius: 6px;
      padding: 3px 5px;
      font-weight: 600;
      font-size: 0.72rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `]
})
export class BookingCalendarComponent implements OnInit {
  private readonly service = inject(ReservationService);
  readonly i18n = inject(TranslationService);

  readonly startOffset = signal(0);
  readonly loading = signal(false);
  readonly calendar = signal<BookingCalendarDto | null>(null);

  readonly fromDate = computed(() => this.addDays(new Date(), this.startOffset()));
  readonly toDate = computed(() => this.addDays(new Date(), this.startOffset() + 13));

  readonly days = computed(() => {
    const arr: string[] = [];
    const base = new Date();
    base.setDate(base.getDate() + this.startOffset());
    for (let i = 0; i < 14; i++) {
      const d = new Date(base);
      d.setDate(d.getDate() + i);
      arr.push(d.toISOString().slice(0, 10));
    }
    return arr;
  });

  ngOnInit(): void {
    this.load();
  }

  shiftDays(delta: number): void {
    this.startOffset.update((v) => v + delta);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getCalendar(this.fromDate(), this.toDate()).subscribe({
      next: (cal) => {
        this.calendar.set(cal);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getBookingForDay(bookings: CalendarBookingBlockDto[], day: string): CalendarBookingBlockDto | undefined {
    return bookings.find((b) => day >= b.checkInDate && day < b.checkOutDate);
  }

  statusLabel(status: number): string {
    return RESERVATION_STATUS_LABELS[status]?.en ?? '';
  }

  private addDays(date: Date, days: number): string {
    const d = new Date(date);
    d.setDate(d.getDate() + days);
    return d.toISOString().slice(0, 10);
  }
}

