# TASK: Danh sách và chi tiết món ăn (Foods Index + Details) — MealShop

Đọc `docs/AGENT_GUIDE.md` và `docs/decisions.md` trước. Quy tắc trong AGENT_GUIDE vẫn áp dụng
(Controller mỏng, không thêm package, không thêm pattern lớn, một commit một việc).

Mục tiêu: lần đầu Model + Controller + View chạy cùng nhau. **Chỉ đọc dữ liệu, chưa có Create/Edit/Delete.**

## Phần 0 — Sửa seeder (làm TRƯỚC, commit riêng)
Hiện `Program.cs` tìm `../data/processed/foods_clean.csv` rồi seed mỗi lần khởi động.
Vấn đề: phụ thuộc đường dẫn ngoài project (hỏng khi Docker/deploy) và có thể nạp lặp.
Sửa:
1. Copy `foods_clean.csv` vào `MealShop/Data/Seed/foods_clean.csv`; trong `.csproj` đặt
   `CopyToOutputDirectory = PreserveNewest`. Đọc bằng `Path.Combine(AppContext.BaseDirectory, "Data", "Seed", ...)`.
2. `FoodSeeder.SeedAsync` chỉ nạp khi `!await context.Foods.AnyAsync()`. Có test cho trường hợp gọi hai lần
   (vẫn đúng 853 dòng).
3. Việc seed chỉ chạy khi `env.IsDevelopment()` hoặc khi có tham số `--seed-only`. Không seed trong Production.
4. Đảm bảo `context.Database.Migrate()` được gọi ở Development (nếu chưa), hoặc ghi rõ cách chạy migration trong README.
5. Không đưa file CSV vào repo công khai nếu chưa xác nhận giấy phép nguồn dữ liệu — thêm
   `MealShop/Data/Seed/*.csv` vào `.gitignore` và ghi trong README cách tạo lại (chủ sẽ quyết).

## Phần 1 — Chuẩn hóa chuỗi tìm kiếm
Tạo `Services/TextNormalizer.cs` với `static string StripAccents(string text)` **giống hệt logic Python**:
thay `đ`->`d`, `Đ`->`D` trước, rồi `NormalizationForm.FormD`, bỏ ký tự có `UnicodeCategory.NonSpacingMark`,
gộp khoảng trắng, `.Trim().ToLowerInvariant()`.
Test (xUnit) với ít nhất: "Phở bò" -> "pho bo", "Đậu phụ" -> "dau phu", "  Bún  chả " -> "bun cha", chuỗi rỗng.

## Phần 2 — Danh sách `/Foods`
- `FoodsController.Index(string? q, string? kind, string? category, int page = 1)`.
- Logic tìm kiếm và phân trang nằm trong `Services/FoodQueryService` (không nằm trong Controller):
  - `q` được chuẩn hóa bằng `TextNormalizer.StripAccents`, rồi lọc `NameNorm.Contains(...)`.
  - lọc theo `kind` (`dish`/`ingredient`) và `category` nếu có.
  - sắp xếp theo `NameVi`, 20 dòng mỗi trang.
  - trả về `PagedResult<T>` (Items, Page, PageSize, TotalCount, TotalPages) do ta tự viết, không thêm package.
- `ViewModels/FoodListItemViewModel` (Id, NameVi, Kind, CategoryVi, Kcal, Protein, Fat, Carb).
- View `Foods/Index.cshtml`: ô tìm kiếm, bộ lọc kind/category, bảng kết quả, phân trang giữ nguyên bộ lọc
  trong query string. Dùng Bootstrap có sẵn của template.
- Dùng `AsNoTracking()` cho truy vấn chỉ đọc.

## Phần 3 — Chi tiết `/Foods/Details/{id}`
- `FoodDetailsViewModel`: toàn bộ giá trị dinh dưỡng (trên 100 g), cờ chất lượng, `QaNote`.
  Món có `IsSuspect` hiện cảnh báo nhìn thấy được.
- Hiện danh sách thành phần (`Recipes`) nếu có; hiện "chưa có công thức" nếu rỗng
  (hiện tại bảng `Recipes` còn trống — KHÔNG tự bịa dữ liệu công thức).
- Id không tồn tại -> `NotFound()`.
- Chưa tính dinh dưỡng từ công thức ở bước này.

## Phần 4 — Test
- Test `FoodQueryService` trên SQLite in-memory: tìm không dấu ("pho" ra "Phở bò"), "dau" ra "Đậu...",
  lọc kind, phân trang (trang 1 đủ 20, trang cuối đúng số dư).
- Ghi trong báo cáo: những gì test SQLite KHÔNG chứng minh được so với SQL Server.

## Không làm
Không thêm Create/Edit/Delete, không thêm đăng nhập, không tạo Order/giỏ hàng, không đổi schema,
không thêm package, không sửa migration cũ. Nếu cần đổi schema, dừng và hỏi chủ.

## Báo cáo (định dạng trong AGENT_GUIDE, mục 5)
Thêm: số dòng trả về cho truy vấn `q=dau` và `q=pho` trên dữ liệu thật; ảnh chụp hoặc mô tả trang Index.

## Định nghĩa hoàn thành
`dotnet build` sạch, `dotnet test` xanh, chạy app thấy trang `/Foods` tìm kiếm không dấu,
lọc, phân trang hoạt động; chủ giải thích được vì sao tìm kiếm dùng `NameNorm` thay vì collation.