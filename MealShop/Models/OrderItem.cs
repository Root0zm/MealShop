namespace MealShop.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int PortionId { get; set; }
    public Portion? Portion { get; set; }

    public int FoodId { get; set; }

    // Snapshot thong tin mon va gia tai thoi diem dat
    public string FoodName { get; set; } = string.Empty;
    public string PortionName { get; set; } = string.Empty;
    public decimal PortionGrams { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LinePrice { get; set; }

    // Snapshot dinh duong
    public decimal? Kcal { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Carb { get; set; }
}