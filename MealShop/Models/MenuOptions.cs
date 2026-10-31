namespace MealShop.Models;

// Cau hinh quy tac tao khau phan va phan nhom thuc don
public class MenuOptions
{
    public List<MenuRule> Rules { get; set; } = new();
}

public class MenuRule
{
    // Danh muc trong dataset
    public string CategoryVi { get; set; } = string.Empty;

    // Tab hien thi tren giao dien thuc don
    public string Tab { get; set; } = string.Empty;

    // Ten khau phan mac dinh
    public string PortionName { get; set; } = string.Empty;

    // Khoi luong khau phan (gram)
    public decimal Grams { get; set; }

    // Gia ban (VND)
    public decimal Price { get; set; }

    // Tu khoa bat buoc phai chua trong NameNorm (tuy chon)
    public List<string>? NameContainsAny { get; set; }

    // Tu khoa khong duoc chua trong NameNorm (tuy chon)
    public List<string>? ExcludeNameContainsAny { get; set; }
}
