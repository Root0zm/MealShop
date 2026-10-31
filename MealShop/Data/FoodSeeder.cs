using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using MealShop.Models;

namespace MealShop.Data;

public static class FoodSeeder
{
    public static async Task SeedAsync(AppDbContext context, string? csvFilePath = null)
    {
        // Khong nap lap neu bang Foods da co du lieu
        if (await context.Foods.AnyAsync())
        {
            return;
        }

        var path = csvFilePath ?? Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "foods_clean.csv");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Khong tim thay file du lieu: {path}");
        }

        var foods = new List<Food>();

        using (var parser = new TextFieldParser(path))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = true;

            // Doc dong tieu de
            if (!parser.EndOfData)
            {
                parser.ReadFields();
            }

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields == null || fields.Length < 26)
                {
                    continue;
                }

                var food = new Food
                {
                    Id = int.Parse(fields[0], CultureInfo.InvariantCulture),
                    Kind = fields[1],
                    UsableAsComponent = ParseBool(fields[2]),
                    NameVi = fields[3],
                    NameEn = NullIfEmpty(fields[4]),
                    NameNorm = NullIfEmpty(fields[5]),
                    CategoryVi = NullIfEmpty(fields[6]),
                    Kcal = ParseDecimal(fields[7]),
                    Water = ParseDecimal(fields[8]),
                    Protein = ParseDecimal(fields[9]),
                    Fat = ParseDecimal(fields[10]),
                    Carb = ParseDecimal(fields[11]),
                    Fiber = ParseDecimal(fields[12]),
                    Cholesterol = ParseDecimal(fields[13]),
                    Calcium = ParseDecimal(fields[14]),
                    Phosphorus = ParseDecimal(fields[15]),
                    Iron = ParseDecimal(fields[16]),
                    Sodium = ParseDecimal(fields[17]),
                    Potassium = ParseDecimal(fields[18]),
                    Zinc = ParseDecimal(fields[19]),
                    VitaminA = ParseDecimal(fields[20]),
                    BetaCarotene = ParseDecimal(fields[21]),
                    VitaminC = ParseDecimal(fields[22]),
                    EnergyFlag = NullIfEmpty(fields[23]),
                    QaNote = NullIfEmpty(fields[24]),
                    IsSuspect = ParseBool(fields[25]),
                    IsSellable = string.Equals(fields[1], "dish", StringComparison.OrdinalIgnoreCase)
                };

                foods.Add(food);
            }
        }

        await context.Foods.AddRangeAsync(foods);
        await context.SaveChangesAsync();
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
