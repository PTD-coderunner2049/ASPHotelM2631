using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Persistence;

public sealed class KhachHang
{
    public int MaKH { get; set; }
    public string HoTen { get; set; } = "";
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string MatKhauHash { get; set; } = "";
    public string TrangThai { get; set; } = "ChoXacThuc";
    public byte SoLanSaiLienTiep { get; set; }
    public DateTime? KhoaDen { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}

public sealed class PhienDangNhap
{
    public long MaPhien { get; set; }
    public int? MaKH { get; set; }
    public string TokenHash { get; set; } = "";
    public string? DiaChiIP { get; set; }
    public string? UserAgent { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime HetHan { get; set; }
    public bool ThuHoi { get; set; }
    public DateTime? NgayThuHoi { get; set; }
}

public sealed class LichSuDangNhap
{
    public long MaLichSu { get; set; }
    public int? MaKH { get; set; }
    public string DinhDanhNhap { get; set; } = "";
    public string KetQua { get; set; } = "";
    public string? DiaChiIP { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ThoiGian { get; set; }
}

public sealed class HotelDbContext(DbContextOptions<HotelDbContext> options) : DbContext(options)
{
    public DbSet<KhachHang> KhachHang => Set<KhachHang>();
    public DbSet<PhienDangNhap> PhienDangNhap => Set<PhienDangNhap>();
    public DbSet<LichSuDangNhap> LichSuDangNhap => Set<LichSuDangNhap>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("KhachHang");
            entity.HasKey(user => user.MaKH);
            entity.Property(user => user.HoTen).HasMaxLength(100);
            entity.Property(user => user.Email).HasMaxLength(150).IsUnicode(false);
            entity.Property(user => user.SoDienThoai).HasMaxLength(15).IsUnicode(false);
            entity.Property(user => user.MatKhauHash).HasMaxLength(255).IsUnicode(false);
            entity.Property(user => user.TrangThai).HasMaxLength(15).IsUnicode(false);
            entity.Property(user => user.SoLanSaiLienTiep).HasColumnType("tinyint");
            entity.Property(user => user.KhoaDen).HasColumnType("datetime2");
            entity.Property(user => user.NgayTao).HasColumnType("datetime2");
            entity.Property(user => user.NgayCapNhat).HasColumnType("datetime2");
        });

        builder.Entity<PhienDangNhap>(entity =>
        {
            entity.ToTable("PhienDangNhap");
            entity.HasKey(session => session.MaPhien);
            entity.Property(session => session.TokenHash).HasMaxLength(64).IsUnicode(false).IsFixedLength();
            entity.Property(session => session.DiaChiIP).HasMaxLength(45).IsUnicode(false);
            entity.Property(session => session.UserAgent).HasMaxLength(255).IsUnicode(false);
            entity.Property(session => session.NgayTao).HasColumnType("datetime2");
            entity.Property(session => session.HetHan).HasColumnType("datetime2");
            entity.Property(session => session.NgayThuHoi).HasColumnType("datetime2");
        });

        builder.Entity<LichSuDangNhap>(entity =>
        {
            entity.ToTable("LichSuDangNhap");
            entity.HasKey(log => log.MaLichSu);
            entity.Property(log => log.DinhDanhNhap).HasMaxLength(150).IsUnicode(false);
            entity.Property(log => log.KetQua).HasMaxLength(20).IsUnicode(false);
            entity.Property(log => log.DiaChiIP).HasMaxLength(45).IsUnicode(false);
            entity.Property(log => log.UserAgent).HasMaxLength(255).IsUnicode(false);
            entity.Property(log => log.ThoiGian).HasColumnType("datetime2");
        });
    }
}
