# HotelPro – Phase 1: System Analysis, Architecture & Database Design

## 1. Environment Inspection
- **Workspace**: `c:\Users\ALBAHA\Desktop\hotel` (clean workspace)
- **.NET SDK**: `10.0.400`
- **Node.js**: `v25.7.0`
- **npm**: `11.19.0`
- **Angular CLI**: `21.2.19`
- **Database Target**: Microsoft SQL Server (connection string configurable via `appsettings.Development.json` / User Secrets)

---

## 2. Solution & Project Structure

```text
HotelPro/
├── backend/
│   ├── HotelPro.sln
│   ├── HotelPro.Domain/           # Entities, Enums, Common Base Classes, Domain Exceptions
│   ├── HotelPro.Application/      # DTOs, Service Interfaces, Use Cases, FluentValidation, Mapping
│   ├── HotelPro.Infrastructure/   # EF Core DbContext, Configurations, Migrations, Identity, JWT, SeedData, Export/Email
│   ├── HotelPro.API/              # Controllers, Middleware, Permission Policies, SignalR Hub, Swagger
│   └── HotelPro.Tests/            # xUnit Unit Tests & Integration Tests
├── frontend/
│   └── hotelpro-angular/          # Angular Standalone Components, Material + Custom Navy/Gold Theme, i18n (AR/EN, RTL/LTR)
├── database/                      # Reference SQL / Schema notes
├── docs/                          # Architecture, DB, API documentation
└── README.md
```

### Angular Feature-Based Structure (`frontend/hotelpro-angular/src/app/`)
- `core/` — `auth/` (guards, JWT + refresh interceptor, auth service), `i18n/` (AR/EN translation service + RTL/LTR dir switcher), `services/` (notification SignalR, toast, hotel settings, API base), `models/`
- `shared/` — reusable data-table, confirm-dialog, status-badge, empty-state, loading-spinner, currency/date pipes
- `layout/` — collapsible sidebar, topbar (user menu, notifications bell, language toggle), main shell
- `features/` — `dashboard`, `rooms`, `guests`, `reservations`, `booking-calendar`, `check-in`, `check-out`, `guest-services`, `housekeeping`, `maintenance`, `employees`, `users-roles`, `billing`, `finance`, `rates`, `inventory`, `reports`, `settings`

---

## 3. Database Design (SQL Server + EF Core)

### Design Rules
1. **Monetary precision**: `decimal(18,2)` for all amounts; `decimal(5,2)` for tax/discount rates.
2. **Temporal consistency**: `DateTimeOffset` for audit/event timestamps; `DateOnly` for stay dates (`CheckInDate`, `CheckOutDate`).
3. **Concurrency & Integrity**: `RowVersion` (`byte[]` timestamp) + `IsolationLevel.Serializable` transactions for reservation booking/room assignment and financial settlements; `IdempotencyKey` on `Payment`.
4. **No hard delete on financial records**: `Invoice`, `Payment`, `Expense`, `RevenueRecord`, `StockTransaction` are immutable/reversed via credit/reversal entries, never deleted.
5. **Soft Delete**: Applied to master entities (`Room`, `RoomType`, `Guest`, `Employee`, `GuestService`, `InventoryItem`, `Supplier`) via `IsDeleted`, `DeletedAt`, `DeletedBy`.

### Core Entities & Relationships

#### A. Identity, Security & Audit
- `ApplicationUser` (`IdentityUser<Guid>`): `FullName`, `EmployeeId?`, `IsActive`, `PreferredLanguage`, `CreatedAt`
- `ApplicationRole` (`IdentityRole<Guid>`): `Description`, `IsSystemRole`
- `Permission`: `Id`, `Code` (Unique, e.g. `Rooms.View`, `Reservations.Create`), `Module`, `Action`, `Description`
- `RolePermission`: `RoleId` (FK), `PermissionId` (FK) — Composite PK
- `RefreshToken`: `Id`, `UserId` (FK), `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt?`, `ReplacedByTokenHash?`, `CreatedByIp`
- `AuditLog`: `Id`, `UserId?`, `UserName`, `Action`, `EntityName`, `EntityId`, `OldValuesJson?`, `NewValuesJson?`, `IpAddress`, `Timestamp`

#### B. Hotel & Configuration
- `Hotel`: `Id`, `Name`, `LegalName`, `LogoUrl`, `Address`, `Phone`, `Email`, `TaxNumber`
- `HotelSettings`: `Id`, `HotelId` (FK, Unique), `CurrencyCode` (e.g. `USD`, `SAR`), `CurrencySymbol`, `TimeZoneId`, `DateFormat`, `DefaultLanguage` (`ar`/`en`), `DefaultCheckInTime`, `DefaultCheckOutTime`, `TaxRatePercent`, `ServiceChargePercent`, `RequireInspectionAfterCleaning`, `AllowCheckoutWithBalancePermission`, `CancellationPolicyHours`, `CancellationFeePercent`

