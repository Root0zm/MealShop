using MealShop.Models;
using MealShop.Services;

namespace MealShop.Tests;

public class NutritionCalculatorTests
{
    private readonly NutritionCalculatorService _calculator = new();

    [Fact]
    public void CalculateDishNutrition_EmptyRecipes_ShouldReturnNull()
    {
        var dish = new Food { Id = 1, NameVi = "Món test", Kcal = 100m };
        var result = _calculator.CalculateDishNutrition(dish, Array.Empty<Recipe>());

        Assert.Null(result);
    }

    [Fact]
    public void CalculateDishNutrition_XoiDoXanh_ShouldMatchEstimatedDraft()
    {
        // 15069 — Xoi do xanh (du lieu chuan tu dataset)
        var dish = new Food
        {
            Id = 15069,
            NameVi = "Xôi đỗ xanh",
            Kcal = 230.0m,
            Protein = 5.5m,
            Fat = 0.4m,
            Carb = 51.0m
        };

        // 1002 Gao nep (350 kcal, 8.4 protein, 1.6 fat, 75.4 carb)
        var gaoNep = new Food { Id = 1002, NameVi = "Gạo nếp", Kcal = 350.0m, Protein = 8.4m, Fat = 1.6m, Carb = 75.4m };
        // 3010 Dau xanh (346 kcal, 23.4 protein, 2.4 fat, 57.8 carb)
        var dauXanh = new Food { Id = 3010, NameVi = "Đậu xanh", Kcal = 346.0m, Protein = 23.4m, Fat = 2.4m, Carb = 57.8m };
        // 14057 Nuoc da (1 kcal, 0.02 carb, IsSuspect = true)
        var nuocDa = new Food { Id = 14057, NameVi = "Nước đá", Kcal = 1.0m, Carb = 0.02m, IsSuspect = true, EnergyFlag = "A" };
        // 13005 Muoi (1 kcal)
        var muoi = new Food { Id = 13005, NameVi = "Muối", Kcal = 1.0m };

        var recipes = new[]
        {
            new Recipe { DishId = 15069, ComponentId = 1002, Component = gaoNep, Grams = 100m },
            new Recipe { DishId = 15069, ComponentId = 3010, Component = dauXanh, Grams = 30m },
            new Recipe { DishId = 15069, ComponentId = 14057, Component = nuocDa, Grams = 70m },
            new Recipe { DishId = 15069, ComponentId = 13005, Component = muoi, Grams = 1m }
        };

        var result = _calculator.CalculateDishNutrition(dish, recipes);

        Assert.NotNull(result);
        Assert.Equal(201.0m, result.TotalGrams);

        // Kiem tra gia tri tinh toan tren 100g khop voi bang uoc luong trong docs/Recipes_draft.md
        Assert.Equal(226.1m, result.Kcal.Calculated);
        Assert.Equal(7.7m, result.Protein.Calculated);
        Assert.Equal(1.2m, result.Fat.Calculated);
        Assert.Equal(46.1m, result.Carb.Calculated);

        // Kiem tra ty le lech so voi tham chieu
        Assert.Equal(-1.7m, result.Kcal.DiffPercent); // ~ -2%
        Assert.Equal(40.0m, result.Protein.DiffPercent); // ~ +39-40%

        // Kiem tra co canh bao thanh phan IsSuspect (Nuoc da co IsSuspect = true)
        Assert.NotEmpty(result.SuspectComponentWarnings);
    }

    [Fact]
    public void CalculateDishNutrition_XoiLac_ShouldMatchEstimatedDraft()
    {
        // 15071 — Xoi lac
        var dish = new Food
        {
            Id = 15071,
            NameVi = "Xôi lạc",
            Kcal = 246.0m,
            Protein = 4.2m,
            Fat = 4.3m,
            Carb = 47.5m
        };

        // 1002 Gao nep
        var gaoNep = new Food { Id = 1002, NameVi = "Gạo nếp", Kcal = 350.0m, Protein = 8.4m, Fat = 1.6m, Carb = 75.4m };
        // 3017 Lac hat kho (583 kcal, 27.5 protein, 44.5 fat, 18.0 carb)
        var lac = new Food { Id = 3017, NameVi = "Lạc hạt", Kcal = 583.0m, Protein = 27.5m, Fat = 44.5m, Carb = 18.0m };
        var nuocDa = new Food { Id = 14057, NameVi = "Nước đá", Kcal = 1.0m };
        var muoi = new Food { Id = 13005, NameVi = "Muối", Kcal = 1.0m };

        var recipes = new[]
        {
            new Recipe { DishId = 15071, ComponentId = 1002, Component = gaoNep, Grams = 100m },
            new Recipe { DishId = 15071, ComponentId = 3017, Component = lac, Grams = 20m },
            new Recipe { DishId = 15071, ComponentId = 14057, Component = nuocDa, Grams = 70m },
            new Recipe { DishId = 15071, ComponentId = 13005, Component = muoi, Grams = 1m }
        };

        var result = _calculator.CalculateDishNutrition(dish, recipes);

        Assert.NotNull(result);
        Assert.Equal(191.0m, result.TotalGrams);
        Assert.Equal(244.7m, result.Kcal.Calculated);
        Assert.Equal(7.3m, result.Protein.Calculated);
        Assert.Equal(5.5m, result.Fat.Calculated);
        Assert.Equal(41.4m, result.Carb.Calculated);
    }
}
