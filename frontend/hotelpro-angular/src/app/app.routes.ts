import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/auth/auth.guards';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'reservations',
        canActivate: [permissionGuard('Reservations.View')],
        loadComponent: () => import('./features/reservations/reservations-list.component').then((m) => m.ReservationsListComponent)
      },
      {
        path: 'calendar',
        canActivate: [permissionGuard('Reservations.View')],
        loadComponent: () => import('./features/reservations/booking-calendar.component').then((m) => m.BookingCalendarComponent)
      },
      {
        path: 'front-desk',
        canActivate: [permissionGuard('Reservations.CheckIn')],
        loadComponent: () => import('./features/front-desk/front-desk.component').then((m) => m.FrontDeskComponent)
      },
      {
        path: 'guest-services',
        canActivate: [permissionGuard('GuestServices.View')],
        loadComponent: () => import('./features/guest-services/guest-services.component').then((m) => m.GuestServicesComponent)
      },
      {
        path: 'housekeeping',
        canActivate: [permissionGuard('Housekeeping.View')],
        loadComponent: () => import('./features/housekeeping/housekeeping.component').then((m) => m.HousekeepingComponent)
      },
      {
        path: 'maintenance',
        canActivate: [permissionGuard('Maintenance.View')],
        loadComponent: () => import('./features/maintenance/maintenance.component').then((m) => m.MaintenanceComponent)
      },
      {
        path: 'rates',
        canActivate: [permissionGuard('Rates.View')],
        loadComponent: () => import('./features/rates/rate-plans.component').then((m) => m.RatePlansComponent)
      },
      {
        path: 'billing',
        canActivate: [permissionGuard('Billing.View')],
        loadComponent: () => import('./features/billing/billing.component').then((m) => m.BillingComponent)
      },
      {
        path: 'finance',
        canActivate: [permissionGuard('Finance.View')],
        loadComponent: () => import('./features/finance/finance.component').then((m) => m.FinanceComponent)
      },
      {
        path: 'employees',
        canActivate: [permissionGuard('Employees.View')],
        loadComponent: () => import('./features/employees/employees.component').then((m) => m.EmployeesComponent)
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard('Inventory.View')],
        loadComponent: () => import('./features/inventory/inventory.component').then((m) => m.InventoryComponent)
      },
      {
        path: 'reports',
        canActivate: [permissionGuard('Reports.ViewReports')],
        loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent)
      },
      {
        path: 'rooms',
        canActivate: [permissionGuard('Rooms.View')],
        loadComponent: () => import('./features/rooms/rooms-list.component').then((m) => m.RoomsListComponent)
      },
      {
        path: 'room-types',
        canActivate: [permissionGuard('Rooms.View')],
        loadComponent: () => import('./features/rooms/room-types.component').then((m) => m.RoomTypesComponent)
      },
      {
        path: 'guests',
        canActivate: [permissionGuard('Guests.View')],
        loadComponent: () => import('./features/guests/guests-list.component').then((m) => m.GuestsListComponent)
      },
      {
        path: 'settings',
        canActivate: [permissionGuard('Settings.View')],
        loadComponent: () => import('./features/settings/hotel-settings.component').then((m) => m.HotelSettingsComponent)
      },
      {
        path: 'users',
        canActivate: [permissionGuard('Users.View')],
        loadComponent: () => import('./features/users-roles/users-list/users-list.component').then((m) => m.UsersListComponent)
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('Roles.View')],
        loadComponent: () => import('./features/users-roles/roles-list/roles-list.component').then((m) => m.RolesListComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
