# مخطط قاعدة البيانات الكامل — HotelPro ERD (Entity-Relationship Diagram)

هذا المستند يوثق **البنية الكاملة لقاعدة بيانات نظام HotelPro** (`HotelProDb`) المستخرجة مباشرة من نموذج **Entity Framework Core 10** (`HotelProDbContextModelSnapshot.cs`)، وتشمل **جميع الجداول (48 جدولاً)**، كافة الأعمدة، أنواع البيانات، المفاتيح الأساسية (`PK`)، المفاتيح الأجنبية (`FK`)، القيود الفريدة (`UK`)، والعلاقات بين الوحدات.

---

## نظرة عامة على التصميم

- **عدد الجداول الإجمالي**: **48 جدولاً** (43 جدولاً مجالياً + 5 جداول قياسية لـ ASP.NET Core Identity).
- **المفاتيح الأساسية (`PK`)**: جميع الكيانات المجالية تستخدم `uniqueidentifier` (`Guid`) ما عدا `AspNetRoleClaims` و`AspNetUserClaims` (`int`).
- **الحذف الناعم (Soft Delete)**: مطبق على الكيانات الحساسة (`Guests`, `Rooms`, `RoomTypes`, `RatePlans`, `GuestServices`, `Employees`, `InventoryItems`, `Suppliers`) عبر `IsDeleted`, `DeletedAt`, `DeletedBy`.
- **التحكم بالتزامن (Concurrency)**: مطبق عبر `RowVersion` (`byte[]`) على `Rooms` و`Reservations` لمنع التعارض.
- **التدقيق (Auditing)**: حقول `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` بالإضافة إلى جدول `AuditLogs`.

---

## 1. المخطط الشامل للنظام (Master System ERD)

يوضح هذا المخطط الترابط الكلي بين جميع وحدات النظام الـ 7:

```mermaid
erDiagram
    %% Identity & HR
    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "assigned"
    AspNetRoles ||--o{ RolePermissions : "grants"
    Permissions ||--o{ RolePermissions : "included in"
    AspNetUsers ||--o{ RefreshTokens : "owns"
    Departments ||--o{ Employees : "employs"
    Employees ||--o{ AttendanceRecords : "logs"
    Employees ||--o{ LeaveRequests : "requests"
    Employees |o--o| AspNetUsers : "linked user"

    %% Hotel, Rooms & Guests
    Hotels ||--|| HotelSettings : "configured by"
    Hotels ||--o{ RoomTypes : "defines"
    Hotels ||--o{ Rooms : "contains"
    Hotels ||--o{ RatePlans : "offers"
    Hotels ||--o{ Reservations : "hosts"
    RoomTypes ||--o{ Rooms : "categorizes"
    RoomTypes ||--o{ RoomTypeAmenities : "includes"
    Amenities ||--o{ RoomTypeAmenities : "in"
    Rooms ||--o{ RoomAmenities : "has"
    Amenities ||--o{ RoomAmenities : "in"
    Guests ||--o{ GuestDocuments : "provides"

    %% Rates & Reservations
    RatePlans ||--o{ RoomRates : "prices"
    RoomTypes ||--o{ RoomRates : "priced by"
    Guests ||--o{ Reservations : "primary guest"
    RatePlans ||--o{ Reservations : "applied to"
    Reservations ||--o{ ReservationRooms : "books"
    Rooms ||--o{ ReservationRooms : "assigned"
    RoomTypes ||--o{ ReservationRooms : "type"
    Reservations ||--o{ ReservationGuests : "includes"
    Guests ||--o{ ReservationGuests : "stays in"
    Reservations ||--o{ ReservationStatusHistory : "tracks"

    %% Operations
    Reservations ||--o{ CheckInRecords : "checks in"
    Rooms ||--o{ CheckInRecords : "checked in"
    Reservations ||--o{ CheckOutRecords : "checks out"
    Rooms ||--o{ CheckOutRecords : "checked out"
    Reservations ||--o{ ServiceOrders : "orders"
    Rooms ||--o{ ServiceOrders : "for room"
    Guests ||--o{ ServiceOrders : "by guest"
    ServiceOrders ||--o{ ServiceOrderItems : "contains"
    GuestServices ||--o{ ServiceOrderItems : "ordered as"
    Rooms ||--o{ HousekeepingTasks : "cleaned via"
    Rooms ||--o{ MaintenanceRequests : "repaired via"

    %% Billing & Finance
    Reservations ||--o{ Invoices : "billed by"
    Guests ||--o{ Invoices : "billed to"
    Invoices ||--o{ InvoiceItems : "details"
    Invoices ||--o{ Payments : "settled by"
    Reservations ||--o{ Payments : "paid for"
    ExpenseCategories ||--o{ Expenses : "classifies"

    %% Inventory & Purchasing
    InventoryCategories ||--o{ InventoryItems : "groups"
    Suppliers ||--o{ PurchaseOrders : "supplies"
    PurchaseOrders ||--o{ PurchaseOrderItems : "lines"
    InventoryItems ||--o{ PurchaseOrderItems : "ordered"
    InventoryItems ||--o{ StockTransactions : "moves"
    Departments ||--o{ StockTransactions : "consumed by"
```

