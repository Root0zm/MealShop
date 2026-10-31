using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MealShop.Data;
using MealShop.Models;
using MealShop.ViewModels;

namespace MealShop.Services;

public class MenuService
{
    private readonly AppDbContext _context;
    private readonly MenuOptions _menuOptions;

    private static readonly (string Tint, string Ink)[] Palettes =
    {
        ("#E4F0E6", "#2F7D4F"),
        ("#FBEBD3", "#A8660B"),
        ("#E1ECF5", "#2F6690"),
        ("#F3E3E1", "#A24A3F")
    };

    public MenuService(AppDbContext context, IOptions<MenuOptions> menuOptions)
    {
        _context = context;
        _menuOptions = menuOptions.Value;
    }

    // Tinh toan gia tri dinh duong va % nang luong cho mot khau phan
    public static (bool HasComplete, decimal? Kcal, decimal? Protein, decimal? Fat, decimal? Carb, int ShareP, int ShareF, int ShareC)
        CalculatePortionNutrition(Food food, decimal portionGrams)
    {
        if (portionGrams <= 0 || !food.Kcal.HasValue || !food.Protein.HasValue || !food.Fat.HasValue || !food.Carb.HasValue)
        {
            return (false, null, null, null, null, 0, 0, 0);
        }

        var ratio = portionGrams / 100m;
        var kcal = Math.Round(food.Kcal.Value * ratio, 0);
        var protein = Math.Round(food.Protein.Value * ratio, 1);
        var fat = Math.Round(food.Fat.Value * ratio, 1);
        var carb = Math.Round(food.Carb.Value * ratio, 1);

        var energyP = protein * 4m;
        var energyF = fat * 9m;
        var energyC = carb * 4m;
        var totalEnergy = energyP + energyF + energyC;

        if (totalEnergy <= 0)
        {
            return (true, kcal, protein, fat, carb, 0, 0, 0);
        }

        var shareP = (int)Math.Round((energyP / totalEnergy) * 100m, MidpointRounding.AwayFromZero);
        var shareF = (int)Math.Round((energyF / totalEnergy) * 100m, MidpointRounding.AwayFromZero);
        var shareC = 100 - shareP - shareF;

        return (true, kcal, protein, fat, carb, shareP, shareF, shareC);
    }

    // Xac dinh Tab thuc don cho mon an dua tren CategoryVi va quy tac cau hinh
    public string DetermineTab(Food food)
    {
        foreach (var rule in _menuOptions.Rules)
        {
            if (DefaultPortionService.FoodMatchesRule(food, rule))
            {
                return rule.Tab;
            }
        }

        return string.Empty;
    }

    // Truy van du lieu thuc don
    public async Task<MenuIndexViewModel> GetMenuAsync(string? tab, string? goal, string? search)
    {
        // Chi lay cac mon co IsSellable = true va co it nhat mot Portion IsActive voi Price > 0
        var query = _context.Foods
            .AsNoTracking()
            .Where(f => f.IsSellable && f.Portions.Any(p => p.IsActive && p.Price > 0))
            .Include(f => f.Portions.Where(p => p.IsActive && p.Price > 0))
            .Include(f => f.Recipes)
                .ThenInclude(r => r.Component)
            .AsQueryable();

        // Loc tim kiem khong dau
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = TextNormalizer.StripAccents(search.Trim()).ToLowerInvariant();
            query = query.Where(f => f.NameNorm != null && f.NameNorm.Contains(normSearch));
        }

        var foods = await query.ToListAsync();

        var allItems = new List<MenuItemViewModel>();

        foreach (var food in foods)
        {
            var foodTab = DetermineTab(food);
            if (string.IsNullOrEmpty(foodTab))
            {
                continue;
            }

            var primaryPortion = food.Portions.OrderBy(p => p.Price).First();
            var (hasComplete, kcal, protein, fat, carb, sp, sf, sc) =
                CalculatePortionNutrition(food, primaryPortion.Grams);

            string? desc = null;
            if (food.Recipes.Count > 0)
            {
                desc = string.Join(", ", food.Recipes.Select(r => r.Component?.NameVi ?? "").Where(s => !string.IsNullOrEmpty(s)));
            }
            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = food.CategoryVi;
            }

            var palette = Palettes[Math.Abs(food.Id) % Palettes.Length];

            var item = new MenuItemViewModel
            {
                FoodId = food.Id,
                PortionId = primaryPortion.Id,
                Name = food.NameVi,
                Description = desc,
                Tab = foodTab,
                PortionName = primaryPortion.Name,
                PortionGrams = primaryPortion.Grams,
                Price = primaryPortion.Price,
                HasCompleteNutrition = hasComplete,
                Kcal = kcal,
                Protein = protein,
                Fat = fat,
                Carb = carb,
                ShareProtein = sp,
                ShareFat = sf,
                ShareCarb = sc,
                TintColor = palette.Tint,
                InkColor = palette.Ink
            };

            allItems.Add(item);
        }

        // Loc theo muc tieu
        if (!string.IsNullOrWhiteSpace(goal))
        {
            allItems = goal.ToLowerInvariant() switch
            {
                "under-400" => allItems.Where(i => i.IsUnder400Kcal).ToList(),
                "high-protein" => allItems.Where(i => i.IsHighProtein).ToList(),
                "low-fat" => allItems.Where(i => i.IsLowFat).ToList(),
                _ => allItems
            };
        }

        // Danh sach Tab theo thu tu quy dinh trong cau hinh va chi lay cac tab co mon
        var configuredTabs = _menuOptions.Rules
            .Select(r => r.Tab)
            .Distinct()
            .ToList();

        var availableTabs = configuredTabs
            .Where(t => allItems.Any(i => i.Tab == t))
            .ToList();

        var activeTab = string.IsNullOrWhiteSpace(tab)
            ? (availableTabs.FirstOrDefault() ?? "Món chính")
            : tab;

        var currentTabItems = allItems.Where(i => i.Tab == activeTab).ToList();
        var mainDishes = allItems.Where(i => i.Tab == "Món chính").ToList();

        // Mon kem: lay tu cac tab con lai (Thit, Hai san, Rau cu, Trung, Dau hat)
        var sideItems = allItems
            .Where(i => i.Tab != "Món chính" && i.Tab != "Đồ uống" && i.Tab != "Tráng miệng")
            .Take(8)
            .ToList();

        return new MenuIndexViewModel
        {
            Search = search,
            Goal = goal,
            ActiveTab = activeTab,
            Tabs = availableTabs,
            MainDishes = mainDishes,
            CurrentTabItems = currentTabItems,
            SideItems = sideItems
        };
    }
}
