# Test Case Specification

_Sinh tự động từ code bởi `tools/Generate-TestDocs.py` lúc 2026-11-22 10:06 (commit local). Không sửa tay - sửa attribute `[TcFact]` trong code._

Tổng số test case: **44** (suite: Unit,Api)

## Suite: Api

| ID | Tiêu đề | Ưu tiên | Req | Điều kiện | Các bước | Kết quả mong đợi |
|---|---|---|---|---|---|---|
| TC-API-LOGIN-001 | POST /api/sessions đúng thông tin -&gt; 201 kèm JWT, hạn dùng và thông tin user | High | - | Đã đăng ký tài khoản | POST /api/sessions {identifier, password} | 201; tokenType='Bearer'; token là JWT 3 đoạn; expiresAt ở tương lai; user.role='KhachHang'; user.fullName đúng; không lộ mật khẩu |
| TC-API-LOGIN-002 | Sai mật khẩu và tài khoản không tồn tại trả CÙNG một thông báo 401 (không lộ tài khoản có tồn tại) | High | - | - | Login sai mật khẩu<br>Login tài khoản không tồn tại | Cả hai 401, body.message giống hệt nhau, không có token |
| TC-API-LOGIN-003 | POST /api/sessions dữ liệu sai định dạng -&gt; 400 | Medium | - | - | POST /api/sessions với &lt;identifier&gt;,&lt;password&gt; | HTTP 400 |
| TC-API-LOGIN-004 | Sai mật khẩu 5 lần -&gt; khóa (423); khi đang khóa, mật khẩu ĐÚNG cũng bị từ chối | High | - | Tài khoản mới | Login sai x4<br>Login sai lần 5<br>Login đúng mật khẩu | 4 lần đầu 401; lần 5 = 423; lần đúng mật khẩu = 423, không có token |
| TC-API-LOGIN-005 | Đăng nhập không phân biệt hoa/thường với email, bỏ dấu phân cách với SĐT | Medium | - | - | Đăng ký email thường -&gt; login bằng email HOA<br>Đăng ký SĐT -&gt; login bằng SĐT có dấu '-' | Cả hai 201 |
| TC-API-LOGIN-006 | Lockout của một tài khoản không ảnh hưởng tài khoản khác | Medium | - | - | Khóa tài khoản A<br>Login tài khoản B đúng mật khẩu | B vẫn 201 |
| TC-API-REG-001 | POST /api/users bằng email hợp lệ -&gt; 201 và thông báo thành công | High | - | DB đã có đủ 6 role | POST /api/users {fullName, identifier=email, password} | HTTP 201; body.message = 'Đăng ký thành công.'; body không chứa mật khẩu |
| TC-API-REG-002 | POST /api/users bằng SĐT hợp lệ (có dấu cách) -&gt; 201 | Medium | - | - | POST /api/users identifier='0912 345 678' | HTTP 201 |
| TC-API-REG-003 | Đăng ký trùng tài khoản -&gt; 409; trùng cả khi khác hoa/thường | High | - | - | Đăng ký email X (201)<br>Đăng ký lại X<br>Đăng ký lại X viết HOA | Lần 1: 201 (đối chứng). Lần 2 và 3: 409 + message 'đã được đăng ký' |
| TC-API-REG-004 | POST /api/users dữ liệu không hợp lệ -&gt; 400 | High | - | - | POST /api/users với &lt;name&gt;,&lt;identifier&gt;,&lt;password&gt; sai | HTTP 400; message 'Thông tin đăng ký không hợp lệ.' |
| TC-API-REG-005 | POST /api/users body thiếu/hỏng -&gt; 400 (không 500) | Medium | - | - | POST body: '{}' / JSON hỏng / chuỗi rỗng, Content-Type json | HTTP 400 |
| TC-API-REG-006 | POST /api/users sai Content-Type -&gt; 415 | Medium | - | - | POST body JSON nhưng Content-Type text/plain | HTTP 415 |
| TC-API-REG-008 | Đăng ký trên DB mới migrate (chưa seed role) vẫn phải trả 201, không 500 | High | - | DB trống, chưa seed role KhachHang | POST /api/users hợp lệ | HTTP 201 (hoặc ứng dụng tự tạo role khi thiếu). KHÔNG được 500 và không để lại user mồ côi |
| TC-API-ROLE-001 | Các endpoint phân quyền yêu cầu đăng nhập: không token -&gt; 401 | High | - | - | GET &lt;endpoint&gt; không Authorization | 401 (không phải 200 hay 403) |
| TC-API-SESS-002 | GET /api/sessions/current không token / token rác -&gt; 401 | High | - | - | GET với header Authorization = &lt;giá trị&gt; | 401 |
| TC-API-SESS-006 | DELETE /api/sessions/current không token -&gt; 401 | Medium | - | - | DELETE không header Authorization | 401 |
| TC-API-TLS-001 | Gọi API bằng HTTP bị chuyển hướng sang HTTPS (307), giữ nguyên đường dẫn | High | - | - | GET http://localhost/api/customer (không tự theo redirect) | 307; Location bắt đầu bằng https:// và kết thúc bằng /api/customer |
| TC-API-TLS-002 | Đăng nhập qua HTTP KHÔNG được xử lý: bị redirect và không trả token | High | - | Tài khoản đã tồn tại (đối chứng: login qua HTTPS thành công) | Đăng ký + login qua HTTPS (201)<br>POST /api/sessions qua HTTP | Qua HTTPS: 201. Qua HTTP: 307, body không chứa 'token' |
| TC-API-TLS-003 | Gọi qua HTTPS không bị redirect (không vòng lặp chuyển hướng) | Medium | - | - | POST /api/sessions với dữ liệu sai định dạng qua https://localhost | 400 (do logic nghiệp vụ), KHÔNG phải 307 |
| TC-API-TLS-004 | Môi trường Production gửi header HSTS khi truy cập HTTPS (host không phải localhost) | Medium | - | ASPNETCORE_ENVIRONMENT=Production | GET https://hotel.example/api/customer | Response có header Strict-Transport-Security chứa max-age= |

