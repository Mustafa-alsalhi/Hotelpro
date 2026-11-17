using HotelPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelPro.Tests;

public class HotelProWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"HotelProTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HotelProDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<HotelProDbContext>>();
            services.AddDbContext<HotelProDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelProDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed permissions, roles, and demo users without calling MigrateAsync (InMemory)
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<HotelPro.Domain.Entities.Identity.ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<HotelPro.Domain.Entities.Identity.ApplicationUser>>();

        if (!await db.Permissions.AnyAsync())
        {
            foreach (var def in HotelPro.Domain.Constants.PermissionsCatalog.GetAll())
            {
                db.Permissions.Add(new HotelPro.Domain.Entities.Identity.Permission
                {
                    Code = def.Code,
                    Module = def.Module,
                    Action = def.Action,
                    Description = def.Description
                });
            }
            await db.SaveChangesAsync();
        }

        var perms = await db.Permissions.ToListAsync();

        if (!await roleManager.RoleExistsAsync(HotelPro.Domain.Constants.RoleNames.SuperAdmin))
        {
            var adminRole = new HotelPro.Domain.Entities.Identity.ApplicationRole
            {
                Name = HotelPro.Domain.Constants.RoleNames.SuperAdmin,
                Description = "Super Admin",
                IsSystemRole = true
            };
            await roleManager.CreateAsync(adminRole);
            foreach (var p in perms)
            {
                db.RolePermissions.Add(new HotelPro.Domain.Entities.Identity.RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = p.Id
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await roleManager.RoleExistsAsync(HotelPro.Domain.Constants.RoleNames.Receptionist))
        {
            var recRole = new HotelPro.Domain.Entities.Identity.ApplicationRole
            {
                Name = HotelPro.Domain.Constants.RoleNames.Receptionist,
                Description = "Receptionist",
                IsSystemRole = true
            };
            await roleManager.CreateAsync(recRole);
            var recPerm = perms.First(p => p.Code == HotelPro.Domain.Constants.PermissionsCatalog.Dashboard.View);
            db.RolePermissions.Add(new HotelPro.Domain.Entities.Identity.RolePermission
            {
                RoleId = recRole.Id,
                PermissionId = recPerm.Id
            });
            await db.SaveChangesAsync();
        }

        if (await userManager.FindByEmailAsync("admin@test.local") is null)
        {
            var admin = new HotelPro.Domain.Entities.Identity.ApplicationUser
            {
                UserName = "admin@test.local",
                Email = "admin@test.local",
                EmailConfirmed = true,
                FullName = "Test Admin",
                IsActive = true
            };
            await userManager.CreateAsync(admin, "Admin@12345");
            await userManager.AddToRoleAsync(admin, HotelPro.Domain.Constants.RoleNames.SuperAdmin);
        }

        if (await userManager.FindByEmailAsync("reception@test.local") is null)
        {
            var rec = new HotelPro.Domain.Entities.Identity.ApplicationUser
            {
                UserName = "reception@test.local",
                Email = "reception@test.local",
                EmailConfirmed = true,
                FullName = "Test Reception",
                IsActive = true
            };
            await userManager.CreateAsync(rec, "Reception@12345");
            await userManager.AddToRoleAsync(rec, HotelPro.Domain.Constants.RoleNames.Receptionist);
        }

        if (!await db.Hotels.AnyAsync())
        {
            var hotel = new HotelPro.Domain.Entities.Hotel.Hotel
            {
                Name = "Test Hotel",
                Address = "Test Address",
                Phone = "0500000000",
                Email = "test@hotel.local"
            };
            hotel.Settings = new HotelPro.Domain.Entities.Hotel.HotelSettings { HotelId = hotel.Id };
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
        }
    }
}

