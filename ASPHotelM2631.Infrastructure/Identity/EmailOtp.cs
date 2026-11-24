namespace ASPHotelM2631.Infrastructure.Identity;

public sealed class EmailOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string CodeHash { get; set; } = "";   // không lưu mã gốc
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? UsedAt { get; set; }        // có giá trị = đã dùng hoặc bị thay bằng mã mới
}