---

## 2. المخططات التفصيلية حسب الوحدات (كل الأعمدة والأنواع)

### الوحدة 1: الهوية والصلاحيات والأمان (Identity, RBAC & Audit)

```mermaid
erDiagram
    AspNetUsers {
        Guid Id PK
        string UserName
        string NormalizedUserName
        string Email
        string NormalizedEmail
        bool EmailConfirmed
        string PasswordHash
        string SecurityStamp
        string ConcurrencyStamp
        string PhoneNumber
        bool PhoneNumberConfirmed
        bool TwoFactorEnabled
        DateTimeOffset LockoutEnd
        bool LockoutEnabled
        int AccessFailedCount
        string FullName
        string PreferredLanguage
        bool IsActive
        Guid EmployeeId FK
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }

    AspNetRoles {
        Guid Id PK
        string Name
        string NormalizedName
        string ConcurrencyStamp
        string Description
        bool IsSystemRole
        DateTimeOffset CreatedAt
    }

    AspNetUserRoles {
        Guid UserId PK,FK
        Guid RoleId PK,FK
    }

    AspNetUserClaims {
        int Id PK
        Guid UserId FK
        string ClaimType
        string ClaimValue
    }

    AspNetRoleClaims {
        int Id PK
        Guid RoleId FK
        string ClaimType
        string ClaimValue
    }

    AspNetUserLogins {
        string LoginProvider PK
        string ProviderKey PK
        string ProviderDisplayName
        Guid UserId FK
    }

    AspNetUserTokens {
        Guid UserId PK,FK
        string LoginProvider PK
        string Name PK
        string Value
    }

    Permissions {
        Guid Id PK
        string Code UK
        string Module
        string Action
        string Description
    }

    RolePermissions {
        Guid RoleId PK,FK
        Guid PermissionId PK,FK
    }

    RefreshTokens {
        Guid Id PK
        Guid UserId FK
        string TokenHash
        DateTimeOffset ExpiresAt
        DateTimeOffset CreatedAt
        string CreatedByIp
        DateTimeOffset RevokedAt
        string RevokedByIp
        string ReplacedByTokenHash
        string ReasonRevoked
    }

    AuditLogs {
        Guid Id PK
        Guid UserId
        string UserName
        string Action
        string EntityName
        string EntityId
        string OldValuesJson
        string NewValuesJson
        string IpAddress
        DateTimeOffset Timestamp
    }

    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "has"
    AspNetUsers ||--o{ AspNetUserClaims : "has"
    AspNetRoles ||--o{ AspNetRoleClaims : "has"
    AspNetUsers ||--o{ AspNetUserLogins : "has"
    AspNetUsers ||--o{ AspNetUserTokens : "has"
    AspNetRoles ||--o{ RolePermissions : "has"
    Permissions ||--o{ RolePermissions : "granted in"
    AspNetUsers ||--o{ RefreshTokens : "owns"
```

