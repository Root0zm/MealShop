using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

namespace MealShop.Tests;

public class AdminRecipeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AdminRecipeServiceTests()
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

    [Fact]
    public async Task AddComponent_SelfReference_ShouldFail()
    {
        using var context = CreateContext();
        var dish = new Food
        {
            Id = 1,
            Kind = "dish",
            NameVi = "Món ăn",
            UsableAsComponent = true
        };
        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        var (succeeded, message) = await service.AddComponentAsync(1, 1, 100m);

        Assert.False(succeeded);
        Assert.Contains("tự tham chiếu", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddComponent_NegativeOrZeroGrams_ShouldFail()
    {
        using var context = CreateContext();
        var dish = new Food { Id = 1, Kind = "dish", NameVi = "Món", UsableAsComponent = false };
        var ing = new Food { Id = 2, Kind = "ingredient", NameVi = "Gia vị", UsableAsComponent = true };
        context.Foods.AddRange(dish, ing);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        var (succeeded, message) = await service.AddComponentAsync(1, 2, 0m);

        Assert.False(succeeded);
        Assert.Contains("lớn hơn 0", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddComponent_ComponentNotUsable_ShouldFail()
    {
        using var context = CreateContext();
        var dish = new Food { Id = 1, Kind = "dish", NameVi = "Món A", UsableAsComponent = false };
        var dishB = new Food { Id = 2, Kind = "dish", NameVi = "Món B", UsableAsComponent = false };
        context.Foods.AddRange(dish, dishB);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        var (succeeded, message) = await service.AddComponentAsync(1, 2, 50m);

        Assert.False(succeeded);
        Assert.Contains("UsableAsComponent", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddPortion_ValidData_ShouldSucceed()
    {
        using var context = CreateContext();
        var dish = new Food { Id = 1, Kind = "dish", NameVi = "Cơm bò", UsableAsComponent = false };
        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        var (succeeded, message) = await service.AddPortionAsync(1, "Suất lớn", 350m, 55000m, true);

        Assert.True(succeeded);
        var portion = await context.Portions.FirstOrDefaultAsync(p => p.FoodId == 1);
        Assert.NotNull(portion);
        Assert.Equal("Suất lớn", portion.Name);
        Assert.Equal(350m, portion.Grams);
        Assert.Equal(55000m, portion.Price);
        Assert.True(portion.IsActive);
    }

    [Fact]
    public async Task AddPortion_NegativePrice_ShouldFail()
    {
        using var context = CreateContext();
        var dish = new Food { Id = 1, Kind = "dish", NameVi = "Cơm bò", UsableAsComponent = false };
        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        var (succeeded, message) = await service.AddPortionAsync(1, "Suất lỗi", 200m, -1000m, true);

        Assert.False(succeeded);
        Assert.Contains("không được âm", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddPortion_DuplicateNameInSameDish_ShouldFail()
    {
        using var context = CreateContext();
        var dish = new Food { Id = 1, Kind = "dish", NameVi = "Cơm bò", UsableAsComponent = false };
        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var service = new AdminRecipeService(context);
        await service.AddPortionAsync(1, "Tiêu chuẩn", 200m, 40000m, true);

        var (succeeded, message) = await service.AddPortionAsync(1, "Tiêu chuẩn", 250m, 45000m, true);
        Assert.False(succeeded);
        Assert.Contains("đã tồn tại", message, StringComparison.OrdinalIgnoreCase);
    }
}
