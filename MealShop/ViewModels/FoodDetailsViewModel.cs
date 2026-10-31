namespace MealShop.ViewModels;

public class FoodDetailsViewModel
{
    public int Id { get; init; }
    public string Kind { get; init; } = string.Empty;
    public bool UsableAsComponent { get; init; }
    public bool IsSellable { get; init; }
    public string NameVi { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? CategoryVi { get; init; }

    // Chi so dinh duong tren 100g
    public decimal? Kcal { get; init; }
    public decimal? Water { get; init; }
    public decimal? Protein { get; init; }
    public decimal? Fat { get; init; }
    public decimal? Carb { get; init; }
    public decimal? Fiber { get; init; }
    public decimal? Cholesterol { get; init; }
    public decimal? Calcium { get; init; }
    public decimal? Phosphorus { get; init; }
    public decimal? Iron { get; init; }
    public decimal? Sodium { get; init; }
    public decimal? Potassium { get; init; }
    public decimal? Zinc { get; init; }
    public decimal? VitaminA { get; init; }
    public decimal? BetaCarotene { get; init; }
    public decimal? VitaminC { get; init; }

    // Co chat luong du lieu
    public string? EnergyFlag { get; init; }
    public string? QaNote { get; init; }
    public bool IsSuspect { get; init; }

    // Danh sach thanh phan cau thanh mon an (Recipes)
    public IReadOnlyList<RecipeItemViewModel> Components { get; init; } = Array.Empty<RecipeItemViewModel>();

    // Ket qua tinh toan dinh duong tu cong thuc
    public CalculatedNutritionViewModel? CalculatedNutrition { get; init; }
}

public class RecipeItemViewModel
{
    public int ComponentId { get; init; }
    public string ComponentNameVi { get; init; } = string.Empty;
    public decimal Grams { get; init; }
    public decimal Percentage { get; init; }
}