---

### الوحدة 2: إعدادات الفندق والغرف والنزلاء (Hotel Setup, Rooms & Guests)

```mermaid
erDiagram
    Hotels {
        Guid Id PK
        string Name
        string LegalName
        string Address
        string Phone
        string Email
        string TaxNumber
        string LogoUrl
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    HotelSettings {
        Guid Id PK
        Guid HotelId PK,FK
        string CurrencyCode
        string CurrencySymbol
        decimal TaxRatePercent
        decimal ServiceChargePercent
        TimeOnly DefaultCheckInTime
        TimeOnly DefaultCheckOutTime
        int CancellationPolicyHours
        decimal CancellationFeePercent
        bool AllowCheckoutWithBalancePermission
        bool RequireInspectionAfterCleaning
        string DefaultLanguage
        string DateFormat
        string TimeZoneId
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    RoomTypes {
        Guid Id PK
        Guid HotelId FK
        string Name
        string Code
        string Description
        int MaxAdults
        int MaxChildren
        int BedCount
        decimal AreaSqm
        decimal BasePrice
        bool IsActive
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    Rooms {
        Guid Id PK
        Guid HotelId FK
        Guid RoomTypeId FK
        string RoomNumber UK
        int Floor
        int Status
        int MaxCapacity
        int BedCount
        decimal BasePriceOverride
        string Description
        string Notes
        string ImageUrlsJson
        bytes RowVersion
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    Amenities {
        Guid Id PK
        string Name
        string Icon
        string Category
    }

    RoomTypeAmenities {
        Guid RoomTypeId PK,FK
        Guid AmenityId PK,FK
    }

    RoomAmenities {
        Guid RoomId PK,FK
        Guid AmenityId PK,FK
    }

    Guests {
        Guid Id PK
        string FullName
        string Phone
        string Email
        string Nationality
        DateOnly DateOfBirth
        string Address
        bool IsVip
        string Notes
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    GuestDocuments {
        Guid Id PK
        Guid GuestId FK
        int DocumentType
        string DocumentNumber
        string IssuingCountry
        DateOnly ExpiryDate
    }

    Hotels ||--|| HotelSettings : "has settings"
    Hotels ||--o{ RoomTypes : "defines"
    Hotels ||--o{ Rooms : "owns"
    RoomTypes ||--o{ Rooms : "classifies"
    RoomTypes ||--o{ RoomTypeAmenities : "has"
    Amenities ||--o{ RoomTypeAmenities : "assigned to"
    Rooms ||--o{ RoomAmenities : "has"
    Amenities ||--o{ RoomAmenities : "assigned to"
    Guests ||--o{ GuestDocuments : "has"
```

---

### الوحدة 3: خطط الأسعار والحجوزات (Rates & Reservations)

