# TASK: Đăng nhập, phân quyền, trang quản trị, giỏ hàng, đơn hàng — MealShop

Đọc `docs/AGENT_GUIDE.md` và `docs/decisions.md` trước. Mọi quy tắc trong AGENT_GUIDE vẫn áp dụng
(Controller mỏng, logic trong Services, ViewModel cho View, không thêm pattern lớn, một commit một việc,
báo cáo đúng định dạng). File này thay thế "Bước hiện tại" (mục 6) của AGENT_GUIDE cho bước này.

**Làm theo hai phần. Làm xong PHẦN A thì DỪNG, báo cáo, chờ chủ xác nhận rồi mới làm PHẦN B.**

## Ngoại lệ về package
Được thêm DUY NHẤT: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (cùng major với .NET 9).
Không thêm package nào khác (kể cả Identity.UI, Mvc.Testing, AutoMapper, MediatR). Cần thêm gì khác thì hỏi chủ.

## Quyết định đã chốt cho bước này (ghi vào docs/decisions.md)
1. Dùng **ASP.NET Core Identity** với `AppDbContext : IdentityDbContext<ApplicationUser>` (cùng CSDL).
   **Tự viết `AccountController` + View** (Register, Login, Logout, AccessDenied) bằng `UserManager`/`SignInManager`.
   KHÔNG scaffold Identity UI (Razor Pages).
2. Hai vai trò: `Admin`, `Customer`. Người đăng ký mới mặc định là `Customer`.
3. Khu vực quản trị là **Area `Admin`**, mọi controller trong đó có `[Authorize(Roles = "Admin")]`.
4. **Giá bán gắn vào `Portion`** (giá mỗi khẩu phần): thêm `Portion.Price` (`decimal(12,0)`, đơn vị VND, CHECK `Price >= 0`)
   và `Portion.IsActive` (bool, mặc định true). Một món bán được khi `Food.IsSellable = true` và có ít nhất một
   Portion `IsActive` với `Price > 0`.
5. **Đơn hàng lưu bản chụp (snapshot)** giá và tên tại thời điểm mua; đổi giá sau đó không làm đổi đơn cũ.
6. **Giỏ hàng lưu trong Session** (JSON), không có bảng giỏ hàng. Thanh toán bắt buộc đăng nhập.
7. Dinh dưỡng của món có thành phần là món khác lấy từ dataset (tham chiếu), **không tính đệ quy** → công thức
   vòng tròn không gây lỗi tính toán.
8. Món do Admin tạo mới có `Id` = `max(Id hiện có, 99_999_999) + 1`, để không đụng mã gốc của dataset.
9. Không xóa cứng món đã từng được đặt; ẩn bằng `IsSellable = false`.

## PHẦN A — Đăng nhập, phân quyền, quản trị

### A1. Identity (commit riêng)
- `ApplicationUser : IdentityUser` thêm `FullName` (bắt buộc, tối đa 100).
- Cấu hình: mật khẩu tối thiểu 8 ký tự, có chữ và số; email duy nhất; lockout bật (5 lần sai → khóa 10 phút).
- Migration `AddIdentity`. Đảm bảo bảng Identity không phá migration/seed cũ.
- Seed vai trò `Admin`, `Customer` khi khởi động ở Development, và tạo 1 tài khoản Admin từ cấu hình
  (`Seed:AdminEmail`, `Seed:AdminPassword`) đọc từ **User Secrets**. KHÔNG hardcode mật khẩu trong code hoặc appsettings.
  Nếu thiếu cấu hình thì bỏ qua và ghi log cảnh báo.
- Form POST có `[ValidateAntiForgeryToken]` (hoặc bật auto-validate toàn cục).
- Layout: hiện "Đăng nhập / Đăng ký" hoặc tên người dùng + "Đăng xuất"; liên kết "Quản trị" chỉ hiện với Admin.
- Chặn open redirect khi login có `returnUrl` (dùng `Url.IsLocalUrl`).

