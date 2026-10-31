# TASK: Giá theo quy tắc, giao diện thực đơn, giỏ hàng và đơn hàng — MealShop

Đọc `docs/AGENT_GUIDE.md` và `docs/decisions.md` trước. Mọi quy tắc trong AGENT_GUIDE vẫn áp dụng
(Controller mỏng, logic trong Services, ViewModel cho View, không thêm pattern lớn, một commit một việc,
báo cáo đúng định dạng, không thêm package).

**File này thay thế PHẦN B của `TASK_auth_admin_cart_order.md`** (PHẦN A đã xong và được chủ chấp nhận).
Ngoại lệ package đã cấp cho Identity vẫn giữ nguyên; KHÔNG thêm package mới nào khác.

Làm theo ba phần. **Làm xong PHẦN C (giá + giao diện thực đơn) thì DỪNG, báo cáo, chờ chủ xác nhận
rồi mới làm PHẦN D (giỏ hàng, đơn hàng).**

## Quyết định đã chốt (ghi vào docs/decisions.md)
1. **KHÔNG thêm cột `MenuGroup`.** Giá và nhóm thực đơn tạm suy ra từ `Food.CategoryVi` bằng một bảng quy tắc
   nằm trong **cấu hình** (`appsettings.json`, đọc bằng `IOptions<MenuOptions>`, không thêm package).
   Đây là giải pháp TẠM; ghi trong decisions.md rằng sau này sẽ thay bằng `MenuGroup`
   (lý do tạm: tránh xử lý dữ liệu nhiều).
2. Giá thật vẫn lưu ở `Portion.Price`. Quy tắc chỉ dùng để **tạo khẩu phần mặc định**; đổi quy tắc sau đó
   không làm đổi khẩu phần đã tạo. Admin vẫn sửa giá từng khẩu phần được.
3. Nguyên liệu như thịt, cá, rau, đậu **được bán làm món kèm** (nhà hàng chế biến theo yêu cầu khách).
   Vì dinh dưỡng trong dataset tính trên khối lượng nguyên liệu, giao diện phải ghi chú:
   "Dinh dưỡng tính theo khối lượng nguyên liệu, chưa tính hao hụt khi chế biến."
4. Đơn hàng lưu snapshot (tên, giá, gram); giỏ hàng nằm trong Session; thanh toán bắt buộc đăng nhập
   (như các quyết định của file trước).
5. Dinh dưỡng của một khẩu phần = giá trị trên 100 g của món (giá trị tham chiếu trong dataset) × gram khẩu phần ÷ 100.
   Thiếu số liệu thì hiện "chưa đủ dữ liệu", không đoán, không điền 0.

## PHẦN C — Giá theo quy tắc và giao diện thực đơn

### C1. Quy tắc giá trong cấu hình (commit riêng)
Thêm vào `appsettings.json` mục `Menu:Rules`, mỗi quy tắc gồm: `CategoryVi` (đúng chuỗi trong dataset),
`Tab`, `PortionName`, `Grams`, `Price`, và tùy chọn `NameContainsAny` (lọc theo `NameNorm`), `ExcludeNameContainsAny`.

| CategoryVi (đúng như dataset) | Tab | PortionName | Grams | Price (VND) | Ghi chú |
|---|---|---|---|---|---|
| Thức ăn truyền thống | Món chính | 1 suất | 350 | 35000 | |
| Nước giải khát | Đồ uống | 1 ly | 250 | 15000 | loại tên chứa "ruou", "bia" |
| Đồ ngọt (đường, bánh, mứt, kẹo) | Tráng miệng | 1 phần | 40 | 10000 | |
| Quả chín | Tráng miệng | 1 phần | 100 | 10000 | |
| Thịt và sản phẩm chế biến | Thịt và hải sản | 1 phần | 40 | 5000 | |
| Thủy sản và sản phẩm chế biến | Thịt và hải sản | 1 phần | 40 | 5000 | |
| Rau, quả, củ dùng làm rau | Rau củ | 1 phần | 50 | 5000 | |
| Trứng và sản phẩm chế biến | Trứng | 1 phần | 50 | 5000 | |
| Hạt, quả giàu đạm, béo và sản phẩm chế biến | Đậu và hạt | 1 phần | 40 | 5000 | CHỈ tên chứa "dau phu" |

Các danh mục KHÔNG có quy tắc (Khoai củ, Ngũ cốc, Sữa, Đồ hộp, Dầu mỡ bơ, Gia vị/nước chấm) và rượu/bia:
**không tự tạo khẩu phần**. Admin vẫn tạo tay được. Liệt kê chúng trong báo cáo.
Giá trị `Grams` và `Price` ở trên là mặc định tạm do chủ đặt; đọc từ cấu hình, KHÔNG hardcode trong code.

### C2. Dịch vụ tạo khẩu phần mặc định (commit riêng)
- `Services/DefaultPortionService.PreviewAsync()` và `ApplyAsync()`:
  - với mỗi Food khớp quy tắc, tạo `Portion` (`Name = PortionName`, `Grams`, `Price`, `IsActive = true`)
    và đặt `Food.IsSellable = true`;
  - **bỏ qua** Food có `IsSuspect = true` (cờ chất lượng dữ liệu); đếm và liệt kê chúng;
  - **idempotent**: bỏ qua nếu Food đã có Portion cùng tên; chạy hai lần không tạo trùng;
  - chạy trong một transaction.
