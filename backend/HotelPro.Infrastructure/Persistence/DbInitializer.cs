using HotelPro.Domain.Constants;
using HotelPro.Domain.Entities.Finance;
using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.HR;
using HotelPro.Domain.Entities.Hotel;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Domain.Entities.Inventory;
using HotelPro.Domain.Entities.Notifications;
using HotelPro.Domain.Entities.Operations;
using HotelPro.Domain.Entities.Rates;
using HotelPro.Domain.Entities.Rooms;
using HotelPro.Domain.Enums;
using HotelPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPro.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedDemoUsers)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelProDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        // 1. Seed Permissions
        var existingCodes = await db.Permissions.Select(p => p.Code).ToHashSetAsync();
        var allPermissions = PermissionsCatalog.GetAll();
        foreach (var def in allPermissions)
        {
            if (!existingCodes.Contains(def.Code))
            {
                db.Permissions.Add(new Permission
                {
                    Code = def.Code,
                    Module = def.Module,
                    Action = def.Action,
                    Description = def.Description
                });
            }
        }
        await db.SaveChangesAsync();

        var permissionsByCode = await db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);

        // 2. Seed 9 System Roles + Default Permissions
        var roleDefinitions = GetDefaultRolePermissions(permissionsByCode.Keys);
        foreach (var (roleName, description, permCodes) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole
                {
                    Name = roleName,
                    Description = description,
                    IsSystemRole = true
                };
                await roleManager.CreateAsync(role);

                foreach (var code in permCodes.Distinct())
                {
                    if (permissionsByCode.TryGetValue(code, out var permId))
                    {
                        db.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permId
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        // 3. Seed Development-Only Demo Accounts
        if (seedDemoUsers)
        {
            var demoUsers = new (string Email, string FullName, string Role, string Password)[]
            {
                ("superadmin@hotelpro.local", "System Super Admin", RoleNames.SuperAdmin, "Admin@12345"),
                ("manager@hotelpro.local", "Hotel General Manager", RoleNames.HotelManager, "Manager@12345"),
                ("reception@hotelpro.local", "Front Desk Receptionist", RoleNames.Receptionist, "Reception@12345"),
                ("accountant@hotelpro.local", "Chief Accountant", RoleNames.Accountant, "Accountant@12345"),
                ("hk.supervisor@hotelpro.local", "Housekeeping Supervisor", RoleNames.HousekeepingSupervisor, "Housekeep@12345"),
                ("hk.staff@hotelpro.local", "Housekeeping Staff", RoleNames.HousekeepingStaff, "Housekeep@12345"),
                ("maint.manager@hotelpro.local", "Maintenance Manager", RoleNames.MaintenanceManager, "Maint@12345"),
                ("maint.tech@hotelpro.local", "Maintenance Technician", RoleNames.MaintenanceTechnician, "Maint@12345"),
                ("service@hotelpro.local", "Guest Service Staff", RoleNames.ServiceStaff, "Service@12345")
            };

            foreach (var (email, fullName, role, password) in demoUsers)
            {
                if (await userManager.FindByEmailAsync(email) is null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = fullName,
                        PreferredLanguage = "ar",
                        IsActive = true
                    };
                    var res = await userManager.CreateAsync(user, password);
                    if (res.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }
        }

        // 4. Ensure Default Hotel & Settings exist
        var hotel = await db.Hotels.Include(h => h.Settings).FirstOrDefaultAsync();
        if (hotel is null)
        {
            hotel = new Hotel
            {
                Name = "HotelPro Grand Palace",
                LegalName = "HotelPro Hospitality Co.",
                Address = "King Fahd Road, Riyadh",
                Phone = "+966 11 000 0000",
                Email = "info@hotelpro.local",
                TaxNumber = "300000000000003"
            };
            hotel.Settings = new HotelSettings
            {
                HotelId = hotel.Id,
                CurrencyCode = "SAR",
                CurrencySymbol = "ر.س",
                TimeZoneId = "Arab Standard Time",
                DateFormat = "yyyy-MM-dd",
                DefaultLanguage = "ar",
                DefaultCheckInTime = new TimeOnly(14, 0),
                DefaultCheckOutTime = new TimeOnly(12, 0),
                TaxRatePercent = 15.00m,
                ServiceChargePercent = 5.00m,
                RequireInspectionAfterCleaning = true,
                CancellationPolicyHours = 24
            };
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
        }

        // 5. Seed Demo RoomTypes, Amenities, Rooms, and Guests in Development
        if (seedDemoUsers && !await db.RoomTypes.AnyAsync())
        {
            var amenities = new List<Amenity>
            {
                new() { Name = "High-Speed Wi-Fi", Icon = "wifi", Category = "Connectivity" },
                new() { Name = "Smart TV", Icon = "tv", Category = "Entertainment" },
                new() { Name = "Air Conditioning", Icon = "ac_unit", Category = "Comfort" },
                new() { Name = "Mini Bar", Icon = "local_bar", Category = "Refreshments" },
                new() { Name = "Safe Box", Icon = "lock", Category = "Security" },
                new() { Name = "Balcony", Icon = "balcony", Category = "View" },
                new() { Name = "Jacuzzi", Icon = "hot_tub", Category = "Luxury" }
            };
            db.Amenities.AddRange(amenities);
            await db.SaveChangesAsync();

            var roomTypes = new List<RoomType>
            {
                new() { HotelId = hotel.Id, Name = "Single Room", Code = "SGL", Description = "Comfortable single room for solo travelers", BasePrice = 350m, MaxAdults = 1, MaxChildren = 0, BedCount = 1, AreaSqm = 22 },
                new() { HotelId = hotel.Id, Name = "Double Room", Code = "DBL", Description = "Standard double room with queen bed", BasePrice = 480m, MaxAdults = 2, MaxChildren = 1, BedCount = 1, AreaSqm = 28 },
                new() { HotelId = hotel.Id, Name = "Twin Room", Code = "TWN", Description = "Spacious room with two single beds", BasePrice = 500m, MaxAdults = 2, MaxChildren = 1, BedCount = 2, AreaSqm = 30 },
                new() { HotelId = hotel.Id, Name = "Deluxe Room", Code = "DLX", Description = "Upgraded deluxe room with city view", BasePrice = 720m, MaxAdults = 2, MaxChildren = 2, BedCount = 1, AreaSqm = 38 },
                new() { HotelId = hotel.Id, Name = "Suite", Code = "STE", Description = "Executive suite with separate living area", BasePrice = 1150m, MaxAdults = 3, MaxChildren = 2, BedCount = 2, AreaSqm = 58 },
                new() { HotelId = hotel.Id, Name = "Family Room", Code = "FAM", Description = "Large family room with connecting space", BasePrice = 950m, MaxAdults = 4, MaxChildren = 2, BedCount = 3, AreaSqm = 50 },
                new() { HotelId = hotel.Id, Name = "Presidential Suite", Code = "PRS", Description = "Luxury presidential suite with panoramic view", BasePrice = 2800m, MaxAdults = 4, MaxChildren = 3, BedCount = 2, AreaSqm = 120 }
            };
            db.RoomTypes.AddRange(roomTypes);
            await db.SaveChangesAsync();

            var rooms = new List<Room>
            {
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[0].Id, RoomNumber = "101", Floor = 1, MaxCapacity = 1, BedCount = 1, Status = RoomStatus.Available, Description = "Floor 1 Single" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[1].Id, RoomNumber = "102", Floor = 1, MaxCapacity = 3, BedCount = 1, Status = RoomStatus.Available, Description = "Floor 1 Double" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[2].Id, RoomNumber = "103", Floor = 1, MaxCapacity = 3, BedCount = 2, Status = RoomStatus.Cleaning, Description = "Floor 1 Twin" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[3].Id, RoomNumber = "201", Floor = 2, MaxCapacity = 4, BedCount = 1, Status = RoomStatus.Available, Description = "Floor 2 Deluxe" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[3].Id, RoomNumber = "202", Floor = 2, MaxCapacity = 4, BedCount = 1, Status = RoomStatus.Maintenance, Description = "Floor 2 Deluxe" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[5].Id, RoomNumber = "203", Floor = 2, MaxCapacity = 6, BedCount = 3, Status = RoomStatus.Available, Description = "Floor 2 Family" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[4].Id, RoomNumber = "301", Floor = 3, MaxCapacity = 5, BedCount = 2, Status = RoomStatus.Available, Description = "Floor 3 Suite" },
                new() { HotelId = hotel.Id, RoomTypeId = roomTypes[6].Id, RoomNumber = "302", Floor = 3, MaxCapacity = 7, BedCount = 2, Status = RoomStatus.Available, Description = "Floor 3 Presidential" }
            };
            db.Rooms.AddRange(rooms);

            var g1 = new Guest { FullName = "أحمد محمد العتيبي", Phone = "+966501112233", Email = "ahmed@example.com", Nationality = "Saudi Arabia", IsVip = true };
            g1.Documents.Add(new GuestDocument { GuestId = g1.Id, DocumentType = DocumentType.NationalId, DocumentNumber = "1088776655", IssuingCountry = "SA" });

            var g2 = new Guest { FullName = "Sarah Johnson", Phone = "+447700900123", Email = "sarah.j@example.com", Nationality = "United Kingdom", IsVip = false };
            g2.Documents.Add(new GuestDocument { GuestId = g2.Id, DocumentType = DocumentType.Passport, DocumentNumber = "UK99881122", IssuingCountry = "UK" });

            db.Guests.AddRange(g1, g2);
            await db.SaveChangesAsync();
        }

        // 6. Seed Default Rate Plans
        if (!await db.RatePlans.AnyAsync())
        {
            db.RatePlans.AddRange(
                new RatePlan { HotelId = hotel.Id, Name = "Standard Flexible", Code = "BAR", Description = "Best Available Rate - Refundable", IsRefundable = true, WeekendMultiplier = 1.15m, LongStayMinNights = 7, LongStayDiscountPercent = 10m, IsActive = true },
                new RatePlan { HotelId = hotel.Id, Name = "Non-Refundable Saver", Code = "NRF", Description = "Advance purchase non-refundable", IsRefundable = false, WeekendMultiplier = 1.00m, LongStayMinNights = 5, LongStayDiscountPercent = 15m, IsActive = true },
                new RatePlan { HotelId = hotel.Id, Name = "Corporate Rate", Code = "CORP", Description = "Special rate for corporate partners", IsRefundable = true, WeekendMultiplier = 1.00m, LongStayMinNights = 3, LongStayDiscountPercent = 12m, CorporateCode = "CORP2026", IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // 7. Seed Default Guest Services
        if (!await db.GuestServices.AnyAsync())
        {
            db.GuestServices.AddRange(
                new GuestService { Name = "Airport Pickup (VIP)", Category = ServiceCategory.AirportPickup, Price = 150m, Taxable = true, IsActive = true },
                new GuestService { Name = "Breakfast Room Service", Category = ServiceCategory.RoomService, Price = 65m, Taxable = true, IsActive = true },
                new GuestService { Name = "Express Laundry (Per Bag)", Category = ServiceCategory.Laundry, Price = 45m, Taxable = true, IsActive = true },
                new GuestService { Name = "Spa Massage 60 Min", Category = ServiceCategory.Spa, Price = 320m, Taxable = true, IsActive = true },
                new GuestService { Name = "Extra Bed (Per Night)", Category = ServiceCategory.ExtraBed, Price = 100m, Taxable = true, IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // 8. Seed Default Expense Categories
        if (!await db.ExpenseCategories.AnyAsync())
        {
            db.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "Salaries & Payroll", Description = "Staff salaries and benefits" },
                new ExpenseCategory { Name = "Utilities (Electricity & Water)", Description = "Monthly utility bills" },
                new ExpenseCategory { Name = "Maintenance & Repairs", Description = "Facility and room repairs" },
                new ExpenseCategory { Name = "Housekeeping Supplies", Description = "Cleaning agents and linens" },
                new ExpenseCategory { Name = "Internet & Telecom", Description = "Connectivity and phone services" },
                new ExpenseCategory { Name = "Administrative & Other", Description = "General operating expenses" }
            );
            await db.SaveChangesAsync();
        }

        // 9. Seed Default Departments & Inventory Categories
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Name = "Reception", Code = "REC", Description = "Front desk & guest relations" },
                new Department { Name = "Housekeeping", Code = "HK", Description = "Room cleaning & laundry" },
                new Department { Name = "Maintenance", Code = "MNT", Description = "Engineering & repairs" },
                new Department { Name = "Accounting", Code = "ACC", Description = "Finance & billing" },
                new Department { Name = "Management", Code = "MGT", Description = "Executive management" },
                new Department { Name = "Security", Code = "SEC", Description = "Hotel security" },
                new Department { Name = "Food & Beverage", Code = "FB", Description = "Restaurant & room service" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.InventoryCategories.AnyAsync())
        {
            db.InventoryCategories.AddRange(
                new InventoryCategory { Name = "Cleaning Supplies" },
                new InventoryCategory { Name = "Room Amenities" },
                new InventoryCategory { Name = "Tools & Equipment" },
                new InventoryCategory { Name = "Food & Beverages" },
                new InventoryCategory { Name = "Spare Parts" }
            );
            await db.SaveChangesAsync();
        }

        // 10. Seed Welcome Notification
        if (!await db.Notifications.AnyAsync())
        {
            db.Notifications.Add(new Notification
            {
                Title = "Welcome to HotelPro",
                Message = "HotelPro enterprise management system is initialized and ready.",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static List<(string Role, string Description, IEnumerable<string> Permissions)> GetDefaultRolePermissions(
        IEnumerable<string> allCodes)
    {
        var all = allCodes.ToList();
        return
        [
            (RoleNames.SuperAdmin, "Full unrestricted system access", all),
            (RoleNames.HotelManager, "Full hotel operations, finance, HR, settings, and reports",
                all.Where(c => !c.StartsWith("Roles.Delete"))),
            (RoleNames.Receptionist, "Front desk reservations, check-in, check-out, guests, billing",
                [
                    PermissionsCatalog.Dashboard.View,
                    PermissionsCatalog.Rooms.View,
                    PermissionsCatalog.Rates.View,
                    PermissionsCatalog.Guests.View, PermissionsCatalog.Guests.Create, PermissionsCatalog.Guests.Edit,
                    PermissionsCatalog.Reservations.View, PermissionsCatalog.Reservations.Create, PermissionsCatalog.Reservations.Edit,
                    PermissionsCatalog.Reservations.Approve, PermissionsCatalog.Reservations.Cancel,
                    PermissionsCatalog.Reservations.CheckIn, PermissionsCatalog.Reservations.CheckOut,
                    PermissionsCatalog.GuestServices.View, PermissionsCatalog.GuestServices.Create,
                    PermissionsCatalog.Housekeeping.View, PermissionsCatalog.Housekeeping.Create,
                    PermissionsCatalog.Maintenance.View, PermissionsCatalog.Maintenance.Create,
                    PermissionsCatalog.Billing.View, PermissionsCatalog.Billing.Create, PermissionsCatalog.Billing.ManagePayments
                ]),
            (RoleNames.Accountant, "Invoices, payments, expenses, revenues, and financial reports",
                [
                    PermissionsCatalog.Dashboard.View,
                    PermissionsCatalog.Guests.View,
                    PermissionsCatalog.Reservations.View,
                    PermissionsCatalog.Billing.View, PermissionsCatalog.Billing.Create,
                    PermissionsCatalog.Billing.Cancel, PermissionsCatalog.Billing.ManagePayments,
                    PermissionsCatalog.Finance.View, PermissionsCatalog.Finance.Create,
                    PermissionsCatalog.Finance.Edit, PermissionsCatalog.Finance.ViewReports,
                    PermissionsCatalog.Reports.ViewReports
                ]),
            (RoleNames.HousekeepingSupervisor, "Manage housekeeping tasks and inspect rooms",
                [
                    PermissionsCatalog.Dashboard.View,
                    PermissionsCatalog.Rooms.View, PermissionsCatalog.Rooms.Edit,
                    PermissionsCatalog.Housekeeping.View, PermissionsCatalog.Housekeeping.Create,
                    PermissionsCatalog.Housekeeping.Edit, PermissionsCatalog.Housekeeping.Approve,
                    PermissionsCatalog.Maintenance.Create
                ]),
            (RoleNames.HousekeepingStaff, "Execute assigned room cleaning tasks",
                [
                    PermissionsCatalog.Rooms.View,
                    PermissionsCatalog.Housekeeping.View, PermissionsCatalog.Housekeeping.Edit,
                    PermissionsCatalog.Maintenance.Create
                ]),
            (RoleNames.MaintenanceManager, "Manage maintenance requests, technicians, and room restoration",
                [
                    PermissionsCatalog.Dashboard.View,
                    PermissionsCatalog.Rooms.View, PermissionsCatalog.Rooms.Edit,
                    PermissionsCatalog.Maintenance.View, PermissionsCatalog.Maintenance.Create,
                    PermissionsCatalog.Maintenance.Edit, PermissionsCatalog.Maintenance.Approve,
                    PermissionsCatalog.Inventory.View
                ]),
            (RoleNames.MaintenanceTechnician, "Perform assigned repairs and log spare parts",
                [
                    PermissionsCatalog.Rooms.View,
                    PermissionsCatalog.Maintenance.View, PermissionsCatalog.Maintenance.Edit
                ]),
            (RoleNames.ServiceStaff, "Fulfill guest service orders",
                [
                    PermissionsCatalog.Rooms.View,
                    PermissionsCatalog.GuestServices.View, PermissionsCatalog.GuestServices.Edit
                ])
        ];
    }
}