```mermaid
erDiagram
    RatePlans {
        Guid Id PK
        Guid HotelId FK
        string Name
        string Code
        string Description
        bool IsRefundable
        decimal WeekendMultiplier
        int LongStayMinNights
        decimal LongStayDiscountPercent
        string CorporateCode
        bool IsActive
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    RoomRates {
        Guid Id PK
        Guid RatePlanId FK
        Guid RoomTypeId FK
        DateOnly StartDate
        DateOnly EndDate
        decimal PricePerNight
        int Priority
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    Reservations {
        Guid Id PK
        Guid HotelId FK
        string ReservationNumber UK
        Guid PrimaryGuestId FK
        Guid RatePlanId FK
        int Source
        int Status
        DateOnly CheckInDate
        DateOnly CheckOutDate
        int Nights
        int AdultsCount
        int ChildrenCount
        decimal SubTotal
        decimal DiscountAmount
        decimal TaxAmount
        decimal ServiceChargeAmount
        decimal TotalAmount
        decimal DepositRequired
        string Notes
        string CancellationReason
        bytes RowVersion
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    ReservationRooms {
        Guid Id PK
        Guid ReservationId FK
        Guid RoomId FK
        Guid RoomTypeId FK
        DateOnly CheckInDate
        DateOnly CheckOutDate
        int Nights
        decimal AgreedNightlyRate
        decimal TotalRoomAmount
    }

    ReservationGuests {
        Guid ReservationId PK,FK
        Guid GuestId PK,FK
        bool IsPrimary
    }

    ReservationStatusHistory {
        Guid Id PK
        Guid ReservationId FK
        int FromStatus
        int ToStatus
        string Reason
        string ChangedBy
        DateTimeOffset ChangedAt
    }

    RatePlans ||--o{ RoomRates : "contains"
    RoomTypes ||--o{ RoomRates : "priced by"
    RatePlans ||--o{ Reservations : "used in"
    Guests ||--o{ Reservations : "primary guest"
    Reservations ||--o{ ReservationRooms : "reserves"
    Rooms ||--o{ ReservationRooms : "booked in"
    RoomTypes ||--o{ ReservationRooms : "type"
    Reservations ||--o{ ReservationGuests : "lists"
    Guests ||--o{ ReservationGuests : "accompanies"
    Reservations ||--o{ ReservationStatusHistory : "logs"
```

---

### الوحدة 4: العمليات الفندقية (Operations: Front Desk, Guest Services, Housekeeping, Maintenance)

```mermaid
erDiagram
    CheckInRecords {
        Guid Id PK
        Guid ReservationId FK
        Guid RoomId FK
        DateTimeOffset ActualCheckInTime
        int ActualAdults
        int ActualChildren
        bool DocumentsVerified
        Guid PerformedByUserId
        string Notes
    }

    CheckOutRecords {
        Guid Id PK
        Guid ReservationId FK
        Guid RoomId FK
        DateTimeOffset ActualCheckOutTime
        decimal FinalBalanceSettled
        Guid ExceptionalBalanceApprovedBy
        Guid PerformedByUserId
        string Notes
    }

    GuestServices {
        Guid Id PK
        string Name
        int Category
        string Description
        decimal Price
        bool Taxable
        bool IsActive
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    ServiceOrders {
        Guid Id PK
        string OrderNumber UK
        Guid ReservationId FK
        Guid RoomId FK
        Guid GuestId FK
        DateTimeOffset RequestedAt
        DateTimeOffset CompletedAt
        int Status
        decimal SubTotal
        decimal TaxAmount
        decimal TotalAmount
        Guid AssignedUserId
        string Notes
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    ServiceOrderItems {
        Guid Id PK
        Guid ServiceOrderId FK
        Guid GuestServiceId FK
        int Quantity
        decimal UnitPrice
        decimal TaxAmount
        decimal LineTotal
    }

    HousekeepingTasks {
        Guid Id PK
        Guid RoomId FK
        int TaskType
        int Priority
        int Status
        DateOnly ScheduledFor
        Guid AssignedToUserId
        DateTimeOffset StartedAt
        DateTimeOffset CompletedAt
        Guid InspectedByUserId
        DateTimeOffset InspectedAt
        bool DamageReported
        string Notes
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    MaintenanceRequests {
        Guid Id PK
        string RequestNumber UK
        Guid RoomId FK
        string IssueType
        string Description
        int Priority
        int Status
        bool TakeRoomOutOfService
        Guid AssignedTechnicianUserId
        decimal Cost
        string SparePartsUsed
        DateTimeOffset CompletedAt
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    Reservations ||--o{ CheckInRecords : "has"
    Rooms ||--o{ CheckInRecords : "has"
    Reservations ||--o{ CheckOutRecords : "has"
    Rooms ||--o{ CheckOutRecords : "has"
    Reservations ||--o{ ServiceOrders : "places"
    Rooms ||--o{ ServiceOrders : "delivered to"
    Guests ||--o{ ServiceOrders : "requests"
    ServiceOrders ||--o{ ServiceOrderItems : "contains"
    GuestServices ||--o{ ServiceOrderItems : "referenced by"
    Rooms ||--o{ HousekeepingTasks : "assigned"
    Rooms ||--o{ MaintenanceRequests : "requires"
```