- Trang `Admin/Portions/Defaults`: bấm "Xem trước" hiện số sẽ tạo / bỏ qua vì đã có / bỏ qua vì `IsSuspect`
  (theo từng tab); "Áp dụng" là POST có anti-forgery.
- Test: tạo đúng số lượng cho dữ liệu mẫu, chạy hai lần không trùng, `IsSuspect` bị bỏ qua, rượu không được tạo,
  quy tắc "dau phu" chỉ khớp món đậu phụ, đổi giá trong cấu hình không đổi khẩu phần đã tạo.

### C3. Giao diện thực đơn `/Menu` (commit riêng)
Mục tiêu: bớt cảm giác "bảng cơ sở dữ liệu". Mẫu thiết kế do chủ cung cấp (ảnh chụp hoặc file thiết kế);
nếu không có thì làm theo mô tả dưới đây. `/Foods` (trang tra cứu dữ liệu) vẫn giữ, chuyển thành trang "Tra cứu dinh dưỡng".

**Phong cách:** nền #F4F7F2, thẻ trắng viền #DDE5DF bo góc 16px, màu chính xanh #2F7D4F (nút, tab chọn),
giá màu #B4390F, chữ #16231B, chữ phụ #3F5046. Font **Be Vietnam Pro** (Google Fonts) cho cả site.
Viết trong `wwwroot/css/site.css` trên nền Bootstrap có sẵn, dùng biến CSS cho màu. Mọi nút và ô nhập
cao tối thiểu 44px; có nhãn `<label>`; nút chỉ có biểu tượng phải có `aria-label`; không dùng emoji;
chữ phụ phải đủ tương phản (tối thiểu 4.5:1).

**Bố cục `/Menu`:**
1. Tiêu đề "Thực đơn hôm nay" + một câu mô tả.
2. Hàng "Chọn theo mục tiêu": Dưới 400 kcal (năng lượng cả khẩu phần ≤ 400), Giàu đạm (đạm ≥ 20 g/khẩu phần),
   Ít béo (béo ≤ 5 g/khẩu phần). Bỏ "Nhiều rau". Lọc ở phía server qua query string.
3. Hàng tab theo `Tab` (lấy từ cấu hình, theo thứ tự trong bảng quy tắc, chỉ hiện tab có món bán được).
4. Khu vực chính hai cột: danh sách (rộng) + giỏ "Bữa của bạn" bên phải (hẹp). Màn hình hẹp thì giỏ xuống dưới.
5. Tab "Món chính": **thẻ món** lớn gồm khung hình (ô màu + biểu tượng bát SVG, vì dataset không có ảnh),
   tên, mô tả ngắn, **kcal cả khẩu phần** (số lớn), thanh tỉ lệ đạm/béo/bột đường theo % năng lượng
   (đạm×4, béo×9, bột đường×4), chú thích "Đạm x g · Béo y g · Bột đường z g", giá, nút "Thêm".
   Nhãn "Ít béo" chỉ hiện khi đúng điều kiện.
6. Các tab còn lại: thẻ nhỏ gọn một hàng (tên, "40 g · 60 kcal", giá, nút "+" 44×44px). Phần "Thêm món kèm" cũng hiện
   dưới tab Món chính (lấy từ các tab món kèm).
7. Chân trang: ghi chú về dinh dưỡng tính theo khối lượng nguyên liệu (quyết định 3).
8. Tìm kiếm không dấu ở thanh đầu trang, dùng lại `FoodQueryService` (chỉ trong món có khẩu phần bán được).

**Cấu trúc code:** `Views/Menu/Index.cshtml`, partial `_DishCard.cshtml`, `_SideItem.cshtml`, ViewComponent `MiniCart`
(giỏ bên phải; PHẦN C hiển thị trạng thái rỗng, nối dữ liệu thật ở PHẦN D). `ViewModels/MenuItemViewModel`
tính sẵn kcal/đạm/béo/bột đường theo khẩu phần và `% năng lượng` (logic tính nằm trong service, không trong View).

### C4. Test PHẦN C
- Tính dinh dưỡng khẩu phần (kể cả thiếu số liệu → "chưa đủ dữ liệu"), tỉ lệ % năng lượng (tổng 100% sau làm tròn),
  bộ lọc mục tiêu, thứ tự tab, món không có khẩu phần active không hiện.

### Định nghĩa hoàn thành PHẦN C
`dotnet build` sạch, `dotnet test` xanh, chủ chạy "Áp dụng khẩu phần mặc định" và thấy `/Menu` có tab và thẻ món;
đổi một giá trong Admin thì `/Menu` đổi theo. **DỪNG tại đây và báo cáo.**

## PHẦN D — Giỏ hàng và đơn hàng (chỉ làm sau khi chủ xác nhận PHẦN C)

