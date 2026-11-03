using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MealShop.Data;
using MealShop.Models;
using MealShop.ViewModels;

namespace MealShop.Services;

// Dich vu quan ly gio hang luu trong Session
public class CartService
{
    private const string CartSessionKey = "MEALSHOP_CART_SESSION";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;
    private readonly NutritionOptions _nutritionOptions;

    public CartService(
        IHttpContextAccessor httpContextAccessor,
        AppDbContext context,
        IOptions<NutritionOptions>? nutritionOptions = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _nutritionOptions = nutritionOptions?.Value ?? new NutritionOptions();
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Khong tim thay HTTP Session.");

    // Doc danh sach PortionId va Quantity tu Session
    public List<CartSessionItem> GetSessionItems()
    {
        var json = Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<CartSessionItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
        }
        catch
        {
            return new List<CartSessionItem>();
        }
    }

    private void SaveSessionItems(List<CartSessionItem> items)
    {
        var json = JsonSerializer.Serialize(items);
        Session.SetString(CartSessionKey, json);
    }

    // Them san pham vao gio (so luong tu 1 den 20)
    public async Task AddToCartAsync(int portionId, int quantity = 1)
    {
        if (!await _context.Portions.AnyAsync(p => p.Id == portionId && p.IsActive && p.Price > 0 && p.Food.IsSellable))
        {
            throw new InvalidOperationException("Món này hiện không còn bán. Vui lòng chọn món khác.");
        }

        if (quantity < 1) quantity = 1;

        var items = GetSessionItems();
        var existing = items.FirstOrDefault(i => i.PortionId == portionId);

        if (existing != null)
        {
            existing.Quantity = Math.Clamp(existing.Quantity + quantity, 1, 20);
        }
        else
        {
            items.Add(new CartSessionItem
            {
                PortionId = portionId,
                Quantity = Math.Clamp(quantity, 1, 20)
            });
        }

        SaveSessionItems(items);
    }

    // Cap nhat so luong
    public Task UpdateQuantityAsync(int portionId, int quantity)
    {
        var items = GetSessionItems();
        if (quantity <= 0)
        {
            items.RemoveAll(i => i.PortionId == portionId);
        }
        else
        {
            var existing = items.FirstOrDefault(i => i.PortionId == portionId);
            if (existing != null)
            {
                existing.Quantity = Math.Clamp(quantity, 1, 20);
            }
        }

        SaveSessionItems(items);
        return Task.CompletedTask;
    }

    // Xoa mot dong
    public Task RemoveFromCartAsync(int portionId)
    {
        var items = GetSessionItems();
        items.RemoveAll(i => i.PortionId == portionId);
        SaveSessionItems(items);
        return Task.CompletedTask;
    }

    // Xoa toan bo gio hang
    public Task ClearCartAsync()
    {
        Session.Remove(CartSessionKey);
        return Task.CompletedTask;
    }

    // Lay chi tiet gio hang voi gia va dinh duong doc tu CSDL
    public async Task<CartViewModel> GetCartAsync()
    {
        var sessionItems = GetSessionItems();
        var result = new CartViewModel
        {
            ReferenceKcal = _nutritionOptions.ReferenceKcal
        };

        if (sessionItems.Count == 0)
        {
            return result;
        }

        var portionIds = sessionItems.Select(s => s.PortionId).Distinct().ToList();
        var portions = await _context.Portions
            .AsNoTracking()
            .Include(p => p.Food)
            .Where(p => portionIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var validSessionItems = new List<CartSessionItem>();
        bool sessionChanged = false;

        foreach (var item in sessionItems)
        {
            if (!portions.TryGetValue(item.PortionId, out var portion))
            {
                result.Warnings.Add("Một món trong giỏ hàng không còn tồn tại và đã được loại bỏ.");
                sessionChanged = true;
                continue;
            }

            if (!portion.IsActive || !portion.Food.IsSellable || portion.Price <= 0)
            {
                result.Warnings.Add($"Món '{portion.Food.NameVi}' ({portion.Name}) hiện không còn bán và đã được loại khỏi giỏ hàng.");
                sessionChanged = true;
                continue;
            }

            validSessionItems.Add(item);

            var food = portion.Food;
            var line = new CartLineViewModel
            {
                PortionId = portion.Id,
                FoodId = food.Id,
                FoodName = food.NameVi,
                PortionName = portion.Name,
                PortionGrams = portion.Grams,
                Quantity = Math.Clamp(item.Quantity, 1, 20),
                UnitPrice = portion.Price
            };

            // Tinh dinh duong cho dong
            if (food.Kcal.HasValue && food.Protein.HasValue && food.Fat.HasValue && food.Carb.HasValue)
            {
                line.HasNutrition = true;
                var totalGrams = portion.Grams * line.Quantity;
                var ratio = totalGrams / 100m;

                line.Kcal = Math.Round(food.Kcal.Value * ratio, 0);
                line.Protein = Math.Round(food.Protein.Value * ratio, 1);
                line.Fat = Math.Round(food.Fat.Value * ratio, 1);
                line.Carb = Math.Round(food.Carb.Value * ratio, 1);

                result.TotalKcal += line.Kcal.Value;
                result.TotalProtein += line.Protein.Value;
                result.TotalFat += line.Fat.Value;
                result.TotalCarb += line.Carb.Value;
            }
            else
            {
                line.HasNutrition = false;
                result.HasCompleteNutrition = false;
            }

            result.Items.Add(line);
        }

        if (sessionChanged)
        {
            SaveSessionItems(validSessionItems);
        }

        // Tinh ti le % nang luong cua 3 chat da luong cho tong bua an
        var ep = result.TotalProtein * 4m;
        var ef = result.TotalFat * 9m;
        var ec = result.TotalCarb * 4m;
        var totalEnergy = ep + ef + ec;

        if (totalEnergy > 0)
        {
            result.ShareProtein = (int)Math.Round((ep / totalEnergy) * 100m, MidpointRounding.AwayFromZero);
            result.ShareFat = (int)Math.Round((ef / totalEnergy) * 100m, MidpointRounding.AwayFromZero);
            result.ShareCarb = 100 - result.ShareProtein - result.ShareFat;
        }

        return result;
    }
}
