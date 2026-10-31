namespace MealShop.ViewModels;

public class FoodsIndexViewModel
{
    public string? Q { get; init; }
    public string? Kind { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> AvailableCategories { get; init; } = Array.Empty<string>();
    public PagedResult<FoodListItemViewModel> PagedFoods { get; init; } = new();
}
