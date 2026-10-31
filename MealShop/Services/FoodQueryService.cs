using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.ViewModels;

namespace MealShop.Services;

public class FoodQueryService
{
    private readonly AppDbContext _context;
    private readonly NutritionCalculatorService _calculatorService;

    public FoodQueryService(AppDbContext context, NutritionCalculatorService? calculatorService = null)
    {
        _context = context;
        _calculatorService = calculatorService ?? new NutritionCalculatorService();
    }

    public async Task<PagedResult<FoodListItemViewModel>> SearchFoodsAsync(
        string? q,
        string? kind,
        string? category,
        int page = 1,
        int pageSize = 20,
        bool? isSuspect = null,
        bool? isSellable = null)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        // Truy van chi doc khong can tracking state
        var query = _context.Foods.AsNoTracking().AsQueryable();

        // Loc theo chuoi tim kiem khong dau
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalizedQuery = TextNormalizer.StripAccents(q);
            query = query.Where(f => f.NameNorm != null && f.NameNorm.Contains(normalizedQuery));
        }

        // Loc theo loai: dish hoac ingredient
        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(f => f.Kind == kind);
        }

        // Loc theo danh muc
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.CategoryVi == category);
        }

        // Loc theo du lieu nghi van
        if (isSuspect.HasValue)
        {
            query = query.Where(f => f.IsSuspect == isSuspect.Value);
        }

        // Loc theo trang thai co the ban
        if (isSellable.HasValue)
        {
            query = query.Where(f => f.IsSellable == isSellable.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.NameVi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FoodListItemViewModel
            {
                Id = f.Id,
                NameVi = f.NameVi,
                Kind = f.Kind,
                CategoryVi = f.CategoryVi,
                Kcal = f.Kcal,
                Protein = f.Protein,
                Fat = f.Fat,
                Carb = f.Carb,
                UsableAsComponent = f.UsableAsComponent,
                IsSellable = f.IsSellable,
                IsSuspect = f.IsSuspect,
                EnergyFlag = f.EnergyFlag,
                QaNote = f.QaNote
            })
            .ToListAsync();

        return new PagedResult<FoodListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        return await _context.Foods
            .AsNoTracking()
            .Where(f => !string.IsNullOrEmpty(f.CategoryVi))
            .Select(f => f.CategoryVi!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<FoodDetailsViewModel?> GetFoodDetailsAsync(int id)
    {
        var food = await _context.Foods
            .AsNoTracking()
            .Include(f => f.Recipes)
                .ThenInclude(r => r.Component)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (food == null)
        {
            return null;
        }

        var totalGrams = food.Recipes.Sum(r => r.Grams);

        var components = food.Recipes
            .OrderByDescending(r => r.Grams)
            .Select(r => new RecipeItemViewModel
            {
                ComponentId = r.ComponentId,
                ComponentNameVi = r.Component.NameVi,
                Grams = r.Grams,
                Percentage = totalGrams > 0 ? Math.Round((r.Grams / totalGrams) * 100m, 1) : 0m
            })
            .ToList();

        // Tinh dinh duong tu cong thuc
        var calculatedNutrition = _calculatorService.CalculateDishNutrition(food, food.Recipes.ToList());

        return new FoodDetailsViewModel
        {
            Id = food.Id,
            Kind = food.Kind,
            UsableAsComponent = food.UsableAsComponent,
            IsSellable = food.IsSellable,
            NameVi = food.NameVi,
            NameEn = food.NameEn,
            CategoryVi = food.CategoryVi,
            Kcal = food.Kcal,
            Water = food.Water,
            Protein = food.Protein,
            Fat = food.Fat,
            Carb = food.Carb,
            Fiber = food.Fiber,
            Cholesterol = food.Cholesterol,
            Calcium = food.Calcium,
            Phosphorus = food.Phosphorus,
            Iron = food.Iron,
            Sodium = food.Sodium,
            Potassium = food.Potassium,
            Zinc = food.Zinc,
            VitaminA = food.VitaminA,
            BetaCarotene = food.BetaCarotene,
            VitaminC = food.VitaminC,
            EnergyFlag = food.EnergyFlag,
            QaNote = food.QaNote,
            IsSuspect = food.IsSuspect,
            Components = components,
            CalculatedNutrition = calculatedNutrition
        };
    }
}
