using ASPHotelM2631.Domain.Bookings;
using ASPHotelM2631.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

internal static class BookingModel
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<RoomType>(entity =>
        {
            entity.ToTable("LOAIPHONG", table => table.HasCheckConstraint("CK_LOAIPHONG_GiaCoBan", "[GiaCoBan] >= 0"));
            entity.Property(x => x.Id).HasColumnName("MaLoaiPhong");
            entity.Property(x => x.Name).HasColumnName("TenLoaiPhong").HasMaxLength(50);
            entity.Property(x => x.BaseNightlyRate).HasColumnName("GiaCoBan").HasPrecision(18, 2);
            entity.Property(x => x.Status).HasColumnName("TrangThai").HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Room>(entity =>
        {
            entity.ToTable("PHONG");
            entity.Property(x => x.Id).HasColumnName("MaPhong");
            entity.Property(x => x.Number).HasColumnName("SoPhong").HasMaxLength(10);
            entity.Property(x => x.Floor).HasColumnName("SoTang");
            entity.Property(x => x.RoomTypeId).HasColumnName("MaLoaiPhong");
            entity.Property(x => x.Condition).HasColumnName("TrangThaiPhong").HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.Number).IsUnique();
        });

        builder.Entity<SeasonalRate>(entity =>
        {
            entity.ToTable("GIAMUAVU", table =>
            {
                table.HasCheckConstraint("CK_GIAMUAVU_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
                table.HasCheckConstraint("CK_GIAMUAVU_Gia", "[GiaApDung] >= 0");
            });
            entity.Property(x => x.Id).HasColumnName("MaGia");
            entity.Property(x => x.RoomTypeId).HasColumnName("MaLoaiPhong");
            entity.Property(x => x.Start).HasColumnName("NgayBatDau");
            entity.Property(x => x.End).HasColumnName("NgayKetThuc");
            entity.Property(x => x.NightlyRate).HasColumnName("GiaApDung").HasPrecision(18, 2);
            entity.HasOne<RoomType>().WithMany(x => x.Rates).HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GuestProfile>(entity =>
        {
            entity.ToTable("KHACHHANG");
            entity.Property(x => x.Id).HasColumnName("MaKH");
            entity.Property(x => x.FullName).HasColumnName("HoTen").HasMaxLength(100);
            entity.Property(x => x.Phone).HasColumnName("SoDienThoai").HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(150);
            entity.HasIndex(x => x.Phone);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("DATPHONG", table =>
            {
                table.HasCheckConstraint("CK_DATPHONG_Ngay", "[NgayTra] > [NgayNhan]");
                table.HasCheckConstraint("CK_DATPHONG_SoKhach", "[SoKhach] > 0");
                table.HasCheckConstraint("CK_DATPHONG_Tien",
                    "[TongTien] >= 0 AND [DaThu] >= 0 AND [DaThu] <= [TongTien] AND ([SoTienHoan] IS NULL OR ([SoTienHoan] >= 0 AND [SoTienHoan] <= [DaThu]))");
            });
            entity.Property(x => x.Id).HasColumnName("MaDatPhong");
            entity.Property(x => x.GuestId).HasColumnName("MaKH");
            entity.Property(x => x.RoomId).HasColumnName("MaPhong");
            entity.Property(x => x.CreatedByUserId).HasColumnName("NguoiTaoId").HasMaxLength(450);
            entity.Property(x => x.CheckIn).HasColumnName("NgayNhan");
            entity.Property(x => x.CheckOut).HasColumnName("NgayTra");
            entity.Property(x => x.GuestCount).HasColumnName("SoKhach");
            entity.Property(x => x.Channel).HasColumnName("KenhDatPhong").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasColumnName("TrangThaiDatPhong").HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Total).HasColumnName("TongTien").HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasColumnName("DaThu").HasPrecision(18, 2);
            entity.Property(x => x.RefundAmount).HasColumnName("SoTienHoan").HasPrecision(18, 2);
            entity.Property(x => x.CancelledAt).HasColumnName("HuyLuc");
            entity.HasOne(x => x.Guest).WithMany().HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });
        });
    }
}
