namespace MealShop.Models;

public static class OrderStatus
{
    public static IReadOnlyList<string> Next(string status) => status switch
    {
        "Pending" => new[] { "Confirmed", "Cancelled" },
        "Confirmed" => new[] { "Preparing", "Cancelled" },
        "Preparing" => new[] { "Completed" },
        _ => Array.Empty<string>()
    };

    public static string Label(string status) => status switch
    {
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "Preparing" => "Đang chuẩn bị",
        "Completed" => "Đã giao",
        "Cancelled" => "Đã hủy",
        _ => "Chưa xác định"
    };
}
