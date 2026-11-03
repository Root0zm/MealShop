using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;
using Xunit;

namespace MealShop.Tests;

public class MockSession : ISession
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public bool IsAvailable => true;
    public string Id => "test-session-id";
    public IEnumerable<string> Keys => _storage.Keys;

    public void Clear() => _storage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _storage.Remove(key);

    public void Set(string key, byte[] value) => _storage[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value!);
}

public class CartServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public CartServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private (CartService service, AppDbContext context) CreateService()
    {
        var context = new AppDbContext(_options);
        var session = new MockSession();
        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var service = new CartService(accessor, context);
        return (service, context);
    }

    [Fact]
    public async Task AddToCart_ShouldClampQuantityBetween1And20()
    {
        var (service, context) = CreateService();

        var food = new Food
        {
            Id = 1,
            NameVi = "Phở bò",
            NameEn = "Beef Pho",
            Kind = "dish",
            Kcal = 350,
            Protein = 20,
            Fat = 10,
            Carb = 40,
            IsSellable = true
        };
        var portion = new Portion
        {
            Id = 10,
            FoodId = 1,
            Food = food,
            Name = "Bát tiêu chuẩn",
            Grams = 400,
            Price = 45000,
            IsActive = true
        };
        context.Foods.Add(food);
        context.Portions.Add(portion);
        await context.SaveChangesAsync();

        // Them 15 mon
        await service.AddToCartAsync(10, 15);
        var cart = await service.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(15, cart.Items[0].Quantity);

        // Them tiep 10 mon -> vuot 20, clamp ve 20
        await service.AddToCartAsync(10, 10);
        cart = await service.GetCartAsync();
        Assert.Equal(20, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task GetCart_ShouldCalculateNutritionAndAmountCorrectly()
    {
        var (service, context) = CreateService();

        var food = new Food
        {
            Id = 1,
            NameVi = "Cơm trắng",
            NameEn = "Steamed rice",
            Kind = "dish",
            Kcal = 130, // tren 100g
            Protein = 2.7m,
            Fat = 0.3m,
            Carb = 28.2m,
            IsSellable = true
        };
        var portion = new Portion
        {
            Id = 101,
            FoodId = 1,
            Food = food,
            Name = "Bát con",
            Grams = 150, // 1.5 x 100g
            Price = 10000,
            IsActive = true
        };
        context.Foods.Add(food);
        context.Portions.Add(portion);
        await context.SaveChangesAsync();

        // 2 bat com trang (tong 300g -> x3 gia tri tren 100g)
        await service.AddToCartAsync(101, 2);
        var cart = await service.GetCartAsync();

        Assert.Equal(20000m, cart.TotalAmount);
        Assert.Equal(2, cart.TotalItemsCount);
        // 130 * 3 = 390 kcal
        Assert.Equal(390m, cart.TotalKcal);
        // Tong 3 ti le nang luong phai la 100%
        Assert.Equal(100, cart.ShareProtein + cart.ShareFat + cart.ShareCarb);
    }

    [Fact]
    public async Task GetCart_ShouldRemoveInactiveOrUnsellableItems()
    {
        var (service, context) = CreateService();

        var food = new Food
        {
            Id = 2,
            NameVi = "Món tạm ngưng",
            NameEn = "Inactive Food",
            Kind = "dish",
            Kcal = 200,
            IsSellable = true
        };
        var portion = new Portion
        {
            Id = 202,
            FoodId = 2,
            Food = food,
            Name = "Khẩu phần",
            Grams = 200,
            Price = 30000,
            IsActive = true
        };
        context.Foods.Add(food);
        context.Portions.Add(portion);
        await context.SaveChangesAsync();

        await service.AddToCartAsync(202, 1);
        food.IsSellable = false;
        await context.SaveChangesAsync();
        var cart = await service.GetCartAsync();

        // Phai tu dong loai bo khoi gio hang va sinh thong bao warning
        Assert.Empty(cart.Items);
        Assert.NotEmpty(cart.Warnings);
    }
}
