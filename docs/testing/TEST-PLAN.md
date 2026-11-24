# Kế hoạch & quy trình kiểm thử – ASPHotelM2631

> Tài liệu quy trình (viết tay, ít thay đổi). Test Case doc và Test Result **không viết tay** – được sinh từ code, xem mục 4.

## 1. Phạm vi hiện tại
Module xác thực: đăng ký (`POST /api/users`), đăng nhập (`POST /api/sessions`), phiên hiện tại (`GET/DELETE /api/sessions/current`),
phân quyền theo 6 vai trò (`/api/customer|reception|housekeeping|accounting|manager|admin`), JWT, HTTPS.
Giao diện hiện chỉ có Home/Privacy (template MVC); sẽ mở rộng khi có màn hình đăng nhập/đặt phòng.

## 2. Các tầng test (mỗi tầng một suite)
| Suite | Project | Kiểm tra gì | Cần gì để chạy | Chạy ở CI |
|---|---|---|---|---|
| Unit | `ASPHotelM2631.Tests.Unit` | Luật nghiệp vụ (`IdentifierRules`, `AuthenticationService`, `RoleNames`, `JwtTokenService`) với fake viết tay | Không | Có |
| Api | `ASPHotelM2631.Tests.Api` | Toàn bộ pipeline thật (Identity, JWT, middleware, controller) in-process qua HTTP; gồm redirect/HSTS | Không (DB InMemory) | Có |
| Live | `LiveHttpsTests` (trong Tests.Api) | Server Kestrel thật: HTTP→HTTPS, bắt tay TLS với chứng chỉ được tin cậy | Bật app + `dotnet dev-certs https --trust` | Không |
| Ui | `ASPHotelM2631.Tests.Ui` | Selenium + Chrome thật, có bằng chứng ảnh | Bật app + Chrome | Không (cần người xem) |

Vì sao Api không thay bằng Unit: unit test chỉ thấy từng mảnh; lỗi do **ghép** các mảnh (cấu hình JWT, tên claim, thứ tự middleware, role chưa seed) chỉ lộ khi chạy qua pipeline thật.

## 3. Thế nào là "pass ảo" – và bị coi là FAIL
Một test xanh nhưng không chứng minh được hành vi đúng. Các quy tắc bắt buộc:
1. **Assert giá trị cụ thể**, không chỉ "không ném lỗi" / "status khác null".
2. **Case dương: kiểm tra tham số/kết quả chính xác** (vd store nhận email đã chuẩn hoá, không chỉ trả `Succeeded`).
3. **Case âm: kiểm tra việc KHÔNG xảy ra** (vd store không bị gọi, không có token trong body).
4. **Mọi test âm về xác thực phải có đối chứng dương**: token giả/hết hạn trả 401 chỉ có ý nghĩa nếu token thật *được* chấp nhận. Nếu server từ chối mọi token thì test âm vẫn xanh nhưng vì lý do sai.
5. Fake/mock trả giá trị **sentinel** (không phải mặc định) để test quên cấu hình sẽ lộ ra.
6. **Không skip.** Test thiếu điều kiện chạy (vd Live thiếu biến môi trường) phải FAIL, không được bỏ qua.
7. **Test case không có kết quả = NOT RUN = FAIL.** Test bị Skipped = FAIL. Kết quả mồ côi (không có trong tài liệu) = FAIL.
8. **Test UI cần bằng chứng + người xem** (mục 5), nếu không = INVALID = FAIL.
9. Khi thêm/sửa test quan trọng: làm **kiểm tra đột biến thủ công** – cố ý phá code thật (đổi `>= 8` thành `>= 7`, bỏ một lệnh validate…) và xác nhận ít nhất một test chuyển sang đỏ. Nếu vẫn xanh → test vô dụng, viết lại. (Đợt đầu: 12/12 đột biến trên tầng Unit bị bắt.)

## 4. Quy trình làm tài liệu (nhất quán, không viết tay)
```
 viết test với [TcFact]/[TcTheory](ID, tiêu đề, Pre, Steps, Expected, Priority, Req)
        │
        ▼  dotnet test  (hoặc tools/run-tests.ps1)
   TestResults/*.trx  (+ ảnh bằng chứng cho UI)
        │
        ▼  python tools/Generate-TestDocs.py
 docs/testing/generated/TestCases.md      ← Test Case doc  (từ attribute trong code)
 docs/testing/generated/TestResult.md/.html ← Test Result  (khớp ID giữa code và TRX)
```
- **Thêm test case = thêm 1 attribute.** ID dạng `TC-<NHÓM>-<3 số>` (vd `TC-REG-007`), duy nhất, **không đổi/tái sử dụng** sau khi đã dùng.
- Trường bắt buộc: tiêu đề, `Steps`, `Expected` cụ thể (nêu mã HTTP/văn bản/giá trị). `Pre` khi có điều kiện. `Req` nếu nhóm có mã yêu cầu.
- Bug ghi ở `docs/testing/defects.csv` (cột `tc_id` → `defect_id`); báo cáo tự hiện cột Bug.
- Không sửa tay thư mục `generated/`; sửa nguồn rồi sinh lại.

## 5. Test giao diện (Selenium): phải có người XEM nó chạy
Chạy tự động mà không ai nhìn rất dễ xanh giả (selector sai vẫn "không lỗi", trang lỗi vẫn có `<body>`...). Quy trình:
1. Bật app (profile `https`), chạy `tools/run-tests.ps1 -Ui` **trên máy có màn hình**. Chrome mở thật và dừng ~0,7 giây mỗi bước (`UI_PACE_MS`).
2. Người test **ngồi xem**, đối chiếu với cột *Expected* của TC.
3. Mỗi bước tự chụp ảnh vào `TestResults/evidence/<TC-ID>/` kèm `steps.log`; cần ≥ 2 ảnh/TC.
4. Xem xong, ghi một dòng vào `docs/testing/ui-signoff.csv` (TC, tên, ngày, ghi chú). Báo cáo cuối dùng `--require-ui-signoff`: thiếu ảnh hoặc thiếu chữ ký → **INVALID = FAIL**.
5. Không dùng `--ignore-certificate-errors` để "cho chạy được": lỗi HTTPS phải làm test đỏ.
6. Assert dựa trên cái **thấy được** (h1, title, URL cuối, phần tử hiện/ẩn), kèm **đối chứng trước khi bấm** (đang ở trang nào, phần tử đang ẩn).

## 6. Tiêu chí hoàn thành một đợt test
- `Generate-TestDocs.py` trả exit 0 (không NOT RUN / FAIL / SKIPPED / INVALID / ORPHAN).
- Mọi lỗi tìm thấy đã có dòng trong `defects.csv`.
- TestCases.md và TestResult.* được commit kèm mã (hoặc đính kèm artifact CI) – cùng commit hash.

## 7. Rủi ro / ghi chú đã biết
- Tầng Api dùng EF **InMemory**, không kiểm chứng migration/SQL Server thật (ràng buộc unique index, collation). Bổ sung suite chạy trên SQL Server thật khi có DB test.
- Test lockout/khoá tài khoản dùng tài khoản tạo mới mỗi lần để không ảnh hưởng dữ liệu khác.
