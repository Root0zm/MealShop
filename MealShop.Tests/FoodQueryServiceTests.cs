using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

namespace MealShop.Tests;

public class FoodQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FoodQueryServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    private AppDbContext CreateContext() => new(_options);

    [Fact]
    public async Task SearchFoodsAsync_WithUnaccentedQuery_ShouldFindAccentedFoods()
    {
        using var context = CreateContext();

        var foods = new[]
        {
            new Food { Id = 101, NameVi = "Phở bò", NameNorm = "pho bo", Kind = "dish", UsableAsComponent = true },
            new Food { Id = 102, NameVi = "Đậu phụ rán", NameNorm = "dau phu ran", Kind = "dish", UsableAsComponent = true },
            new Food { Id = 103, NameVi = "Đậu đen khô", NameNorm = "dau den kho", Kind = "ingredient", UsableAsComponent = true },
            new Food { Id = 104, NameVi = "Cơm trắng", NameNorm = "com trang", Kind = "dish", UsableAsComponent = true }
        };

        context.Foods.AddRange(foods);
        await context.SaveChangesAsync();

        var service = new FoodQueryService(context);

        // Tim kiem khong dau "pho"
        var phoResult = await service.SearchFoodsAsync(q: "pho", kind: null, category: null, page: 1, pageSize: 10);
        Assert.Single(phoResult.Items);
        Assert.Equal("Phở bò", phoResult.Items[0].NameVi);

        // Tim kiem khong dau "dau"
        var dauResult = await service.SearchFoodsAsync(q: "dau", kind: null, category: null, page: 1, pageSize: 10);
        Assert.Equal(2, dauResult.Items.Count);
        Assert.Contains(dauResult.Items, f => f.NameVi == "Đậu phụ rán");
        Assert.Contains(dauResult.Items, f => f.NameVi == "Đậu đen khô");
    }

    [Fact]
    public async Task SearchFoodsAsync_FilterByKind_ShouldOnlyReturnMatchingKind()
    {
        using var context = CreateContext();

        var foods = new[]
        {
            new Food { Id = 201, NameVi = "Gà rán", NameNorm = "ga ran", Kind = "dish", UsableAsComponent = true },
            new Food { Id = 202, NameVi = "Thịt gà sống", NameNorm = "thit ga song", Kind = "ingredient", UsableAsComponent = true },
            new Food { Id = 203, NameVi = "Trứng luộc", NameNorm = "trung luoc", Kind = "dish", UsableAsComponent = true }
        };

        context.Foods.AddRange(foods);
        await context.SaveChangesAsync();

        var service = new FoodQueryService(context);

        var dishResult = await service.SearchFoodsAsync(q: null, kind: "dish", category: null, page: 1, pageSize: 10);
        Assert.Equal(2, dishResult.Items.Count);
        Assert.All(dishResult.Items, f => Assert.Equal("dish", f.Kind));

        var ingredientResult = await service.SearchFoodsAsync(q: null, kind: "ingredient", category: null, page: 1, pageSize: 10);
        Assert.Single(ingredientResult.Items);
        Assert.Equal("Thịt gà sống", ingredientResult.Items[0].NameVi);
    }

    [Fact]
    public async Task SearchFoodsAsync_Pagination_ShouldReturnCorrectPageSizeAndRemainder()
    {
        using var context = CreateContext();

        // Tao 45 thuc pham de test 3 trang (20 + 20 + 5)
        var foods = new List<Food>();
        for (int i = 1; i <= 45; i++)
        {
            foods.Add(new Food
            {
                Id = 1000 + i,
                NameVi = $"Món ăn số {i:D2}",
                NameNorm = $"mon an so {i:D2}",
                Kind = "dish",
                UsableAsComponent = true
            });
        }

        context.Foods.AddRange(foods);
        await context.SaveChangesAsync();

        var service = new FoodQueryService(context);

        // Trang 1 du 20
        var page1 = await service.SearchFoodsAsync(q: null, kind: null, category: null, page: 1, pageSize: 20);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(45, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);

        // Trang 2 du 20
        var page2 = await service.SearchFoodsAsync(q: null, kind: null, category: null, page: 2, pageSize: 20);
        Assert.Equal(20, page2.Items.Count);
        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);

        // Trang 3 (trang cuoi) dung so du 5
        var page3 = await service.SearchFoodsAsync(q: null, kind: null, category: null, page: 3, pageSize: 20);
        Assert.Equal(5, page3.Items.Count);
        Assert.True(page3.HasPreviousPage);
        Assert.False(page3.HasNextPage);
    }

    [Fact]
    public async Task GetFoodDetailsAsync_WhenFound_ShouldReturnDetailsAndEmptyRecipes()
    {
        using var context = CreateContext();

        var food = new Food
        {
            Id = 501,
            NameVi = "Cháo yến mạch",
            NameNorm = "chao yen mach",
            Kind = "dish",
            UsableAsComponent = true,
            Kcal = 350m,
            Protein = 12m,
            Fat = 6m,
            Carb = 60m,
            IsSuspect = true,
            QaNote = "Can kiem tra lai luong fat"
        };

        context.Foods.Add(food);
        await context.SaveChangesAsync();

        var service = new FoodQueryService(context);
        var details = await service.GetFoodDetailsAsync(501);

        Assert.NotNull(details);
        Assert.Equal("Cháo yến mạch", details.NameVi);
        Assert.True(details.IsSuspect);
        Assert.Equal("Can kiem tra lai luong fat", details.QaNote);
        Assert.Empty(details.Components);
    }

    [Fact]
    public async Task GetFoodDetailsAsync_WhenNotFound_ShouldReturnNull()
    {
        using var context = CreateContext();
        var service = new FoodQueryService(context);

        var details = await service.GetFoodDetailsAsync(99999);
        Assert.Null(details);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
