# Kết nối SQL Server database có sẵn

## 1. Không tạo hoặc xóa database

Database trong file SQL bạn gửi tên `HotelManagement`. File bắt đầu bằng lệnh xóa database cùng tên rồi mới tạo lại. **Không chạy toàn bộ file đó trên database đang có dữ liệu.** Không chạy `dotnet ef database update`, `EnsureCreated()` hoặc migration cho mục đích kết nối này.

Đổi connection string không tạo hay xóa database. Ứng dụng hiện không tự chạy migration khi khởi động.

## 2. Cấu hình chuỗi kết nối

Mặc định trong `src/Api/appsettings.json` trỏ đến SQL Server LocalDB `HotelManagement`. Nếu database đang ở SQL Server/instance khác, mở PowerShell tại thư mục gốc repo và đặt connection string cho phiên chạy hiện tại.

Windows Authentication:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=TEN_MAY_CHU\TEN_INSTANCE;Database=HotelManagement;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
```

SQL Server Authentication:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=TEN_MAY_CHU\TEN_INSTANCE;Database=HotelManagement;User Id=TEN_USER;Password=MAT_KHAU;Encrypt=True;TrustServerCertificate=True"
```

Thay các giá trị mẫu theo SQL Server của bạn. Nếu dùng Azure SQL, thường dùng `Encrypt=True` và cấu hình chứng chỉ theo môi trường. Không đưa user/password thật vào Git hoặc chia sẻ connection string chứa bí mật.

## 3. Chạy riêng API và giao diện

API cho phép CORS từ `http://localhost:5500` và `http://127.0.0.1:5500` trong `src/Api/appsettings.json`. Trình duyệt coi hai hostname này là hai origin khác nhau. Để đổi địa chỉ frontend, thêm origin chính xác vào `Cors:AllowedOrigins`.

Mở terminal thứ nhất ở thư mục gốc repository. Cấu hình chạy `Api` đặt môi trường `Development` và HTTP tại `http://localhost:5000`, nên sẽ không chuyển hướng sang HTTPS khi chạy local. Nếu dùng SQL Server khác LocalDB, đặt connection string trong chính terminal này:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=TEN_MAY_CHU\TEN_INSTANCE;Database=HotelManagement;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
dotnet run --project .\src\Api\Api.csproj --urls http://localhost:5000
```

Nếu dùng LocalDB mặc định, bỏ dòng đặt biến môi trường. Lệnh `dotnet run` tiếp tục chạy và hiển thị log trong terminal; giữ terminal mở.

Mở terminal thứ hai, dùng VS Code Live Server mở `src/Frontend/index.html`; Live Server mặc định phục vụ ở `http://localhost:5500`. Hoặc chạy một static file server tương đương tại origin đó. Không mở HTML trực tiếp bằng `file://`. Frontend chọn hostname API khớp hostname trang trong `src/Frontend/config.js`.

Frontend có thể mở bằng `localhost` hoặc `127.0.0.1`; cả hai đã được cho phép trong cấu hình CORS. Dùng cùng hostname cho cả giao diện và API để trình duyệt gửi cookie đăng nhập.

## 4. Ánh xạ vào schema hiện có

File SQL có bảng `KhachHang` và `TaiKhoan`; thông tin đăng nhập ở `Email`, `SoDienThoai`, `MatKhauHash`, trạng thái khóa ở `SoLanSaiLienTiep` và `KhoaDen`. Mật khẩu được ghi chú là BCrypt.

Backend ánh xạ khách hàng tới `dbo.KhachHang`, phiên đăng nhập tới `dbo.PhienDangNhap` và nhật ký tới `dbo.LichSuDangNhap`. Mật khẩu mới được hash BCrypt; mật khẩu BCrypt hiện có được xác minh trực tiếp. Lần đăng ký mới được đặt `TrangThai = 'HoatDong'` (theo lựa chọn hiện tại, chưa có OTP).

- Sau 5 lần nhập sai, tài khoản được đặt `TamKhoa` và `KhoaDen` cách thời điểm khóa 15 phút; thao tác cập nhật bộ đếm được tuần tự hóa theo hàng khách hàng.
- Token phiên ngẫu nhiên chỉ lưu dạng SHA-256 trong bảng `PhienDangNhap`; cookie `HttpOnly` được kiểm tra lại với bảng phiên ở mỗi request được bảo vệ. Phiên hết hạn sau 30 phút không hoạt động; mỗi request được xác thực gia hạn thời điểm hết hạn thêm 30 phút ở cả database và cookie.
- Chỉ tài khoản khách (`KhachHang`) được hỗ trợ qua form hiện tại. Tài khoản nhân viên (`TaiKhoan`) chưa được xử lý.

Ứng dụng không tự tạo database, không chạy migration và không gọi `EnsureCreated`. Kết nối yêu cầu database đã có đúng các bảng/cột trong file SQL. API hiện có là `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` và `POST /api/auth/logout`.