#### C. Rooms & Rates
- `RoomType`: `Id`, `HotelId` (FK), `Name`, `Description`, `BasePrice`, `MaxAdults`, `MaxChildren`, `BedCount`, `AreaSqm`, `IsActive`
- `Amenity`: `Id`, `Name`, `Icon`, `Category`
- `RoomTypeAmenity` / `RoomAmenity`: join tables
- `Room`: `Id`, `HotelId` (FK), `RoomTypeId` (FK), `RoomNumber` (Unique per Hotel), `Floor`, `MaxCapacity`, `BedCount`, `BasePriceOverride?`, `Status` (`Available`, `Reserved`, `Occupied`, `Cleaning`, `Maintenance`, `OutOfService`), `Description`, `Notes`, `ImageUrlsJson`, `RowVersion`
- `RatePlan`: `Id`, `HotelId` (FK), `Name`, `Code`, `IsRefundable`, `WeekendMultiplier`, `LongStayMinNights`, `LongStayDiscountPercent`, `CorporateCode?`, `IsActive`
- `RoomRate`: `Id`, `RoomTypeId` (FK), `RatePlanId` (FK), `StartDate`, `EndDate`, `PricePerNight`, `Priority`

#### D. Guests
- `Guest`: `Id`, `FullName`, `Phone` (Indexed), `Email` (Indexed), `Nationality`, `DateOfBirth?`, `Address`, `Notes`, `IsVip`, `CreatedAt`, `IsDeleted`
- `GuestDocument`: `Id`, `GuestId` (FK), `DocumentType` (`NationalId`, `Passport`, `DriverLicense`, `Other`), `DocumentNumber` (Indexed), `IssuingCountry`, `ExpiryDate?`, `FilePath?`

#### E. Reservations & Front Desk Operations
- `Reservation`: `Id`, `ReservationNumber` (Unique), `HotelId` (FK), `PrimaryGuestId` (FK), `RatePlanId` (FK), `CheckInDate`, `CheckOutDate`, `Nights`, `AdultsCount`, `ChildrenCount`, `Source` (`Direct`, `Phone`, `Website`, `TravelAgent`, `WalkIn`, `Other`), `Status` (`Pending`, `Confirmed`, `CheckedIn`, `CheckedOut`, `Cancelled`, `NoShow`), `SubTotal`, `DiscountAmount`, `TaxAmount`, `ServiceChargeAmount`, `TotalAmount`, `DepositRequired`, `Notes`, `CancellationReason?`, `RowVersion`, `CreatedAt`, `CreatedBy`
- `ReservationRoom`: `Id`, `ReservationId` (FK), `RoomId` (FK), `RoomTypeId` (FK), `AgreedNightlyRate`, `Nights`, `TotalRoomAmount`, `CheckInDate`, `CheckOutDate`
- `ReservationGuest`: `ReservationId` (FK), `GuestId` (FK), `IsPrimary`
- `ReservationStatusHistory`: `Id`, `ReservationId` (FK), `FromStatus`, `ToStatus`, `Reason?`, `ChangedBy`, `ChangedAt`
- `CheckInRecord`: `Id`, `ReservationId` (FK), `RoomId` (FK), `ActualCheckInTime`, `ActualAdults`, `ActualChildren`, `DocumentsVerified`, `Notes`, `PerformedByUserId`
- `CheckOutRecord`: `Id`, `ReservationId` (FK), `RoomId` (FK), `ActualCheckOutTime`, `FinalBalanceSettled`, `ExceptionalBalanceApprovedBy?`, `Notes`, `PerformedByUserId`

#### F. Guest Services
- `GuestService`: `Id`, `Name`, `Description`, `Category` (`RoomService`, `Restaurant`, `Laundry`, `Spa`, `Transportation`, `AirportPickup`, `MiniBar`, `ExtraBed`, `Other`), `DepartmentId` (FK), `Price`, `Taxable`, `IsActive`
- `ServiceOrder`: `Id`, `OrderNumber` (Unique), `ReservationId` (FK), `RoomId` (FK), `GuestId` (FK), `Status` (`Pending`, `Accepted`, `InProgress`, `Completed`, `Cancelled`), `SubTotal`, `TaxAmount`, `TotalAmount`, `AssignedEmployeeId?` (FK), `RequestedAt`, `CompletedAt?`, `Notes`
- `ServiceOrderItem`: `Id`, `ServiceOrderId` (FK), `GuestServiceId` (FK), `Quantity`, `UnitPrice`, `TaxAmount`, `LineTotal`

