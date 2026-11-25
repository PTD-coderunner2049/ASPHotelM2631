# Test Result Report

- Thời gian: 2026-11-25 11:05
- Commit: local
- Số file TRX đọc: 2
- Tổng test case: 44  (FAIL: 1 | PASS: 43)
- **Kết luận: FAILED**

| ID | Suite | Tiêu đề | Kết quả | Số lần chạy | Thời gian (s) | Bug | Ghi chú / lỗi | Bằng chứng |
|---|---|---|---|---|---|---|---|---|
| TC-API-LOGIN-001 | Api | POST /api/sessions đúng thông tin -> 201 kèm JWT, hạn dùng và thông tin user | PASS | 1 | 1.45 | - | - | - |
| TC-API-LOGIN-002 | Api | Sai mật khẩu và tài khoản không tồn tại trả CÙNG một thông báo 401 (không lộ tài khoản có tồn tại) | PASS | 1 | 0.43 | - | - | - |
| TC-API-LOGIN-003 | Api | POST /api/sessions dữ liệu sai định dạng -> 400 | PASS | 2 | 0.53 | - | - | - |
| TC-API-LOGIN-004 | Api | Sai mật khẩu 5 lần -> khóa (423); khi đang khóa, mật khẩu ĐÚNG cũng bị từ chối | PASS | 1 | 0.58 | - | - | - |
| TC-API-LOGIN-005 | Api | Đăng nhập không phân biệt hoa/thường với email, bỏ dấu phân cách với SĐT | PASS | 1 | 0.55 | - | - | - |
| TC-API-LOGIN-006 | Api | Lockout của một tài khoản không ảnh hưởng tài khoản khác | PASS | 1 | 0.58 | - | - | - |
| TC-API-REG-001 | Api | POST /api/users bằng email hợp lệ -> 201 và thông báo thành công | PASS | 1 | 0.40 | - | - | - |
| TC-API-REG-002 | Api | POST /api/users bằng SĐT hợp lệ (có dấu cách) -> 201 | PASS | 1 | 0.18 | - | - | - |
| TC-API-REG-003 | Api | Đăng ký trùng tài khoản -> 409; trùng cả khi khác hoa/thường | PASS | 1 | 0.48 | - | - | - |
| TC-API-REG-004 | Api | POST /api/users dữ liệu không hợp lệ -> 400 | PASS | 4 | 1.86 | - | - | - |
| TC-API-REG-005 | Api | POST /api/users body thiếu/hỏng -> 400 (không 500) | PASS | 3 | 0.57 | - | - | - |
| TC-API-REG-006 | Api | POST /api/users sai Content-Type -> 415 | PASS | 1 | 0.16 | - | - | - |
| TC-API-REG-008 | Api | Đăng ký trên DB mới migrate (chưa seed role) vẫn phải trả 201, không 500 | FAIL | 1 | 1.13 | DEF-002 | 1/1 lần chạy fail. Assert.Equal() Failure: Values differ / Expected: Created / Actual:   InternalServerError | - |
| TC-API-ROLE-001 | Api | Các endpoint phân quyền yêu cầu đăng nhập: không token -> 401 | PASS | 6 | 2.33 | - | - | - |
| TC-API-SESS-002 | Api | GET /api/sessions/current không token / token rác -> 401 | PASS | 4 | 2.75 | - | - | - |
| TC-API-SESS-006 | Api | DELETE /api/sessions/current không token -> 401 | PASS | 1 | 0.19 | - | - | - |
| TC-API-TLS-001 | Api | Gọi API bằng HTTP bị chuyển hướng sang HTTPS (307), giữ nguyên đường dẫn | PASS | 1 | 0.27 | - | - | - |
| TC-API-TLS-002 | Api | Đăng nhập qua HTTP KHÔNG được xử lý: bị redirect và không trả token | PASS | 1 | 1.43 | - | - | - |
| TC-API-TLS-003 | Api | Gọi qua HTTPS không bị redirect (không vòng lặp chuyển hướng) | PASS | 1 | 0.28 | - | - | - |
| TC-API-TLS-004 | Api | Môi trường Production gửi header HSTS khi truy cập HTTPS (host không phải localhost) | PASS | 1 | 0.23 | - | - | - |
| TC-ID-001 | Unit | Normalize: email được trim và hạ chữ thường | PASS | 2 | 0.00 | - | - | - |
| TC-ID-002 | Unit | Normalize: SĐT bỏ khoảng trắng, dấu chấm, gạch ngang | PASS | 4 | 0.00 | - | - | - |
| TC-ID-003 | Unit | Normalize: null/rỗng trả về chuỗi rỗng, không ném lỗi | PASS | 3 | 0.00 | - | - | - |
| TC-ID-004 | Unit | IsValid: chấp nhận email và SĐT VN đúng định dạng | PASS | 4 | 0.00 | - | - | - |
| TC-ID-005 | Unit | IsValid: từ chối định dạng sai (biên độ dài SĐT, thiếu @, thiếu tên miền...) | PASS | 11 | 0.00 | - | - | - |
| TC-ID-006 | Unit | Normalize rồi IsValid: email có khoảng trắng GIỮA vẫn bị từ chối (không bị 'dọn' thành hợp lệ) | PASS | 1 | 0.00 | - | - | - |
| TC-JWT-001 | Unit | Token chứa đủ claim sub/name/fullName/securityStamp/role, issuer và audience đúng | PASS | 1 | 0.00 | - | - | - |
| TC-JWT-002 | Unit | Nhiều vai trò -> nhiều claim role | PASS | 1 | 0.01 | - | - | - |
| TC-JWT-003 | Unit | Thời hạn token theo Jwt:ExpireMinutes; thiếu/sai -> mặc định 15 phút | PASS | 4 | 0.07 | - | - | - |
| TC-JWT-004 | Unit | Chữ ký hợp lệ với đúng key và bị từ chối với key khác | PASS | 1 | 0.01 | - | - | - |
| TC-JWT-005 | Unit | Thiếu Jwt:Key -> ném InvalidOperationException (fail-fast, không ký bằng key rỗng) | PASS | 1 | 0.00 | - | - | - |
| TC-LOGIN-001 | Unit | Đăng nhập hợp lệ: store nhận định danh đã chuẩn hoá, mật khẩu GIỮ NGUYÊN, kết quả trả nguyên vẹn | PASS | 1 | 0.00 | - | - | - |
| TC-LOGIN-002 | Unit | Đăng nhập: định danh sai hoặc mật khẩu rỗng bị chặn trước khi chạm store | PASS | 5 | 0.01 | - | - | - |
| TC-LOGIN-003 | Unit | Đăng nhập: các trạng thái thất bại từ store (sai mật khẩu, khóa) được trả nguyên vẹn, không kèm token | PASS | 2 | 0.00 | - | - | - |
| TC-LOGOUT-001 | Unit | Đăng xuất: service uỷ quyền cho store đúng 1 lần | PASS | 1 | 0.00 | - | - | - |
| TC-PW-001 | Unit | Mật khẩu hợp lệ: >= 8 ký tự, có chữ và số | PASS | 4 | 0.00 | - | - | - |
| TC-PW-002 | Unit | Mật khẩu không hợp lệ: ngắn / thiếu chữ / thiếu số / null | PASS | 6 | 0.00 | - | - | - |
| TC-REG-001 | Unit | Đăng ký hợp lệ: chuyển cho store dữ liệu đã chuẩn hoá, trả đúng kết quả của store | PASS | 1 | 0.00 | - | - | - |
| TC-REG-002 | Unit | Đăng ký bằng SĐT có dấu phân cách: store nhận SĐT đã bỏ dấu | PASS | 1 | 0.00 | - | - | - |
| TC-REG-003 | Unit | Đăng ký: dữ liệu sai bị chặn TRƯỚC khi chạm store | PASS | 6 | 0.00 | - | - | - |
| TC-REG-004 | Unit | Đăng ký: tên dài 100 ký tự được chấp nhận, 101 ký tự bị từ chối (biên) | PASS | 1 | 0.00 | - | - | - |
| TC-REG-005 | Unit | Đăng ký: null không làm service ném exception | PASS | 1 | 0.00 | - | - | - |
| TC-REG-006 | Unit | Đăng ký: trạng thái từ store (Duplicate...) được trả nguyên vẹn | PASS | 3 | 0.00 | - | - | - |
| TC-ROLE-001 | Unit | RoleNames.All chứa đủ 6 vai trò, không trùng, đúng giá trị chuỗi (đang là khoá trong DB) | PASS | 1 | 0.01 | - | - | - |