### A2. Admin — quản lý món ăn (commit riêng)
- `Areas/Admin/Controllers/FoodsController`: Index (dùng lại `FoodQueryService`), Create, Edit, Delete.
- Dùng ViewModel riêng cho form (KHÔNG bind thẳng entity → tránh over-posting). Validation: tên bắt buộc,
  `Kind` thuộc {ingredient, dish}, giá trị dinh dưỡng ≥ 0 và hợp lý (kcal ≤ 900/100 g), ô trống = thiếu (null), KHÔNG tự điền 0.
- Khi lưu: tự cập nhật `NameNorm` bằng `TextNormalizer.StripAccents(NameVi)`.
- Nguyên liệu bắt buộc `UsableAsComponent = true` (form tự đặt, không cho sửa).
- Xóa: nếu món đang có trong `Recipes` (làm thành phần) hoặc trong `OrderItems` thì KHÔNG xóa, hiện thông báo thân thiện
  và gợi ý ẩn bằng `IsSellable = false`.

### A3. Admin — công thức và khẩu phần (commit riêng)
- Trên trang chi tiết/sửa của **món (`Kind = dish`)**: bảng thành phần hiện tại (tên, gram), thêm thành phần
  (chọn từ danh sách chỉ gồm món/nguyên liệu có `UsableAsComponent = true`, tìm không dấu), sửa gram, xóa thành phần.
- Hiện kết quả `NutritionCalculator` ngay trên trang đó (bảng đối soát như đã có).
- Validation: gram > 0, không trùng thành phần trong cùng món, không tự tham chiếu. Lỗi từ CSDL (vi phạm ràng buộc)
  phải được bắt và hiện thông báo dễ hiểu, không để lộ lỗi 500.
- Quản lý `Portion` của món: thêm/sửa/xóa (tên, gram, giá, IsActive), tên khẩu phần duy nhất trong cùng món.
- Migration `AddPortionPrice` (thêm `Price`, `IsActive`, CHECK).

### A4. Test (xUnit, SQLite in-memory như hiện tại)
- Test dịch vụ/ViewModel: tạo món mới có `NameNorm` đúng; mã mới ≥ 100_000_000; xóa món đang là thành phần bị từ chối.
- Test phân quyền bằng phản chiếu (reflection): mọi controller trong Area Admin có `[Authorize(Roles = "Admin")]`.
  Ghi rõ trong báo cáo: test này KHÔNG thay cho kiểm thử thực tế đăng nhập bằng trình duyệt.

### Định nghĩa hoàn thành PHẦN A
`dotnet build` sạch, `dotnet test` xanh, migration áp dụng được trên SQL Server Express. Chủ tự kiểm tra:
đăng ký/đăng nhập/đăng xuất, người thường truy cập `/Admin/...` bị chuyển tới AccessDenied, Admin tạo/sửa/xóa món,
thêm thành phần, tạo khẩu phần có giá. **DỪNG tại đây và báo cáo.**

## PHẦN B — Giỏ hàng và đơn hàng (chỉ làm sau khi chủ xác nhận PHẦN A)

### B1. Giỏ hàng (commit riêng)
- `Services/CartService` làm việc trên Session: thêm, đổi số lượng (1–20), xóa dòng, xóa hết, tính tổng.
- Giỏ chỉ lưu `PortionId` và `Quantity`. **Giá và tên luôn đọc lại từ CSDL** khi hiển thị và khi đặt hàng;
  KHÔNG tin bất kỳ giá nào gửi lên từ trình duyệt.
- Bỏ qua/loại khỏi giỏ các Portion không còn `IsActive`, món không còn `IsSellable`, kèm thông báo.
- Trang `/Cart`: bảng sản phẩm, tổng tiền, tổng kcal (dinh dưỡng trên 100 g của món × gram khẩu phần ÷ 100;
  thiếu số liệu thì hiện "chưa đủ dữ liệu", không đoán).
- Nút "Thêm vào giỏ" trên trang chi tiết món (chọn khẩu phần) và trang danh sách.

