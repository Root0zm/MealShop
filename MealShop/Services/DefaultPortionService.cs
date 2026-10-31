using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MealShop.Data;
using MealShop.Models;

namespace MealShop.Services;

public class TabPreviewSummary
{
    public string Tab { get; set; } = string.Empty;
    public string CategoryVi { get; set; } = string.Empty;
    public string PortionName { get; set; } = string.Empty;
    public decimal Grams { get; set; }
    public decimal Price { get; set; }
    public int ToCreateCount { get; set; }
    public int AlreadyExistsCount { get; set; }
    public int SuspectCount { get; set; }
    public List<string> SuspectFoodNames { get; set; } = new();
    public List<string> ToCreateFoodNames { get; set; } = new();
}

public class DefaultPortionPreviewResult
{
    public List<TabPreviewSummary> TabSummaries { get; set; } = new();
    public int TotalToCreate => TabSummaries.Sum(t => t.ToCreateCount);
    public int TotalAlreadyExists => TabSummaries.Sum(t => t.AlreadyExistsCount);
    public int TotalSuspect => TabSummaries.Sum(t => t.SuspectCount);
}

public class DefaultPortionApplyResult
{
    public int CreatedCount { get; set; }
    public int SkippedExistingCount { get; set; }
    public int SkippedSuspectCount { get; set; }
    public List<TabPreviewSummary> TabSummaries { get; set; } = new();
}

// Dich vu tao khau phan mac dinh theo quy tac cau hinh
public class DefaultPortionService
{
    private readonly AppDbContext _context;
    private readonly MenuOptions _menuOptions;

    public DefaultPortionService(AppDbContext context, IOptions<MenuOptions> menuOptions)
    {
        _context = context;
        _menuOptions = menuOptions.Value;
    }

    // Kiem tra mon an co khop quy tac hay khong
    public static bool FoodMatchesRule(Food food, MenuRule rule)
    {
        if (!string.Equals(food.CategoryVi, rule.CategoryVi, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var norm = food.NameNorm ?? TextNormalizer.StripAccents(food.NameVi).ToLowerInvariant();

        // Neu co yeu cau tu khoa bat buoc
        if (rule.NameContainsAny != null && rule.NameContainsAny.Count > 0)
        {
            var hasMatch = rule.NameContainsAny.Any(keyword => norm.Contains(keyword.ToLowerInvariant()));
            if (!hasMatch)
            {
                return false;
            }
        }

        // Neu co tu khoa loai tru
        if (rule.ExcludeNameContainsAny != null && rule.ExcludeNameContainsAny.Count > 0)
        {
            var hasExcluded = rule.ExcludeNameContainsAny.Any(keyword => norm.Contains(keyword.ToLowerInvariant()));
            if (hasExcluded)
            {
                return false;
            }
        }

        return true;
    }

    // Xem truoc so luong khau phan se tao, bo qua vi da co hoac vi IsSuspect
    public async Task<DefaultPortionPreviewResult> PreviewAsync()
    {
        var allFoods = await _context.Foods
            .Include(f => f.Portions)
            .ToListAsync();

        var result = new DefaultPortionPreviewResult();

        foreach (var rule in _menuOptions.Rules)
        {
            var summary = new TabPreviewSummary
            {
                Tab = rule.Tab,
                CategoryVi = rule.CategoryVi,
                PortionName = rule.PortionName,
                Grams = rule.Grams,
                Price = rule.Price
            };

            foreach (var food in allFoods)
            {
                if (!FoodMatchesRule(food, rule))
                {
                    continue;
                }

                if (food.IsSuspect)
                {
                    summary.SuspectCount++;
                    summary.SuspectFoodNames.Add($"{food.NameVi} (#{food.Id} - {food.EnergyFlag})");
                    continue;
                }

                var hasPortion = food.Portions.Any(p => string.Equals(p.Name, rule.PortionName, StringComparison.OrdinalIgnoreCase));
                if (hasPortion)
                {
                    summary.AlreadyExistsCount++;
                }
                else
                {
                    summary.ToCreateCount++;
                    summary.ToCreateFoodNames.Add($"{food.NameVi} (#{food.Id})");
                }
            }

            result.TabSummaries.Add(summary);
        }

        return result;
    }

    // Ap dung tao khau phan mac dinh va bat co IsSellable cho mon an
    public async Task<DefaultPortionApplyResult> ApplyAsync()
    {
        var allFoods = await _context.Foods
            .Include(f => f.Portions)
            .ToListAsync();

        var preview = await PreviewAsync();
        int createdCount = 0;
        int skippedExisting = 0;
        int skippedSuspect = 0;

        IDbContextTransaction? transaction = _context.Database.CurrentTransaction == null
            ? await _context.Database.BeginTransactionAsync()
            : null;

        try
        {
            foreach (var rule in _menuOptions.Rules)
            {
                foreach (var food in allFoods)
                {
                    if (!FoodMatchesRule(food, rule))
                    {
                        continue;
                    }

                    if (food.IsSuspect)
                    {
                        skippedSuspect++;
                        continue;
                    }

                    var hasPortion = food.Portions.Any(p => string.Equals(p.Name, rule.PortionName, StringComparison.OrdinalIgnoreCase));
                    if (hasPortion)
                    {
                        skippedExisting++;
                        continue;
                    }

                    var portion = new Portion
                    {
                        FoodId = food.Id,
                        Name = rule.PortionName,
                        Grams = rule.Grams,
                        Price = rule.Price,
                        IsActive = true
                    };

                    _context.Portions.Add(portion);
                    food.IsSellable = true;
                    createdCount++;
                }
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return new DefaultPortionApplyResult
        {
            CreatedCount = createdCount,
            SkippedExistingCount = skippedExisting,
            SkippedSuspectCount = skippedSuspect,
            TabSummaries = preview.TabSummaries
        };
    }
}
