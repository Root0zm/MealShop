# 10 công thức ƯỚC LƯỢNG (bản nháp để chạy thử hệ thống)

Bo qua Source trong dataset 

Quy ước: gram là khối lượng nguyên liệu theo dataset (sống/chưa chế biến, chưa xét hao hụt). Dinh dưỡng món = Σ(giá trị nguyên liệu trên 100 g × gram / 100) ÷ tổng gram × 100. Ô thiếu (NaN) của nguyên liệu được coi là 0 (cùng giả định với bước làm sạch).

Món 15063 (Nem rán) và 15068 (Thịt xiên nướng) thuộc nhóm `usable_as_component` và được dùng làm thành phần của Bún nem (15038) và Bún chả (15034); khi đó giá trị lấy từ dataset (tham chiếu), không tính đệ quy.


## 15069 — Xôi đỗ xanh

| Mã | Thành phần | Gram |
|---|---|---|
| 1002 | Gạo, trắng, nếp thường, sống | 100 |
| 3010 | Đậu xanh (đậu tắt), hạt, khô | 30 |
| 14057 | Nước đá | 70 |
| 13005 | Muối | 1 |

Tổng khối lượng: **201 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 226.1 | 230.0 | -2% |
| protein | 7.7 | 5.5 | +39% |
| fat | 1.2 | 0.4 | +189% |
| carb | 46.1 | 51.0 | -10% |

## 15071 — Xôi lạc

| Mã | Thành phần | Gram |
|---|---|---|
| 1002 | Gạo, trắng, nếp thường, sống | 100 |
| 3017 | Lạc hạt, khô | 20 |
| 14057 | Nước đá | 70 |
| 13005 | Muối | 1 |

Tổng khối lượng: **191 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 244.7 | 246.0 | -1% |
| protein | 7.3 | 4.2 | +73% |
| fat | 5.5 | 4.3 | +28% |
| carb | 41.4 | 47.5 | -13% |

## 15050 — Chè đỗ xanh

| Mã | Thành phần | Gram |
|---|---|---|
| 3010 | Đậu xanh (đậu tắt), hạt, khô | 12 |
| 12013 | Đường cát | 15 |
| 13046 | Bột năng | 2 |
| 14057 | Nước đá | 100 |

Tổng khối lượng: **129 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 82.8 | 79.0 | +5% |
| protein | 2.3 | 1.4 | +72% |
| fat | 0.2 | 0.4 | -43% |
| carb | 17.7 | 17.5 | +1% |

## 15056 — Cơm rang

| Mã | Thành phần | Gram |
|---|---|---|
| 1003 | Gạo, trắng, tẻ, giã tay, sống | 60 |
| 14057 | Nước đá | 90 |
| 9001 | Trứng gà ta | 30 |
| 6010 | Dầu đậu tương | 8 |
| 4038 | Hành lá (hành hoa), tươi | 5 |
| 13005 | Muối | 1 |

Tổng khối lượng: **194 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 168.8 | 198.0 | -15% |
| protein | 4.5 | 6.0 | -24% |
| fat | 6.1 | 9.7 | -37% |
| carb | 23.7 | 21.9 | +9% |

## 15043 — Cháo sườn

| Mã | Thành phần | Gram |
|---|---|---|
| 1003 | Gạo, trắng, tẻ, giã tay, sống | 7 |
| 14057 | Nước đá | 80 |
| 7053 | Sườn lợn, tươi | 8 |
| 4038 | Hành lá (hành hoa), tươi | 2 |

Tổng khối lượng: **97 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 41.8 | 40.0 | +5% |
| protein | 2.1 | 1.5 | +39% |
| fat | 1.1 | 0.2 | +475% |
| carb | 5.6 | 8.0 | -30% |

## 15065 — Phở bò chín

| Mã | Thành phần | Gram |
|---|---|---|
| 1013 | Bánh phở | 50 |
| 7003 | Thịt bò, loại I, tươi | 20 |
| 7141 | Nước dùng | 100 |
| 4038 | Hành lá (hành hoa), tươi | 3 |
| 4039 | Hành tây, tươi | 3 |

Tổng khối lượng: **176 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 56.7 | 73.0 | -22% |
| protein | 3.3 | 5.2 | -36% |
| fat | 0.7 | 0.9 | -23% |
| carb | 9.1 | 11.1 | -18% |

## 15063 — Nem rán

| Mã | Thành phần | Gram |
|---|---|---|
| 7083 | Thịt lợn, nạc vai, tươi | 40 |
| 2015 | Miến dong, khô | 10 |
| 4007 | Củ cà rốt, tươi | 15 |
| 4037 | Hành củ, tươi | 5 |
| 9001 | Trứng gà ta | 10 |
| 1010 | Bánh đa nem | 12 |
| 6010 | Dầu đậu tương | 8 |
| 13004 | Hạt tiêu | 0.3 |
| 13017 | Nước mắm cá | 2 |

