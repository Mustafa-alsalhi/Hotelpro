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
using HotelPro.Domain.Entities.Reservations;
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

        // 11. Seed Comprehensive Demo Operational Data in Development
        if (seedDemoUsers)
        {
            await SeedComprehensiveDemoDataAsync(db, userManager, hotel);
        }
    }

    private static async Task SeedComprehensiveDemoDataAsync(
        HotelProDbContext db,
        UserManager<ApplicationUser> userManager,
        Hotel hotel)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var adminUser = await userManager.FindByEmailAsync("superadmin@hotelpro.local");
        var receptionUser = await userManager.FindByEmailAsync("reception@hotelpro.local");
        var hkStaffUser = await userManager.FindByEmailAsync("hk.staff@hotelpro.local");
        var hkSupUser = await userManager.FindByEmailAsync("hk.supervisor@hotelpro.local");
        var maintTechUser = await userManager.FindByEmailAsync("maint.tech@hotelpro.local");
        var accountantUser = await userManager.FindByEmailAsync("accountant@hotelpro.local");
        var serviceUser = await userManager.FindByEmailAsync("service@hotelpro.local");
        var managerUser = await userManager.FindByEmailAsync("manager@hotelpro.local");

        var actorId = adminUser?.Id ?? Guid.Empty;

        // A. Additional Rooms across types & statuses
        var roomTypes = await db.RoomTypes.Where(rt => rt.HotelId == hotel.Id).ToListAsync();
        if (roomTypes.Count >= 7 && !await db.Rooms.AnyAsync(r => r.RoomNumber == "104"))
        {
            var sgl = roomTypes.First(r => r.Code == "SGL");
            var dbl = roomTypes.First(r => r.Code == "DBL");
            var dlx = roomTypes.First(r => r.Code == "DLX");
            var ste = roomTypes.First(r => r.Code == "STE");
            var fam = roomTypes.First(r => r.Code == "FAM");

            db.Rooms.AddRange(
                new Room { HotelId = hotel.Id, RoomTypeId = sgl.Id, RoomNumber = "104", Floor = 1, MaxCapacity = 1, BedCount = 1, Status = RoomStatus.Occupied, Description = "Floor 1 Single (Occupied)" },
                new Room { HotelId = hotel.Id, RoomTypeId = dbl.Id, RoomNumber = "105", Floor = 1, MaxCapacity = 3, BedCount = 1, Status = RoomStatus.Available, Description = "Floor 1 Double" },
                new Room { HotelId = hotel.Id, RoomTypeId = dlx.Id, RoomNumber = "204", Floor = 2, MaxCapacity = 4, BedCount = 1, Status = RoomStatus.Reserved, Description = "Floor 2 Deluxe (Reserved)" },
                new Room { HotelId = hotel.Id, RoomTypeId = fam.Id, RoomNumber = "205", Floor = 2, MaxCapacity = 6, BedCount = 3, Status = RoomStatus.Available, Description = "Floor 2 Family Suite" },
                new Room { HotelId = hotel.Id, RoomTypeId = ste.Id, RoomNumber = "303", Floor = 3, MaxCapacity = 5, BedCount = 2, Status = RoomStatus.Occupied, Description = "Floor 3 Executive Suite (Occupied)" },
                new Room { HotelId = hotel.Id, RoomTypeId = ste.Id, RoomNumber = "304", Floor = 3, MaxCapacity = 5, BedCount = 2, Status = RoomStatus.OutOfService, Description = "Floor 3 Suite (Renovation)" }
            );
            await db.SaveChangesAsync();
        }

        // B. Additional Demo Guests
        if (!await db.Guests.AnyAsync(g => g.Email == "khaled.mansour@example.com"))
        {
            var g3 = new Guest { FullName = "خالد منصور القحطاني", Phone = "+966552233445", Email = "khaled.mansour@example.com", Nationality = "Saudi Arabia", Address = "Riyadh, Al Olaya", IsVip = true };
            g3.Documents.Add(new GuestDocument { GuestId = g3.Id, DocumentType = DocumentType.NationalId, DocumentNumber = "1099223344", IssuingCountry = "SA" });

            var g4 = new Guest { FullName = "نورة عبدالله الدوسري", Phone = "+966503344556", Email = "noura.d@example.com", Nationality = "Saudi Arabia", Address = "Jeddah, Al Rawdah", IsVip = false };
            g4.Documents.Add(new GuestDocument { GuestId = g4.Id, DocumentType = DocumentType.NationalId, DocumentNumber = "1077665544", IssuingCountry = "SA" });

            var g5 = new Guest { FullName = "Omar Farooq", Phone = "+971501234567", Email = "omar.farooq@example.com", Nationality = "UAE", Address = "Dubai Marina", IsVip = true };
            g5.Documents.Add(new GuestDocument { GuestId = g5.Id, DocumentType = DocumentType.Passport, DocumentNumber = "AE44556677", IssuingCountry = "AE" });

            var g6 = new Guest { FullName = "Elena Rossi", Phone = "+393401234567", Email = "elena.rossi@example.com", Nationality = "Italy", Address = "Milan, Via Roma", IsVip = false };
            g6.Documents.Add(new GuestDocument { GuestId = g6.Id, DocumentType = DocumentType.Passport, DocumentNumber = "IT77889900", IssuingCountry = "IT" });

            var g7 = new Guest { FullName = "فيصل سعد الحربي", Phone = "+966549988776", Email = "faisal.harbi@example.com", Nationality = "Saudi Arabia", Address = "Dammam", IsVip = false };
            g7.Documents.Add(new GuestDocument { GuestId = g7.Id, DocumentType = DocumentType.DriverLicense, DocumentNumber = "DL55443322", IssuingCountry = "SA" });

            db.Guests.AddRange(g3, g4, g5, g6, g7);
            await db.SaveChangesAsync();
        }

        // C. Reservations in all statuses + Front Desk + Housekeeping + Maintenance + ServiceOrders
        var allRooms = await db.Rooms.Include(r => r.RoomType).ToListAsync();
        var allGuests = await db.Guests.ToListAsync();
        var defaultRatePlan = await db.RatePlans.FirstOrDefaultAsync();

        if (!await db.Reservations.AnyAsync() && allRooms.Count >= 8 && allGuests.Count >= 5 && defaultRatePlan is not null)
        {
            var r104 = allRooms.FirstOrDefault(r => r.RoomNumber == "104") ?? allRooms[0];
            var r303 = allRooms.FirstOrDefault(r => r.RoomNumber == "303") ?? allRooms[1];
            var r204 = allRooms.FirstOrDefault(r => r.RoomNumber == "204") ?? allRooms[2];
            var r201 = allRooms.FirstOrDefault(r => r.RoomNumber == "201") ?? allRooms[3];
            var r102 = allRooms.FirstOrDefault(r => r.RoomNumber == "102") ?? allRooms[4];
            var r203 = allRooms.FirstOrDefault(r => r.RoomNumber == "203") ?? allRooms[5];
            var r105 = allRooms.FirstOrDefault(r => r.RoomNumber == "105") ?? allRooms[6];

            var g1 = allGuests[0];
            var g2 = allGuests[1];
            var g3 = allGuests[2];
            var g4 = allGuests[3];
            var g5 = allGuests[4];
            var g6 = allGuests.Count > 5 ? allGuests[5] : allGuests[0];
            var g7 = allGuests.Count > 6 ? allGuests[6] : allGuests[1];

            var resCheckedIn1 = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0001",
                PrimaryGuestId = g1.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.DirectBooking,
                Status = ReservationStatus.CheckedIn,
                CheckInDate = today.AddDays(-1),
                CheckOutDate = today.AddDays(3),
                Nights = 4,
                AdultsCount = 1,
                ChildrenCount = 0,
                SubTotal = 1400m,
                TaxAmount = 210m,
                ServiceChargeAmount = 70m,
                TotalAmount = 1680m,
                DepositRequired = 500m,
                Notes = "VIP guest - high floor preference"
            };
            resCheckedIn1.Rooms.Add(new ReservationRoom { ReservationId = resCheckedIn1.Id, RoomId = r104.Id, RoomTypeId = r104.RoomTypeId, CheckInDate = resCheckedIn1.CheckInDate, CheckOutDate = resCheckedIn1.CheckOutDate, Nights = 4, AgreedNightlyRate = 350m, TotalRoomAmount = 1400m });
            resCheckedIn1.Guests.Add(new ReservationGuest { ReservationId = resCheckedIn1.Id, GuestId = g1.Id, IsPrimary = true });

            var resCheckedIn2 = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0002",
                PrimaryGuestId = g3.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.Website,
                Status = ReservationStatus.CheckedIn,
                CheckInDate = today.AddDays(-2),
                CheckOutDate = today.AddDays(2),
                Nights = 4,
                AdultsCount = 2,
                ChildrenCount = 1,
                SubTotal = 4600m,
                TaxAmount = 690m,
                ServiceChargeAmount = 230m,
                TotalAmount = 5520m,
                DepositRequired = 1500m,
                Notes = "Executive Suite stay"
            };
            resCheckedIn2.Rooms.Add(new ReservationRoom { ReservationId = resCheckedIn2.Id, RoomId = r303.Id, RoomTypeId = r303.RoomTypeId, CheckInDate = resCheckedIn2.CheckInDate, CheckOutDate = resCheckedIn2.CheckOutDate, Nights = 4, AgreedNightlyRate = 1150m, TotalRoomAmount = 4600m });
            resCheckedIn2.Guests.Add(new ReservationGuest { ReservationId = resCheckedIn2.Id, GuestId = g3.Id, IsPrimary = true });

            var resConfirmed = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0003",
                PrimaryGuestId = g2.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.TravelAgent,
                Status = ReservationStatus.Confirmed,
                CheckInDate = today.AddDays(1),
                CheckOutDate = today.AddDays(4),
                Nights = 3,
                AdultsCount = 2,
                ChildrenCount = 0,
                SubTotal = 2160m,
                TaxAmount = 324m,
                ServiceChargeAmount = 108m,
                TotalAmount = 2592m,
                DepositRequired = 600m,
                Notes = "Airport pickup requested"
            };
            resConfirmed.Rooms.Add(new ReservationRoom { ReservationId = resConfirmed.Id, RoomId = r204.Id, RoomTypeId = r204.RoomTypeId, CheckInDate = resConfirmed.CheckInDate, CheckOutDate = resConfirmed.CheckOutDate, Nights = 3, AgreedNightlyRate = 720m, TotalRoomAmount = 2160m });
            resConfirmed.Guests.Add(new ReservationGuest { ReservationId = resConfirmed.Id, GuestId = g2.Id, IsPrimary = true });

            var resPending = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0004",
                PrimaryGuestId = g4.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.Phone,
                Status = ReservationStatus.Pending,
                CheckInDate = today.AddDays(3),
                CheckOutDate = today.AddDays(6),
                Nights = 3,
                AdultsCount = 2,
                ChildrenCount = 0,
                SubTotal = 2160m,
                TaxAmount = 324m,
                ServiceChargeAmount = 108m,
                TotalAmount = 2592m,
                DepositRequired = 500m,
                Notes = "Awaiting deposit confirmation"
            };
            resPending.Rooms.Add(new ReservationRoom { ReservationId = resPending.Id, RoomId = r201.Id, RoomTypeId = r201.RoomTypeId, CheckInDate = resPending.CheckInDate, CheckOutDate = resPending.CheckOutDate, Nights = 3, AgreedNightlyRate = 720m, TotalRoomAmount = 2160m });
            resPending.Guests.Add(new ReservationGuest { ReservationId = resPending.Id, GuestId = g4.Id, IsPrimary = true });

            var resCheckedOut = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0005",
                PrimaryGuestId = g5.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.WalkIn,
                Status = ReservationStatus.CheckedOut,
                CheckInDate = today.AddDays(-5),
                CheckOutDate = today.AddDays(-2),
                Nights = 3,
                AdultsCount = 2,
                ChildrenCount = 0,
                SubTotal = 1440m,
                TaxAmount = 216m,
                ServiceChargeAmount = 72m,
                TotalAmount = 1728m,
                DepositRequired = 500m,
                Notes = "Completed stay"
            };
            resCheckedOut.Rooms.Add(new ReservationRoom { ReservationId = resCheckedOut.Id, RoomId = r102.Id, RoomTypeId = r102.RoomTypeId, CheckInDate = resCheckedOut.CheckInDate, CheckOutDate = resCheckedOut.CheckOutDate, Nights = 3, AgreedNightlyRate = 480m, TotalRoomAmount = 1440m });
            resCheckedOut.Guests.Add(new ReservationGuest { ReservationId = resCheckedOut.Id, GuestId = g5.Id, IsPrimary = true });

            var resCancelled = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0006",
                PrimaryGuestId = g6.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.Website,
                Status = ReservationStatus.Cancelled,
                CheckInDate = today.AddDays(5),
                CheckOutDate = today.AddDays(8),
                Nights = 3,
                AdultsCount = 3,
                ChildrenCount = 1,
                SubTotal = 2850m,
                TaxAmount = 427.5m,
                ServiceChargeAmount = 142.5m,
                TotalAmount = 3420m,
                CancellationReason = "Change of travel plans"
            };
            resCancelled.Rooms.Add(new ReservationRoom { ReservationId = resCancelled.Id, RoomId = r203.Id, RoomTypeId = r203.RoomTypeId, CheckInDate = resCancelled.CheckInDate, CheckOutDate = resCancelled.CheckOutDate, Nights = 3, AgreedNightlyRate = 950m, TotalRoomAmount = 2850m });
            resCancelled.Guests.Add(new ReservationGuest { ReservationId = resCancelled.Id, GuestId = g6.Id, IsPrimary = true });

            var resNoShow = new Reservation
            {
                HotelId = hotel.Id,
                ReservationNumber = "RES-2026-0007",
                PrimaryGuestId = g7.Id,
                RatePlanId = defaultRatePlan.Id,
                Source = BookingSource.Phone,
                Status = ReservationStatus.NoShow,
                CheckInDate = today.AddDays(-1),
                CheckOutDate = today,
                Nights = 1,
                AdultsCount = 1,
                ChildrenCount = 0,
                SubTotal = 480m,
                TaxAmount = 72m,
                ServiceChargeAmount = 24m,
                TotalAmount = 576m,
                Notes = "Guest did not arrive by midnight"
            };
            resNoShow.Rooms.Add(new ReservationRoom { ReservationId = resNoShow.Id, RoomId = r105.Id, RoomTypeId = r105.RoomTypeId, CheckInDate = resNoShow.CheckInDate, CheckOutDate = resNoShow.CheckOutDate, Nights = 1, AgreedNightlyRate = 480m, TotalRoomAmount = 480m });
            resNoShow.Guests.Add(new ReservationGuest { ReservationId = resNoShow.Id, GuestId = g7.Id, IsPrimary = true });

            db.Reservations.AddRange(resCheckedIn1, resCheckedIn2, resConfirmed, resPending, resCheckedOut, resCancelled, resNoShow);
            await db.SaveChangesAsync();

            // CheckIn / CheckOut Records
            db.CheckInRecords.AddRange(
                new CheckInRecord { ReservationId = resCheckedIn1.Id, RoomId = r104.Id, ActualCheckInTime = DateTimeOffset.UtcNow.AddDays(-1), ActualAdults = 1, DocumentsVerified = true, PerformedByUserId = receptionUser?.Id ?? actorId, Notes = "Smooth check-in" },
                new CheckInRecord { ReservationId = resCheckedIn2.Id, RoomId = r303.Id, ActualCheckInTime = DateTimeOffset.UtcNow.AddDays(-2), ActualAdults = 2, ActualChildren = 1, DocumentsVerified = true, PerformedByUserId = receptionUser?.Id ?? actorId, Notes = "Welcome basket delivered" },
                new CheckInRecord { ReservationId = resCheckedOut.Id, RoomId = r102.Id, ActualCheckInTime = DateTimeOffset.UtcNow.AddDays(-5), ActualAdults = 2, DocumentsVerified = true, PerformedByUserId = receptionUser?.Id ?? actorId }
            );
            db.CheckOutRecords.Add(
                new CheckOutRecord { ReservationId = resCheckedOut.Id, RoomId = r102.Id, ActualCheckOutTime = DateTimeOffset.UtcNow.AddDays(-2), FinalBalanceSettled = 1728m, PerformedByUserId = receptionUser?.Id ?? actorId, Notes = "All settled by card" }
            );

            // Service Orders
            var guestServices = await db.GuestServices.ToListAsync();
            if (guestServices.Count >= 2)
            {
                var order1 = new ServiceOrder
                {
                    OrderNumber = "SRV-2026-0001",
                    ReservationId = resCheckedIn1.Id,
                    RoomId = r104.Id,
                    GuestId = g1.Id,
                    RequestedAt = DateTimeOffset.UtcNow.AddHours(-6),
                    CompletedAt = DateTimeOffset.UtcNow.AddHours(-5),
                    Status = ServiceOrderStatus.Completed,
                    SubTotal = 130m,
                    TaxAmount = 19.5m,
                    TotalAmount = 149.5m,
                    AssignedUserId = serviceUser?.Id,
                    Notes = "Morning breakfast"
                };
                order1.Items.Add(new ServiceOrderItem { ServiceOrderId = order1.Id, GuestServiceId = guestServices[1].Id, Quantity = 2, UnitPrice = 65m, TaxAmount = 19.5m, LineTotal = 149.5m });

                var order2 = new ServiceOrder
                {
                    OrderNumber = "SRV-2026-0002",
                    ReservationId = resCheckedIn2.Id,
                    RoomId = r303.Id,
                    GuestId = g3.Id,
                    RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-45),
                    Status = ServiceOrderStatus.InProgress,
                    SubTotal = 320m,
                    TaxAmount = 48m,
                    TotalAmount = 368m,
                    AssignedUserId = serviceUser?.Id,
                    Notes = "Spa appointment"
                };
                order2.Items.Add(new ServiceOrderItem { ServiceOrderId = order2.Id, GuestServiceId = guestServices.Count > 3 ? guestServices[3].Id : guestServices[0].Id, Quantity = 1, UnitPrice = 320m, TaxAmount = 48m, LineTotal = 368m });

                db.ServiceOrders.AddRange(order1, order2);
            }

            // D. Invoices & Payments
            if (!await db.Invoices.AnyAsync())
            {
                var invPaid = new Invoice
                {
                    InvoiceNumber = "INV-2026-0001",
                    ReservationId = resCheckedOut.Id,
                    GuestId = g5.Id,
                    IssueDate = today.AddDays(-2),
                    DueDate = today.AddDays(-2),
                    AccommodationTotal = 1440m,
                    TaxTotal = 216m,
                    ServiceChargeTotal = 72m,
                    GrandTotal = 1728m,
                    PaidAmount = 1728m,
                    BalanceDue = 0m,
                    Status = InvoiceStatus.Paid
                };
                invPaid.Items.Add(new InvoiceItem { InvoiceId = invPaid.Id, ItemType = InvoiceItemType.RoomCharge, Description = "Double Room 102 (3 Nights)", Quantity = 3, UnitPrice = 480m, Total = 1440m });
                invPaid.Items.Add(new InvoiceItem { InvoiceId = invPaid.Id, ItemType = InvoiceItemType.Tax, Description = "VAT 15%", Quantity = 1, UnitPrice = 216m, Total = 216m });
                invPaid.Items.Add(new InvoiceItem { InvoiceId = invPaid.Id, ItemType = InvoiceItemType.ServiceCharge, Description = "Service Charge 5%", Quantity = 1, UnitPrice = 72m, Total = 72m });

                var invPartial = new Invoice
                {
                    InvoiceNumber = "INV-2026-0002",
                    ReservationId = resCheckedIn1.Id,
                    GuestId = g1.Id,
                    IssueDate = today.AddDays(-1),
                    DueDate = today.AddDays(3),
                    AccommodationTotal = 1400m,
                    ServicesTotal = 130m,
                    TaxTotal = 229.5m,
                    ServiceChargeTotal = 70m,
                    GrandTotal = 1829.5m,
                    PaidAmount = 500m,
                    BalanceDue = 1329.5m,
                    Status = InvoiceStatus.PartiallyPaid
                };
                invPartial.Items.Add(new InvoiceItem { InvoiceId = invPartial.Id, ItemType = InvoiceItemType.RoomCharge, Description = "Single Room 104 (4 Nights)", Quantity = 4, UnitPrice = 350m, Total = 1400m });
                invPartial.Items.Add(new InvoiceItem { InvoiceId = invPartial.Id, ItemType = InvoiceItemType.ServiceCharge, Description = "Breakfast Room Service x2", Quantity = 2, UnitPrice = 65m, Total = 130m });

                var invIssued = new Invoice
                {
                    InvoiceNumber = "INV-2026-0003",
                    ReservationId = resCheckedIn2.Id,
                    GuestId = g3.Id,
                    IssueDate = today,
                    DueDate = today.AddDays(2),
                    AccommodationTotal = 4600m,
                    TaxTotal = 690m,
                    ServiceChargeTotal = 230m,
                    GrandTotal = 5520m,
                    PaidAmount = 0m,
                    BalanceDue = 5520m,
                    Status = InvoiceStatus.Issued
                };
                invIssued.Items.Add(new InvoiceItem { InvoiceId = invIssued.Id, ItemType = InvoiceItemType.RoomCharge, Description = "Executive Suite 303 (4 Nights)", Quantity = 4, UnitPrice = 1150m, Total = 4600m });

                var invDraft = new Invoice
                {
                    InvoiceNumber = "INV-2026-0004",
                    ReservationId = resConfirmed.Id,
                    GuestId = g2.Id,
                    IssueDate = today,
                    DueDate = today.AddDays(4),
                    AccommodationTotal = 2160m,
                    TaxTotal = 324m,
                    ServiceChargeTotal = 108m,
                    GrandTotal = 2592m,
                    PaidAmount = 0m,
                    BalanceDue = 2592m,
                    Status = InvoiceStatus.Draft
                };

                db.Invoices.AddRange(invPaid, invPartial, invIssued, invDraft);
                await db.SaveChangesAsync();

                db.Payments.AddRange(
                    new Payment { ReceiptNumber = "REC-2026-0001", InvoiceId = invPaid.Id, ReservationId = resCheckedOut.Id, Amount = 1728m, PaymentMethod = PaymentMethod.Card, ReferenceNumber = "VISA-8841", PaymentDate = DateTimeOffset.UtcNow.AddDays(-2), RecordedByUserId = accountantUser?.Id ?? actorId, IdempotencyKey = "seed-pay-0001", Notes = "Full checkout payment" },
                    new Payment { ReceiptNumber = "REC-2026-0002", InvoiceId = invPartial.Id, ReservationId = resCheckedIn1.Id, Amount = 500m, PaymentMethod = PaymentMethod.Cash, ReferenceNumber = "CASH-DEP-01", PaymentDate = DateTimeOffset.UtcNow.AddDays(-1), RecordedByUserId = receptionUser?.Id ?? actorId, IdempotencyKey = "seed-pay-0002", Notes = "Check-in deposit" }
                );
            }

            await db.SaveChangesAsync();
        }

        // E. Housekeeping & Maintenance Demo Tasks
        if (!await db.HousekeepingTasks.AnyAsync() && allRooms.Count >= 4)
        {
            var r103 = allRooms.FirstOrDefault(r => r.RoomNumber == "103") ?? allRooms[0];
            var r102 = allRooms.FirstOrDefault(r => r.RoomNumber == "102") ?? allRooms[1];
            var r104 = allRooms.FirstOrDefault(r => r.RoomNumber == "104") ?? allRooms[2];

            db.HousekeepingTasks.AddRange(
                new HousekeepingTask { RoomId = r103.Id, TaskType = HousekeepingTaskType.CheckoutCleaning, Priority = TaskPriority.High, Status = HousekeepingStatus.InProgress, ScheduledFor = today, AssignedToUserId = hkStaffUser?.Id, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-30), Notes = "Prepare for afternoon arrivals" },
                new HousekeepingTask { RoomId = r104.Id, TaskType = HousekeepingTaskType.DailyCleaning, Priority = TaskPriority.Normal, Status = HousekeepingStatus.Pending, ScheduledFor = today, Notes = "Standard daily turndown" },
                new HousekeepingTask { RoomId = r102.Id, TaskType = HousekeepingTaskType.Inspection, Priority = TaskPriority.Normal, Status = HousekeepingStatus.Inspected, ScheduledFor = today.AddDays(-1), AssignedToUserId = hkStaffUser?.Id, CompletedAt = DateTimeOffset.UtcNow.AddDays(-1), InspectedByUserId = hkSupUser?.Id, InspectedAt = DateTimeOffset.UtcNow.AddDays(-1), Notes = "Passed inspection" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.MaintenanceRequests.AnyAsync() && allRooms.Count >= 5)
        {
            var r202 = allRooms.FirstOrDefault(r => r.RoomNumber == "202") ?? allRooms[0];
            var r304 = allRooms.FirstOrDefault(r => r.RoomNumber == "304") ?? allRooms[1];

            db.MaintenanceRequests.AddRange(
                new MaintenanceRequest { RequestNumber = "MNT-2026-0001", RoomId = r202.Id, IssueType = "HVAC / AC", Description = "AC compressor noisy and needs coolant check", Priority = TaskPriority.High, Status = MaintenanceStatus.InProgress, TakeRoomOutOfService = false, AssignedTechnicianUserId = maintTechUser?.Id, Cost = 350m, SparePartsUsed = "R410A Refrigerant" },
                new MaintenanceRequest { RequestNumber = "MNT-2026-0002", RoomId = r304.Id, IssueType = "Plumbing & Renovation", Description = "Suite bathroom marble & fixture upgrade", Priority = TaskPriority.Normal, Status = MaintenanceStatus.WaitingParts, TakeRoomOutOfService = true, AssignedTechnicianUserId = maintTechUser?.Id, Cost = 2400m }
            );
            await db.SaveChangesAsync();
        }

        // F. Expenses & Revenue Records (for Reports & Dashboard KPIs)
        if (!await db.Expenses.AnyAsync())
        {
            var expCats = await db.ExpenseCategories.ToListAsync();
            if (expCats.Count >= 4)
            {
                db.Expenses.AddRange(
                    new Expense { ExpenseNumber = "EXP-2026-0001", CategoryId = expCats[1].Id, Description = "Monthly Electricity & Water Bill", Amount = 4200m, ExpenseDate = today.AddDays(-4), PaymentMethod = PaymentMethod.BankTransfer, RecordedByUserId = accountantUser?.Id ?? actorId },
                    new Expense { ExpenseNumber = "EXP-2026-0002", CategoryId = expCats[3].Id, Description = "Linens & Cleaning Detergents Restock", Amount = 1850m, ExpenseDate = today.AddDays(-3), PaymentMethod = PaymentMethod.Card, RecordedByUserId = accountantUser?.Id ?? actorId },
                    new Expense { ExpenseNumber = "EXP-2026-0003", CategoryId = expCats[2].Id, Description = "HVAC Spare Parts & Maintenance", Amount = 950m, ExpenseDate = today.AddDays(-1), PaymentMethod = PaymentMethod.Cash, RecordedByUserId = accountantUser?.Id ?? actorId }
                );
                await db.SaveChangesAsync();
            }
        }

        if (!await db.RevenueRecords.AnyAsync())
        {
            db.RevenueRecords.AddRange(
                new RevenueRecord { Source = RevenueSource.Rooms, Amount = 1728m, RecognizedDate = today.AddDays(-2), Description = "Room Revenue - RES-2026-0005", RecordedByUserId = accountantUser?.Id ?? actorId },
                new RevenueRecord { Source = RevenueSource.Rooms, Amount = 1400m, RecognizedDate = today.AddDays(-1), Description = "Room Revenue - RES-2026-0001", RecordedByUserId = accountantUser?.Id ?? actorId },
                new RevenueRecord { Source = RevenueSource.Services, Amount = 149.5m, RecognizedDate = today, Description = "Guest Room Service - SRV-2026-0001", RecordedByUserId = accountantUser?.Id ?? actorId }
            );
            await db.SaveChangesAsync();
        }

        // G. Employees, Attendance & Leave Requests
        if (!await db.Employees.AnyAsync())
        {
            var depts = await db.Departments.ToListAsync();
            if (depts.Count >= 5)
            {
                var recDept = depts.First(d => d.Code == "REC");
                var hkDept = depts.First(d => d.Code == "HK");
                var mntDept = depts.First(d => d.Code == "MNT");
                var accDept = depts.First(d => d.Code == "ACC");
                var mgtDept = depts.First(d => d.Code == "MGT");
                var fbDept = depts.FirstOrDefault(d => d.Code == "FB") ?? recDept;

                var emp1 = new Employee { EmployeeCode = "EMP-001", FullName = "طارق عبدالعزيز السالم", Phone = "+966500001101", Email = "manager@hotelpro.local", DepartmentId = mgtDept.Id, JobTitle = "Hotel General Manager", HireDate = new DateOnly(2023, 1, 15), BaseSalary = 18000m, Status = EmployeeStatus.Active, UserId = managerUser?.Id };
                var emp2 = new Employee { EmployeeCode = "EMP-002", FullName = "سارة فهد المطيري", Phone = "+966500001102", Email = "reception@hotelpro.local", DepartmentId = recDept.Id, JobTitle = "Senior Receptionist", HireDate = new DateOnly(2024, 3, 1), BaseSalary = 7500m, Status = EmployeeStatus.Active, UserId = receptionUser?.Id };
                var emp3 = new Employee { EmployeeCode = "EMP-003", FullName = "محمد حسن الزهراني", Phone = "+966500001103", Email = "accountant@hotelpro.local", DepartmentId = accDept.Id, JobTitle = "Chief Accountant", HireDate = new DateOnly(2023, 6, 10), BaseSalary = 12500m, Status = EmployeeStatus.Active, UserId = accountantUser?.Id };
                var emp4 = new Employee { EmployeeCode = "EMP-004", FullName = "منى سالم الغامدي", Phone = "+966500001104", Email = "hk.supervisor@hotelpro.local", DepartmentId = hkDept.Id, JobTitle = "Housekeeping Supervisor", HireDate = new DateOnly(2024, 2, 1), BaseSalary = 6800m, Status = EmployeeStatus.Active, UserId = hkSupUser?.Id };
                var emp5 = new Employee { EmployeeCode = "EMP-005", FullName = "عبدالرحمن علي الشهري", Phone = "+966500001105", Email = "maint.tech@hotelpro.local", DepartmentId = mntDept.Id, JobTitle = "HVAC & Maintenance Technician", HireDate = new DateOnly(2024, 5, 20), BaseSalary = 6200m, Status = EmployeeStatus.Active, UserId = maintTechUser?.Id };
                var emp6 = new Employee { EmployeeCode = "EMP-006", FullName = "يوسف إبراهيم كمال", Phone = "+966500001106", Email = "service@hotelpro.local", DepartmentId = fbDept.Id, JobTitle = "Guest Service Specialist", HireDate = new DateOnly(2025, 1, 10), BaseSalary = 5500m, Status = EmployeeStatus.OnLeave, UserId = serviceUser?.Id };

                db.Employees.AddRange(emp1, emp2, emp3, emp4, emp5, emp6);
                await db.SaveChangesAsync();

                db.AttendanceRecords.AddRange(
                    new AttendanceRecord { EmployeeId = emp1.Id, Date = today, CheckInTime = new TimeOnly(8, 5), Status = AttendanceStatus.Present },
                    new AttendanceRecord { EmployeeId = emp2.Id, Date = today, CheckInTime = new TimeOnly(7, 55), Status = AttendanceStatus.Present },
                    new AttendanceRecord { EmployeeId = emp3.Id, Date = today, CheckInTime = new TimeOnly(8, 25), Status = AttendanceStatus.Late, Notes = "Traffic delay" },
                    new AttendanceRecord { EmployeeId = emp4.Id, Date = today, CheckInTime = new TimeOnly(8, 0), Status = AttendanceStatus.Present },
                    new AttendanceRecord { EmployeeId = emp6.Id, Date = today, Status = AttendanceStatus.OnLeave, Notes = "Approved annual leave" }
                );

                db.LeaveRequests.AddRange(
                    new LeaveRequest { EmployeeId = emp6.Id, LeaveType = "Annual Leave", StartDate = today.AddDays(-1), EndDate = today.AddDays(3), Reason = "Family vacation", Status = LeaveStatus.Approved, ApprovedByUserId = managerUser?.Id },
                    new LeaveRequest { EmployeeId = emp2.Id, LeaveType = "Emergency Leave", StartDate = today.AddDays(7), EndDate = today.AddDays(8), Reason = "Personal appointment", Status = LeaveStatus.Pending }
                );
                await db.SaveChangesAsync();
            }
        }

        // H. Inventory, Suppliers, Purchase Orders & Stock Transactions
        if (!await db.InventoryItems.AnyAsync())
        {
            var invCats = await db.InventoryCategories.ToListAsync();
            var hkDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HK");
            if (invCats.Count >= 5)
            {
                var sup1 = new Supplier { Name = "Al-Futtaim Hospitality Supplies", ContactPerson = "Tariq Nasser", Phone = "+966114455667", Email = "sales@alfuttaim-hosp.local", Address = "Riyadh Industrial City", IsActive = true };
                var sup2 = new Supplier { Name = "Riyadh Linen & Textiles", ContactPerson = "Huda Al-Rashed", Phone = "+966112233448", Email = "orders@riyadhlinen.local", Address = "Al Malaz, Riyadh", IsActive = true };
                var sup3 = new Supplier { Name = "Gulf Technical Spare Parts", ContactPerson = "Sami Khoury", Phone = "+966118899001", Email = "parts@gulftech.local", Address = "Sulay, Riyadh", IsActive = true };
                db.Suppliers.AddRange(sup1, sup2, sup3);
                await db.SaveChangesAsync();

                var item1 = new InventoryItem { CategoryId = invCats[0].Id, Name = "Multi-Surface Disinfectant (5L)", Sku = "CLN-001", UnitOfMeasure = "Bottle", CurrentStock = 42m, ReorderLevel = 15m, UnitCost = 38m, IsActive = true };
                var item2 = new InventoryItem { CategoryId = invCats[1].Id, Name = "Luxury Bath Towel Set", Sku = "AMN-001", UnitOfMeasure = "Set", CurrentStock = 120m, ReorderLevel = 30m, UnitCost = 65m, IsActive = true };
                var item3 = new InventoryItem { CategoryId = invCats[1].Id, Name = "Guest Dental & Vanity Kit", Sku = "AMN-002", UnitOfMeasure = "Box", CurrentStock = 8m, ReorderLevel = 20m, UnitCost = 45m, IsActive = true }; // Low stock alert!
                var item4 = new InventoryItem { CategoryId = invCats[3].Id, Name = "Bottled Mineral Water (24x330ml)", Sku = "FB-001", UnitOfMeasure = "Case", CurrentStock = 65m, ReorderLevel = 20m, UnitCost = 18m, IsActive = true };
                var item5 = new InventoryItem { CategoryId = invCats[4].Id, Name = "Smart Door Lock Battery Pack", Sku = "PRT-001", UnitOfMeasure = "Pack", CurrentStock = 25m, ReorderLevel = 10m, UnitCost = 55m, IsActive = true };

                db.InventoryItems.AddRange(item1, item2, item3, item4, item5);
                await db.SaveChangesAsync();

                var poReceived = new PurchaseOrder { OrderNumber = "PO-2026-0001", SupplierId = sup2.Id, OrderDate = today.AddDays(-6), ExpectedDate = today.AddDays(-3), Status = PurchaseOrderStatus.Received, TotalAmount = 7800m };
                poReceived.Items.Add(new PurchaseOrderItem { PurchaseOrderId = poReceived.Id, InventoryItemId = item2.Id, QuantityOrdered = 120m, QuantityReceived = 120m, UnitCost = 65m, LineTotal = 7800m });

                var poOrdered = new PurchaseOrder { OrderNumber = "PO-2026-0002", SupplierId = sup1.Id, OrderDate = today, ExpectedDate = today.AddDays(3), Status = PurchaseOrderStatus.Ordered, TotalAmount = 1800m };
                poOrdered.Items.Add(new PurchaseOrderItem { PurchaseOrderId = poOrdered.Id, InventoryItemId = item3.Id, QuantityOrdered = 40m, QuantityReceived = 0m, UnitCost = 45m, LineTotal = 1800m });

                db.PurchaseOrders.AddRange(poReceived, poOrdered);

                db.StockTransactions.AddRange(
                    new StockTransaction { InventoryItemId = item2.Id, TransactionType = StockTransactionType.PurchaseIn, Quantity = 120m, BalanceAfter = 120m, ReferenceNumber = "PO-2026-0001", PerformedByUserId = actorId, TransactionDate = DateTimeOffset.UtcNow.AddDays(-3), Notes = "Initial stock received" },
                    new StockTransaction { InventoryItemId = item3.Id, TransactionType = StockTransactionType.DepartmentIssue, Quantity = -12m, BalanceAfter = 8m, ReferenceNumber = "ISS-2026-0001", DepartmentId = hkDept?.Id, PerformedByUserId = hkSupUser?.Id ?? actorId, TransactionDate = DateTimeOffset.UtcNow.AddDays(-1), Notes = "Issued to Housekeeping (Below reorder level)" }
                );
                await db.SaveChangesAsync();
            }
        }

        // I. Additional Demo Notifications
        if (await db.Notifications.CountAsync() <= 1)
        {
            db.Notifications.AddRange(
                new Notification { Title = "تنبيه انخفاض المخزون", Message = "الصنف Guest Dental & Vanity Kit (AMN-002) وصل إلى 8 وحدات أقل من حد إعادة الطلب (20).", Type = "Warning", RelatedEntityType = "InventoryItem", IsRead = false, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30) },
                new Notification { Title = "حجز جديد مؤكد", Message = "تم تأكيد الحجز RES-2026-0003 للنزيلة Sarah Johnson للغرفة 204.", Type = "Success", RelatedEntityType = "Reservation", IsRead = false, CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                new Notification { Title = "طلب صيانة قيد التنفيذ", Message = "الغرفة 202 (MNT-2026-0001) قيد الصيانة لفحص نظام التكييف.", Type = "Info", RelatedEntityType = "MaintenanceRequest", IsRead = true, CreatedAt = DateTimeOffset.UtcNow.AddHours(-5) }
            );
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