### D1. Giỏ hàng (commit riêng)
- `Services/CartService` trên Session: thêm, đổi số lượng (1–20), xóa dòng, xóa hết, tính tổng.
  Giỏ chỉ lưu `PortionId` và `Quantity`. **Giá, tên, gram luôn đọc lại từ CSDL**; không tin giá từ trình duyệt.
  Loại khỏi giỏ khẩu phần không còn `IsActive` hoặc món không còn `IsSellable`, kèm thông báo.
- Nút "Thêm" / "+" trên `/Menu` gọi POST (có anti-forgery) và cập nhật `MiniCart`.
- `MiniCart` hiển thị dòng món, giá, kcal; **tổng năng lượng, tổng đạm/béo/bột đường, % so với 2000 kcal**
  (hằng số tham khảo, không phải tư vấn cá nhân; ghi chú nhỏ trong giao diện); tạm tính; nút "Xem giỏ và đặt hàng".
- Trang `/Cart`: bảng dòng món có nút giảm/tăng/xóa; cột phải "Tóm tắt dinh dưỡng" (tổng kcal, thanh % so với 2000 kcal,
  ba thanh đạm/béo/bột đường kèm gram và % năng lượng) và tổng tiền; khối "Thông tin nhận hàng".
  Thiếu số liệu của một dòng thì hiện "chưa đủ dữ liệu" cho dòng đó và đánh dấu tổng là "chưa đầy đủ".

### D2. Đặt hàng (commit riêng)
- `Order` (Id, UserId, CreatedAt UTC, Status, TotalAmount, ReceiverName, Phone, Address, Note) và
  `OrderItem` (OrderId, FoodId, PortionId, FoodNameSnapshot, PortionNameSnapshot, PortionGramsSnapshot,
  UnitPriceSnapshot, Quantity). `Status`: Pending, Confirmed, Preparing, Delivered, Cancelled (enum lưu chuỗi).
  CHECK: `Quantity BETWEEN 1 AND 20`, `UnitPriceSnapshot >= 0`, `TotalAmount >= 0`.
  FK `OrderItem → Order` Cascade; `OrderItem → Food/Portion` Restrict. Migration `AddOrders`.
- `Services/OrderService.PlaceOrderAsync(userId, checkoutInfo, cart)`: giỏ rỗng bị từ chối; đọc lại giá và khẩu phần
  từ CSDL, kiểm tra còn bán; tạo Order + OrderItems với snapshot; tính `TotalAmount` ở server; **một transaction**,
  lỗi thì rollback và không xóa giỏ; thành công mới xóa giỏ.
- `OrdersController` (`[Authorize]`): form thanh toán (họ tên, số điện thoại VN, địa chỉ), xác nhận, "Đơn của tôi",
  chi tiết đơn. **Chống IDOR:** chỉ xem được đơn của chính mình, đơn người khác trả `NotFound`.
  Khách hủy được đơn khi `Status = Pending`.

### D3. Admin — quản lý đơn (commit riêng)
- `Areas/Admin/Controllers/OrdersController`: danh sách (lọc theo trạng thái, phân trang), chi tiết, đổi trạng thái.
  Chuyển trạng thái hợp lệ: Pending→Confirmed→Preparing→Delivered; Pending/Confirmed→Cancelled. Logic trong service, có test.

### D4. Test PHẦN D
- CartService: thêm, cộng dồn, giới hạn 1–20, xóa, tổng tiền, tổng dinh dưỡng, dòng thiếu số liệu.
- OrderService: đặt thành công (đúng snapshot); **đổi giá sau khi đặt không làm đổi đơn cũ**; giỏ rỗng bị từ chối;
  khẩu phần không active bị từ chối; lỗi giữa chừng thì rollback; chuyển trạng thái sai bị từ chối.
- IDOR: người dùng A không lấy được đơn của B.
- Ghi trong báo cáo những gì SQLite KHÔNG chứng minh được so với SQL Server (transaction, CHECK).

### Định nghĩa hoàn thành PHẦN D
`dotnet build` sạch, `dotnet test` xanh. Chủ tự kiểm tra bằng trình duyệt: thêm món chính + món kèm → xem giỏ và
dinh dưỡng → đặt hàng → xem "Đơn của tôi" → Admin đổi trạng thái; sửa giá khẩu phần sau khi đặt, đơn cũ không đổi giá.

## Không làm (cả hai phần)
Cổng thanh toán, xác nhận email, mạng xã hội, tồn kho, mã giảm giá, phí giao hàng (hiện "chưa tính"), upload ảnh món,
cột `MenuGroup`, chatbot, Docker/deploy, Web API, đổi schema ngoài migration nêu trên. Cần đổi schema thì dừng và hỏi chủ.

## Báo cáo (định dạng trong AGENT_GUIDE, mục 5)
Thêm: route chính; số khẩu phần được tạo theo từng tab và số món bị bỏ qua (`IsSuspect`, đã có); danh mục không có quy tắc;
những gì test KHÔNG kiểm chứng được; các giả định hoặc quyết định agent tự đưa ra ngoài file này (để chủ duyệt).