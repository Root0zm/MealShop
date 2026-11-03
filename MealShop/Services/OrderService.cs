using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.ViewModels;
using MealShop.Extensions;

namespace MealShop.Services;

public class OrderService
{
    // Bo 28 ky tu de doc, loai bo ky tu de nham: 0, O, 1, I, L
    private const string PublicCodeCharset = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    private readonly AppDbContext _context;
    private readonly CartService _cartService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext context, CartService cartService, ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _logger = logger;
    }

    // Sinh ma cong khai 12 ky tu ngau nhien bang RandomNumberGenerator
    public string GeneratePublicCode()
    {
        var chars = new char[12];
        for (int i = 0; i < 12; i++)
        {
            var index = RandomNumberGenerator.GetInt32(PublicCodeCharset.Length);
            chars[i] = PublicCodeCharset[index];
        }
        return new string(chars);
    }

    // Chuan hoa so dien thoai VN: bo khoang trang, dau cham, gach noi, +84 -> 0
    public static string NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var cleaned = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");

        if (cleaned.StartsWith("+84"))
        {
            cleaned = "0" + cleaned.Substring(3);
        }
        else if (cleaned.StartsWith("84") && cleaned.Length >= 11)
        {
            cleaned = "0" + cleaned.Substring(2);
        }

        return cleaned;
    }

    // Tao don hang moi tu gio hang
    public async Task<Order> CreateOrderAsync(CheckoutViewModel model, string? userId)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Warnings.Count > 0)
        {
            throw new InvalidOperationException("Một số món không còn bán đã được bỏ khỏi giỏ. Vui lòng kiểm tra lại giỏ hàng trước khi đặt.");
        }
        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Giỏ hàng đang trống, không thể tạo đơn hàng.");
        }

        // Sinh ma PublicCode duy nhat
        string publicCode;
        do
        {
            publicCode = GeneratePublicCode();
        } while (await _context.Orders.AnyAsync(o => o.PublicCode == publicCode));

        var order = new Order
        {
            PublicCode = publicCode,
            UserId = userId,
            CustomerName = model.CustomerName.Trim(),
            PhoneNumber = NormalizePhoneNumber(model.PhoneNumber),
            ShippingAddress = model.ShippingAddress.Trim(),
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            Status = "Pending",
            TotalAmount = cart.TotalAmount,
            TotalKcal = cart.HasCompleteNutrition ? cart.TotalKcal : null,
            TotalProtein = cart.HasCompleteNutrition ? cart.TotalProtein : null,
            TotalFat = cart.HasCompleteNutrition ? cart.TotalFat : null,
            TotalCarb = cart.HasCompleteNutrition ? cart.TotalCarb : null,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                PortionId = item.PortionId,
                FoodId = item.FoodId,
                FoodName = item.FoodName,
                PortionName = item.PortionName,
                PortionGrams = item.PortionGrams,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LinePrice = item.LinePrice,
                Kcal = item.Kcal,
                Protein = item.Protein,
                Fat = item.Fat,
                Carb = item.Carb
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Xoa sach gio hang sau khi dat thanh cong
        await _cartService.ClearCartAsync();

        // Ghi log don duoc tao: Id, tong tien, so dong (KHONG ghi ten, sdt, dia chi, PublicCode)
        _logger.LogInformation("Don hang moi duoc tao thanh cong. OrderId: {OrderId}, TotalAmount: {TotalAmount}, LinesCount: {LinesCount}",
            order.Id, order.TotalAmount, order.Items.Count);

        return order;
    }

    // Lay danh sach don cua mot nguoi dung da dang nhap
    public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    // Lay chi tiet don cua user (chi don do chinh user so huu, nguoi khac hoac khach thi null)
    public async Task<Order?> GetOrderForUserAsync(int orderId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
    }

    public async Task<Order?> GetConfirmationAsync(string publicCode, string? userId, int? sessionOrderId)
    {
        if (string.IsNullOrWhiteSpace(publicCode)) return null;
        var code = publicCode.Trim().ToUpperInvariant();
        return await _context.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicCode == code &&
                (o.Id == sessionOrderId || (userId != null && userId != "" && o.UserId == userId)));
    }

    // Tra cuu don hang cho khach vang lai bang ma cong khai va so dien thoai
    public async Task<Order?> TrackOrderAsync(string publicCode, string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(publicCode) || string.IsNullOrWhiteSpace(rawPhone))
        {
            return null;
        }

        var cleanCode = publicCode.Trim().ToUpperInvariant();
        var cleanPhone = NormalizePhoneNumber(rawPhone);

        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicCode == cleanCode && o.PhoneNumber == cleanPhone);
    }

    // Dat lai (Reorder) dua tren don cu, dung gia va khau phan hien tai
    public async Task<ReorderResult> ReorderAsync(int orderId)
    {
        var result = new ReorderResult();

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Items.Count == 0)
        {
            result.Success = false;
            result.Message = "Không tìm thấy dữ liệu đơn hàng cũ để đặt lại.";
            return result;
        }

        var portionIds = order.Items.Select(i => i.PortionId).Distinct().ToList();
        var currentPortions = await _context.Portions
            .Include(p => p.Food)
            .Where(p => portionIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        int addedCount = 0;

        foreach (var item in order.Items)
        {
            if (!currentPortions.TryGetValue(item.PortionId, out var currentPortion)
                || !currentPortion.IsActive
                || currentPortion.Price <= 0
                || !currentPortion.Food.IsSellable)
            {
                result.SkippedItems.Add($"Món '{item.FoodName}' ({item.PortionName}) hiện không còn bán hoặc tạm ngưng.");
                continue;
            }

            if (currentPortion.Price != item.UnitPrice)
            {
                result.PriceChangedItems.Add($"Món '{item.FoodName}' giá đã thay đổi từ {item.UnitPrice.ToVnd()} sang {currentPortion.Price.ToVnd()}.");
            }

            await _cartService.AddToCartAsync(currentPortion.Id, item.Quantity);
            addedCount++;
        }

        result.AddedItemsCount = addedCount;
        result.Success = addedCount > 0;
        result.Message = addedCount > 0
            ? $"Đã thêm {addedCount} món vào bữa ăn của bạn."
            : "Không có món nào trong đơn cũ có thể đặt lại vào thời điểm này.";

        return result;
    }

    // Doi trang thai don hang (Admin)
    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null || !OrderStatus.Next(order.Status).Contains(newStatus))
        {
            return false;
        }

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Trang thai don hang {OrderId} da doi thanh {NewStatus}", orderId, newStatus);
        return true;
    }
}
