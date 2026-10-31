using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;

namespace MealShop.Services;

// Dich vu quan ly cong thuc va khau phan trong trang quan tri
public class AdminRecipeService
{
    private readonly AppDbContext _context;

    public AdminRecipeService(AppDbContext context)
    {
        _context = context;
    }

    // Lay danh sach cac nguyen lieu va mon an duoc phep lam thanh phan (UsableAsComponent = true)
    public async Task<List<Food>> GetUsableComponentsAsync(int excludeDishId)
    {
        return await _context.Foods
            .AsNoTracking()
            .Where(f => f.UsableAsComponent && f.Id != excludeDishId)
            .OrderBy(f => f.NameVi)
            .ToListAsync();
    }

    // Them thanh phan vao cong thuc cua mon an
    public async Task<(bool Succeeded, string Message)> AddComponentAsync(int dishId, int componentId, decimal grams)
    {
        if (grams <= 0)
        {
            return (false, "Khoi luong thanh phan phai lon hon 0 gram.");
        }

        if (dishId == componentId)
        {
            return (false, "Mon an khong the tu tham chieu chinh no lam thanh phan.");
        }

        var dish = await _context.Foods.FirstOrDefaultAsync(f => f.Id == dishId);
        if (dish == null || dish.Kind != "dish")
        {
            return (false, "Chi co the them thanh phan vao mon an (dish).");
        }

        var component = await _context.Foods.FirstOrDefaultAsync(f => f.Id == componentId);
        if (component == null)
        {
            return (false, "Khong tim thay thanh phan duoc chon.");
        }

        if (!component.UsableAsComponent)
        {
            return (false, "Thuc pham nay khong duoc phep dung lam thanh phan (UsableAsComponent = false).");
        }

        var exists = await _context.Recipes.AnyAsync(r => r.DishId == dishId && r.ComponentId == componentId);
        if (exists)
        {
            return (false, "Thanh phan nay da ton tai trong cong thuc cua mon an.");
        }

        var recipe = new Recipe
        {
            DishId = dishId,
            DishKind = dish.Kind,
            ComponentId = componentId,
            ComponentUsableAsComponent = component.UsableAsComponent,
            Grams = grams
        };

        try
        {
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return (true, $"Da them thanh phan '{component.NameVi}' ({grams:F1} g) vao cong thuc.");
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Loi tu co so du lieu khi them thanh phan: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Cap nhat khoi luong thanh phan trong cong thuc
    public async Task<(bool Succeeded, string Message)> UpdateComponentGramsAsync(int dishId, int componentId, decimal grams)
    {
        if (grams <= 0)
        {
            return (false, "Khoi luong thanh phan phai lon hon 0 gram.");
        }

        var recipe = await _context.Recipes
            .Include(r => r.Component)
            .FirstOrDefaultAsync(r => r.DishId == dishId && r.ComponentId == componentId);

        if (recipe == null)
        {
            return (false, "Khong tim thay thanh phan nay trong cong thuc.");
        }

        recipe.Grams = grams;

        try
        {
            await _context.SaveChangesAsync();
            return (true, $"Da cap nhat khoi luong '{recipe.Component.NameVi}' thanh {grams:F1} g.");
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Loi khi cap nhat thanh phan: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Xoa thanh phan khoi cong thuc
    public async Task<(bool Succeeded, string Message)> RemoveComponentAsync(int dishId, int componentId)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Component)
            .FirstOrDefaultAsync(r => r.DishId == dishId && r.ComponentId == componentId);

        if (recipe == null)
        {
            return (false, "Khong tim thay thanh phan trong cong thuc de xoa.");
        }

        var name = recipe.Component?.NameVi ?? componentId.ToString();
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return (true, $"Da xoa thanh phan '{name}' khoi cong thuc.");
    }

    // Them khau phan moi cho mon an
    public async Task<(bool Succeeded, string Message)> AddPortionAsync(int foodId, string name, decimal grams, decimal price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Ten khau phan khong duoc de trong.");
        }

        if (grams <= 0)
        {
            return (false, "Khoi luong khau phan phai lon hon 0 gram.");
        }

        if (price < 0)
        {
            return (false, "Gia ban khau phan khong duoc am.");
        }

        var trimmedName = name.Trim();
        var exists = await _context.Portions.AnyAsync(p => p.FoodId == foodId && p.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            return (false, $"Khau phan '{trimmedName}' da ton tai trong mon an nay.");
        }

        var portion = new Portion
        {
            FoodId = foodId,
            Name = trimmedName,
            Grams = grams,
            Price = price,
            IsActive = isActive
        };

        try
        {
            _context.Portions.Add(portion);
            await _context.SaveChangesAsync();
            return (true, $"Da them khau phan '{trimmedName}' thanh cong.");
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Loi CSDL khi tao khau phan: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Cap nhat khau phan
    public async Task<(bool Succeeded, string Message)> UpdatePortionAsync(int portionId, string name, decimal grams, decimal price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Ten khau phan khong duoc de trong.");
        }

        if (grams <= 0)
        {
            return (false, "Khoi luong khau phan phai lon hon 0 gram.");
        }

        if (price < 0)
        {
            return (false, "Gia ban khau phan khong duoc am.");
        }

        var portion = await _context.Portions.FirstOrDefaultAsync(p => p.Id == portionId);
        if (portion == null)
        {
            return (false, "Khong tim thay khau phan can cap nhat.");
        }

        var trimmedName = name.Trim();
        var exists = await _context.Portions.AnyAsync(p => p.FoodId == portion.FoodId && p.Id != portionId && p.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            return (false, $"Khau phan trung ten '{trimmedName}' da ton tai trong mon an nay.");
        }

        portion.Name = trimmedName;
        portion.Grams = grams;
        portion.Price = price;
        portion.IsActive = isActive;

        try
        {
            await _context.SaveChangesAsync();
            return (true, $"Cap nhat khau phan '{trimmedName}' thanh cong.");
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Loi CSDL khi cap nhat khau phan: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Xoa khau phan
    public async Task<(bool Succeeded, string Message)> DeletePortionAsync(int portionId)
    {
        var portion = await _context.Portions.FirstOrDefaultAsync(p => p.Id == portionId);
        if (portion == null)
        {
            return (false, "Khong tim thay khau phan de xoa.");
        }

        var name = portion.Name;
        _context.Portions.Remove(portion);
        await _context.SaveChangesAsync();
        return (true, $"Da xoa khau phan '{name}'.");
    }
}
