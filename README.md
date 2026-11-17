# HotelPro — Enterprise Hotel Management System

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core%2010-CC2927?logo=microsoftsqlserver)](https://learn.microsoft.com/ef/core/)
[![TypeScript](https://img.shields.io/badge/TypeScript-Strict-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-0F2540)](#architecture)

**HotelPro** is a full-stack, enterprise-grade **Hotel Management System (HMS)** engineered with **ASP.NET Core 10 Web API** (Clean Architecture), **Entity Framework Core + SQL Server**, and **Angular 21** (Standalone Components, Angular Material, Signals, and bilingual **Arabic/English** UI with automatic **RTL/LTR** layout switching).

Designed to handle real-world hospitality workflows end-to-end—from reservations and dynamic pricing to front-desk check-in/out, housekeeping, maintenance, idempotent billing, HR, inventory control, and executive analytics.

---

## Key Engineering Highlights

- **Clean Architecture & Separation of Concerns**: Strict layering across `Domain`, `Application`, `Infrastructure`, `API`, and `Tests` projects with zero circular dependencies.
- **Fine-Grained RBAC & JWT Security**:
  - **9 predefined operational roles** and **50+ granular permissions** (`PermissionsCatalog`) enforced via custom `PermissionAuthorizationHandler` and `PermissionPolicyProvider` on the backend, and route guards + `*hasPermission` structural directive on the frontend.
  - **JWT Bearer Authentication + Refresh Token Rotation** with reuse detection and revocation tracking, plus `AuditLog` tracking for critical actions.
  - Centralized `GlobalExceptionHandlerMiddleware` returning RFC-compliant error responses without leaking internal stack traces.
- **Transactional Integrity & Concurrency Safety**:
  - **Double-booking prevention**: Room reservation creation and modifications execute inside `IsolationLevel.Serializable` database transactions with explicit overlap checks and locked nightly rates.
  - **Idempotent financial transactions**: Payments support `IdempotencyKey` deduplication and immutable reversal records inside `Serializable` transactions.
  - **Ledger-backed inventory**: Stock quantities are mutated exclusively through `StockTransaction` records and Purchase Order receipts with insufficient-stock guards.
- **Modular EF Core Migrations**: Database schema evolution is cleanly partitioned into **7 module-based migrations** (Identity, Hotel/Rooms/Guests, Rates/Reservations, Operations, Billing/Finance, HR/Inventory, Notifications) for maintainability and targeted rollbacks.
- **Modern Angular 21 Frontend**:
  - 100% **Standalone Components** with lazy-loaded feature routes and reactive **Angular Signals** state management.
  - Custom **Navy (`#0F2540`) / Gold (`#C9A227`) / White** hospitality theme built on Angular Material.
  - Full **Arabic / English (`AR/EN`)** internationalization (`TranslationService`) with live **RTL / LTR** directionality switching.
  - Multiple operational views including Table, Card Grid, Interactive Floor Map, and Booking Calendar.
- **Automated Integration Testing**: End-to-end API integration test suite (`HotelPro.Tests`) powered by `WebApplicationFactory<Program>` covering security, RBAC, reservations, operations, billing, HR, inventory, and reporting.

---

## Architecture

```text
HotelPro
├── backend/
│   ├── HotelPro.Domain/           # Entities, Enums, Domain Constants (PermissionsCatalog, RoleNames)
│   ├── HotelPro.Application/      # Service Interfaces, DTOs, Business Contracts
│   ├── HotelPro.Infrastructure/   # EF Core DbContext, Migrations, Identity/JWT, Service Implementations, DbInitializer
│   ├── HotelPro.API/              # REST Controllers, Authorization Policies, Exception Middleware, Swagger
│   └── HotelPro.Tests/            # Integration Tests (WebApplicationFactory<Program>)
├── frontend/
│   └── hotelpro-angular/          # Angular 21 SPA (Standalone Components, Signals, Material, AR/EN i18n)
├── docs/                          # System Architecture & Design Documentation
└── README.md
```

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | ASP.NET Core 10 Web API, C#, Clean Architecture, Swashbuckle (Swagger/OpenAPI) |
| **Data & ORM** | Entity Framework Core 10, SQL Server (`LocalDB` / SQL Server), Code-First Modular Migrations |
| **Security** | ASP.NET Core Identity, JWT Bearer, Refresh Token Rotation, Custom Policy-Based RBAC |
| **Frontend** | Angular 21, TypeScript, Angular Material 21, Angular Signals, RxJS, HttpClient Interceptors |
| **i18n & UX** | Custom AR/EN Translation Engine, Dynamic RTL/LTR Layout, Responsive Sidenav & Topbar Notifications |
| **Testing** | xUnit, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), EF Core InMemory |

---

## System Modules & Features

1. **Identity, RBAC & Audit**:
   - User management, 9 operational roles (`SuperAdmin`, `HotelManager`, `Receptionist`, `Accountant`, `HousekeepingSupervisor`, `HousekeepingStaff`, `MaintenanceManager`, `MaintenanceTechnician`, `ServiceStaff`), 50+ permissions, and audit logging.
2. **Hotel Setup, Rooms & Guests**:
   - Hotel profile & tax/currency configuration, Room Types, Amenities, Rooms (with Table, Card, and **Floor Map** views), and Guest profiles with document tracking.
3. **Dynamic Rates, Reservations & Calendar**:
   - Seasonal rate plans, weekend multipliers, long-stay discounts, live quote calculator, overlap-safe multi-room reservations, status history tracking, and an interactive **Booking Calendar**.
4. **Hotel Operations**:
   - **Front Desk**: Check-In / Check-Out workflows with room status synchronization.
   - **Guest Services**: Service catalog & guest service orders.
   - **Housekeeping**: Room cleaning task assignment and status tracking.
   - **Maintenance**: Maintenance requests, technician assignment, and room restoration (`OutOfService` management).
5. **Billing, Payments & Finance**:
   - Folio/Invoice generation with line items, idempotent payment processing & payment reversals, expense categories, operating expenses, and revenue tracking.
6. **HR & Inventory Management**:
   - **HR**: Departments, employee directory, attendance records, and leave request approvals.
   - **Inventory**: Categories, stock items, suppliers, purchase orders, PO receiving, and auditable stock transactions.
7. **Executive Dashboard, Reports & Notifications**:
   - Live KPIs (occupancy rate excluding `OutOfService` rooms, ADR, revenue, active reservations), charts, Occupancy/Reservations/Financial reports with **UTF-8 CSV export**, and a real-time **Topbar Notification Bell** with unread counter and mark-as-read support.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (`10.0+`)
- [Node.js](https://nodejs.org/) (`20+`) & npm
- SQL Server / SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)

### 1. Configure Secrets & Database

Default SQL Server LocalDB connection string is configured in `backend/HotelPro.API/appsettings.json`.
For local development, configure your JWT secret via .NET User Secrets (never commit production secrets):

```powershell
cd backend/HotelPro.API
dotnet user-secrets set "Jwt:SecretKey" "YOUR_STRONG_SECRET_KEY_AT_LEAST_32_CHARS"
```

### 2. Apply EF Core Migrations

```powershell
cd backend
dotnet ef database update --project HotelPro.Infrastructure --startup-project HotelPro.API
```

### 3. Run the Backend API

```powershell
cd backend/HotelPro.API
dotnet run
```
- **API Base URL**: `http://localhost:5296`
- **Swagger UI (Development)**: `http://localhost:5296/swagger`

### 4. Run the Angular Frontend

```powershell
cd frontend/hotelpro-angular
npm install
npm start
```
- **Frontend URL**: `http://localhost:4200`

---

## Verification & Testing

### Backend Integration Tests
```powershell
cd backend
dotnet test
```

### Frontend Type Check & Production Build
```powershell
cd frontend/hotelpro-angular
npx tsc --noEmit -p tsconfig.app.json
npm run build
```

---

## Development Demo Accounts

When running in `Development` mode, `DbInitializer` seeds sample accounts for testing role-based access across all 9 roles:

| Role | Email | Password |
|---|---|---|
| `SuperAdmin` | `superadmin@hotelpro.local` | `Admin@12345` |
| `HotelManager` | `manager@hotelpro.local` | `Manager@12345` |
| `Receptionist` | `reception@hotelpro.local` | `Reception@12345` |
| `Accountant` | `accountant@hotelpro.local` | `Accountant@12345` |
| `HousekeepingSupervisor` | `hk.supervisor@hotelpro.local` | `Housekeep@12345` |
| `HousekeepingStaff` | `hk.staff@hotelpro.local` | `Housekeep@12345` |
| `MaintenanceManager` | `maint.manager@hotelpro.local` | `Maint@12345` |
| `MaintenanceTechnician` | `maint.tech@hotelpro.local` | `Maint@12345` |
| `ServiceStaff` | `service@hotelpro.local` | `Service@12345` |
