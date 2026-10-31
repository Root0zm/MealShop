using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

namespace MealShop.Tests;

public class MenuServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MenuServiceTests()
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

    public void Dispose()
    {
        _connection.Dispose();
    }

    private IOptions<MenuOptions> CreateSampleMenuOptions()
    {
        var options = new MenuOptions
        {
            Rules = new List<MenuRule>
            {
                new()
                {
                    CategoryVi = "Thức ăn truyền thống",
                    Tab = "Món chính",
                    PortionName = "1 suất",
                    Grams = 350m,
                    Price = 35000m
                },
                new()
                {
                    CategoryVi = "Nước giải khát",
                    Tab = "Đồ uống",
                    PortionName = "1 ly",
                    Grams = 250m,
                    Price = 15000m
                },
                new()
                {
                    CategoryVi = "Rau, quả, củ dùng làm rau",
                    Tab = "Rau củ",
                    PortionName = "1 phần",
                    Grams = 50m,
                    Price = 5000m
                }
            }
        };

        return Options.Create(options);
    }

    [Fact]
    public void CalculatePortionNutrition_CompleteData_ShouldComputeAndSumTo100Percent()
    {
        var food = new Food
        {
            Id = 1,
            NameVi = "Bún chả",
            Kcal = 136m,
            Protein = 6.0m,
            Fat = 4.2m,
            Carb = 18.0m
        };

        var (hasComplete, kcal, protein, fat, carb, sp, sf, sc) =
            MenuService.CalculatePortionNutrition(food, 350m);

        Assert.True(hasComplete);
        Assert.NotNull(kcal);
        Assert.Equal(476m, kcal.Value);
        Assert.Equal(21.0m, protein!.Value);
        Assert.Equal(14.7m, fat!.Value);
        Assert.Equal(63.0m, carb!.Value);

        // Tong ti le % nang luong phai luon bang chinh xac 100%
        Assert.Equal(100, sp + sf + sc);
    }

    [Fact]
    public void CalculatePortionNutrition_MissingData_ShouldReturnHasCompleteFalse()
    {
        var food = new Food
        {
            Id = 2,
            NameVi = "Mon thieu so lieu",
            Kcal = 200m,
            Protein = 10m,
            Fat = null, // Thieu fat
            Carb = 20m
        };

        var (hasComplete, kcal, protein, fat, carb, sp, sf, sc) =
            MenuService.CalculatePortionNutrition(food, 250m);

        Assert.False(hasComplete);
        Assert.Null(kcal);
        Assert.Null(fat);
    }

    [Fact]
    public async Task GetMenuAsync_GoalFilters_ShouldFilterCorrectly()
    {
        using var context = CreateContext();

        // Mon A: 300 kcal (<= 400), Protein 10g (< 20), Fat 2g (<= 5) -> Under400 & LowFat
        var foodA = new Food
        {
            Id = 10,
            NameVi = "Món thanh đạm",
            CategoryVi = "Thức ăn truyền thống",
            Kind = "dish",
            IsSellable = true,
            Kcal = 85.7m,
            Protein = 2.85m,
            Fat = 0.57m,
            Carb = 15m,
            Portions = new List<Portion> { new() { Id = 101, Name = "1 suất", Grams = 350m, Price = 35000m, IsActive = true } }
        };

        // Mon B: 550 kcal (> 400), Protein 25g (>= 20), Fat 8g (> 5) -> HighProtein
        var foodB = new Food
        {
            Id = 11,
            NameVi = "Món nhiều thịt",
            CategoryVi = "Thức ăn truyền thống",
            Kind = "dish",
            IsSellable = true,
            Kcal = 157.1m,
            Protein = 7.14m,
            Fat = 2.28m,
            Carb = 10m,
            Portions = new List<Portion> { new() { Id = 102, Name = "1 suất", Grams = 350m, Price = 35000m, IsActive = true } }
        };

        context.Foods.AddRange(foodA, foodB);
        await context.SaveChangesAsync();

        var service = new MenuService(context, CreateSampleMenuOptions());

        var menuAll = await service.GetMenuAsync("Món chính", null, null);
        Assert.Equal(2, menuAll.MainDishes.Count);

        var menuUnder400 = await service.GetMenuAsync("Món chính", "under-400", null);
        Assert.Single(menuUnder400.MainDishes);
        Assert.Equal("Món thanh đạm", menuUnder400.MainDishes[0].Name);

        var menuHighProtein = await service.GetMenuAsync("Món chính", "high-protein", null);
        Assert.Single(menuHighProtein.MainDishes);
        Assert.Equal("Món nhiều thịt", menuHighProtein.MainDishes[0].Name);

        var menuLowFat = await service.GetMenuAsync("Món chính", "low-fat", null);
        Assert.Single(menuLowFat.MainDishes);
        Assert.Equal("Món thanh đạm", menuLowFat.MainDishes[0].Name);
    }

    [Fact]
    public async Task GetMenuAsync_FoodWithoutActivePortion_ShouldNotAppear()
    {
        using var context = CreateContext();

        // Mon co Portion IsActive = false
        var foodInactive = new Food
        {
            Id = 30,
            NameVi = "Món tạm ngưng bán",
            CategoryVi = "Thức ăn truyền thống",
            Kind = "dish",
            IsSellable = true,
            Kcal = 100m,
            Protein = 5m,
            Fat = 2m,
            Carb = 15m,
            Portions = new List<Portion> { new() { Id = 301, Name = "1 suất", Grams = 350m, Price = 35000m, IsActive = false } }
        };

        // Mon co Portion Price = 0
        var foodZeroPrice = new Food
        {
            Id = 31,
            NameVi = "Món chưa đặt giá",
            CategoryVi = "Thức ăn truyền thống",
            Kind = "dish",
            IsSellable = true,
            Kcal = 100m,
            Protein = 5m,
            Fat = 2m,
            Carb = 15m,
            Portions = new List<Portion> { new() { Id = 302, Name = "1 suất", Grams = 350m, Price = 0m, IsActive = true } }
        };

        context.Foods.AddRange(foodInactive, foodZeroPrice);
        await context.SaveChangesAsync();

        var service = new MenuService(context, CreateSampleMenuOptions());
        var menu = await service.GetMenuAsync("Món chính", null, null);

        Assert.Empty(menu.MainDishes);
    }

    [Fact]
    public async Task GetMenuAsync_TabOrder_ShouldFollowConfiguredRulesOrder()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 40,
            NameVi = "Món chính 1",
            CategoryVi = "Thức ăn truyền thống",
            Kind = "dish",
            IsSellable = true,
            Kcal = 100m,
            Protein = 5m,
            Fat = 2m,
            Carb = 15m,
            Portions = new List<Portion> { new() { Id = 401, Name = "1 suất", Grams = 350m, Price = 35000m, IsActive = true } }
        };

        var drink = new Food
        {
            Id = 41,
            NameVi = "Trà đá",
            CategoryVi = "Nước giải khát",
            Kind = "ingredient",
            UsableAsComponent = true,
            IsSellable = true,
            Kcal = 1m,
            Protein = 0m,
            Fat = 0m,
            Carb = 0m,
            Portions = new List<Portion> { new() { Id = 402, Name = "1 ly", Grams = 250m, Price = 15000m, IsActive = true } }
        };

        var veggie = new Food
        {
            Id = 42,
            NameVi = "Rau muống",
            CategoryVi = "Rau, quả, củ dùng làm rau",
            Kind = "ingredient",
            UsableAsComponent = true,
            IsSellable = true,
            Kcal = 20m,
            Protein = 2m,
            Fat = 0m,
            Carb = 3m,
            Portions = new List<Portion> { new() { Id = 403, Name = "1 phần", Grams = 50m, Price = 5000m, IsActive = true } }
        };

        context.Foods.AddRange(dish, drink, veggie);
        await context.SaveChangesAsync();

        var service = new MenuService(context, CreateSampleMenuOptions());
        var menu = await service.GetMenuAsync(null, null, null);

        // Thu tu tab phai theo dung Rules trong MenuOptions: "Món chính" -> "Đồ uống" -> "Rau củ"
        Assert.Equal(3, menu.Tabs.Count);
        Assert.Equal("Món chính", menu.Tabs[0]);
        Assert.Equal("Đồ uống", menu.Tabs[1]);
        Assert.Equal("Rau củ", menu.Tabs[2]);
    }
}
