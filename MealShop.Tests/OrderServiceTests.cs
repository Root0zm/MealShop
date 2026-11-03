using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;
using MealShop.ViewModels;
using Xunit;

namespace MealShop.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public OrderServiceTests()
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

    private (OrderService orderService, CartService cartService, AppDbContext context) CreateServices()
    {
        var context = new AppDbContext(_options);
        var session = new MockSession();
        var httpContext = new DefaultHttpContext { Session = session };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var cartService = new CartService(accessor, context);
        var orderService = new OrderService(context, cartService, NullLogger<OrderService>.Instance);

        return (orderService, cartService, context);
    }

    private async Task<Portion> SeedSampleFoodAndPortion(AppDbContext context, int foodId = 1, int portionId = 10, decimal price = 35000, bool isActive = true, bool isSellable = true)
    {
        var food = new Food
        {
            Id = foodId,
            NameVi = $"Món ăn #{foodId}",
            NameEn = $"Food #{foodId}",
            Kind = "dish",
            Kcal = 300,
            Protein = 15,
            Fat = 8,
            Carb = 40,
            IsSellable = isSellable
        };

        var portion = new Portion
        {
            Id = portionId,
            FoodId = foodId,
            Food = food,
            Name = "1 suất",
            Grams = 350,
            Price = price,
            IsActive = isActive
        };

        context.Foods.Add(food);
        context.Portions.Add(portion);
        await context.SaveChangesAsync();

        return portion;
    }

    [Fact]
    public void NormalizePhoneNumber_ShouldCleanVariantsCorrectly()
    {
        Assert.Equal("0912345678", OrderService.NormalizePhoneNumber("0912 345 678"));
        Assert.Equal("0912345678", OrderService.NormalizePhoneNumber("0912.345.678"));
        Assert.Equal("0912345678", OrderService.NormalizePhoneNumber("0912-345-678"));
        Assert.Equal("0912345678", OrderService.NormalizePhoneNumber("+84912345678"));
        Assert.Equal("0912345678", OrderService.NormalizePhoneNumber("84912345678"));
        Assert.Equal("", OrderService.NormalizePhoneNumber(null));
        Assert.Equal("", OrderService.NormalizePhoneNumber("   "));
    }

    private static CheckoutViewModel CheckoutInfo() => new()
    {
        CustomerName = "Khách kiểm thử", PhoneNumber = "0912345678", ShippingAddress = "Địa chỉ kiểm thử"
    };

    [Fact]
    public async Task Confirmation_RequiresMatchingSessionOrOwner()
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var portion = await SeedSampleFoodAndPortion(context);
        await cart.AddToCartAsync(portion.Id);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);

        Assert.Null(await service.GetConfirmationAsync(order.PublicCode, null, null));
        Assert.Null(await service.GetConfirmationAsync(order.PublicCode, "other-user", order.Id + 1));
        Assert.NotNull(await service.GetConfirmationAsync(order.PublicCode, null, order.Id));

        context.Users.Add(new ApplicationUser { Id = "owner", UserName = "owner", FullName = "Chủ đơn" });
        order.UserId = "owner";
        await context.SaveChangesAsync();
        Assert.NotNull(await service.GetConfirmationAsync(order.PublicCode, "owner", null));
        Assert.Null(await service.GetConfirmationAsync(order.PublicCode, "other-user", null));
    }

    [Fact]
    public async Task Status_CannotSkipStepsReverseOrReopenFinishedOrder()
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var portion = await SeedSampleFoodAndPortion(context);
        await cart.AddToCartAsync(portion.Id);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);

        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Completed"));
        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Unknown"));
        Assert.True(await service.UpdateOrderStatusAsync(order.Id, "Confirmed"));
        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Pending"));
        Assert.True(await service.UpdateOrderStatusAsync(order.Id, "Preparing"));
        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Cancelled"));
        Assert.True(await service.UpdateOrderStatusAsync(order.Id, "Completed"));
        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Confirmed"));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    public async Task Status_CanCancelBeforePreparation(string status)
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var portion = await SeedSampleFoodAndPortion(context);
        await cart.AddToCartAsync(portion.Id);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);
        if (status == "Confirmed") Assert.True(await service.UpdateOrderStatusAsync(order.Id, status));
        Assert.True(await service.UpdateOrderStatusAsync(order.Id, "Cancelled"));
        Assert.False(await service.UpdateOrderStatusAsync(order.Id, "Pending"));
    }

    [Fact]
    public async Task OrderedFoodAndPortion_CannotBeDeleted_AndSnapshotKeepsOriginalPrice()
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var portion = await SeedSampleFoodAndPortion(context);
        await cart.AddToCartAsync(portion.Id);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);
        portion.Price = 45000;
        await context.SaveChangesAsync();
        var foodService = new AdminFoodService(context);
        var portionService = new AdminRecipeService(context);
        Assert.False((await foodService.DeleteFoodAsync(portion.FoodId)).Succeeded);
        Assert.False((await portionService.DeletePortionAsync(portion.Id)).Succeeded);
        var saved = await context.OrderItems.AsNoTracking().SingleAsync(i => i.OrderId == order.Id);
        Assert.Equal(35000m, saved.UnitPrice);
    }

    [Fact]
    public async Task MissingNutrition_DoesNotBecomeCompleteOrderTotals()
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var complete = await SeedSampleFoodAndPortion(context);
        var incomplete = await SeedSampleFoodAndPortion(context, 2, 11);
        incomplete.Food.Fat = null;
        await context.SaveChangesAsync();
        await cart.AddToCartAsync(complete.Id);
        await cart.AddToCartAsync(incomplete.Id);
        var summary = await cart.GetCartAsync();
        Assert.False(summary.HasCompleteNutrition);
        Assert.Equal(0, summary.PercentOf2000Kcal);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);
        Assert.Null(order.TotalKcal);
        Assert.Null(order.TotalProtein);
        Assert.Equal(70000m, order.TotalAmount);
    }

    [Fact]
    public async Task ZeroPrice_IsRejectedByAddCartAndReorder()
    {
        var (service, cart, context) = CreateServices();
        using var db = context;
        var portion = await SeedSampleFoodAndPortion(context);
        await cart.AddToCartAsync(portion.Id);
        var order = await service.CreateOrderAsync(CheckoutInfo(), null);
        await cart.AddToCartAsync(portion.Id);
        portion.Price = 0;
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => cart.AddToCartAsync(portion.Id));
        var summary = await cart.GetCartAsync();
        Assert.Empty(summary.Items);
        Assert.NotEmpty(summary.Warnings);
        var reorder = await service.ReorderAsync(order.Id);
        Assert.False(reorder.Success);
        Assert.Single(reorder.SkippedItems);
        Assert.Equal(1, await context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_GuestOrder_ShouldHaveNullUserId_AndValidUniquePublicCode()
    {
        var (orderService, cartService, context) = CreateServices();
        var portion = await SeedSampleFoodAndPortion(context, foodId: 101, portionId: 1001);

        // Them mon vao gio hang
        await cartService.AddToCartAsync(portion.Id, 2);

        var model = new CheckoutViewModel
        {
            CustomerName = "Trần Thị Khách",
            PhoneNumber = "+84 987.654.321",
            ShippingAddress = "123 Đường Láng, Hà Nội",
            Note = "Giao giờ hành chính"
        };

        var order1 = await orderService.CreateOrderAsync(model, userId: null);

        Assert.NotNull(order1);
        Assert.Null(order1.UserId);
        Assert.Equal("0987654321", order1.PhoneNumber);
        Assert.Equal(12, order1.PublicCode.Length);
        Assert.Single(order1.Items);
        Assert.Equal(70000m, order1.TotalAmount);

        // Gio hang phai duoc xoa sau khi tao don
        var cart = await cartService.GetCartAsync();
        Assert.Empty(cart.Items);

        // Tao tiep don thu 2
        await cartService.AddToCartAsync(portion.Id, 1);
        var order2 = await orderService.CreateOrderAsync(model, userId: null);

        Assert.NotEqual(order1.PublicCode, order2.PublicCode);
        Assert.Equal(12, order2.PublicCode.Length);
    }

    [Fact]
    public async Task TrackOrder_ShouldOnlyReturnOrderWhenBothCodeAndPhoneMatch()
    {
        var (orderService, cartService, context) = CreateServices();
        var portion = await SeedSampleFoodAndPortion(context, foodId: 102, portionId: 1002);

        await cartService.AddToCartAsync(portion.Id, 1);
        var model = new CheckoutViewModel
        {
            CustomerName = "Nguyễn Văn Test",
            PhoneNumber = "0912 345 678",
            ShippingAddress = "Hà Nội"
        };
        var order = await orderService.CreateOrderAsync(model, userId: null);

        // 1. Dung ca ma va sdt (co format khac nhau van match nho normalize)
        var tracked = await orderService.TrackOrderAsync(order.PublicCode, "+84 912.345.678");
        Assert.NotNull(tracked);
        Assert.Equal(order.Id, tracked.Id);

        // 2. Dung ma nhung sai sdt -> tra ve null
        var trackWrongPhone = await orderService.TrackOrderAsync(order.PublicCode, "0999999999");
        Assert.Null(trackWrongPhone);

        // 3. Sai ma nhung dung sdt -> tra ve null
        var trackWrongCode = await orderService.TrackOrderAsync("WRONGCODE123", "0912345678");
        Assert.Null(trackWrongCode);
    }

    [Fact]
    public async Task GetOrderForUser_UserCannotViewOtherUserOrGuestOrder()
    {
        var (orderService, cartService, context) = CreateServices();
        var portion = await SeedSampleFoodAndPortion(context, foodId: 103, portionId: 1003);

        context.Users.Add(new ApplicationUser
        {
            Id = "user-a-id",
            UserName = "usera@example.com",
            Email = "usera@example.com",
            FullName = "User A"
        });
        await context.SaveChangesAsync();

        // Don cua User A
        await cartService.AddToCartAsync(portion.Id, 1);
        var orderA = await orderService.CreateOrderAsync(new CheckoutViewModel
        {
            CustomerName = "User A",
            PhoneNumber = "0901111111",
            ShippingAddress = "Dia chi A"
        }, userId: "user-a-id");

        // Don cua Khach vang lai
        await cartService.AddToCartAsync(portion.Id, 1);
        var guestOrder = await orderService.CreateOrderAsync(new CheckoutViewModel
        {
            CustomerName = "Khach",
            PhoneNumber = "0902222222",
            ShippingAddress = "Dia chi Khach"
        }, userId: null);

        // User A xem don cua minh -> Thay
        var foundA = await orderService.GetOrderForUserAsync(orderA.Id, "user-a-id");
        Assert.NotNull(foundA);

        // User B tim don cua User A -> null
        var notFoundForB = await orderService.GetOrderForUserAsync(orderA.Id, "user-b-id");
        Assert.Null(notFoundForB);

        // User A tim don cua Khach theo Id -> null
        var notFoundGuestForA = await orderService.GetOrderForUserAsync(guestOrder.Id, "user-a-id");
        Assert.Null(notFoundGuestForA);
    }

    [Fact]
    public async Task Reorder_ShouldUseCurrentPrice_SkipInactivePortions_AndRespectQuantityLimit()
    {
        var (orderService, cartService, context) = CreateServices();

        // Tao 2 mon: mon 1 gia ban dau 30.000, mon 2 gia 20.000
        var portion1 = await SeedSampleFoodAndPortion(context, foodId: 201, portionId: 2001, price: 30000);
        var portion2 = await SeedSampleFoodAndPortion(context, foodId: 202, portionId: 2002, price: 20000);

        // Dat don cu: 2 portion1 va 1 portion2
        await cartService.AddToCartAsync(portion1.Id, 2);
        await cartService.AddToCartAsync(portion2.Id, 1);

        context.Users.Add(new ApplicationUser
        {
            Id = "user-reorder",
            UserName = "reorder@example.com",
            Email = "reorder@example.com",
            FullName = "User Reorder"
        });
        await context.SaveChangesAsync();

        var oldOrder = await orderService.CreateOrderAsync(new CheckoutViewModel
        {
            CustomerName = "Khách Reorder",
            PhoneNumber = "0933333333",
            ShippingAddress = "Hà Nội"
        }, userId: "user-reorder");

        // Thay doi thuc te sau do:
        // - portion1 tang gia len 45.000
        // - portion2 bi tat (IsActive = false)
        portion1.Price = 45000;
        portion2.IsActive = false;
        await context.SaveChangesAsync();

        // Truoc khi reorder, gia su gio hang hien tai da co san 19 portion1
        await cartService.AddToCartAsync(portion1.Id, 19);

        // Thuc hien Reorder tu don cu
        var result = await orderService.ReorderAsync(oldOrder.Id);

        Assert.True(result.Success);
        Assert.Single(result.SkippedItems); // portion2 bi bo qua
        Assert.Single(result.PriceChangedItems); // portion1 doi gia

        var cart = await cartService.GetCartAsync();
        Assert.Single(cart.Items); // chi co portion1
        Assert.Equal(20, cart.Items[0].Quantity); // 19 + 2 vuot 20 -> clamp ve 20
        Assert.Equal(45000m, cart.Items[0].UnitPrice); // gia hien tai la 45.000
    }
}
