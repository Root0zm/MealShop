namespace MealShop.ViewModels;

public class FoodListItemViewModel
{
    public int Id { get; init; }
    public string NameVi { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string? CategoryVi { get; init; }
    public decimal? Kcal { get; init; }
    public decimal? Protein { get; init; }
    public decimal? Fat { get; init; }
    public decimal? Carb { get; init; }
    public bool UsableAsComponent { get; init; }
    public bool IsSellable { get; init; }
    public bool IsSuspect { get; init; }
    public string? EnergyFlag { get; init; }
    public string? QaNote { get; init; }
}
