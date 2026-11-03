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
            return (false, "Khối lượng thành phần phải lớn hơn 0 gram.");
        }

        if (dishId == componentId)
        {
            return (false, "Món ăn không thể tự tham chiếu chính nó làm thành phần.");
        }

        var dish = await _context.Foods.FirstOrDefaultAsync(f => f.Id == dishId);
        if (dish == null || dish.Kind != "dish")
        {
            return (false, "Chỉ có thể thêm thành phần vào món ăn (dish).");
        }

        var component = await _context.Foods.FirstOrDefaultAsync(f => f.Id == componentId);
        if (component == null)
        {
            return (false, "Không tìm thấy thành phần được chọn.");
        }

        if (!component.UsableAsComponent)
        {
            return (false, "Thực phẩm này không được phép dùng làm thành phần (UsableAsComponent = false).");
        }

        var exists = await _context.Recipes.AnyAsync(r => r.DishId == dishId && r.ComponentId == componentId);
        if (exists)
        {
            return (false, "Thành phần này đã tồn tại trong công thức của món ăn.");
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
            return (true, $"Đã thêm thành phần '{component.NameVi}' ({grams:F1} g) vào công thức.");
        }
        catch (DbUpdateException)
        {
            return (false, "Không thể thêm thành phần. Vui lòng kiểm tra món, nguyên liệu và khối lượng rồi thử lại.");
        }
    }

    // Cap nhat khoi luong thanh phan trong cong thuc
    public async Task<(bool Succeeded, string Message)> UpdateComponentGramsAsync(int dishId, int componentId, decimal grams)
    {
        if (grams <= 0)
        {
            return (false, "Khối lượng thành phần phải lớn hơn 0 gram.");
        }

        var recipe = await _context.Recipes
            .Include(r => r.Component)
            .FirstOrDefaultAsync(r => r.DishId == dishId && r.ComponentId == componentId);

        if (recipe == null)
        {
            return (false, "Không tìm thấy thành phần này trong công thức.");
        }

        recipe.Grams = grams;

        try
        {
            await _context.SaveChangesAsync();
            return (true, $"Đã cập nhật khối lượng '{recipe.Component.NameVi}' thành {grams:F1} g.");
        }
        catch (DbUpdateException)
        {
            return (false, "Không thể cập nhật thành phần. Vui lòng kiểm tra khối lượng rồi thử lại.");
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
            return (false, "Không tìm thấy thành phần trong công thức để xóa.");
        }

        var name = recipe.Component?.NameVi ?? componentId.ToString();
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa thành phần '{name}' khỏi công thức.");
    }

    // Them khau phan moi cho mon an
    public async Task<(bool Succeeded, string Message)> AddPortionAsync(int foodId, string name, decimal grams, decimal price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Tên khẩu phần không được để trống.");
        }

        if (grams <= 0)
        {
            return (false, "Khối lượng khẩu phần phải lớn hơn 0 gram.");
        }

        if (price < 0)
        {
            return (false, "Giá bán khẩu phần không được âm.");
        }

        var trimmedName = name.Trim();
        var exists = await _context.Portions.AnyAsync(p => p.FoodId == foodId && p.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            return (false, $"Khẩu phần '{trimmedName}' đã tồn tại trong món ăn này.");
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
            return (true, $"Đã thêm khẩu phần '{trimmedName}' thành công.");
        }
        catch (DbUpdateException)
        {
            return (false, "Không thể tạo khẩu phần. Vui lòng kiểm tra tên, khối lượng và giá bán rồi thử lại.");
        }
    }

    // Cap nhat khau phan
    public async Task<(bool Succeeded, string Message)> UpdatePortionAsync(int portionId, string name, decimal grams, decimal price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Tên khẩu phần không được để trống.");
        }

        if (grams <= 0)
        {
            return (false, "Khối lượng khẩu phần phải lớn hơn 0 gram.");
        }

        if (price < 0)
        {
            return (false, "Giá bán khẩu phần không được âm.");
        }

        var portion = await _context.Portions.FirstOrDefaultAsync(p => p.Id == portionId);
        if (portion == null)
        {
            return (false, "Không tìm thấy khẩu phần cần cập nhật.");
        }

        var trimmedName = name.Trim();
        var exists = await _context.Portions.AnyAsync(p => p.FoodId == portion.FoodId && p.Id != portionId && p.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            return (false, $"Khẩu phần trùng tên '{trimmedName}' đã tồn tại trong món ăn này.");
        }

        portion.Name = trimmedName;
        portion.Grams = grams;
        portion.Price = price;
        portion.IsActive = isActive;

        try
        {
            await _context.SaveChangesAsync();
            return (true, $"Cập nhật khẩu phần '{trimmedName}' thành công.");
        }
        catch (DbUpdateException)
        {
            return (false, "Không thể cập nhật khẩu phần. Vui lòng kiểm tra tên, khối lượng và giá bán rồi thử lại.");
        }
    }

    // Xoa khau phan
    public async Task<(bool Succeeded, string Message)> DeletePortionAsync(int portionId)
    {
        if (await _context.OrderItems.AnyAsync(i => i.PortionId == portionId))
        {
            return (false, "Khẩu phần này đã có trong đơn hàng nên không thể xóa. Hãy tắt trạng thái bán của khẩu phần.");
        }

        var portion = await _context.Portions.FirstOrDefaultAsync(p => p.Id == portionId);
        if (portion == null)
        {
            return (false, "Không tìm thấy khẩu phần để xóa.");
        }

        var name = portion.Name;
        _context.Portions.Remove(portion);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa khẩu phần '{name}'.");
    }
}
