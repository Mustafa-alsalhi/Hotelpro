using HotelPro.Domain.Entities.Finance;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.HR;
using HotelPro.Domain.Entities.Hotel;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Domain.Entities.Inventory;
using HotelPro.Domain.Entities.Notifications;
using HotelPro.Domain.Entities.Operations;
using HotelPro.Domain.Entities.Rates;
using HotelPro.Domain.Entities.Reservations;
using HotelPro.Domain.Entities.Rooms;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelPro.Infrastructure.Persistence;

public class HotelProDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public HotelProDbContext(DbContextOptions<HotelProDbContext> options) : base(options) { }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelSettings> HotelSettings => Set<HotelSettings>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<RoomTypeAmenity> RoomTypeAmenities => Set<RoomTypeAmenity>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RoomRate> RoomRates => Set<RoomRate>();

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationRoom> ReservationRooms => Set<ReservationRoom>();
    public DbSet<ReservationGuest> ReservationGuests => Set<ReservationGuest>();
    public DbSet<ReservationStatusHistory> ReservationStatusHistory => Set<ReservationStatusHistory>();

    public DbSet<CheckInRecord> CheckInRecords => Set<CheckInRecord>();
    public DbSet<CheckOutRecord> CheckOutRecords => Set<CheckOutRecord>();
    public DbSet<GuestService> GuestServices => Set<GuestService>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderItem> ServiceOrderItems => Set<ServiceOrderItem>();
    public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RevenueRecord> RevenueRecords => Set<RevenueRecord>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(HotelProDbContext).Assembly);
    }
}

