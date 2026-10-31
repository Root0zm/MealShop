# AGENT_GUIDE.md — MealShop (ASP.NET Core MVC)

Đọc file này trước mỗi phiên làm việc. Chủ project là sinh viên đang học; mục tiêu là
**project chạy được + chủ hiểu từng phần**, không phải code nhiều.
IMPORTANT: USE COMMENT BUT DONT USE "--- ABC ---" OR "========" INSTEAD JUST "ABC" AND SKIP THE "======" MAKE COMMENT SIMPLE
## 1. Dự án
Web bán suất ăn lành mạnh. Dinh dưỡng của món được **tính từ công thức** (nguyên liệu x gram),
dữ liệu gốc là bộ `foodData.csv` (853 dòng, mỗi giá trị tính trên 100 g).
Sau này một chatbot Python (RAG) sẽ gọi vào web này; chưa làm ở giai đoạn hiện tại.

## 2. Quyết định đã chốt (đừng tự đổi — xem docs/decisions.md)
- .NET SDK 9.0 (`net9.0`), **ASP.NET Core MVC** (không Razor Pages). Có thể thêm vài API controller trả JSON sau.
- CSDL: **SQL Server** (Express/LocalDB khi dev), truy cập bằng **EF Core code-first + migration**.
- Cấu trúc: **một project web `MealShop`** + một project test `MealShop.Tests`. Chưa tách Core/Data.
- Một bảng `Foods` cho cả nguyên liệu lẫn món ăn, phân biệt bằng `Kind` (`Ingredient` / `Dish`).
  Cờ `UsableAsComponent` cho biết món nào được dùng làm thành phần của món khác.
- Chuỗi tiếng Việt dùng `nvarchar`; tìm kiếm không dấu dùng collation `Vietnamese_CI_AI`.
- Dữ liệu dinh dưỡng được làm sạch bằng Python trước (xuất CSV sạch vào `data/processed/`);
  web chỉ **nạp (seed)** từ CSV đó, không làm sạch lại.

## 3. Quy tắc làm việc
1. **Làm đúng bước hiện tại** (mục 6). Không làm trước, không "tiện tay" thêm tính năng.
2. **Controller mỏng.** Logic nghiệp vụ (tính dinh dưỡng món, tổng giỏ hàng) đặt trong `Services/`
   hoặc Model. View không chứa logic nghiệp vụ.
3. Dùng **ViewModel** cho View khi dữ liệu khác với entity; không đưa entity thẳng ra form nếu có trường nhạy cảm.
4. **Không thêm NuGet package** nếu chưa hỏi chủ. Không đổi phiên bản .NET.
5. Không cài thêm pattern lớn (Repository, CQRS, MediatR, Clean Architecture...) trừ khi chủ yêu cầu.
   `DbContext` đã là unit-of-work.
6. Mỗi thay đổi **nhỏ, một commit một việc**, thông điệp commit nói rõ làm gì.
7. Sau khi làm xong, **báo cáo ngắn** (xem mục 5). Không viết tài liệu dài dòng.
8. Khi gặp điểm có hai hướng hợp lý, **dừng và hỏi chủ**, nêu 2 phương án và cái giá của từng cái.
9. Test: logic tính toán phải có unit test (xUnit). Không sửa test cho khớp với code sai.
10. Bí mật (connection string thật, API key) nằm ở User Secrets / biến môi trường, **không commit**.

## 4. Cấu trúc thư mục mong đợi
```
MealShop/
  Controllers/   Models/   ViewModels/   Views/   Services/   Data/ (DbContext, Migrations, Seed)
  wwwroot/
MealShop.Tests/
docs/            decisions.md, AGENT_GUIDE.md
data/processed/  (CSV sạch, không commit nếu nặng)
```

## 5. Báo cáo sau mỗi bước (định dạng)
- Đã làm: (1-3 dòng)
- File thêm/sửa: (danh sách)
- Cách chạy/kiểm tra: (lệnh cụ thể)
- Điểm cần chủ quyết định / cần hiểu: (nếu có)

## 6. Bước hiện tại: MODEL

Mục tiêu: tạo entity từ bộ dữ liệu, **chưa làm View/Controller**.

Nhiệm vụ:
1. `Food` entity — cột tương ứng schema Python:
   `Id (int, key, giữ mã gốc từ dataset)`, `Kind`, `UsableAsComponent`, `NameVi`, `NameEn`,
   `CategoryVi`, `Kcal, Water, Protein, Fat, Carb, Fiber, Cholesterol, Calcium, Phosphorus, Iron,
   Sodium, Potassium, Zinc, VitaminA, BetaCarotene, VitaminC` (kiểu `decimal?`, nullable vì dữ liệu có thiếu),
   `EnergyFlag`, `QaNote`, `IsSuspect`.
2. `Recipe` (món + thành phần + gram, khóa chính ghép `DishId, ComponentId`, `Grams > 0`,
   món không được là thành phần của chính nó).
3. `Portion` (món + tên khẩu phần + gram) — **tạm để trống**, nạp dữ liệu sau.
4. `AppDbContext` với cấu hình bằng Fluent API: CHECK constraint, khóa ngoại ghép ràng buộc
   (thành phần phải có `UsableAsComponent = true`), index cho tên.
5. Migration đầu tiên + lệnh `dotnet ef database update` tạo được CSDL trên SQL Server Express.
6. Seeder đọc `data/processed/foods_clean.csv` (chủ sẽ cung cấp) và nạp vào `Foods`.
7. Test: xUnit kiểm tra ràng buộc (thành phần không hợp lệ bị từ chối, tự tham chiếu bị từ chối).
   Dùng SQLite in-memory hoặc LocalDB cho test; ghi rõ lựa chọn trong báo cáo.

**Chưa quyết (HỎI CHỦ, đừng tự chọn):**
- Cách đánh dấu món **bán được** (`IsSellable` hay theo danh mục).
- Bảng `Order`, `OrderItem`, giỏ hàng — làm ở bước sau, sau khi Foods chạy được.

## 7. Định nghĩa hoàn thành của bước này
`dotnet build` sạch, `dotnet test` xanh, CSDL tạo được bằng migration, 853 dòng nạp đúng,
chủ giải thích lại được vì sao có từng cột và ràng buộc.