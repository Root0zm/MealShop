using MealShop.Models;
using MealShop.ViewModels;
using MealShop.Extensions;

namespace MealShop.Services;

public class NutritionCalculatorService
{
    public CalculatedNutritionViewModel? CalculateDishNutrition(Food dish, IReadOnlyCollection<Recipe> recipes)
    {
        if (recipes == null || recipes.Count == 0)
        {
            return null;
        }

        var totalGrams = recipes.Sum(r => r.Grams);
        if (totalGrams <= 0)
        {
            return null;
        }

        // Tinh tong gia tri cua tung chi so dinh duong
        decimal sumKcal = 0m, sumProtein = 0m, sumFat = 0m, sumCarb = 0m;
        decimal sumWater = 0m, sumFiber = 0m, sumSodium = 0m;
        decimal sumCalcium = 0m, sumIron = 0m, sumVitaminC = 0m;

        var suspectWarnings = new List<string>();

        foreach (var r in recipes)
        {
            var component = r.Component;
            var grams = r.Grams;
            var ratio = grams / 100m;

            sumKcal += (component.Kcal ?? 0m) * ratio;
            sumProtein += (component.Protein ?? 0m) * ratio;
            sumFat += (component.Fat ?? 0m) * ratio;
            sumCarb += (component.Carb ?? 0m) * ratio;
            sumWater += (component.Water ?? 0m) * ratio;
            sumFiber += (component.Fiber ?? 0m) * ratio;
            sumSodium += (component.Sodium ?? 0m) * ratio;
            sumCalcium += (component.Calcium ?? 0m) * ratio;
            sumIron += (component.Iron ?? 0m) * ratio;
            sumVitaminC += (component.VitaminC ?? 0m) * ratio;

            // Kiem tra neu thanh phan co co IsSuspect
            if (component.IsSuspect)
            {
                suspectWarnings.Add($"Thành phần '{component.NameVi}': {DisplayText.QualityNote(component.QaNote)}.");
            }
        }

        decimal CalcPer100g(decimal sum, int decimals = 1)
        {
            return Math.Round((sum / totalGrams) * 100m, decimals);
        }

        return new CalculatedNutritionViewModel
        {
            TotalGrams = Math.Round(totalGrams, 1),

            Kcal = new NutrientComparison
            {
                Calculated = CalcPer100g(sumKcal, 1),
                Reference = dish.Kcal
            },
            Protein = new NutrientComparison
            {
                Calculated = CalcPer100g(sumProtein, 1),
                Reference = dish.Protein
            },
            Fat = new NutrientComparison
            {
                Calculated = CalcPer100g(sumFat, 1),
                Reference = dish.Fat
            },
            Carb = new NutrientComparison
            {
                Calculated = CalcPer100g(sumCarb, 1),
                Reference = dish.Carb
            },
            Water = new NutrientComparison
            {
                Calculated = CalcPer100g(sumWater, 1),
                Reference = dish.Water
            },
            Fiber = new NutrientComparison
            {
                Calculated = CalcPer100g(sumFiber, 1),
                Reference = dish.Fiber
            },
            Sodium = new NutrientComparison
            {
                Calculated = CalcPer100g(sumSodium, 1),
                Reference = dish.Sodium
            },
            Calcium = new NutrientComparison
            {
                Calculated = CalcPer100g(sumCalcium, 1),
                Reference = dish.Calcium
            },
            Iron = new NutrientComparison
            {
                Calculated = CalcPer100g(sumIron, 2),
                Reference = dish.Iron
            },
            VitaminC = new NutrientComparison
            {
                Calculated = CalcPer100g(sumVitaminC, 1),
                Reference = dish.VitaminC
            },

            SuspectComponentWarnings = suspectWarnings
        };
    }
}