Tổng khối lượng: **102.3 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 225.3 | 145.0 | +55% |
| protein | 10.3 | 4.5 | +130% |
| fat | 11.8 | 7.7 | +53% |
| carb | 19.5 | 14.4 | +35% |

## 15038 — Bún nem

| Mã | Thành phần | Gram |
|---|---|---|
| 1020 | Bún, tươi | 150 |
| 15063 | Nem rán | 60 |
| 4094 | Rau thơm, tươi | 20 |
| 4027 | Dưa chuột, tươi | 25 |
| 13017 | Nước mắm cá | 10 |
| 12013 | Đường cát | 8 |
| 5003 | Chanh, tươi | 5 |
| 14057 | Nước đá | 30 |
| 4103 | Tỏi ta, tươi | 2 |

Tổng khối lượng: **310 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 100.0 | 120.0 | -17% |
| protein | 2.1 | 3.3 | -35% |
| fat | 1.9 | 3.9 | -50% |
| carb | 18.5 | 17.8 | +4% |

## 15068 — Thịt xiên nướng

| Mã | Thành phần | Gram |
|---|---|---|
| 7018 | Thịt lợn, nửa nạc, nửa mỡ, tươi | 100 |
| 4037 | Hành củ, tươi | 5 |
| 4103 | Tỏi ta, tươi | 2 |
| 12013 | Đường cát | 4 |
| 13017 | Nước mắm cá | 4 |
| 6010 | Dầu đậu tương | 3 |
| 13004 | Hạt tiêu | 0.2 |

Tổng khối lượng: **118.2 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 260.9 | 322.0 | -19% |
| protein | 14.3 | 26.5 | -46% |
| fat | 20.8 | 23.8 | -13% |
| carb | 4.1 | 0.5 | +655% |

## 15034 — Bún chả

| Mã | Thành phần | Gram |
|---|---|---|
| 1020 | Bún, tươi | 150 |
| 15068 | Thịt xiên nướng | 60 |
| 14057 | Nước đá | 80 |
| 13017 | Nước mắm cá | 10 |
| 12013 | Đường cát | 8 |
| 13034 | Giấm ăn | 5 |
| 4103 | Tỏi ta, tươi | 3 |
| 4090 | Rau sà lách, tươi | 20 |
| 4094 | Rau thơm, tươi | 10 |

Tổng khối lượng: **346 g**.

| Chỉ số /100 g | Tính từ công thức | Tham chiếu dataset | Lệch |
|---|---|---|---|
| kcal | 119.5 | 136.0 | -12% |
| protein | 5.7 | 6.1 | -7% |
| fat | 4.5 | 4.4 | +3% |
| carb | 13.9 | 17.9 | -22% |

## Tóm tắt lệch kcal

| Mã | Món | Tính | Tham chiếu | Lệch |
|---|---|---|---|---|
| 15069 | Xôi đỗ xanh | 226 | 230 | -2% |
| 15071 | Xôi lạc | 245 | 246 | -1% |
| 15050 | Chè đỗ xanh | 83 | 79 | +5% |
| 15056 | Cơm rang | 169 | 198 | -15% |
| 15043 | Cháo sườn | 42 | 40 | +5% |
| 15065 | Phở bò chín | 57 | 73 | -22% |
| 15063 | Nem rán | 225 | 145 | +55% |
| 15038 | Bún nem | 100 | 120 | -17% |
| 15068 | Thịt xiên nướng | 261 | 322 | -19% |
| 15034 | Bún chả | 120 | 136 | -12% |

## Lưu ý khi dùng
- **Nước:** dataset không có mục "nước lọc"; ở đây dùng `14057 Nước đá` (99,7 g nước, kcal ≈ 0, bị gắn cờ A) làm đại diện. Cần quyết định có thêm một mục "Nước" riêng trong `Foods` hay không.
- **Nước dùng (7141)** không có số liệu (cờ B), nên trong Phở bò chín nó góp khối lượng nhưng không góp dinh dưỡng, làm kết quả thấp hơn tham chiếu. Calculator nên cảnh báo khi thành phần có `IsSuspect`.
- **Lệch lớn ở Nem rán (+55%) và Thịt xiên nướng (−19%, protein chỉ bằng nửa):** phần lớn do chênh lệch sống/chín và hao hụt khi chiên nướng (món chín mất nước, đậm đặc hơn) và do tỉ lệ nguyên liệu do mình ước lượng. Bạn đã chọn tạm bỏ qua chênh lệch sống/chín, nên con số này là bằng chứng định lượng cho quyết định đó.
- Không chỉnh gram để cho khớp tham chiếu; mọi con số là ước lượng ban đầu.