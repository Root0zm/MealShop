This Project will use the decision below
# SQL server ( SQL EXPRESS ) as database
# Follow MVC structure
# This solution will have 2 project: 1 for main project and 1 for testing.

## Quyet dinh bo sung cho Dang nhap, Phan quyen, Quan tri, Gio hang, Don hang
1. Dung ASP.NET Core Identity voi AppDbContext : IdentityDbContext<ApplicationUser> (cung CSDL). Tu viet AccountController + View (Register, Login, Logout, AccessDenied) bang UserManager/SignInManager. KHONG scaffold Identity UI (Razor Pages).
2. Hai vai tro: Admin, Customer. Nguoi dang ky moi mac dinh la Customer.
3. Khu vuc quan tri la Area Admin, moi controller trong do co [Authorize(Roles = "Admin")].
4. Gia ban gan vao Portion (gia moi khau phan): them Portion.Price (decimal(12,0), don vi VND, CHECK Price >= 0) va Portion.IsActive (bool, mac dinh true). Mot mon ban duoc khi Food.IsSellable = true va co it nhat mot Portion IsActive voi Price > 0.
5. Don hang luu ban chup (snapshot) gia va ten tai thoi diem mua; doi gia sau do khong lam doi don cu.
6. Gio hang luu trong Session (JSON), khong co bang gio hang. Thanh toan bat buoc dang nhap.
7. Dinh duong cua mon co thanh phan la mon khac lay tu dataset (tham chieu), khong tinh de quy -> cong thuc vong tron khong gay loi tinh toan.
8. Mon do Admin tao moi co Id = max(Id hien co, 99_999_999) + 1, de khong dung ma goc cua dataset.
9. Khong xoa cung mon da tung duoc dat; an bang IsSellable = false.
