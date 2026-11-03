using System.ComponentModel.DataAnnotations;
using MealShop.Models;

namespace MealShop.ViewModels;

// Form dat hang
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên người nhận")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)[0-9\s\.\-]{8,15}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ: 0912345678).")]
    [Display(Name = "Số điện thoại liên hệ")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(250, ErrorMessage = "Địa chỉ không được vượt quá 250 ký tự.")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    [Display(Name = "Ghi chú đơn hàng (nếu có)")]
    public string? Note { get; set; }

    // Thong tin gio hang de hien thi tom tat
    public CartViewModel? Cart { get; set; }
}

// Form tra cuu don cho khach vang lai
public class OrderTrackViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng (12 ký tự).")]
    [StringLength(12, MinimumLength = 12, ErrorMessage = "Mã đơn hàng phải đúng 12 ký tự.")]
    [Display(Name = "Mã đơn hàng")]
    public string PublicCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại đặt hàng.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}

// Chi tiet don hang
public class OrderDetailsViewModel
{
    public Order Order { get; set; } = null!;
    public bool CanReorder { get; set; }
    public bool IsAdmin { get; set; }
}

// Ket qua dat lai (Reorder)
public class ReorderResult
{
    public bool Success { get; set; }
    public int AddedItemsCount { get; set; }
    public List<string> SkippedItems { get; set; } = new();
    public List<string> PriceChangedItems { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}