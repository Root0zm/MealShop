namespace MealShop.Models;

public class Portion
{
    // Khoa chinh
    public int Id { get; set; }

    // Khoa ngoai den Food
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;

    // Ten khau phan
    public string Name { get; set; } = string.Empty;

    // Khoi luong khau phan (gram)
    public decimal Grams { get; set; }
}
