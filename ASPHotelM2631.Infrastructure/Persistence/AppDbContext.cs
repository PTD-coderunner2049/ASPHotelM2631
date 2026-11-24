using ASPHotelM2631.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // bắt buộc giữ dòng này, nếu không mất các bảng AspNet...

        builder.Entity<EmailOtp>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.CodeHash).HasMaxLength(64).IsRequired();
            e.HasIndex(o => o.UserId);
            e.HasOne<ApplicationUser>().WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);   // xóa người dùng thì xóa luôn mã OTP của họ
        });
    }
}