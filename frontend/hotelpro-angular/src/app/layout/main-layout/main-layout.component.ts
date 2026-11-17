import { Component, inject, signal, HostListener, OnInit, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../core/auth/auth.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { DashboardReportsService } from '../../core/services/dashboard-reports.service';
import { NotificationDto } from '../../core/models/notification.model';

interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
  permission?: string;
}

interface NavGroup {
  titleKey?: string;
  items: NavItem[];
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterModule,
    RouterOutlet,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    MatBadgeModule,
    DatePipe
  ],
  template: `
    <mat-sidenav-container class="hp-shell" [dir]="i18n.isRtl() ? 'rtl' : 'ltr'">
      <mat-sidenav
        [mode]="isMobile() ? 'over' : 'side'"
        [opened]="sidebarOpen()"
        (closedStart)="sidebarOpen.set(false)"
        class="hp-sidebar"
        [class.hp-sidebar-collapsed]="collapsed() && !isMobile()">

        <div class="hp-brand">
          <div class="hp-brand-logo">H</div>
          @if (!collapsed() || isMobile()) {
            <div class="hp-brand-text">
              <span class="hp-brand-name">HotelPro</span>
              <span class="hp-brand-tag">HMS Enterprise</span>
            </div>
          }
        </div>

        <nav class="hp-nav">
          @for (group of navGroups; track $index) {
            @if (group.titleKey && (!collapsed() || isMobile())) {
              <div class="hp-nav-group">{{ i18n.t(group.titleKey) }}</div>
            }
            @for (item of group.items; track item.route) {
              @if (!item.permission || auth.hasPermission(item.permission)) {
                <a
                  class="hp-nav-item"
                  [routerLink]="item.route"
                  routerLinkActive="hp-nav-active"
                  [matTooltip]="collapsed() && !isMobile() ? i18n.t(item.labelKey) : ''">
                  <mat-icon>{{ item.icon }}</mat-icon>
                  @if (!collapsed() || isMobile()) {
                    <span>{{ i18n.t(item.labelKey) }}</span>
                  }
                </a>
              }
            }
          }
        </nav>
      </mat-sidenav>

      <mat-sidenav-content class="hp-main">
        <mat-toolbar class="hp-topbar">
          <div class="hp-topbar-left">
            <button mat-icon-button (click)="toggleSidebar()">
              <mat-icon>menu</mat-icon>
            </button>
            <span class="hp-topbar-subtitle">{{ i18n.t('app.subtitle') }}</span>
          </div>

          <div class="hp-topbar-right">
            <button mat-stroked-button (click)="i18n.toggleLanguage()" class="hp-lang-btn">
              <mat-icon>translate</mat-icon>
              <span>{{ i18n.lang() === 'ar' ? 'English' : 'العربية' }}</span>
            </button>

            <button
              mat-icon-button
              [matMenuTriggerFor]="notifMenu"
              (menuOpened)="loadNotifications()"
              [matBadge]="unreadCount()"
              [matBadgeHidden]="unreadCount() === 0"
              matBadgeColor="warn"
              matBadgeSize="small"
              aria-label="Notifications"
            >
              <mat-icon>notifications</mat-icon>
            </button>

            <mat-menu #notifMenu="matMenu">
              <div style="padding:10px 16px;min-width:290px;max-width:340px;display:flex;justify-content:space-between;align-items:center;" (click)="$event.stopPropagation()">
                <span style="font-weight:700;color:#0f2540;">
                  {{ i18n.lang() === 'ar' ? 'الإشعارات' : 'Notifications' }}
                </span>
                @if (unreadCount() > 0) {
                  <button mat-button color="primary" style="font-size:0.75rem;line-height:24px;" (click)="markAllRead()">
                    {{ i18n.lang() === 'ar' ? 'تحديد الكل كمقروء' : 'Mark all read' }}
                  </button>
                }
              </div>
              <mat-divider></mat-divider>
              @if (notifications().length === 0) {
                <div style="padding:16px;text-align:center;color:#64748b;font-size:0.85rem;" (click)="$event.stopPropagation()">
                  {{ i18n.lang() === 'ar' ? 'لا توجد إشعارات' : 'No notifications' }}
                </div>
              } @else {
                @for (n of notifications().slice(0, 8); track n.id) {
                  <button mat-menu-item (click)="markRead(n)" [style.background]="n.isRead ? 'transparent' : '#f8fafc'" style="height:auto;line-height:1.35;padding-top:8px;padding-bottom:8px;white-space:normal;">
                    <div style="display:flex;flex-direction:column;gap:2px;max-width:290px;">
                      <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
                        <span [style.font-weight]="n.isRead ? '500' : '700'" style="font-size:0.85rem;color:#0f2540;">{{ n.title }}</span>
                        @if (!n.isRead) {
                          <span style="width:7px;height:7px;border-radius:50%;background:#c9a227;flex-shrink:0;"></span>
                        }
                      </div>
                      <span style="font-size:0.78rem;color:#475569;">{{ n.message }}</span>
                      <span style="font-size:0.7rem;color:#94a3b8;">{{ n.createdAt | date:'short' }}</span>
                    </div>
                  </button>
                }
              }
            </mat-menu>

            <button mat-icon-button [matMenuTriggerFor]="userMenu">
              <mat-icon>account_circle</mat-icon>
            </button>
            <span class="hp-user-name" [matMenuTriggerFor]="userMenu">
              {{ auth.currentUser()?.fullName }}
            </span>

            <mat-menu #userMenu="matMenu">
              <div style="padding:10px 16px;min-width:200px;">
                <div style="font-weight:700;color:#0f2540;">{{ auth.currentUser()?.fullName }}</div>
                <div style="font-size:0.8rem;color:#64748b;">{{ auth.currentUser()?.email }}</div>
                <div style="margin-top:6px;">
                  @for (role of auth.currentUser()?.roles ?? []; track role) {
                    <span class="hp-badge hp-badge-gold" style="margin-inline-end:4px;">{{ role }}</span>
                  }
                </div>
              </div>
              <mat-divider></mat-divider>
              <button mat-menu-item (click)="auth.logout()">
                <mat-icon color="warn">logout</mat-icon>
                <span>{{ i18n.t('auth.logout') }}</span>
              </button>
            </mat-menu>
          </div>
        </mat-toolbar>

        <main class="hp-content">
          <router-outlet></router-outlet>
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .hp-shell { height: 100vh; }
    .hp-sidebar {
      width: 260px;
      background: #0f2540;
      color: #e2e8f0;
      border: none;
      transition: width 0.2s ease;
    }
    .hp-sidebar-collapsed { width: 74px; }
    .hp-brand {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 18px 16px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }
    .hp-brand-logo {
      width: 40px;
      height: 40px;
      border-radius: 10px;
      background: linear-gradient(135deg, #c9a227, #e4c766);
      color: #09182b;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
      font-size: 1.25rem;
      flex-shrink: 0;
    }
    .hp-brand-text { display: flex; flex-direction: column; }
    .hp-brand-name { font-weight: 700; font-size: 1.1rem; color: #fff; line-height: 1.2; }
    .hp-brand-tag { font-size: 0.72rem; color: #c9a227; letter-spacing: 0.4px; }
    .hp-nav { padding: 12px 10px; display: flex; flex-direction: column; gap: 4px; }
    .hp-nav-group {
      font-size: 0.72rem;
      text-transform: uppercase;
      color: #94a3b8;
      padding: 12px 12px 4px;
      font-weight: 600;
    }
    .hp-nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 12px;
      border-radius: 8px;
      color: #cbd5e1;
      text-decoration: none;
      font-size: 0.92rem;
      transition: background 0.15s, color 0.15s;
    }
    .hp-nav-item:hover { background: rgba(255, 255, 255, 0.08); color: #fff; }
    .hp-nav-active {
      background: rgba(201, 162, 39, 0.16) !important;
      color: #e4c766 !important;
      font-weight: 600;
    }
    .hp-main { background: #f4f6f9; display: flex; flex-direction: column; }
    .hp-topbar {
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
      position: sticky;
      top: 0;
      z-index: 10;
    }
    .hp-topbar-left, .hp-topbar-right { display: flex; align-items: center; gap: 10px; }
    .hp-topbar-subtitle { font-size: 0.95rem; font-weight: 600; color: #0f2540; }
    .hp-user-name { font-size: 0.9rem; font-weight: 600; color: #0f2540; cursor: pointer; }
    .hp-lang-btn { border-color: #cbd5e1 !important; color: #0f2540 !important; }
    .hp-content { padding: 24px; flex: 1; }
    @media (max-width: 768px) {
      .hp-topbar-subtitle, .hp-user-name { display: none; }
      .hp-content { padding: 14px; }
    }
  `]
})
export class MainLayoutComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(TranslationService);
  private readonly dashboardReports = inject(DashboardReportsService);

  readonly isMobile = signal(window.innerWidth < 900);
  readonly sidebarOpen = signal(window.innerWidth >= 900);
  readonly collapsed = signal(false);
  readonly notifications = signal<NotificationDto[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter((n) => !n.isRead).length);

  readonly navGroups: NavGroup[] = [
    {
      items: [
        { labelKey: 'nav.dashboard', icon: 'dashboard', route: '/dashboard', permission: 'Dashboard.View' }
      ]
    },
    {
      titleKey: 'nav.operations',
      items: [
        { labelKey: 'nav.reservations', icon: 'event_note', route: '/reservations', permission: 'Reservations.View' },
        { labelKey: 'nav.calendar', icon: 'calendar_month', route: '/calendar', permission: 'Reservations.View' },
        { labelKey: 'nav.checkIn', icon: 'front_hand', route: '/front-desk', permission: 'Reservations.CheckIn' },
        { labelKey: 'nav.guestServices', icon: 'room_service', route: '/guest-services', permission: 'GuestServices.View' },
        { labelKey: 'nav.housekeeping', icon: 'cleaning_services', route: '/housekeeping', permission: 'Housekeeping.View' },
        { labelKey: 'nav.maintenance', icon: 'build', route: '/maintenance', permission: 'Maintenance.View' },
        { labelKey: 'nav.rooms', icon: 'meeting_room', route: '/rooms', permission: 'Rooms.View' },
        { labelKey: 'nav.guests', icon: 'groups', route: '/guests', permission: 'Guests.View' }
      ]
    },
    {
      titleKey: 'nav.finance',
      items: [
        { labelKey: 'nav.billing', icon: 'receipt_long', route: '/billing', permission: 'Billing.View' },
        { labelKey: 'nav.finance', icon: 'account_balance', route: '/finance', permission: 'Finance.View' },
        { labelKey: 'nav.rates', icon: 'sell', route: '/rates', permission: 'Rates.View' }
      ]
    },
    {
      titleKey: 'nav.hrInventory',
      items: [
        { labelKey: 'nav.employees', icon: 'badge', route: '/employees', permission: 'Employees.View' },
        { labelKey: 'nav.inventory', icon: 'inventory_2', route: '/inventory', permission: 'Inventory.View' },
        { labelKey: 'nav.reports', icon: 'analytics', route: '/reports', permission: 'Reports.ViewReports' }
      ]
    },
    {
      titleKey: 'nav.admin',
      items: [
        { labelKey: 'nav.rates', icon: 'hotel_class', route: '/room-types', permission: 'Rooms.View' },
        { labelKey: 'nav.users', icon: 'manage_accounts', route: '/users', permission: 'Users.View' },
        { labelKey: 'nav.settings', icon: 'settings', route: '/settings', permission: 'Settings.View' },
        { labelKey: 'nav.roles', icon: 'admin_panel_settings', route: '/roles', permission: 'Roles.View' }
      ]
    }
  ];

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.dashboardReports.getNotifications().subscribe({
      next: (items) => this.notifications.set(items),
      error: () => {}
    });
  }

  markRead(n: NotificationDto): void {
    if (n.isRead) return;
    this.dashboardReports.markAsRead(n.id).subscribe({
      next: () => {
        this.notifications.update((list) =>
          list.map((item) => (item.id === n.id ? { ...item, isRead: true } : item))
        );
      }
    });
  }

  markAllRead(): void {
    this.dashboardReports.markAllAsRead().subscribe({
      next: () => {
        this.notifications.update((list) => list.map((item) => ({ ...item, isRead: true })));
      }
    });
  }

  @HostListener('window:resize')
  onResize(): void {
    const mobile = window.innerWidth < 900;
    this.isMobile.set(mobile);
    this.sidebarOpen.set(!mobile);
  }

  toggleSidebar(): void {
    if (this.isMobile()) {
      this.sidebarOpen.update((v) => !v);
    } else {
      this.collapsed.update((v) => !v);
    }
  }
}

