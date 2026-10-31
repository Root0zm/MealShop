namespace MealShop.Models;

public class Food
{
    // Primary key giu ma goc tu dataset
    public int Id { get; set; }

    // ingredient hoac dish
    public string Kind { get; set; } = "ingredient";

    // Co the dung lam thanh phan cho mon khac
    public bool UsableAsComponent { get; set; }

    // Mon co the ban tren menu
    public bool IsSellable { get; set; }

    // Ten tieng Viet
    public string NameVi { get; set; } = string.Empty;

    // Ten tieng Anh
    public string? NameEn { get; set; }

    // Ten khong dau de tim kiem
    public string? NameNorm { get; set; }

    // Danh muc tieng Viet
    public string? CategoryVi { get; set; }

    // Chi so dinh duong tren 100g
    public decimal? Kcal { get; set; }
    public decimal? Water { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Carb { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Cholesterol { get; set; }
    public decimal? Calcium { get; set; }
    public decimal? Phosphorus { get; set; }
    public decimal? Iron { get; set; }
    public decimal? Sodium { get; set; }
    public decimal? Potassium { get; set; }
    public decimal? Zinc { get; set; }
    public decimal? VitaminA { get; set; }
    public decimal? BetaCarotene { get; set; }
    public decimal? VitaminC { get; set; }

    // Co chat luong du lieu
    public string? EnergyFlag { get; set; }
    public string? QaNote { get; set; }
    public bool IsSuspect { get; set; }

    // Navigation properties
    // Cong thuc gom cac thanh phan cua mon nay
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    // Cac cong thuc su dung mon nay lam thanh phan
    public ICollection<Recipe> UsedInRecipes { get; set; } = new List<Recipe>();

    // Khau phan cua mon nay
    public ICollection<Portion> Portions { get; set; } = new List<Portion>();
}
