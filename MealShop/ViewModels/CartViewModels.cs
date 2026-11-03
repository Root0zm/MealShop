namespace MealShop.ViewModels;

// Dong san pham trong gio hang
public class CartLineViewModel
{
    public int PortionId { get; set; }
    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string PortionName { get; set; } = string.Empty;
    public decimal PortionGrams { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LinePrice => UnitPrice * Quantity;

    // Dinh duong theo so luong khau phan
    public bool HasNutrition { get; set; }
    public decimal? Kcal { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Carb { get; set; }
}

// Thong tin toan bo gio hang va tong hop dinh duong
public class CartViewModel
{
    public List<CartLineViewModel> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.LinePrice);
    public int TotalItemsCount => Items.Sum(i => i.Quantity);

    // Dinh duong tong the
    public bool HasCompleteNutrition { get; set; } = true;
    public decimal TotalKcal { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalFat { get; set; }
    public decimal TotalCarb { get; set; }

    // Muc tham khao kcal (cau hinh qua NutritionOptions)
    public int ReferenceKcal { get; set; } = 2000;

    // % so voi muc kcal tham khao moi ngay
    public int PercentOf2000Kcal => HasCompleteNutrition && TotalKcal > 0 && ReferenceKcal > 0
        ? (int)Math.Round((TotalKcal / ReferenceKcal) * 100m) : 0;

    // % nang luong cua 3 chat da luong (tong luon bang 100% sau lam tron)
    public int ShareProtein { get; set; }
    public int ShareFat { get; set; }
    public int ShareCarb { get; set; }

    // Thong bao neu co mon khong con hop le bi go bo
    public List<string> Warnings { get; set; } = new();
}

// Model luu trong Session (chi luu PortionId va Quantity)
public class CartSessionItem
{
    public int PortionId { get; set; }
    public int Quantity { get; set; }
}