---

### الوحدة 5: الفوترة والمدفوعات والمالية (Billing, Payments & Finance)

```mermaid
erDiagram
    Invoices {
        Guid Id PK
        string InvoiceNumber UK
        Guid ReservationId FK
        Guid GuestId FK
        DateOnly IssueDate
        DateOnly DueDate
        decimal AccommodationTotal
        decimal ServicesTotal
        decimal DiscountTotal
        decimal TaxTotal
        decimal ServiceChargeTotal
        decimal GrandTotal
        decimal PaidAmount
        decimal BalanceDue
        int Status
        string CancellationReason
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    InvoiceItems {
        Guid Id PK
        Guid InvoiceId FK
        int ItemType
        string Description
        int Quantity
        decimal UnitPrice
        decimal Total
    }

    Payments {
        Guid Id PK
        string ReceiptNumber UK
        Guid InvoiceId FK
        Guid ReservationId FK
        decimal Amount
        int PaymentMethod
        string ReferenceNumber
        DateTimeOffset PaymentDate
        Guid RecordedByUserId
        string IdempotencyKey UK
        bool IsReversal
        Guid ReversedPaymentId
        string Notes
    }

    ExpenseCategories {
        Guid Id PK
        string Name
        string Description
    }

    Expenses {
        Guid Id PK
        string ExpenseNumber UK
        Guid CategoryId FK
        string Description
        decimal Amount
        DateOnly ExpenseDate
        int PaymentMethod
        string AttachmentPath
        Guid RecordedByUserId
        string Notes
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    RevenueRecords {
        Guid Id PK
        int Source
        Guid ReferenceId
        decimal Amount
        DateOnly RecognizedDate
        string Description
        Guid RecordedByUserId
    }

    Reservations ||--o{ Invoices : "billed by"
    Guests ||--o{ Invoices : "billed to"
    Invoices ||--o{ InvoiceItems : "contains"
    Invoices ||--o{ Payments : "receives"
    Reservations ||--o{ Payments : "settled by"
    ExpenseCategories ||--o{ Expenses : "categorizes"
```

---

### الوحدة 6: الموارد البشرية والمخزون والمشتريات (HR & Inventory)

```mermaid
erDiagram
    Departments {
        Guid Id PK
        string Name
        string Code
        string Description
    }

    Employees {
        Guid Id PK
        string EmployeeCode UK
        string FullName
        string Phone
        string Email
        Guid DepartmentId FK
        string JobTitle
        DateOnly HireDate
        decimal BaseSalary
        int Status
        string EmergencyContactName
        string EmergencyContactPhone
        Guid UserId
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    AttendanceRecords {
        Guid Id PK
        Guid EmployeeId FK
        DateOnly Date
        TimeOnly CheckInTime
        TimeOnly CheckOutTime
        int Status
        string Notes
    }

    LeaveRequests {
        Guid Id PK
        Guid EmployeeId FK
        string LeaveType
        DateOnly StartDate
        DateOnly EndDate
        string Reason
        int Status
        Guid ApprovedByUserId
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    InventoryCategories {
        Guid Id PK
        string Name
    }

    InventoryItems {
        Guid Id PK
        Guid CategoryId FK
        string Name
        string Sku UK
        string UnitOfMeasure
        decimal CurrentStock
        decimal ReorderLevel
        decimal UnitCost
        bool IsActive
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    Suppliers {
        Guid Id PK
        string Name
        string ContactPerson
        string Phone
        string Email
        string Address
        bool IsActive
        bool IsDeleted
        DateTimeOffset DeletedAt
        string DeletedBy
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    PurchaseOrders {
        Guid Id PK
        string OrderNumber UK
        Guid SupplierId FK
        DateOnly OrderDate
        DateOnly ExpectedDate
        int Status
        decimal TotalAmount
        DateTimeOffset CreatedAt
        string CreatedBy
        DateTimeOffset UpdatedAt
        string UpdatedBy
    }

    PurchaseOrderItems {
        Guid Id PK
        Guid PurchaseOrderId FK
        Guid InventoryItemId FK
        decimal QuantityOrdered
        decimal QuantityReceived
        decimal UnitCost
        decimal LineTotal
    }

    StockTransactions {
        Guid Id PK
        Guid InventoryItemId FK
        int TransactionType
        decimal Quantity
        decimal BalanceAfter
        string ReferenceNumber
        Guid DepartmentId FK
        Guid PerformedByUserId
        DateTimeOffset TransactionDate
        string Notes
    }

    Departments ||--o{ Employees : "employs"
    Employees ||--o{ AttendanceRecords : "records"
    Employees ||--o{ LeaveRequests : "submits"
    InventoryCategories ||--o{ InventoryItems : "contains"
    Suppliers ||--o{ PurchaseOrders : "receives"
    PurchaseOrders ||--o{ PurchaseOrderItems : "has"
    InventoryItems ||--o{ PurchaseOrderItems : "ordered in"
    InventoryItems ||--o{ StockTransactions : "tracks"
    Departments ||--o{ StockTransactions : "issued to"
```