### B2. Đặt hàng (commit riêng)
- Entity `Order` (Id, UserId, CreatedAt UTC, Status, TotalAmount, ReceiverName, Phone, Address, Note) và
  `OrderItem` (OrderId, FoodId, PortionId, FoodNameSnapshot, PortionNameSnapshot, PortionGramsSnapshot,
  UnitPriceSnapshot, Quantity). `Status`: Pending, Confirmed, Preparing, Delivered, Cancelled (enum lưu dạng chuỗi).
  CHECK: `Quantity BETWEEN 1 AND 20`, `UnitPriceSnapshot >= 0`, `TotalAmount >= 0`.
  FK `OrderItem → Order` Cascade; `OrderItem → Food/Portion` Restrict. Migration `AddOrders`.
- `Services/OrderService.PlaceOrderAsync(userId, checkoutInfo, cart)`:
  - giỏ rỗng → từ chối;
  - đọc lại giá/khẩu phần từ CSDL, kiểm tra còn bán;
  - tạo Order + OrderItems với snapshot, tính TotalAmount ở server;
  - toàn bộ trong **một transaction**; lỗi thì rollback và giỏ không bị xóa;
  - thành công mới xóa giỏ.
- `OrdersController` (Customer, `[Authorize]`): form thanh toán (validation họ tên, số điện thoại VN, địa chỉ),
  trang xác nhận, "Đơn của tôi", chi tiết đơn.
  **Chống IDOR:** người dùng chỉ xem được đơn của chính mình; truy cập đơn người khác trả `NotFound`.
- Khách hủy đơn khi `Status = Pending`.

### B3. Admin — quản lý đơn (commit riêng)
- `Areas/Admin/Controllers/OrdersController`: danh sách (lọc theo trạng thái, phân trang), chi tiết, đổi trạng thái.
  Chỉ cho phép chuyển trạng thái hợp lệ (Pending→Confirmed→Preparing→Delivered; Pending/Confirmed→Cancelled);
  logic chuyển trạng thái đặt trong service, có test.

### B4. Test
- CartService: thêm, cộng dồn số lượng, giới hạn 1–20, xóa, tổng tiền.
- OrderService: đặt thành công (đúng snapshot); **đổi giá sau khi đặt không làm đổi đơn cũ**; giỏ rỗng bị từ chối;
  portion không còn active bị từ chối; lỗi giữa chừng thì rollback (không để lại đơn dở); chuyển trạng thái sai bị từ chối.
- Kiểm tra IDOR bằng test ở tầng service/controller: người dùng A không lấy được đơn của B.
- Ghi trong báo cáo những gì SQLite KHÔNG chứng minh được so với SQL Server (đặc biệt transaction và CHECK).

### Định nghĩa hoàn thành PHẦN B
`dotnet build` sạch, `dotnet test` xanh. Chủ tự kiểm tra bằng trình duyệt: thêm vào giỏ → thanh toán → xem đơn →
Admin đổi trạng thái; thử sửa giá khẩu phần trong Admin sau khi đặt rồi xem đơn cũ không đổi giá.

## Không làm (cả hai phần)
Cổng thanh toán, xác nhận email, đăng nhập mạng xã hội, quản lý tồn kho, mã giảm giá, chatbot, Docker/deploy,
Web API, đổi schema ngoài các migration nêu trên. Cần đổi thêm schema thì dừng và hỏi chủ.

## Việc cần hỏi chủ trước khi code (nếu chưa rõ)
- Có muốn thêm trường `Image`/ảnh món không? (mặc định: KHÔNG, để sau)
- Đơn vị tiền: VND, không có phần thập phân (mặc định như mục 4).

## Báo cáo (định dạng trong AGENT_GUIDE, mục 5)
Thêm: danh sách route chính; những gì test KHÔNG kiểm chứng được; những giả định hoặc quyết định agent tự đưa ra
ngoài file này (để chủ duyệt).