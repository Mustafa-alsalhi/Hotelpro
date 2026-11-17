using HotelPro.Domain.Entities.Guests;
using HotelPro.Domain.Entities.Hotel;
using HotelPro.Domain.Entities.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPro.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(250);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.TaxNumber).HasMaxLength(80);
    }
}

public class HotelSettingsConfiguration : IEntityTypeConfiguration<HotelSettings>
{
    public void Configure(EntityTypeBuilder<HotelSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.CurrencySymbol).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DateFormat).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DefaultLanguage).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        builder.Property(x => x.ServiceChargePercent).HasPrecision(5, 2);
        builder.Property(x => x.CancellationFeePercent).HasPrecision(5, 2);

        builder.HasIndex(x => x.HotelId).IsUnique();
        builder.HasOne(x => x.Hotel)
            .WithOne(h => h.Settings)
            .HasForeignKey<HotelSettings>(x => x.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(60);
        builder.Property(x => x.Category).HasMaxLength(60);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
        builder.Property(x => x.AreaSqm).HasPrecision(8, 2);
        builder.HasIndex(x => new { x.HotelId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RoomTypeAmenityConfiguration : IEntityTypeConfiguration<RoomTypeAmenity>
{
    public void Configure(EntityTypeBuilder<RoomTypeAmenity> builder)
    {
        builder.HasKey(x => new { x.RoomTypeId, x.AmenityId });
        builder.HasOne(x => x.RoomType).WithMany(rt => rt.RoomTypeAmenities).HasForeignKey(x => x.RoomTypeId);
        builder.HasOne(x => x.Amenity).WithMany(a => a.RoomTypeAmenities).HasForeignKey(x => x.AmenityId);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoomNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BasePriceOverride).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.HotelId, x.RoomNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Floor, x.Status });
        builder.HasOne(x => x.RoomType).WithMany(rt => rt.Rooms).HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RoomAmenityConfiguration : IEntityTypeConfiguration<RoomAmenity>
{
    public void Configure(EntityTypeBuilder<RoomAmenity> builder)
    {
        builder.HasKey(x => new { x.RoomId, x.AmenityId });
        builder.HasOne(x => x.Room).WithMany(r => r.RoomAmenities).HasForeignKey(x => x.RoomId);
        builder.HasOne(x => x.Amenity).WithMany(a => a.RoomAmenities).HasForeignKey(x => x.AmenityId);
    }
}

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Nationality).HasMaxLength(80);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.FullName);
        builder.HasIndex(x => x.Phone);
        builder.HasIndex(x => x.Email);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class GuestDocumentConfiguration : IEntityTypeConfiguration<GuestDocument>
{
    public void Configure(EntityTypeBuilder<GuestDocument> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNumber).HasMaxLength(60).IsRequired();
        builder.Property(x => x.IssuingCountry).HasMaxLength(80);
        builder.HasIndex(x => x.DocumentNumber);

        builder.HasOne(x => x.Guest)
            .WithMany(g => g.Documents)
            .HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

