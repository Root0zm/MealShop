using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

namespace MealShop.Tests;

public class DefaultPortionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public DefaultPortionServiceTests()
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
                    Price = 15000m,
                    ExcludeNameContainsAny = new List<string> { "ruou", "bia" }
                },
                new()
                {
                    CategoryVi = "Hạt, quả giàu đạm, béo và sản phẩm chế biến",
                    Tab = "Đậu và hạt",
                    PortionName = "1 phần",
                    Grams = 40m,
                    Price = 5000m,
                    NameContainsAny = new List<string> { "dau phu" }
                }
            }
        };

        return Options.Create(options);
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreatePortionsAndSetIsSellable()
    {
        using var context = CreateContext();
        context.Foods.AddRange(
            new Food
            {
                Id = 1,
                CategoryVi = "Thức ăn truyền thống",
                NameVi = "Bún chả",
                NameNorm = "bun cha",
                Kind = "dish",
                IsSuspect = false
            },
            new Food
            {
                Id = 2,
                CategoryVi = "Nước giải khát",
                NameVi = "Nước chanh",
                NameNorm = "nuoc chanh",
                Kind = "ingredient",
                UsableAsComponent = true,
                IsSuspect = false
            }
        );
        await context.SaveChangesAsync();

        var service = new DefaultPortionService(context, CreateSampleMenuOptions());
        var result = await service.ApplyAsync();

        Assert.Equal(2, result.CreatedCount);

        var food1 = await context.Foods.Include(f => f.Portions).FirstAsync(f => f.Id == 1);
        Assert.True(food1.IsSellable);
        Assert.Single(food1.Portions);
        Assert.Equal("1 suất", food1.Portions.First().Name);
        Assert.Equal(350m, food1.Portions.First().Grams);
        Assert.Equal(35000m, food1.Portions.First().Price);
        Assert.True(food1.Portions.First().IsActive);
    }

    [Fact]
    public async Task ApplyAsync_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        using var context = CreateContext();
        context.Foods.Add(new Food
        {
            Id = 10,
            CategoryVi = "Thức ăn truyền thống",
            NameVi = "Phở bò",
            NameNorm = "pho bo",
            Kind = "dish",
            IsSuspect = false
        });
        await context.SaveChangesAsync();

        var service = new DefaultPortionService(context, CreateSampleMenuOptions());

        var firstRun = await service.ApplyAsync();
        Assert.Equal(1, firstRun.CreatedCount);

        var secondRun = await service.ApplyAsync();
        Assert.Equal(0, secondRun.CreatedCount);
        Assert.Equal(1, secondRun.SkippedExistingCount);

        var portions = await context.Portions.Where(p => p.FoodId == 10).ToListAsync();
        Assert.Single(portions);
    }

    [Fact]
    public async Task ApplyAsync_SuspectFoods_ShouldBeSkipped()
    {
        using var context = CreateContext();
        context.Foods.AddRange(
            new Food
            {
                Id = 20,
                CategoryVi = "Thức ăn truyền thống",
                NameVi = "Món nghi vấn",
                NameNorm = "mon nghi van",
                Kind = "dish",
                IsSuspect = true,
                EnergyFlag = "C"
            },
            new Food
            {
                Id = 21,
                CategoryVi = "Thức ăn truyền thống",
                NameVi = "Món chuẩn",
                NameNorm = "mon chuan",
                Kind = "dish",
                IsSuspect = false
            }
        );
        await context.SaveChangesAsync();

        var service = new DefaultPortionService(context, CreateSampleMenuOptions());
        var result = await service.ApplyAsync();

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedSuspectCount);

        var food20 = await context.Foods.Include(f => f.Portions).FirstAsync(f => f.Id == 20);
        Assert.Empty(food20.Portions);
        Assert.False(food20.IsSellable);
    }

    [Fact]
    public void FoodMatchesRule_Alcohol_ShouldBeExcluded()
    {
        var rule = new MenuRule
        {
            CategoryVi = "Nước giải khát",
            Tab = "Đồ uống",
            PortionName = "1 ly",
            ExcludeNameContainsAny = new List<string> { "ruou", "bia" }
        };

        var beer = new Food { CategoryVi = "Nước giải khát", NameVi = "Bia hơi", NameNorm = "bia hoi" };
        var wine = new Food { CategoryVi = "Nước giải khát", NameVi = "Rượu vang", NameNorm = "ruou vang" };
        var juice = new Food { CategoryVi = "Nước giải khát", NameVi = "Nước cam", NameNorm = "nuoc cam" };

        Assert.False(DefaultPortionService.FoodMatchesRule(beer, rule));
        Assert.False(DefaultPortionService.FoodMatchesRule(wine, rule));
        Assert.True(DefaultPortionService.FoodMatchesRule(juice, rule));
    }

    [Fact]
    public void FoodMatchesRule_DauPhu_ShouldOnlyMatchTofu()
    {
        var rule = new MenuRule
        {
            CategoryVi = "Hạt, quả giàu đạm, béo và sản phẩm chế biến",
            Tab = "Đậu và hạt",
            PortionName = "1 phần",
            NameContainsAny = new List<string> { "dau phu" }
        };

        var tofu = new Food
        {
            CategoryVi = "Hạt, quả giàu đạm, béo và sản phẩm chế biến",
            NameVi = "Đậu phụ",
            NameNorm = "dau phu"
        };
        var peanut = new Food
        {
            CategoryVi = "Hạt, quả giàu đạm, béo và sản phẩm chế biến",
            NameVi = "Lạc nhân",
            NameNorm = "lac nhan"
        };

        Assert.True(DefaultPortionService.FoodMatchesRule(tofu, rule));
        Assert.False(DefaultPortionService.FoodMatchesRule(peanut, rule));
    }

    [Fact]
    public async Task ChangeConfigurationPrice_ShouldNotAffectExistingPortions()
    {
        using var context = CreateContext();
        context.Foods.Add(new Food
        {
            Id = 30,
            CategoryVi = "Thức ăn truyền thống",
            NameVi = "Bún riêu",
            NameNorm = "bun rieu",
            Kind = "dish",
            IsSuspect = false
        });
        await context.SaveChangesAsync();

        var initialOptions = CreateSampleMenuOptions();
        var service1 = new DefaultPortionService(context, initialOptions);
        await service1.ApplyAsync();

        var createdPortion = await context.Portions.FirstAsync(p => p.FoodId == 30);
        Assert.Equal(35000m, createdPortion.Price);

        // Gia su Admin hoac appsettings doi gia quy tac thanh 40000
        initialOptions.Value.Rules[0].Price = 40000m;

        var service2 = new DefaultPortionService(context, initialOptions);
        await service2.ApplyAsync(); // Chay lai

        // Gia ban cua khau phan da tao khong duoc bi thay doi
        var refreshedPortion = await context.Portions.FirstAsync(p => p.FoodId == 30);
        Assert.Equal(35000m, refreshedPortion.Price);
    }
}