#### G. Housekeeping & Maintenance
- `HousekeepingTask`: `Id`, `RoomId` (FK), `TaskType` (`CheckoutCleaning`, `DailyCleaning`, `DeepCleaning`, `Inspection`), `Priority` (`Low`, `Normal`, `High`, `Urgent`), `Status` (`Pending`, `Assigned`, `InProgress`, `Completed`, `Inspected`), `AssignedEmployeeId?` (FK), `InspectedByEmployeeId?` (FK), `ScheduledFor`, `StartedAt?`, `CompletedAt?`, `InspectedAt?`, `Notes`, `DamageReported`
- `MaintenanceRequest`: `Id`, `RequestNumber` (Unique), `RoomId?` (FK), `DepartmentId?` (FK), `IssueType`, `Description`, `Priority` (`Low`, `Normal`, `High`, `Critical`), `Status` (`Open`, `Assigned`, `InProgress`, `WaitingParts`, `Completed`, `Closed`), `AssignedTechnicianId?` (FK), `TakeRoomOutOfService`, `SparePartsUsed`, `Cost`, `CreatedAt`, `CompletedAt?`

#### H. HR & Employees
- `Department`: `Id`, `Name`, `Code` (Unique), `Description`
- `Employee`: `Id`, `EmployeeCode` (Unique), `FullName`, `Phone`, `Email`, `JobTitle`, `DepartmentId` (FK), `HireDate`, `BaseSalary`, `Status` (`Active`, `OnLeave`, `Suspended`, `Terminated`), `EmergencyContactName`, `EmergencyContactPhone`, `UserId?` (FK)
- `AttendanceRecord`: `Id`, `EmployeeId` (FK), `Date`, `CheckInTime?`, `CheckOutTime?`, `Status` (`Present`, `Absent`, `Late`, `HalfDay`, `OnLeave`), `Notes`
- `LeaveRequest`: `Id`, `EmployeeId` (FK), `LeaveType`, `StartDate`, `EndDate`, `Status` (`Pending`, `Approved`, `Rejected`), `ApprovedByUserId?`, `Reason`

#### I. Billing, Payments & Finance
- `Invoice`: `Id`, `InvoiceNumber` (Unique), `ReservationId` (FK), `GuestId` (FK), `IssueDate`, `DueDate`, `AccommodationTotal`, `ServicesTotal`, `DiscountTotal`, `TaxTotal`, `ServiceChargeTotal`, `GrandTotal`, `PaidAmount`, `BalanceDue`, `Status` (`Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Cancelled`), `CreatedBy`
- `InvoiceItem`: `Id`, `InvoiceId` (FK), `ItemType` (`RoomCharge`, `ServiceCharge`, `Tax`, `Fee`, `Discount`), `Description`, `Quantity`, `UnitPrice`, `Total`
- `Payment`: `Id`, `ReceiptNumber` (Unique), `InvoiceId` (FK), `ReservationId` (FK), `Amount`, `PaymentMethod` (`Cash`, `BankTransfer`, `Card`, `Other`), `ReferenceNumber?`, `PaymentDate`, `IsReversal`, `ReversedPaymentId?`, `IdempotencyKey` (Unique), `RecordedByUserId`, `Notes`
- `ExpenseCategory`: `Id`, `Name`, `Description`
- `Expense`: `Id`, `ExpenseNumber` (Unique), `CategoryId` (FK), `Description`, `Amount`, `ExpenseDate`, `PaymentMethod`, `RecordedByUserId`, `AttachmentPath?`, `Notes`
- `RevenueRecord`: `Id`, `Source` (`Rooms`, `Services`, `Restaurant`, `Other`), `ReferenceId?`, `Description`, `Amount`, `RecognizedDate`, `RecordedByUserId`

#### J. Inventory & Purchasing
- `InventoryCategory`: `Id`, `Name`
- `InventoryItem`: `Id`, `Sku` (Unique), `Name`, `CategoryId` (FK), `UnitOfMeasure`, `CurrentStock`, `ReorderLevel`, `UnitCost`, `IsActive`
- `Supplier`: `Id`, `Name`, `ContactPerson`, `Phone`, `Email`, `Address`, `IsActive`
- `PurchaseOrder`: `Id`, `OrderNumber` (Unique), `SupplierId` (FK), `OrderDate`, `ExpectedDate?`, `Status` (`Draft`, `Ordered`, `Received`, `Cancelled`), `TotalAmount`, `CreatedBy`
- `PurchaseOrderItem`: `Id`, `PurchaseOrderId` (FK), `InventoryItemId` (FK), `QuantityOrdered`, `QuantityReceived`, `UnitCost`, `LineTotal`
- `StockTransaction`: `Id`, `InventoryItemId` (FK), `TransactionType` (`PurchaseIn`, `DepartmentIssue`, `AdjustmentIn`, `AdjustmentOut`), `Quantity`, `BalanceAfter`, `DepartmentId?` (FK), `ReferenceNumber?`, `Notes`, `PerformedByUserId`, `TransactionDate`

