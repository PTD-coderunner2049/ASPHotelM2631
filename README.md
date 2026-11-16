# Hotel Management — xác thực tài khoản

Ứng dụng đăng ký/đăng nhập dùng ASP.NET Core 9, Clean Architecture và SQL Server hiện có.

```text
docs/                     # Hướng dẫn cấu hình và API
src/
├── Api/                  # ASP.NET Core Web API, controller và cấu hình
├── Frontend/             # Giao diện HTML/CSS/JS độc lập
├── Application/          # Use case và abstraction
├── Domain/               # Entity nghiệp vụ
└── Infrastructure/       # EF Core và adapter cho schema SQL Server hiện có
tests/                    # Bài kiểm thử
```

## Kết nối database sẵn có

Yêu cầu .NET SDK 9 và SQL Server/LocalDB. Xem [hướng dẫn kết nối database và chạy riêng frontend/API](./docs/authentication.md).

**Không chạy script SQL được gửi kèm trên database đang dùng:** script đó có lệnh `DROP DATABASE HotelManagement`. Không chạy `database update` hoặc tạo migration để kết nối database có sẵn.

Đăng ký/đăng nhập khách hàng sử dụng bảng `KhachHang`, các hash BCrypt hiện có, và bảng `PhienDangNhap`/`LichSuDangNhap` từ schema gửi kèm. Tài khoản nhân viên trong `TaiKhoan` chưa được bật qua form này.
