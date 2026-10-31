using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;
using MealShop.ViewModels;

namespace MealShop.Tests;

public class AdminFoodServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AdminFoodServiceTests()
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
    public async Task GenerateNextFoodId_WhenMaxIdLessThan100M_ShouldReturn100Million()
    {
        using var context = CreateContext();
        context.Foods.Add(new Food
        {
            Id = 853,
            NameVi = "Thuc pham cu",
            Kind = "ingredient",
            UsableAsComponent = true
        });
        await context.SaveChangesAsync();

        var service = new AdminFoodService(context);
        var nextId = await service.GenerateNextFoodIdAsync();

        Assert.Equal(100_000_000, nextId);
    }

    [Fact]
    public async Task GenerateNextFoodId_WhenAdminFoodExists_ShouldIncrement()
    {
        using var context = CreateContext();
        context.Foods.Add(new Food
        {
            Id = 100_000_005,
            NameVi = "Mon admin da tao",
            Kind = "dish",
            UsableAsComponent = true
        });
        await context.SaveChangesAsync();

        var service = new AdminFoodService(context);
        var nextId = await service.GenerateNextFoodIdAsync();

        Assert.Equal(100_000_006, nextId);
    }

    [Fact]
    public async Task CreateFood_ShouldSetCorrectNameNormAndId()
    {
        using var context = CreateContext();
        var service = new AdminFoodService(context);

        var model = new AdminFoodFormViewModel
        {
            Kind = "dish",
            NameVi = "Bún bò Huế Chay",
            CategoryVi = "Món nước",
            Kcal = 350m,
            IsSellable = true,
            UsableAsComponent = false
        };

        var created = await service.CreateFoodAsync(model);

        Assert.NotNull(created);
        Assert.True(created.Id >= 100_000_000);
        Assert.Equal("bun bo hue chay", created.NameNorm);
        Assert.False(created.UsableAsComponent);
    }

    [Fact]
    public async Task CreateFood_WhenIngredient_ShouldForceUsableAsComponentTrue()
    {
        using var context = CreateContext();
        var service = new AdminFoodService(context);

        var model = new AdminFoodFormViewModel
        {
            Kind = "ingredient",
            NameVi = "Rau cải thìa",
            UsableAsComponent = false // Admin form gui false
        };

        var created = await service.CreateFoodAsync(model);

        Assert.True(created.UsableAsComponent);
    }

    [Fact]
    public async Task DeleteFood_WhenUsedInRecipe_ShouldBeRejected()
    {
        using var context = CreateContext();
        var dish = new Food
        {
            Id = 1,
            Kind = "dish",
            NameVi = "Cơm chiên",
            UsableAsComponent = false
        };
        var ingredient = new Food
        {
            Id = 2,
            Kind = "ingredient",
            NameVi = "Gạo thơm",
            UsableAsComponent = true
        };
        context.Foods.AddRange(dish, ingredient);
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            DishId = 1,
            ComponentId = 2,
            Grams = 120m,
            DishKind = "dish",
            ComponentUsableAsComponent = true
        };
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        var service = new AdminFoodService(context);
        var (canDelete, reason) = await service.CanDeleteFoodAsync(2);

        Assert.False(canDelete);
        Assert.Contains("thành phần trong công thức khác", reason, StringComparison.OrdinalIgnoreCase);

        var (succeeded, error) = await service.DeleteFoodAsync(2);
        Assert.False(succeeded);
        Assert.NotEmpty(error);
    }
}
