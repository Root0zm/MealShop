namespace MealShop.ViewModels;

// ViewModel dai dien cho mot mon kem theo khau phan tren giao dien thuc don
public class MenuItemViewModel
{
    public int FoodId { get; set; }
    public int PortionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Tab { get; set; } = string.Empty;
    public string PortionName { get; set; } = string.Empty;
    public decimal PortionGrams { get; set; }
    public decimal Price { get; set; }

    // Co day du so lieu dinh duong khong
    public bool HasCompleteNutrition { get; set; }
    public decimal? Kcal { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Carb { get; set; }

    // % nang luong (tong luon bang 100% sau khi lam tron)
    public int ShareProtein { get; set; }
    public int ShareFat { get; set; }
    public int ShareCarb { get; set; }

    // Mon co cong thuc thanh phan
    public bool HasRecipes { get; set; }

    // Cac co nhan dien muc tieu
    public bool IsLowFat => Fat.HasValue && Fat.Value <= 5m;
    public bool IsUnder400Kcal => Kcal.HasValue && Kcal.Value <= 400m;
    public bool IsHighProtein => Protein.HasValue && Protein.Value >= 20m;

    // Mau sac khung hinh the mon
    public string TintColor { get; set; } = "#E4F0E6";
    public string InkColor { get; set; } = "#2F7D4F";
}

// ViewModel cho toan bo trang Menu
public class MenuIndexViewModel
{
    public string? Search { get; set; }
    public string? Goal { get; set; }
    public string ActiveTab { get; set; } = "Món chính";

    public List<string> Tabs { get; set; } = new();
    public List<MenuItemViewModel> MainDishes { get; set; } = new();
    public List<MenuItemViewModel> CurrentTabItems { get; set; } = new();
    public List<MenuItemViewModel> SideItems { get; set; } = new();
}
