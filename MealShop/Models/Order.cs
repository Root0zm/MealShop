namespace MealShop.Models;

public class Order
{
    public int Id { get; set; }

    // Ma cong khai 12 ky tu khong trung lap
    public string PublicCode { get; set; } = string.Empty;

    // Khoa ngoai toi nguoi dung (null neu la khach vang lai)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Thong tin nguoi nhan
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }

    // Trang thai: Pending, Confirmed, Preparing, Completed, Cancelled
    public string Status { get; set; } = "Pending";

    // Tong gia tri va dinh duong
    public decimal TotalAmount { get; set; }
    public decimal? TotalKcal { get; set; }
    public decimal? TotalProtein { get; set; }
    public decimal? TotalFat { get; set; }
    public decimal? TotalCarb { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}