---

### الوحدة 7: الإشعارات (Notifications)

```mermaid
erDiagram
    Notifications {
        Guid Id PK
        Guid TargetUserId
        string TargetRole
        string Title
        string Message
        string Type
        string RelatedEntityType
        string RelatedEntityId
        bool IsRead
        DateTimeOffset CreatedAt
    }
```

---

## 3. مرجع العلاقات المنطقية عبر الوحدات (Cross-Module Logical References)

بعض الأعمدة تمثل ارتباطات منطقية معرفية (`Guid`) دون قيد `FOREIGN KEY` صلب في SQL Server لتجنب مسارات الحذف المتسلسل المتعددة (`Multiple Cascade Paths`) أو لدعم التوسع المستقبلي:

| الجدول | العمود | الكيان المرتبط | الغرض |
|---|---|---|---|
| `AspNetUsers` | `EmployeeId` | `Employees.Id` | ربط حساب المستخدم بملف الموظف |
| `Employees` | `UserId` | `AspNetUsers.Id` | ربط الموظف بحساب الدخول |
| `CheckInRecords` / `CheckOutRecords` | `PerformedByUserId` | `AspNetUsers.Id` | موظف الاستقبال المنفذ |
| `CheckOutRecords` | `ExceptionalBalanceApprovedBy` | `AspNetUsers.Id` | المدير الموافق على المغادرة برصيد متبقٍ |
| `HousekeepingTasks` | `AssignedToUserId`, `InspectedByUserId` | `AspNetUsers.Id` | عامل النظافة والمشرف المفتش |
| `MaintenanceRequests` | `AssignedTechnicianUserId` | `AspNetUsers.Id` | فني الصيانة المعين |
| `ServiceOrders` | `AssignedUserId` | `AspNetUsers.Id` | الموظف المكلف بالطلب |
| `Payments` | `ReversedPaymentId` | `Payments.Id` | ربط قيد العكس بعملية الدفع الأصلية |
| `Payments` / `Expenses` / `RevenueRecords` / `StockTransactions` | `RecordedByUserId` / `PerformedByUserId` | `AspNetUsers.Id` | المستخدم الذي سجل الحركة المالية أو المخزنية |
| `RevenueRecords` | `ReferenceId` | `Invoices.Id` / `Payments.Id` | المرجع المصدري للإيراد |
| `LeaveRequests` | `ApprovedByUserId` | `AspNetUsers.Id` | المدير المعتمد للإجازة |
| `Notifications` | `TargetUserId`, `RelatedEntityId` | `AspNetUsers.Id` / Any Entity | المستخدم المستهدف والكيان المرتبط بالإشعار |

