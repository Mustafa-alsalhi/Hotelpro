using HotelPro.Application.Common.Interfaces;
using HotelPro.Domain.Entities.Identity;
using HotelPro.Infrastructure.Persistence;
using HotelPro.Infrastructure.Security;
using HotelPro.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<HotelProDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(HotelProDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<HotelProDbContext>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IHotelSettingsService, HotelSettingsService>();
        services.AddScoped<IRoomTypeService, RoomTypeService>();
        services.AddScoped<IAmenityService, AmenityService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IGuestService, GuestService>();

        services.AddScoped<IRateManagementService, RateManagementService>();
        services.AddScoped<IReservationService, ReservationService>();

        services.AddScoped<IFrontDeskService, FrontDeskService>();
        services.AddScoped<IGuestServicesAppService, GuestServicesAppService>();
        services.AddScoped<IHousekeepingService, HousekeepingService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();

        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IFinanceService, FinanceService>();

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IInventoryService, InventoryService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

