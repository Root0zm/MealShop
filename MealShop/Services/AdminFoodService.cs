using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.ViewModels;

namespace MealShop.Services;

// Dich vu quan ly thuc pham va mon an cho Admin
public class AdminFoodService
{
    private readonly AppDbContext _context;

    public AdminFoodService(AppDbContext context)
    {
        _context = context;
    }

    // Sinh ma moi cho mon do Admin tao: max(Id hien co, 99_999_999) + 1
    public async Task<int> GenerateNextFoodIdAsync()
    {
        var maxId = await _context.Foods.MaxAsync(f => (int?)f.Id) ?? 0;
        return Math.Max(maxId, 99_999_999) + 1;
    }

    // Tao moi thuc pham hoac mon an
    public async Task<Food> CreateFoodAsync(AdminFoodFormViewModel model)
    {
        var nextId = await GenerateNextFoodIdAsync();

        // Nguyen lieu bat buoc phai co UsableAsComponent = true
        var usable = model.Kind == "ingredient" ? true : model.UsableAsComponent;

        var food = new Food
        {
            Id = nextId,
            Kind = model.Kind,
            UsableAsComponent = usable,
            IsSellable = model.IsSellable,
            NameVi = model.NameVi.Trim(),
            NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim(),
            NameNorm = TextNormalizer.StripAccents(model.NameVi.Trim()),
            CategoryVi = string.IsNullOrWhiteSpace(model.CategoryVi) ? null : model.CategoryVi.Trim(),
            Kcal = model.Kcal,
            Water = model.Water,
            Protein = model.Protein,
            Fat = model.Fat,
            Carb = model.Carb,
            Fiber = model.Fiber,
            Cholesterol = model.Cholesterol,
            Calcium = model.Calcium,
            Phosphorus = model.Phosphorus,
            Iron = model.Iron,
            Sodium = model.Sodium,
            Potassium = model.Potassium,
            Zinc = model.Zinc,
            VitaminA = model.VitaminA,
            BetaCarotene = model.BetaCarotene,
            VitaminC = model.VitaminC,
            EnergyFlag = model.EnergyFlag,
            QaNote = model.QaNote,
            IsSuspect = model.IsSuspect
        };

        _context.Foods.Add(food);
        await _context.SaveChangesAsync();
        return food;
    }

    // Cap nhat thuc pham hoac mon an
    public async Task UpdateFoodAsync(int id, AdminFoodFormViewModel model)
    {
        var food = await _context.Foods.FirstOrDefaultAsync(f => f.Id == id);
        if (food == null)
        {
            throw new KeyNotFoundException($"Khong tim thay thuc pham voi ma {id}.");
        }

        // Nguyen lieu bat buoc phai co UsableAsComponent = true
        var usable = model.Kind == "ingredient" ? true : model.UsableAsComponent;

        food.Kind = model.Kind;
        food.UsableAsComponent = usable;
        food.IsSellable = model.IsSellable;
        food.NameVi = model.NameVi.Trim();
        food.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? null : model.NameEn.Trim();
        food.NameNorm = TextNormalizer.StripAccents(model.NameVi.Trim());
        food.CategoryVi = string.IsNullOrWhiteSpace(model.CategoryVi) ? null : model.CategoryVi.Trim();
        food.Kcal = model.Kcal;
        food.Water = model.Water;
        food.Protein = model.Protein;
        food.Fat = model.Fat;
        food.Carb = model.Carb;
        food.Fiber = model.Fiber;
        food.Cholesterol = model.Cholesterol;
        food.Calcium = model.Calcium;
        food.Phosphorus = model.Phosphorus;
        food.Iron = model.Iron;
        food.Sodium = model.Sodium;
        food.Potassium = model.Potassium;
        food.Zinc = model.Zinc;
        food.VitaminA = model.VitaminA;
        food.BetaCarotene = model.BetaCarotene;
        food.VitaminC = model.VitaminC;
        food.EnergyFlag = model.EnergyFlag;
        food.QaNote = model.QaNote;
        food.IsSuspect = model.IsSuspect;

        await _context.SaveChangesAsync();
    }

    // Kiem tra mon an co the xoa hay khong
    public async Task<(bool CanDelete, string Reason)> CanDeleteFoodAsync(int id)
    {
        var isUsedAsComponent = await _context.Recipes.AnyAsync(r => r.ComponentId == id);
        if (isUsedAsComponent)
        {
            return (false, "Khong the xoa mon nay vi dang duoc dung lam thanh phan trong cong thuc khac. Goi y: ban co the an mon bang cach tat 'Co the ban tren menu' (IsSellable = false).");
        }

        return (true, string.Empty);
    }

    // Xoa mon an neu thoa man dieu kien
    public async Task<(bool Succeeded, string ErrorMessage)> DeleteFoodAsync(int id)
    {
        var (canDelete, reason) = await CanDeleteFoodAsync(id);
        if (!canDelete)
        {
            return (false, reason);
        }

        var food = await _context.Foods.FirstOrDefaultAsync(f => f.Id == id);
        if (food == null)
        {
            return (false, $"Khong tim thay thuc pham voi ma {id}.");
        }

        _context.Foods.Remove(food);
        await _context.SaveChangesAsync();
        return (true, string.Empty);
    }
}
