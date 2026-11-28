using ASPHotelM2631.Domain.Entities;
using ASPHotelM2631.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Keep this call so ASP.NET Identity tables are included in the model.
        base.OnModelCreating(builder);

        builder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(otp => otp.Id);
            entity.Property(otp => otp.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(otp => otp.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Guest>(entity =>
        {
            entity.HasKey(guest => guest.GuestId);
            entity.Property(guest => guest.FullName).HasMaxLength(150).IsRequired();
            entity.Property(guest => guest.Phone).HasMaxLength(20);
            entity.Property(guest => guest.Email).HasMaxLength(256);
            entity.Property(guest => guest.IdNumber).HasMaxLength(50);
            entity.Property(guest => guest.UserId).HasMaxLength(450);
            entity.HasIndex(guest => guest.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(guest => guest.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RoomType>(entity =>
        {
            entity.Property(roomType => roomType.Id).HasColumnName("RoomTypeId");
            entity.Property(roomType => roomType.Name).HasMaxLength(100).IsRequired();
            entity.Property(roomType => roomType.Description).HasMaxLength(1000);
            entity.Property(roomType => roomType.Amenities).HasMaxLength(1000).IsRequired();
            entity.Property(roomType => roomType.ImageUrl).HasMaxLength(2048);
            entity.Property(roomType => roomType.BasePrice).HasPrecision(18, 2);
        });

        builder.Entity<Room>(entity =>
        {
            entity.Property(room => room.Id).HasColumnName("RoomId");
            entity.Property(room => room.RoomNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(room => room.RoomNumber).IsUnique();
            entity.Property(room => room.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(room => room.RoomType)
                .WithMany(roomType => roomType.Rooms)
                .HasForeignKey(room => room.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.Property(booking => booking.Id).HasColumnName("BookingId");
            entity.Property(booking => booking.Code).HasMaxLength(12).IsRequired();
            entity.HasIndex(booking => booking.Code).IsUnique();
            entity.Property(booking => booking.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(booking => booking.Source).HasMaxLength(20).IsRequired();
            entity.Property(booking => booking.CancelReason).HasMaxLength(200);
            entity.Property(booking => booking.NightlyRate).HasPrecision(18, 2);
            entity.Property(booking => booking.TotalAmount).HasPrecision(18, 2);
            entity.Property(booking => booking.RowVersion).IsRowVersion();
            entity.HasIndex(booking => new { booking.RoomId, booking.CheckInDate, booking.CheckOutDate });
            entity.HasIndex(booking => new { booking.RoomTypeId, booking.CheckInDate, booking.CheckOutDate });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Booking_Dates", "[CheckOutDate] > [CheckInDate]");
                table.HasCheckConstraint("CK_Booking_Guests", "[GuestCount] >= 1");
            });

            entity.HasOne(booking => booking.Guest)
                .WithMany(guest => guest.Bookings)
                .HasForeignKey(booking => booking.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(booking => booking.RoomType)
                .WithMany(roomType => roomType.Bookings)
                .HasForeignKey(booking => booking.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(booking => booking.Room)
                .WithMany()
                .HasForeignKey(booking => booking.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.PaymentId);
            entity.Property(payment => payment.Type).HasMaxLength(20).IsRequired();
            entity.Property(payment => payment.Method).HasMaxLength(20).IsRequired();
            entity.Property(payment => payment.Status).HasMaxLength(20).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.GatewayRef).HasMaxLength(50);
            entity.Property(payment => payment.TxnRef).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.ResponseCode).HasMaxLength(10);
            entity.HasIndex(payment => payment.TxnRef).IsUnique();
            entity.HasIndex(payment => payment.GatewayRef)
                .IsUnique()
                .HasFilter("[GatewayRef] IS NOT NULL");
            entity.HasIndex(payment => payment.BookingId);
            entity.HasOne(payment => payment.Booking)
                .WithMany()
                .HasForeignKey(payment => payment.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
                table.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
        });

    }
}
