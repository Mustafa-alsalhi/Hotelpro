using HotelPro.Domain.Entities.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelPro.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservationNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.ServiceChargeAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositRequired).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.ReservationNumber).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CheckInDate, x.CheckOutDate });

        builder.HasOne(x => x.PrimaryGuest)
            .WithMany()
            .HasForeignKey(x => x.PrimaryGuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RatePlan)
            .WithMany()
            .HasForeignKey(x => x.RatePlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReservationRoomConfiguration : IEntityTypeConfiguration<ReservationRoom>
{
    public void Configure(EntityTypeBuilder<ReservationRoom> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AgreedNightlyRate).HasPrecision(18, 2);
        builder.Property(x => x.TotalRoomAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.RoomId, x.CheckInDate, x.CheckOutDate });

        builder.HasOne(x => x.Reservation)
            .WithMany(r => r.Rooms)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RoomType)
            .WithMany()
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReservationGuestConfiguration : IEntityTypeConfiguration<ReservationGuest>
{
    public void Configure(EntityTypeBuilder<ReservationGuest> builder)
    {
        builder.HasKey(x => new { x.ReservationId, x.GuestId });

        builder.HasOne(x => x.Reservation)
            .WithMany(r => r.Guests)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Guest)
            .WithMany()
            .HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReservationStatusHistoryConfiguration : IEntityTypeConfiguration<ReservationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReservationStatusHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.ChangedBy).HasMaxLength(150);

        builder.HasOne(x => x.Reservation)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

