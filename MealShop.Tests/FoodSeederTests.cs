using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;

namespace MealShop.Tests;

public class FoodSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FoodSeederTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    private static string FindCsvPath()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var candidateInSeed = Path.Combine(currentDir.FullName, "Data", "Seed", "foods_clean.csv");
            if (File.Exists(candidateInSeed))
            {
                return candidateInSeed;
            }

            var candidateInProcessed = Path.Combine(currentDir.FullName, "data", "processed", "foods_clean.csv");
            if (File.Exists(candidateInProcessed))
            {
                return candidateInProcessed;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException("Khong tim thay foods_clean.csv");
    }

    [Fact]
    public async Task FoodSeeder_ShouldLoad853FoodsCorrectly()
    {
        using var context = new AppDbContext(_options);
        var csvPath = FindCsvPath();

        await FoodSeeder.SeedAsync(context, csvPath);

        var totalFoods = await context.Foods.CountAsync();
        Assert.Equal(853, totalFoods);

        // Kiem tra mot mon an cu the co du lieu chinh xac
        var sampleFood = await context.Foods.FirstOrDefaultAsync(f => f.Id == 10001);
        Assert.NotNull(sampleFood);
        Assert.Equal("Sữa bò tươi", sampleFood.NameVi);
        Assert.Equal("ingredient", sampleFood.Kind);
        Assert.True(sampleFood.UsableAsComponent);
        Assert.Equal(74.0m, sampleFood.Kcal);
        Assert.Equal(86.2m, sampleFood.Water);
        Assert.Equal(3.9m, sampleFood.Protein);
        Assert.Equal(4.4m, sampleFood.Fat);
        Assert.Equal(4.8m, sampleFood.Carb);
    }

    [Fact]
    public async Task FoodSeeder_CalledTwice_ShouldNotDuplicateRecords()
    {
        using var context = new AppDbContext(_options);
        var csvPath = FindCsvPath();

        // Lan goi dau tien
        await FoodSeeder.SeedAsync(context, csvPath);
        Assert.Equal(853, await context.Foods.CountAsync());

        // Lan goi thu hai khong duoc duplicate
        await FoodSeeder.SeedAsync(context, csvPath);
        Assert.Equal(853, await context.Foods.CountAsync());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
