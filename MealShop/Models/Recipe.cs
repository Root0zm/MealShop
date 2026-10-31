namespace MealShop.Models;

public class Recipe
{
    // Ma mon an duoc tao thanh
    public int DishId { get; set; }
    public Food Dish { get; set; } = null!;

    // Truong ho tro khoa ngoai ghep bat buoc Dish phai co Kind = 'dish'
    public string DishKind { get; set; } = "dish";

    // Ma thanh phan
    public int ComponentId { get; set; }
    public Food Component { get; set; } = null!;

    // Khoi luong thanh phan (gram)
    public decimal Grams { get; set; }

    // Truong ho tro khoa ngoai ghep bat buoc UsableAsComponent = true
    public bool ComponentUsableAsComponent { get; set; } = true;
}
