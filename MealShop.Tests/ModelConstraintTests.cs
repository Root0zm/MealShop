using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;

namespace MealShop.Tests;

public class ModelConstraintTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ModelConstraintTests()
    {
        // Tao ket noi SQLite in-memory
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
    public async Task Recipe_WithValidData_ShouldSucceed()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 1,
            Kind = "dish",
            NameVi = "Salad uc ga",
            UsableAsComponent = false
        };

        var component = new Food
        {
            Id = 2,
            Kind = "ingredient",
            NameVi = "Uc ga",
            UsableAsComponent = true
        };

        context.Foods.AddRange(dish, component);
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            DishId = dish.Id,
            ComponentId = component.Id,
            Grams = 150m,
            Dish = dish,
            Component = component
        };

        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        var saved = await context.Recipes.FirstOrDefaultAsync(r => r.DishId == 1 && r.ComponentId == 2);
        Assert.NotNull(saved);
        Assert.Equal(150m, saved.Grams);
        Assert.Equal("dish", saved.DishKind);
    }

    [Fact]
    public async Task Recipe_SelfReference_ShouldBeRejected()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 10,
            Kind = "dish",
            NameVi = "Mon tu tham chieu",
            UsableAsComponent = true
        };

        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var invalidRecipe = new Recipe
        {
            DishId = dish.Id,
            ComponentId = dish.Id,
            Grams = 100m,
            Dish = dish,
            Component = dish
        };

        context.Recipes.Add(invalidRecipe);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Recipe_InvalidComponent_WhenUsableAsComponentFalse_ShouldBeRejected()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 20,
            Kind = "dish",
            NameVi = "Com ga",
            UsableAsComponent = false
        };

        var invalidComponent = new Food
        {
            Id = 21,
            Kind = "dish",
            NameVi = "Mon khong duoc lam thanh phan",
            UsableAsComponent = false
        };

        context.Foods.AddRange(dish, invalidComponent);
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            DishId = dish.Id,
            ComponentId = invalidComponent.Id,
            Grams = 50m,
            Dish = dish,
            Component = invalidComponent
        };

        context.Recipes.Add(recipe);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Recipe_WithZeroOrNegativeGrams_ShouldBeRejected()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 30,
            Kind = "dish",
            NameVi = "Mon test gram",
            UsableAsComponent = false
        };

        var component = new Food
        {
            Id = 31,
            Kind = "ingredient",
            NameVi = "Rau xanh",
            UsableAsComponent = true
        };

        context.Foods.AddRange(dish, component);
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            DishId = dish.Id,
            ComponentId = component.Id,
            Grams = 0m,
            Dish = dish,
            Component = component
        };

        context.Recipes.Add(recipe);

        await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Recipe_WhenDishIsIngredient_ShouldBeRejected()
    {
        using var context = CreateContext();

        // Mon duoc tao lai la ingredient chu khong phai dish
        var invalidDish = new Food
        {
            Id = 40,
            Kind = "ingredient",
            NameVi = "Thit heo",
            UsableAsComponent = true
        };

        var component = new Food
        {
            Id = 41,
            Kind = "ingredient",
            NameVi = "Hanh tay",
            UsableAsComponent = true
        };

        context.Foods.AddRange(invalidDish, component);
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            DishId = invalidDish.Id,
            ComponentId = component.Id,
            Grams = 100m,
            Dish = invalidDish,
            Component = component
        };

        context.Recipes.Add(recipe);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Food_IngredientWithUsableAsComponentFalse_ShouldBeRejected()
    {
        using var context = CreateContext();

        var invalidIngredient = new Food
        {
            Id = 50,
            Kind = "ingredient",
            NameVi = "Nguyen lieu bi khoa dung",
            UsableAsComponent = false
        };

        context.Foods.Add(invalidIngredient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Portion_DuplicateNameForSameFood_ShouldBeRejected()
    {
        using var context = CreateContext();

        var dish = new Food
        {
            Id = 60,
            Kind = "dish",
            NameVi = "Pho bo",
            UsableAsComponent = true
        };

        context.Foods.Add(dish);
        await context.SaveChangesAsync();

        var portion1 = new Portion
        {
            FoodId = 60,
            Name = "Bat nho",
            Grams = 200m
        };

        context.Portions.Add(portion1);
        await context.SaveChangesAsync();

        var portion2 = new Portion
        {
            FoodId = 60,
            Name = "Bat nho",
            Grams = 250m
        };

        context.Portions.Add(portion2);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