#### K. Notifications
- `Notification`: `Id`, `TargetUserId?` (FK), `TargetRole?`, `Title`, `Message`, `Type`, `RelatedEntityType?`, `RelatedEntityId?`, `IsRead`, `CreatedAt`

---

## 4. Roles & Permissions Matrix

### Roles (9)
1. `SuperAdmin`
2. `HotelManager`
3. `Receptionist`
4. `Accountant`
5. `HousekeepingSupervisor`
6. `HousekeepingStaff`
7. `MaintenanceManager`
8. `MaintenanceTechnician`
9. `ServiceStaff`

### Core Permission Actions
`View`, `Create`, `Edit`, `Delete`, `Approve`, `Cancel`, `CheckIn`, `CheckOut`, `ManagePayments`, `ViewReports`, `ManageUsers`, `ManageSettings` (applied as `<Module>.<Action>` claims, stored in DB and editable via UI for `SuperAdmin` / `HotelManager`).

---

## 5. API Contracts (`/api/v1/...`)
- `/api/v1/auth`: `login`, `refresh`, `logout`, `me`, `change-password`
- `/api/v1/users` & `/api/v1/roles`: CRUD, assign roles, update role permissions
- `/api/v1/hotel-settings`: `GET`, `PUT`
- `/api/v1/room-types`, `/api/v1/rooms`, `/api/v1/amenities`: CRUD, status transitions, occupancy history, availability search
- `/api/v1/rate-plans`, `/api/v1/room-rates`: CRUD, quote calculation
- `/api/v1/guests`: CRUD, search, stays/invoices/services history
- `/api/v1/reservations`: CRUD, confirm, cancel, change-room, extend/shorten stay, calendar view
- `/api/v1/check-in` & `/api/v1/check-out`: front-desk workflows
- `/api/v1/guest-services` & `/api/v1/service-orders`: catalog + order lifecycle
- `/api/v1/housekeeping`: tasks, assign, start, complete, inspect
- `/api/v1/maintenance`: requests, assign technician, log parts/cost, close
- `/api/v1/employees`, `/api/v1/departments`, `/api/v1/attendance`, `/api/v1/leaves`
- `/api/v1/invoices` & `/api/v1/payments`: issue, partial/full payment, reversal, PDF receipt
- `/api/v1/finance`: expenses, revenues, P&L summary
- `/api/v1/inventory`, `/api/v1/suppliers`, `/api/v1/purchase-orders`
- `/api/v1/dashboard` & `/api/v1/reports`: KPIs, charts, PDF/Excel exports
- `/api/v1/notifications` + SignalR `/hubs/notifications`
- `/api/v1/audit-logs`: paginated audit trail

---

## 6. 10-Phase Execution Roadmap
1. **Phase 1 (Current)**: Environment check, analysis, DB & architecture design, execution plan.
2. **Phase 2**: Backend solution setup (Clean Architecture + EF Core + SQL Server + Identity + JWT + Refresh Tokens + RBAC + Swagger) & Angular workspace setup (Material + Navy/Gold theme + Auth + Layout + AR/EN RTL/LTR).
3. **Phase 3**: Hotel Settings, Room Types, Rooms, Amenities, Guests.
4. **Phase 4**: Rate Management, Reservation Engine (overlap prevention + transactions), Booking Calendar.
5. **Phase 5**: Check-in, Check-out, Guest Services, Housekeeping, Maintenance.
6. **Phase 6**: Billing & Invoices, Payments (idempotency + reversals), Finance (Revenues & Expenses).
7. **Phase 7**: Employees, Departments, Attendance/Leaves, Inventory, Suppliers & Purchase Orders.
8. **Phase 8**: Dashboard KPIs & Charts, Reports Center (PDF & Excel export), Notifications (DB + SignalR).
9. **Phase 9**: Unit & Integration Tests (Backend + Frontend), security & performance review, E2E business scenario verification.
10. **Phase 10**: Final Polish, Migrations, Demo Seed Data, README & API documentation.

