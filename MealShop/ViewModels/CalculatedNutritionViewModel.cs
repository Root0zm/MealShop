namespace MealShop.ViewModels;

public class NutrientComparison
{
    public decimal Calculated { get; set; }
    public decimal? Reference { get; set; }
    public decimal? DiffPercent => (Reference.HasValue && Reference.Value > 0)
        ? Math.Round(((Calculated - Reference.Value) / Reference.Value) * 100m, 1)
        : null;
}

public class CalculatedNutritionViewModel
{
    public decimal TotalGrams { get; set; }

    // 4 chi so chinh theo ban uoc luong
    public NutrientComparison Kcal { get; set; } = new();
    public NutrientComparison Protein { get; set; } = new();
    public NutrientComparison Fat { get; set; } = new();
    public NutrientComparison Carb { get; set; } = new();

    // Cac chi so mo rong
    public NutrientComparison Water { get; set; } = new();
    public NutrientComparison Fiber { get; set; } = new();
    public NutrientComparison Sodium { get; set; } = new();
    public NutrientComparison Calcium { get; set; } = new();
    public NutrientComparison Iron { get; set; } = new();
    public NutrientComparison VitaminC { get; set; } = new();

    // Canh bao khi co thanh phan thuoc nhom nghi van IsSuspect
    public IReadOnlyList<string> SuspectComponentWarnings { get; set; } = Array.Empty<string>();
}
