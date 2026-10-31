namespace MealShop.ViewModels;

public class FoodsIndexViewModel
{
    public string? Q { get; init; }
    public string? Kind { get; init; }
    public string? Category { get; init; }
    public bool? IsSuspect { get; init; }
    public bool? IsSellable { get; init; }
    public IReadOnlyList<string> AvailableCategories { get; init; } = Array.Empty<string>();
    public PagedResult<FoodListItemViewModel> PagedFoods { get; init; } = new();
}