## Suite: Unit

| ID | Tiêu đề | Ưu tiên | Req | Điều kiện | Các bước | Kết quả mong đợi |
|---|---|---|---|---|---|---|
| TC-ID-001 | Normalize: email được trim và hạ chữ thường | High | - | - | Gọi Normalize(&lt;input&gt;) | Trả đúng chuỗi chuẩn hoá (thường, không khoảng trắng đầu/cuối) |
| TC-ID-002 | Normalize: SĐT bỏ khoảng trắng, dấu chấm, gạch ngang | High | - | - | Gọi Normalize(&lt;input&gt;) | Chỉ còn chữ số (và '+' đầu nếu có) |
| TC-ID-003 | Normalize: null/rỗng trả về chuỗi rỗng, không ném lỗi | Medium | - | - | Gọi Normalize(null / '' / '   ') | Trả "" |
| TC-ID-004 | IsValid: chấp nhận email và SĐT VN đúng định dạng | High | - | - | Gọi IsValid(&lt;input&gt;) | true |
| TC-ID-005 | IsValid: từ chối định dạng sai (biên độ dài SĐT, thiếu @, thiếu tên miền...) | High | - | - | Gọi IsValid(&lt;input&gt;) | false |
| TC-ID-006 | Normalize rồi IsValid: email có khoảng trắng GIỮA vẫn bị từ chối (không bị 'dọn' thành hợp lệ) | Medium | - | - | IsValid(Normalize('a b@c.com')) | false |
| TC-JWT-001 | Token chứa đủ claim sub/name/fullName/securityStamp/role, issuer và audience đúng | High | - | - | CreateToken('u1','user','Ten Day Du',['KhachHang'],'stamp-1') | Các claim đúng giá trị; iss='iss'; aud='aud' |
| TC-JWT-002 | Nhiều vai trò -&gt; nhiều claim role | Medium | - | - | CreateToken(..., roles=['LeTan','Manager'], ...) | Có đúng 2 claim role: LeTan và Manager |
| TC-JWT-003 | Thời hạn token theo Jwt:ExpireMinutes; thiếu/sai -&gt; mặc định 15 phút | Medium | - | - | CreateToken với ExpireMinutes=&lt;cfg&gt; | ExpiresAt ~ now + &lt;phút&gt; (sai số 1 phút) và khớp claim exp |
| TC-JWT-004 | Chữ ký hợp lệ với đúng key và bị từ chối với key khác | High | - | - | Validate token bằng key đúng, rồi bằng key sai | Key đúng: hợp lệ; key sai: SecurityTokenSignatureKeyNotFound/InvalidSignature |
| TC-JWT-005 | Thiếu Jwt:Key -&gt; ném InvalidOperationException (fail-fast, không ký bằng key rỗng) | Medium | - | - | Cấu hình không có Jwt:Key; CreateToken | InvalidOperationException |
| TC-LOGIN-001 | Đăng nhập hợp lệ: store nhận định danh đã chuẩn hoá, mật khẩu GIỮ NGUYÊN, kết quả trả nguyên vẹn | High | - | - | SignInAsync('  A@Hotel.VN ', ' Abcd1234 ') | Store.SignInAsync gọi 1 lần ('a@hotel.vn', ' Abcd1234 ' - không trim mật khẩu); trả đúng object của store |
| TC-LOGIN-002 | Đăng nhập: định danh sai hoặc mật khẩu rỗng bị chặn trước khi chạm store | High | - | - | SignInAsync(&lt;id&gt;,&lt;pw&gt;) | InvalidInput và Store.SignInAsync KHÔNG được gọi |
| TC-LOGIN-003 | Đăng nhập: các trạng thái thất bại từ store (sai mật khẩu, khóa) được trả nguyên vẹn, không kèm token | High | - | - | Store trả &lt;status&gt;; SignInAsync(hợp lệ) | Status = &lt;status&gt;, Token = null |
| TC-LOGOUT-001 | Đăng xuất: service uỷ quyền cho store đúng 1 lần | Medium | - | - | SignOutAsync() | Store.SignOutAsync được gọi đúng 1 lần |
| TC-PW-001 | Mật khẩu hợp lệ: &gt;= 8 ký tự, có chữ và số | High | - | - | IsStrongPassword(&lt;pw&gt;) | true |
| TC-PW-002 | Mật khẩu không hợp lệ: ngắn / thiếu chữ / thiếu số / null | High | - | - | IsStrongPassword(&lt;pw&gt;) | false |
| TC-REG-001 | Đăng ký hợp lệ: chuyển cho store dữ liệu đã chuẩn hoá, trả đúng kết quả của store | High | - | - | RegisterAsync('  Nguyen Van A  ', '  A@Hotel.VN ', 'Abcd1234') | Store.CreateAsync gọi đúng 1 lần với ('Nguyen Van A','a@hotel.vn','Abcd1234'); kết quả = kết quả store |
| TC-REG-002 | Đăng ký bằng SĐT có dấu phân cách: store nhận SĐT đã bỏ dấu | Medium | - | - | RegisterAsync('B', '0912 345-678', 'Abcd1234') | Store nhận '0912345678' |
| TC-REG-003 | Đăng ký: dữ liệu sai bị chặn TRƯỚC khi chạm store | High | - | - | RegisterAsync(&lt;name&gt;,&lt;id&gt;,&lt;pw&gt;) | InvalidInput và Store.CreateAsync KHÔNG được gọi |
| TC-REG-004 | Đăng ký: tên dài 100 ký tự được chấp nhận, 101 ký tự bị từ chối (biên) | Medium | - | - | RegisterAsync(100 ký tự) và RegisterAsync(101 ký tự) | 100 -&gt; gọi store; 101 -&gt; InvalidInput, không gọi store |
| TC-REG-005 | Đăng ký: null không làm service ném exception | Medium | - | - | RegisterAsync(null, null, null) | InvalidInput, không ném lỗi, không gọi store |
| TC-REG-006 | Đăng ký: trạng thái từ store (Duplicate...) được trả nguyên vẹn | High | - | - | Store trả &lt;status&gt;; RegisterAsync(dữ liệu hợp lệ) | Service trả đúng &lt;status&gt; |
| TC-ROLE-001 | RoleNames.All chứa đủ 6 vai trò, không trùng, đúng giá trị chuỗi (đang là khoá trong DB) | High | - | - | Đọc RoleNames.All | Đúng 6 giá trị: KhachHang, LeTan, BuongPhong, KeToan, Manager, Admin |
