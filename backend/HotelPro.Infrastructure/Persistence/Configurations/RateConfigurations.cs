using HotelPro.Domain.Entities.Rates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPro.Infrastructure.Persistence.Configurations;

public class RatePlanConfiguration : IEntityTypeConfiguration<RatePlan>
{
    public void Configure(EntityTypeBuilder<RatePlan> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.WeekendMultiplier).HasPrecision(5, 2);
        builder.Property(x => x.LongStayDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.CorporateCode).HasMaxLength(50);

        builder.HasIndex(x => new { x.HotelId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RoomRateConfiguration : IEntityTypeConfiguration<RoomRate>
{
    public void Configure(EntityTypeBuilder<RoomRate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PricePerNight).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.RoomTypeId, x.RatePlanId, x.StartDate, x.EndDate });

        builder.HasOne(x => x.RoomType)
            .WithMany()
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RatePlan)
            .WithMany(rp => rp.RoomRates)
            .HasForeignKey(x => x.RatePlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

